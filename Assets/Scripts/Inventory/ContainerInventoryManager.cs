using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
Builds and runs the UI of external inventories (chests and other VX_ContainerBehaviour voxels)

The panel is created at runtime by PlayerInventoryManager.Init() as a child of the inventory canvas, and is only active while a container is open.
While it's open, PlayerInventoryManager is in container mode: the bag is on the left, the container on the right,
and equipment, item details and the action hotbar are hidden

Container slots use InventoryButtons with Constants.CONTAINER_INVENTORY_CODE, so their clicks reach this class through PlayerInventoryManager.
The held stack is the same one used by the player inventories, so items move freely between them

Changes are optimistic: they're applied right away and sent to the Server as CONTAINEROP (slot contents before and after).
The Server answers each of them with CONTAINERRESULT, listing the slots whose change was rejected and their real contents,
and this class undoes the rejected changes in the player inventories
Slots changed by other players arrive as CONTAINERSLOTS. They're ignored for slots with changes still waiting for an answer,
since that answer will carry the right contents
*/
public class ContainerInventoryManager : MonoBehaviour{
	// Unity Reference
	private PlayerInventoryManager playerInventoryManager;
	private MainControllerManager mainControllerManager;
	private ChunkLoader cl;
	private GameObject emptyObject;

	// UI
	private TextMeshProUGUI titleText;
	private TextMeshProUGUI counterText;
	private GameObject gridObject;
	private GridLayoutGroup grid;
	private RectTransform gridTransform;
	private ScrollRectNoDrag scroll;
	private RectTransform takeAllTransform;
	private Image[] slotImages = new Image[0];
	private TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[0];
	private int builtInventoryType = -1;

	// State
	private bool isOpen = false;
	private ContainerKey key;
	private ushort blockCode;
	private Inventory container;

	// Changes sent to the Server and still waiting for an answer, in the order they were sent
	private Queue<PendingOp> pendingOps = new Queue<PendingOp>();
	private ushort nextOpID = 0;

	// Change being built
	private List<ushort> touchedSlots = new List<ushort>();
	private Dictionary<ushort, byte[]> touchedBefore = new Dictionary<ushort, byte[]>();

	// Layout
	private static readonly Vector2 SLOT_SIZE = new Vector2(80f, 80f);
	private static readonly float SLOT_SPACING = 4f;
	private static readonly Vector2 PANEL_SIZE = new Vector2(600f, 800f);
	private static readonly int PANEL_X = 460; // Right side. The bag is shifted to the left while a container is open
	private static readonly int PANEL_Y = 210;
	private static readonly Vector2 SCROLL_SIZE = new Vector2(580f, 660f);
	private static readonly int SCROLL_Y = 70; // Bottom of the scroll area, from the bottom of the panel
	private static readonly int GRID_PADDING = 10; // Top and bottom padding of the slot grid
	private static readonly float BUTTON_GAP = 10f; // Space between the last row of slots and "Take All"
	private static readonly float TAKE_ALL_MIN_Y = 10f;
	private static readonly Vector2 BUTTON_SIZE = new Vector2(220f, 50f);
	private static readonly int PLAYER_BUTTONS_X = 16; // Between the bag and the container
	private static readonly Color PANEL_COLOR = new Color(0.08f, 0.06f, 0.05f, 0.6f);
	private static readonly Color BUTTON_COLOR = new Color(0.16f, 0.12f, 0.09f, 0.9f);
	private static readonly Color BUTTON_HIGHLIGHT_COLOR = new Color(0.28f, 0.21f, 0.15f, 1f);
	private static readonly Color BUTTON_PRESSED_COLOR = new Color(0.10f, 0.08f, 0.06f, 1f);


