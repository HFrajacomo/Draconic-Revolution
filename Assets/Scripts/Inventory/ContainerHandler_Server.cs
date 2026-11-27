using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

/*
Server authority over the contents of container voxels (VX_ContainerBehaviour)

Containers are loaded from the ContainerFileHandler when the first player opens them and stay in memory while anyone is looking at them
Every player can look at a single container. Many players can look at the same one: changes of one are sent to the others (CONTAINERSLOTS)

Clients change containers optimistically: they apply a change right away and send CONTAINEROP with every changed slot before and after it
A change is only applied if the slot still is what the client had before. Every CONTAINEROP is answered with CONTAINERRESULT,
listing the current contents of the slots whose change was rejected, so the client can undo it
Changes are written to disk as soon as they're applied
*/
public class ContainerHandler_Server{
	private ChunkLoader_Server cl;
	private ContainerFileHandler fileHandler;

	// Containers being looked at
	private Dictionary<ContainerKey, ServerContainer> openContainers = new Dictionary<ContainerKey, ServerContainer>();
	// Container each player is looking at
	private Dictionary<ulong, ContainerKey> viewing = new Dictionary<ulong, ContainerKey>();

	// Cache
	private byte[] fileBuffer = new byte[1 + 256 * InventorySlotCodec.MAX_SLOT_SIZE];
	private byte[] slotCache = new byte[InventorySlotCodec.MAX_SLOT_SIZE];
	private List<ushort> changedSlots = new List<ushort>();
	private List<ushort> rejectedSlots = new List<ushort>();
	private List<byte[]> rejectedTruth = new List<byte[]>();
	private List<ulong> cachedViewers = new List<ulong>();
	private static readonly byte[] EMPTY_SLOT = new byte[]{(byte)MemoryStorageType.EMPTY};


	public ContainerHandler_Server(ChunkLoader_Server cl){
		this.cl = cl;
		this.fileHandler = new ContainerFileHandler();
	}

	// Saves nothing: every change was already written. Closes the files
	public void CloseFiles(){
		this.fileHandler.CloseAll();
	}

	/*
	Opens a container for a player, sending its contents
	The first player to open a container runs its onContainerToggle (opening the lid of a chest)
	*/
	public void Open(ContainerKey key, byte inventoryType, ulong playerID){
		ContainerKey current;
		ServerContainer container;
		bool isFirstViewer;

		if(!IsInRange(playerID, key))
			return;

		// Interacting again with the container that is already open only sends its contents again
		if(this.viewing.TryGetValue(playerID, out current)){
			if(current == key){
				SendOpen(key, this.openContainers[key], playerID);
				return;
			}

			RemoveViewer(playerID, sendClose:true);
		}

		if(!this.openContainers.TryGetValue(key, out container)){
			container = Load(key, inventoryType);
			this.openContainers.Add(key, container);
		}

		isFirstViewer = container.viewers.Count == 0;
		container.viewers.Add(playerID);
		this.viewing[playerID] = key;

		if(isFirstViewer)
			RunToggle(key, true);

		SendOpen(key, container, playerID);
	}

	/*
	Applies the changes a player made to a container, if the slot of each one is still what the player had before
	Every change list is answered with CONTAINERRESULT, and applied changes are sent to the other viewers with CONTAINERSLOTS
	*/
	public void ApplyOps(ulong playerID, ContainerKey key, ushort opID, List<ContainerOpEntry> entries){
		ContainerKey current;
		ServerContainer container = null;
		ContainerOpEntry entry;
		bool isViewer;
		ItemStack after;

		isViewer = this.viewing.TryGetValue(playerID, out current) && current == key && this.openContainers.TryGetValue(key, out container);

		// Players that walked away can't change the container anymore
		if(isViewer && !IsInRange(playerID, key)){
			RemoveViewer(playerID, sendClose:true);
			isViewer = false;
		}

		// Rejected changes still need the real contents of their slots
		if(!isViewer && !this.openContainers.TryGetValue(key, out container))
			container = LoadIfContainer(key);

		this.changedSlots.Clear();
		this.rejectedSlots.Clear();
		this.rejectedTruth.Clear();

		for(int i=0; i < entries.Count; i++){
			entry = entries[i];

			// Container doesn't exist anymore or slot is out of it
			if(container == null || entry.slot >= container.slots.Length){
				Reject(entry.slot, EMPTY_SLOT);
				continue;
			}

			InventorySlotCodec.Write(container.slots[entry.slot], this.slotCache, 0);

			if(!isViewer || !InventorySlotCodec.AreEqual(this.slotCache, 0, entry.before, 0) || !InventorySlotCodec.IsValid(entry.after, 0, entry.after.Length)){
				Reject(entry.slot, InventorySlotCodec.Copy(this.slotCache, 0));
				continue;
			}

			after = InventorySlotCodec.Read(entry.after, 0, out _);

			if(after != null && (!container.inventory.IsInGlobalWhitelist(after) || !container.inventory.IsInLocalWhitelist(after, entry.slot))){
				Reject(entry.slot, InventorySlotCodec.Copy(this.slotCache, 0));
				continue;
			}

			container.slots[entry.slot] = after;

			if(!this.changedSlots.Contains(entry.slot))
				this.changedSlots.Add(entry.slot);
		}

		NetMessage result = new NetMessage(NetCode.CONTAINERRESULT);
		result.ContainerResult(key, opID, this.rejectedSlots, this.rejectedTruth);
		this.cl.server.Send(result.GetMessage(), result.size, playerID);

		// Changes are only applied for viewers, so the container is in openContainers
		if(this.changedSlots.Count > 0){
			Save(key, container);

			NetMessage slots = new NetMessage(NetCode.CONTAINERSLOTS);
			slots.ContainerSlots(key, this.changedSlots, container.slots);

			foreach(ulong viewer in container.viewers){
				if(viewer == playerID)
					continue;

				this.cl.server.Send(slots.GetMessage(), slots.size, viewer);
			}
		}
	}

