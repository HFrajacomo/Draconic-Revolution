using System;
using System.Collections.Generic;
using UnityEngine;

/*
Turns a voxel into a container: an external inventory that players open by interacting with it, like a chest

inventoryType is an inventory of Resources/Inventory/INVENTORIES.json, which defines the amount of slots, columns and whitelists of the container
The items are kept by the Server's ContainerHandler_Server and saved per voxel by the ContainerFileHandler

Must be used in onPlayerInteract, onPlace and onBreak
Behaviours that should run when the container is opened by its first viewer and closed by its last one (like a lid animation and sounds)
go in onContainerToggle instead of onInteract
*/
[Serializable]
public class VX_ContainerBehaviour : VoxelBehaviour{
	public string inventoryType;

	private byte inventoryID;

	// Inventories are loaded before voxels run this, so a wrong inventoryType fails on load
	public override void PostDeserializationSetup(bool isClient){
		if(string.IsNullOrEmpty(this.inventoryType))
			throw new DeserializationErrorException("[VX_ContainerBehaviour] No inventoryType was given");
		if(!InventoryLoader.Contains(this.inventoryType))
			throw new DeserializationErrorException($"[VX_ContainerBehaviour] inventoryType '{this.inventoryType}' is not an item inventory in INVENTORIES.json");

		this.inventoryID = InventoryLoader.GetInventoryID(this.inventoryType);
	}

	public override int OnPlayerInteract(ChunkPos pos, int x, int y, int z, ulong playerID, ChunkLoader_Server cl){
		cl.containerHandler.Open(new ContainerKey(pos, x, y, z), this.inventoryID, playerID);
		return 0;
	}

	public override int OnPlace(ChunkPos pos, int x, int y, int z, int facing, ChunkLoader_Server cl){
		cl.containerHandler.DeleteStale(new ContainerKey(pos, x, y, z));
		return 0;
	}

	public override int OnBreak(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){
		cl.containerHandler.Break(new ContainerKey(pos, x, y, z));
		return 0;
	}

	// Inventory type of the container in a voxel, or -1 if the voxel isn't a container
	public static int GetInventoryTypeOf(ushort blockCode){
		List<VoxelBehaviour> behaviours;
		VX_ContainerBehaviour container;

		if(blockCode <= ushort.MaxValue/2)
			behaviours = VoxelLoader.GetBlock(blockCode).GetOnPlayerInteract();
		else
			behaviours = VoxelLoader.GetObject(blockCode).GetOnPlayerInteract();

		if(behaviours == null)
			return -1;

		for(int i=0; i < behaviours.Count; i++){
			container = behaviours[i] as VX_ContainerBehaviour;

			if(container != null)
				return container.inventoryID;
		}

		return -1;
	}
}