	// Builds the static part of the panel. Slots are built when a container is opened, since they depend on its inventory type
	public void Setup(PlayerInventoryManager playerInventoryManager, ChunkLoader cl, MainControllerManager mainControllerManager, GameObject emptyObject){
		Vector2 bottom = new Vector2(0.5f, 0f);
		Vector2 top = new Vector2(0.5f, 1f);
		GameObject panel, scrollObject;
		Image panelImage, transparentImage;
		RectMask2D mask;

		this.playerInventoryManager = playerInventoryManager;
		this.mainControllerManager = mainControllerManager;
		this.cl = cl;
		this.emptyObject = emptyObject;

		this.playerInventoryManager.FixTransform(GetComponent<RectTransform>(), Vector2.one, bottom);

		// Panel
		panel = CreateObject(this.gameObject, "ContainerInventory", PANEL_SIZE, bottom, PANEL_X, PANEL_Y);
		panelImage = panel.AddComponent<Image>();
		panelImage.color = PANEL_COLOR;
		panelImage.raycastTarget = false;

		// Header
		this.titleText = this.playerInventoryManager.CreateTextComponent(panel, "Title", new Vector2(PANEL_SIZE.x - 40f, 50f), top);
		this.titleText.rectTransform.localPosition += new Vector3(0f, -10f, 0f);
		this.titleText.alignment = TextAlignmentOptions.Left;
		this.titleText.fontSize = 30;

		this.counterText = this.playerInventoryManager.CreateTextComponent(panel, "Counter", new Vector2(PANEL_SIZE.x - 40f, 50f), top);
		this.counterText.rectTransform.localPosition += new Vector3(0f, -10f, 0f);
		this.counterText.alignment = TextAlignmentOptions.Right;
		this.counterText.fontSize = 26;

		// Scroll area
		scrollObject = CreateObject(panel, "ScrollArea", SCROLL_SIZE, bottom, 0, SCROLL_Y);
		this.scroll = scrollObject.AddComponent<ScrollRectNoDrag>();
		this.scroll.movementType = ScrollRect.MovementType.Clamped;
		this.scroll.scrollSensitivity = 100f;
		this.scroll.viewport = scrollObject.GetComponent<RectTransform>();
		this.scroll.horizontal = false;
		transparentImage = scrollObject.AddComponent<Image>();
		transparentImage.color = new Color(1f, 1f, 1f, 0f);
		mask = scrollObject.AddComponent<RectMask2D>();
		mask.softness = new Vector2Int(0, 20);

		// Grid
		this.gridObject = CreateObject(scrollObject, "ContainerSlots", new Vector2(SCROLL_SIZE.x, SCROLL_SIZE.y), top, 0, 0);
		this.gridTransform = this.gridObject.GetComponent<RectTransform>();
		this.scroll.content = this.gridTransform;

		this.grid = this.gridObject.AddComponent<GridLayoutGroup>();
		this.grid.spacing = new Vector2(SLOT_SPACING, SLOT_SPACING);
		this.grid.cellSize = SLOT_SIZE;
		this.grid.childAlignment = TextAnchor.UpperCenter;
		this.grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		this.grid.padding = new RectOffset(0, 0, GRID_PADDING, GRID_PADDING);

		ContentSizeFitter fitter = this.gridObject.AddComponent<ContentSizeFitter>();
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		// Buttons
		this.takeAllTransform = CreateButton(panel, "TakeAll", "Take All", bottom, 0, (int)TAKE_ALL_MIN_Y, TakeAll).GetComponent<RectTransform>();
		CreateButton(this.gameObject, "StoreAll", "Store All", bottom, PLAYER_BUTTONS_X, PANEL_Y + 420, StoreAll);
		CreateButton(this.gameObject, "QuickStack", "Quick Stack", bottom, PLAYER_BUTTONS_X, PANEL_Y + 350, QuickStack);
	}

	// Closes the container when its voxel is gone or the player walked away from it
	void Update(){
		if(!this.isOpen)
			return;

		// The game session is being torn down (disconnect), and the chunks are already gone
		if(this.cl == null || this.cl.client == null)
			return;

		if(!this.cl.Contains(this.key.pos) || this.cl.Get(this.key.pos).data.GetCell(this.key.x, this.key.y, this.key.z) != this.blockCode){
			this.mainControllerManager.OnCloseUI();
			return;
		}

		Vector3 playerPosition = WorldOrigin.ToAbsolute(this.mainControllerManager.playerCamera.position);

		if((playerPosition - this.key.GetWorldPosition()).sqrMagnitude > Constants.CONTAINER_MAX_DISTANCE * Constants.CONTAINER_MAX_DISTANCE)
			this.mainControllerManager.OnCloseUI();
	}