	// A player closed the container it was looking at
	public void Close(ulong playerID, ContainerKey key){
		ContainerKey current;

		if(this.viewing.TryGetValue(playerID, out current) && current == key)
			RemoveViewer(playerID, sendClose:false);
	}

	// Forgets a player that disconnected. Must run while the chunks around it are still loaded
	public void RemovePlayer(ulong playerID){
		if(this.viewing.ContainsKey(playerID))
			RemoveViewer(playerID, sendClose:false);
	}

	/*
	Runs when a container voxel is broken: drops everything inside of it, closes it for everyone looking at it and deletes its entry
	The voxel is already gone, so onContainerToggle doesn't run
	*/
	public void Break(ContainerKey key){
		ServerContainer container;
		float3 dropPosition;
		Vector3 world;

		if(!this.openContainers.TryGetValue(key, out container))
			container = LoadFromFile(key, -1);

		if(container != null){
			world = key.GetWorldPosition();
			dropPosition = new float3(world.x, world.y + Constants.ITEM_ENTITY_SPAWN_HEIGHT_BONUS, world.z);

			for(int i=0; i < container.slots.Length; i++){
				if(container.slots[i] == null)
					continue;

				this.cl.server.entityHandler.AddItem(dropPosition, Item.GenerateForceVector(), container.slots[i].GetItem(), container.slots[i].GetAmount(), this.cl);
				container.slots[i] = null;
			}

			ForceCloseAll(key, container);
		}

		this.openContainers.Remove(key);
		this.fileHandler.Delete(key);
	}

	// Removes any data left in the position of a newly placed container, so it never starts with old items
	public void DeleteStale(ContainerKey key){
		ServerContainer container;

		if(this.openContainers.TryGetValue(key, out container)){
			ForceCloseAll(key, container);
			this.openContainers.Remove(key);
		}

		this.fileHandler.Delete(key);
	}

	private void Reject(ushort slot, byte[] truth){
		this.rejectedSlots.Add(slot);
		this.rejectedTruth.Add(truth);
	}

	// Removes a player from the viewers of its container. The last viewer runs onContainerToggle (closing the lid of a chest)
	private void RemoveViewer(ulong playerID, bool sendClose){
		ContainerKey key;
		ServerContainer container;

		if(!this.viewing.TryGetValue(playerID, out key))
			return;

		this.viewing.Remove(playerID);

		if(sendClose)
			SendClose(key, playerID);

		if(!this.openContainers.TryGetValue(key, out container))
			return;

		container.viewers.Remove(playerID);

		if(container.viewers.Count == 0){
			this.openContainers.Remove(key);
			RunToggle(key, false);
		}
	}

	// Closes a container for everyone looking at it
	private void ForceCloseAll(ContainerKey key, ServerContainer container){
		this.cachedViewers.Clear();
		this.cachedViewers.AddRange(container.viewers);

		for(int i=0; i < this.cachedViewers.Count; i++){
			this.viewing.Remove(this.cachedViewers[i]);
			SendClose(key, this.cachedViewers[i]);
		}

		container.viewers.Clear();
	}

	private void SendOpen(ContainerKey key, ServerContainer container, ulong playerID){
		NetMessage message = new NetMessage(NetCode.CONTAINEROPEN);
		message.ContainerOpen(key, this.cl.GetBlock(key.GetCoord()), container.inventoryType, container.slots);
		this.cl.server.Send(message.GetMessage(), message.size, playerID);
	}

	private void SendClose(ContainerKey key, ulong playerID){
		NetMessage message = new NetMessage(NetCode.CONTAINERCLOSE);
		message.ContainerClose(key);
		this.cl.server.Send(message.GetMessage(), message.size, playerID);
	}

	// Runs onContainerToggle of the container voxel
	private void RunToggle(ContainerKey key, bool open){
		CastCoord coord = key.GetCoord();
		ushort blockCode;
		int callback;

		if(!this.cl.Contains(key.pos))
			return;

		blockCode = this.cl.GetBlock(coord);

		if(blockCode <= ushort.MaxValue/2)
			callback = VoxelLoader.GetBlock(blockCode).OnContainerToggle(key.pos, key.x, key.y, key.z, open, this.cl);
		else
			callback = VoxelLoader.GetObject(blockCode).OnContainerToggle(key.pos, key.x, key.y, key.z, open, this.cl);

		this.cl.server.CallbackHandler(callback, key.pos, coord, 0);
	}