	public bool IsOpen(){return this.isOpen;}

	// Closes the container UI. The Server is told unless it was the one that closed it
	public void Close(bool notifyServer){
		if(!this.isOpen)
			return;

		this.isOpen = false;

		// No connection is left after a disconnect. The Server already forgot the viewers of a disconnected player
		if(notifyServer && this.cl != null && this.cl.client != null){
			NetMessage message = new NetMessage(NetCode.CONTAINERCLOSE);
			message.ContainerClose(this.key);
			this.cl.client.Send(message);
		}

		this.playerInventoryManager.SetContainerMode(false);
	}

	/*
	Server messages (parsed by Client)
	*/

	// Shows the contents of the container the player opened
	public void Open(ContainerKey openedKey, ushort openedBlock, byte inventoryType, ItemStack[] slots){
		this.key = openedKey;
		this.blockCode = openedBlock;
		this.container = InventoryLoader.GetInventory(inventoryType);

		if(this.builtInventoryType != inventoryType)
			BuildSlots(inventoryType);

		for(ushort i=0; i < slots.Length && i < this.container.GetLimit(); i++)
			SetSlot(i, slots[i]);

		this.titleText.text = GetVoxelName(openedBlock);
		this.scroll.verticalNormalizedPosition = 1f;
		DrawAll();

		this.isOpen = true;
		this.mainControllerManager.OpenContainer();
	}

	// Applies container slots changed by other players
	public void UpdateSlots(ContainerKey changedKey, List<ushort> slots, List<ItemStack> contents){
		if(!this.isOpen || changedKey != this.key)
			return;

		for(int i=0; i < slots.Count; i++){
			if(slots[i] >= this.container.GetLimit() || IsPendingSlot(slots[i]))
				continue;

			SetSlot(slots[i], contents[i]);
			DrawSlot(slots[i]);
		}

		UpdateCounter();
	}

	// Applies the answer to a change. Rejected changes are undone and their slots receive their real contents
	public void ApplyResult(ContainerKey resultKey, ushort opID, List<ushort> rejectedSlots, List<ItemStack> truth){
		PendingOp op = null;
		bool isCurrentContainer = this.isOpen && resultKey == this.key;

		// Answers arrive in order, so older changes without an answer were lost and are forgotten
		while(this.pendingOps.Count > 0){
			op = this.pendingOps.Dequeue();

			if(op.opID == opID)
				break;

			op = null;
		}

		for(int i=0; i < rejectedSlots.Count; i++){
			if(op != null)
				Rollback(op, rejectedSlots[i]);

			if(isCurrentContainer && rejectedSlots[i] < this.container.GetLimit()){
				SetSlot(rejectedSlots[i], truth[i]);
				DrawSlot(rejectedSlots[i]);
			}
		}

		if(rejectedSlots.Count > 0){
			this.playerInventoryManager.SendInventoryDataToServer();

			if(isCurrentContainer)
				UpdateCounter();
		}
	}

	// Server closed the container, because it was broken or the player is too far from it
	public void ForceClose(ContainerKey closedKey){
		if(!this.isOpen || closedKey != this.key)
			return;

		Close(notifyServer:false);
		this.mainControllerManager.OnCloseUI();
	}

	/*
	Slot interaction
	*/

	// Picks, places, swaps or merges the held stack with a container slot. Shift sends the slot to the player inventories
	public void LeftClick(ushort slot){
		ClickableSlot held;
		ItemStack current, heldStack, rest;

		if(!this.isOpen || slot >= this.container.GetLimit())
			return;

		held = this.playerInventoryManager.GetDraggedStack();
		current = this.container.GetSlot(slot);

		BeginOp();
		Touch(slot);

		// Nothing held
		if(held == null){
			if(current == null)
				return;

			if(MainControllerManager.shifting)
				TakeToPlayer(slot);
			else{
				this.container.SetNull(slot);
				this.playerInventoryManager.HoldFromContainer(current, slot);
			}
		}
		// Holding an item
		else if(held.IsItemStack()){
			heldStack = (ItemStack)held;

			if(!CanStore(heldStack, slot))
				return;

			// Puts the stack in an empty slot
			if(current == null){
				this.container.ForceAddStack(heldStack, slot);
				this.playerInventoryManager.ReleaseHeldToContainer();
			}
			// Merges stacks of the same item
			else if(IsStackable(heldStack, current)){
				rest = this.container.Transfer(heldStack, slot);

				if(rest == null)
					this.playerInventoryManager.ReleaseHeldToContainer();
				else
					this.playerInventoryManager.RefreshHeld();
			}
			// Swaps different items
			else{
				this.container.ForceAddStack(heldStack, slot);
				this.playerInventoryManager.SwapHeldWithContainer(current, slot);
			}
		}
		// Actions can't go in containers
		else{
			return;
		}

		CommitOp();
	}

	// Picks half of a slot, or puts a single held item in it
	public void RightClick(ushort slot){
		ClickableSlot held;
		ItemStack current, heldStack, half;

		if(!this.isOpen || slot >= this.container.GetLimit())
			return;

		held = this.playerInventoryManager.GetDraggedStack();
		current = this.container.GetSlot(slot);

		BeginOp();
		Touch(slot);

		// Nothing held: picks half of the stack
		if(held == null){
			if(current == null)
				return;

			half = current.Split();

			if(half == null){
				this.container.SetNull(slot);
				this.playerInventoryManager.HoldFromContainer(current, slot);
			}
			else{
				this.playerInventoryManager.HoldFromContainer(half, slot);
			}
		}
		// Holding an item: puts a single one
		else if(held.IsItemStack()){
			heldStack = (ItemStack)held;

			if(current == null){
				if(!CanStore(heldStack, slot))
					return;

				if(heldStack.GetAmount() == 1){
					this.container.ForceAddStack(heldStack, slot);
					this.playerInventoryManager.ReleaseHeldToContainer();
				}
				else{
					this.container.ForceAddStack(new ItemStack(heldStack.GetItem(), 1), slot);
					heldStack.Decrement();
					this.playerInventoryManager.RefreshHeld();
				}
			}
			else if(IsStackable(heldStack, current)){
				if(current.IsFull())
					return;

				current.Increment();

				if(heldStack.Decrement())
					this.playerInventoryManager.ReleaseHeldToContainer();
				else
					this.playerInventoryManager.RefreshHeld();
			}
			else{
				if(!CanStore(heldStack, slot))
					return;

				this.container.ForceAddStack(heldStack, slot);
				this.playerInventoryManager.SwapHeldWithContainer(current, slot);
			}
		}
		else{
			return;
		}

		CommitOp();
	}

	// Shift-click on a player slot while a container is open: sends as much of it as possible to the container
	public void DepositFromPlayer(byte inventoryCode, ushort slot){
		ItemStack its;
		byte moved;

		if(!this.isOpen)
			return;

		its = this.playerInventoryManager.GetItemStack(inventoryCode, slot);

		if(its == null)
			return;

		BeginOp();
		moved = StoreInContainer(its);

		if(moved == 0)
			return;

		this.playerInventoryManager.RemoveFromSlot(inventoryCode, slot, moved);
		CommitOp();
	}

	/*
	Buttons
	*/

	// Sends everything in the container to the player inventories
	public void TakeAll(){
		if(!this.isOpen)
			return;

		BeginOp();

		for(ushort i=0; i < this.container.GetLimit(); i++){
			if(this.container.GetSlot(i) == null)
				continue;

			Touch(i);
			TakeToPlayer(i);
		}

		CommitOp();
	}

	// Sends everything in the bag and other storages to the container. Hotbar and equipment are kept
	public void StoreAll(){
		StoreFromPlayer(onlyExisting:false);
	}

	// Like StoreAll, but only for items the container already has
	public void QuickStack(){
		StoreFromPlayer(onlyExisting:true);
	}