	private bool IsInRange(ulong playerID, ContainerKey key){
		PlayerData pdat;
		float maxDistance = Constants.CONTAINER_MAX_DISTANCE + Constants.CONTAINER_SERVER_DISTANCE_TOLERANCE;

		if(!this.cl.regionHandler.allPlayerData.TryGetValue(playerID, out pdat))
			return false;

		return (pdat.GetPosition() - key.GetWorldPosition()).sqrMagnitude <= maxDistance * maxDistance;
	}

	// Loads a container to read it, if its voxel still is a container
	private ServerContainer LoadIfContainer(ContainerKey key){
		int inventoryType;

		if(!this.cl.Contains(key.pos))
			return null;

		inventoryType = VX_ContainerBehaviour.GetInventoryTypeOf(this.cl.GetBlock(key.GetCoord()));

		if(inventoryType < 0)
			return null;

		return Load(key, (byte)inventoryType);
	}

	// Loads a container from disk, or creates an empty one if it was never saved
	private ServerContainer Load(ContainerKey key, byte inventoryType){
		ServerContainer container = LoadFromFile(key, inventoryType);

		if(container == null)
			container = new ServerContainer(inventoryType);

		return container;
	}

	/*
	Loads a container from disk. Returns null if it was never saved
	A container saved with another inventory type (its voxel was changed) is converted to the given one:
	slots keep their position and the ones that don't fit anymore are dropped
	inventoryType -1 keeps the saved type
	*/
	private ServerContainer LoadFromFile(ContainerKey key, int inventoryType){
		ServerContainer container;
		List<ItemStack> saved = new List<ItemStack>();
		ItemStack its;
		Vector3 world;
		float3 dropPosition;
		int size, pos, bytesRead;
		bool hasOverflow = false;

		if(!this.fileHandler.TryLoad(key, this.fileBuffer, out size))
			return null;

		if(inventoryType < 0)
			inventoryType = this.fileBuffer[0];

		pos = 1;

		while(pos < size){
			if(!InventorySlotCodec.IsValid(this.fileBuffer, pos, size)){
				pos += InventorySlotCodec.GetSize(this.fileBuffer, pos);
				saved.Add(null);
				continue;
			}

			its = InventorySlotCodec.Read(this.fileBuffer, pos, out bytesRead);
			pos += bytesRead;
			saved.Add(its);
		}

		// Saved type isn't a known inventory anymore: only its items matter, so they can be dropped
		if(!InventoryLoader.IsInventory((byte)inventoryType))
			return new ServerContainer((byte)inventoryType, saved.ToArray());

		container = new ServerContainer((byte)inventoryType);
		world = key.GetWorldPosition();
		dropPosition = new float3(world.x, world.y + Constants.ITEM_ENTITY_SPAWN_HEIGHT_BONUS, world.z);

		for(int i=0; i < saved.Count; i++){
			if(i < container.slots.Length){
				container.slots[i] = saved[i];
			}
			else if(saved[i] != null){
				this.cl.server.entityHandler.AddItem(dropPosition, Item.GenerateForceVector(), saved[i].GetItem(), saved[i].GetAmount(), this.cl);
				hasOverflow = true;
			}
		}

		if(hasOverflow)
			Save(key, container);

		return container;
	}

	// Writes a container to disk. Empty containers have their entry removed
	private void Save(ContainerKey key, ServerContainer container){
		int size = 1;
		bool isEmpty = true;

		if(this.fileBuffer.Length < 1 + container.slots.Length * InventorySlotCodec.MAX_SLOT_SIZE)
			this.fileBuffer = new byte[1 + container.slots.Length * InventorySlotCodec.MAX_SLOT_SIZE];

		this.fileBuffer[0] = container.inventoryType;

		for(int i=0; i < container.slots.Length; i++){
			if(container.slots[i] != null)
				isEmpty = false;

			size += InventorySlotCodec.Write(container.slots[i], this.fileBuffer, size);
		}

		if(isEmpty)
			this.fileHandler.Delete(key);
		else
			this.fileHandler.Save(key, this.fileBuffer, size);
	}


	private class ServerContainer{
		public byte inventoryType;
		public Inventory inventory; // Only used for its whitelists
		public ItemStack[] slots;
		public HashSet<ulong> viewers = new HashSet<ulong>();

		public ServerContainer(byte inventoryType){
			this.inventoryType = inventoryType;
			this.inventory = InventoryLoader.GetInventory(inventoryType);
			this.slots = new ItemStack[this.inventory.GetLimit()];
		}

		// Container saved with an inventory type that doesn't exist anymore. Can't be opened, only broken
		public ServerContainer(byte inventoryType, ItemStack[] slots){
			this.inventoryType = inventoryType;
			this.inventory = null;
			this.slots = slots;
		}
	}
}