	private void StoreFromPlayer(bool onlyExisting){
		HashSet<ushort> existing = null;
		List<byte> sources;
		ItemStack its;
		int limit;
		byte moved;

		if(!this.isOpen)
			return;

		if(onlyExisting){
			existing = new HashSet<ushort>();

			for(ushort i=0; i < this.container.GetLimit(); i++){
				if(this.container.GetSlot(i) != null)
					existing.Add(this.container.GetSlot(i).GetID());
			}

			if(existing.Count == 0)
				return;
		}

		sources = this.playerInventoryManager.GetStoreAllSources();
		BeginOp();

		for(int i=0; i < sources.Count; i++){
			limit = this.playerInventoryManager.GetInventoryLimit(sources[i]);

			for(ushort j=0; j < limit; j++){
				its = this.playerInventoryManager.GetItemStack(sources[i], j);

				if(its == null)
					continue;

				if(onlyExisting && !existing.Contains(its.GetID()))
					continue;

				moved = StoreInContainer(its);

				if(moved > 0)
					this.playerInventoryManager.RemoveFromSlot(sources[i], j, moved);
			}
		}

		CommitOp();
	}

	/*
	Changes
	*/

	private void BeginOp(){
		this.touchedSlots.Clear();
		this.touchedBefore.Clear();
	}

	// Records the contents of a slot before it's changed
	private void Touch(ushort slot){
		if(this.touchedBefore.ContainsKey(slot))
			return;

		this.touchedSlots.Add(slot);
		this.touchedBefore.Add(slot, InventorySlotCodec.ToArray(this.container.GetSlot(slot)));
	}

	// Sends the slots that really changed to the Server and redraws them
	private void CommitOp(){
		List<ContainerOpEntry> entries = new List<ContainerOpEntry>();
		byte[] before, after;
		ushort slot;

		for(int i=0; i < this.touchedSlots.Count; i++){
			slot = this.touchedSlots[i];
			before = this.touchedBefore[slot];
			after = InventorySlotCodec.ToArray(this.container.GetSlot(slot));

			DrawSlot(slot);

			if(InventorySlotCodec.AreEqual(before, 0, after, 0))
				continue;

			entries.Add(new ContainerOpEntry(slot, before, after));
		}

		BeginOp();
		UpdateCounter();

		// Held stack and actions may have changed even if no slot did
		this.playerInventoryManager.SendInventoryDataToServer();

		if(entries.Count == 0)
			return;

		PendingOp op = new PendingOp(this.nextOpID, this.key, entries);
		this.nextOpID++;
		this.pendingOps.Enqueue(op);

		NetMessage message = new NetMessage(NetCode.CONTAINEROP);
		message.ContainerOp(this.key, op.opID, entries);
		this.cl.client.Send(message);
	}

	/*
	Undoes the player side of a rejected change. The slot went from Before to After, so:
		same item, more items: the player gave the difference, which is given back
		same item, less items: the player received the difference, which is taken back
		different items: the player received Before and gave After
	*/
	private void Rollback(PendingOp op, ushort slot){
		ContainerOpEntry entry;
		ItemStack before, after;
		int bytesRead;

		if(!op.TryGetEntry(slot, out entry))
			return;

		before = InventorySlotCodec.Read(entry.before, 0, out bytesRead);
		after = InventorySlotCodec.Read(entry.after, 0, out bytesRead);

		if(before != null && after != null && IsStackable(before, after)){
			if(after.GetAmount() > before.GetAmount())
				this.playerInventoryManager.GiveBackItems(new ItemStack(after.GetItem(), (byte)(after.GetAmount() - before.GetAmount())));
			else if(before.GetAmount() > after.GetAmount())
				this.playerInventoryManager.TakeBackItems(before.GetID(), (byte)(before.GetAmount() - after.GetAmount()));

			return;
		}

		if(before != null)
			this.playerInventoryManager.TakeBackItems(before.GetID(), before.GetAmount());
		if(after != null)
			this.playerInventoryManager.GiveBackItems(after);
	}

	// Whether a slot of the open container has a change waiting for an answer
	private bool IsPendingSlot(ushort slot){
		ContainerOpEntry entry;

		foreach(PendingOp op in this.pendingOps){
			if(op.key == this.key && op.TryGetEntry(slot, out entry))
				return true;
		}

		return false;
	}

	/*
	Item movement
	*/

	// Sends as much of a container slot as possible to the player inventories
	private void TakeToPlayer(ushort slot){
		ItemStack current = this.container.GetSlot(slot);
		byte stored;

		if(current == null)
			return;

		stored = this.playerInventoryManager.StoreInPlayer(current);

		if(stored == 0)
			return;

		if(stored >= current.GetAmount())
			this.container.SetNull(slot);
		else
			current.SetAmount((byte)(current.GetAmount() - stored));
	}

	/*
	Puts as much of a stack as possible in the container, without changing it
	First tops up stacks of the same item, then uses the first empty slot
	Returns the amount of items stored
	*/
	private byte StoreInContainer(ItemStack its){
		ItemStack current;
		byte remaining = its.GetAmount();
		byte stacksize = its.GetStacksize();
		byte toAdd;

		if(remaining == 0 || !this.container.IsInGlobalWhitelist(its))
			return 0;

		if(stacksize > 1){
			for(ushort i=0; i < this.container.GetLimit() && remaining > 0; i++){
				current = this.container.GetSlot(i);

				if(current == null || current.GetID() != its.GetID() || current.IsFull())
					continue;

				Touch(i);
				toAdd = (byte)Mathf.Min(stacksize - current.GetAmount(), remaining);
				current.Add(toAdd);
				remaining -= toAdd;
			}
		}

		if(remaining > 0){
			for(ushort i=0; i < this.container.GetLimit(); i++){
				if(this.container.GetSlot(i) != null || !this.container.IsInLocalWhitelist(its, i))
					continue;

				Touch(i);
				this.container.ForceAddStack(new ItemStack(its.GetItem(), remaining), i);
				remaining = 0;
				break;
			}
		}

		return (byte)(its.GetAmount() - remaining);
	}

	private bool CanStore(ItemStack its, ushort slot){
		return this.container.IsInGlobalWhitelist(its) && this.container.IsInLocalWhitelist(its, slot);
	}

	// Stacks of the same stackable item merge. Unstackable items (like weapons) have their own data, so they're swapped instead
	private bool IsStackable(ItemStack a, ItemStack b){
		return a.GetID() == b.GetID() && a.GetStacksize() > 1;
	}

	private void SetSlot(ushort slot, ItemStack its){
		if(its == null)
			this.container.SetNull(slot);
		else
			this.container.ForceAddStack(its, slot);
	}

	/*
	Drawing
	*/

	// Creates the slots of an inventory type
	private void BuildSlots(byte inventoryType){
		Vector2 center = new Vector2(0.5f, 0.5f);
		int size = this.container.GetLimit();
		int columns = Mathf.Max(1, this.container.columnCount);

		for(int i=0; i < this.slotImages.Length; i++){
			this.slotImages[i].transform.SetParent(null);
			Destroy(this.slotImages[i].gameObject);
		}

		this.grid.constraintCount = columns;
		this.gridTransform.sizeDelta = new Vector2(columns * (SLOT_SIZE.x + SLOT_SPACING), this.gridTransform.sizeDelta.y);

		this.slotImages = new Image[size];
		this.slotTexts = new TextMeshProUGUI[size];

		for(int i=0; i < size; i++){
			this.slotImages[i] = this.playerInventoryManager.CreateImageComponent(this.gridObject, $"Slot-{i+1}", Constants.CONTAINER_INVENTORY_CODE, i, SLOT_SIZE, center);
			this.slotTexts[i] = this.playerInventoryManager.CreateTextComponent(this.slotImages[i].gameObject, $"TSlot-{i+1}", SLOT_SIZE, center);
		}

		PlaceTakeAllButton(size, columns);
		this.builtInventoryType = inventoryType;
	}

	// Puts "Take All" right under the last row of slots. Containers with more rows than the scroll area shows keep it at the bottom of the panel
	private void PlaceTakeAllButton(int size, int columns){
		int rows = Mathf.Max(1, Mathf.CeilToInt(size / (float)columns));
		float gridHeight = rows * SLOT_SIZE.y + (rows - 1) * SLOT_SPACING + GRID_PADDING * 2;
		float visibleHeight = Mathf.Min(gridHeight, SCROLL_SIZE.y);
		float y = SCROLL_Y + SCROLL_SIZE.y - visibleHeight - BUTTON_GAP - BUTTON_SIZE.y;

		this.takeAllTransform.anchoredPosition = new Vector2(this.takeAllTransform.anchoredPosition.x, Mathf.Max(y, TAKE_ALL_MIN_Y));
	}

	private void DrawAll(){
		for(ushort i=0; i < this.container.GetLimit(); i++)
			DrawSlot(i);

		UpdateCounter();
	}

	private void DrawSlot(ushort slot){
		ItemStack its = this.container.GetSlot(slot);
		string iconName;

		if(slot >= this.slotImages.Length)
			return;

		if(its == null){
			this.slotTexts[slot].text = "";

			if(this.container.HasInventoryIcons()){
				iconName = this.container.GetIconName(slot);

				if(iconName != ""){
					this.slotImages[slot].material.SetTexture("_Texture", InventoryLoader.GetSlotIcon(iconName));
					return;
				}
			}

			this.slotImages[slot].material.SetTexture("_Texture", null);
			return;
		}

		this.slotImages[slot].material.SetTexture("_Texture", ItemLoader.GetSprite(its));

		if(its.GetStacksize() > 1)
			this.slotTexts[slot].text = its.GetAmount().ToString();
		else
			this.slotTexts[slot].text = "";
	}

	private void UpdateCounter(){
		int used = 0;

		if(this.container == null)
			return;

		for(ushort i=0; i < this.container.GetLimit(); i++){
			if(this.container.GetSlot(i) != null)
				used++;
		}

		this.counterText.text = $"{used}/{this.container.GetLimit()}";
	}

	private string GetVoxelName(ushort block){
		if(block <= ushort.MaxValue/2)
			return VoxelLoader.GetBlock(block).name;
		return VoxelLoader.GetObject(block).name;
	}

	/*
	UI creation
	*/

	private GameObject CreateObject(GameObject parent, string goName, Vector2 size, Vector2 anchor, int addX, int addY){
		GameObject go = GameObject.Instantiate(this.emptyObject);

		go.name = goName;
		go.transform.SetParent(parent.transform);
		this.playerInventoryManager.FixTransform(go.GetComponent<RectTransform>(), size, anchor, addX:addX, addY:addY);

		return go;
	}

	private GameObject CreateButton(GameObject parent, string goName, string label, Vector2 anchor, int addX, int addY, UnityEngine.Events.UnityAction action){
		GameObject go = CreateObject(parent, goName, BUTTON_SIZE, anchor, addX, addY);
		Image image = go.AddComponent<Image>();
		Button button = go.AddComponent<Button>();
		ColorBlock colors = button.colors;
		Navigation navigation = button.navigation;
		TextMeshProUGUI text;

		// The Image is white so the Button colors are the real colors
		image.color = Color.white;
		image.raycastTarget = true;

		colors.normalColor = BUTTON_COLOR;
		colors.highlightedColor = BUTTON_HIGHLIGHT_COLOR;
		colors.pressedColor = BUTTON_PRESSED_COLOR;
		colors.selectedColor = BUTTON_COLOR;
		button.colors = colors;
		button.targetGraphic = image;
		navigation.mode = Navigation.Mode.None;
		button.navigation = navigation;
		button.onClick.AddListener(action);

		text = this.playerInventoryManager.CreateTextComponent(go, "Label", BUTTON_SIZE, new Vector2(0.5f, 0.5f));
		text.alignment = TextAlignmentOptions.Center;
		text.fontSize = 24;
		text.text = label;

		return go;
	}


	// A change sent to the Server, kept until it's answered
	private class PendingOp{
		public ushort opID;
		public ContainerKey key;
		public List<ContainerOpEntry> entries;

		public PendingOp(ushort opID, ContainerKey key, List<ContainerOpEntry> entries){
			this.opID = opID;
			this.key = key;
			this.entries = entries;
		}

		public bool TryGetEntry(ushort slot, out ContainerOpEntry entry){
			for(int i=0; i < this.entries.Count; i++){
				if(this.entries[i].slot == slot){
					entry = this.entries[i];
					return true;
				}
			}

			entry = default(ContainerOpEntry);
			return false;
		}
	}
}
