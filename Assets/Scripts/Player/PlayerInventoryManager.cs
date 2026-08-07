using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInventoryManager : MonoBehaviour {
    // Unity Reference
    public MainControllerManager mainControllerManager;
    public Image background;
    public GameObject detailsPanel;
    public Image detailsImage;
    public TextMeshProUGUI detailsName;
    public TextMeshProUGUI detailsDescription;
    public TextMeshProUGUI detailsStats;
    public ChunkLoader cl;
    public Material itemIconMaterial;
    public Material backgroundMaterial;
    public TMP_FontAsset liberationSans;

    private bool INIT = false;

    private bool bulkMoveAbove = true; // If inventory shift-move should be done upwards or downwards

    // Cache
	private readonly string EMPTY_OBJECT_PATHNAME = "----- PrefabModels -----/EmptyObjectUI";
	private GameObject EMPTY_OBJECT;
	private ItemStack NULL_STACK;

	// Inventory data and draw info
	private List<BaseInventory> inventory = new List<BaseInventory>();
	private Dictionary<int, Image[]> slotImages = new Dictionary<int, Image[]>();
	private Dictionary<int, TextMeshProUGUI[]> slotText = new Dictionary<int, TextMeshProUGUI[]>();
	private GameObject scrollingAreaContent;

	private byte[] buffer = new byte[30000];
	private Vector2 textPivot = new Vector2(1, 0);

	// Drag and Drop overlay
	private Image dragOverlay;
	private ItemStack draggedStack;
	private TextMeshProUGUI dragStacksize;

	// Constants
	private readonly Vector2 slotSizes = new Vector2(80f, 80f);
	private readonly Item NULL_ITEM = ItemLoader.GetItem(0);

	void OnDisable(){
		if(this.draggedStack != null){
			this.mainControllerManager.DropItem(this.draggedStack);
			ResetSelection();
		}
	}

	public void Init(){
		if(this.INIT)
			return;

		EMPTY_OBJECT = GameObject.Find(EMPTY_OBJECT_PATHNAME);

		if(this.inventory.Count == 0)
			StartInventory();

		CreateActionHotbar(this.gameObject);
		CreateHotbarInventory(this.gameObject);
		CreatePlayerBagInventory(this.gameObject);
		CreateEquipmentInventory(this.gameObject);
		CreateDragSlot(this.gameObject);

		this.detailsImage.material = Instantiate(this.itemIconMaterial);
		this.background.material = Instantiate(this.backgroundMaterial);
		INIT = true;
	}

	// Creates inventories based on byte array
	public void LoadFromBytes(byte[] data, int init){
		// Control variables
		int bytesRead = init;
		int currentInventory = 0;
		byte type;
		MemoryStorageType mst;

		// Cached variables
		ushort id;
		byte quantity;
		uint currentDur;
		byte refineLv;
		EnchantmentType enchant;
		ItemStack its;
		Item item;
		Weapon weapon;

		// Cached Action
		EntityAction ea;
		bool connectedToStack;
		ushort currentCooldown;
		ushort totalCooldown;
		byte connectedStackInventory;
		byte connectedStackSlot;

		if(this.inventory.Count == 0)
			StartInventory();

		while(bytesRead < data.Length){
			type = data[bytesRead];
			bytesRead++;

			if(this.inventory.Count <= currentInventory){
				AddInventory(InventoryLoader.GetInventory(type));
			}
			else if(type != this.inventory[currentInventory].GetID()){
				this.inventory[currentInventory] = InventoryLoader.GetInventory(type);
			}

			for(ushort i=0; i < this.inventory[currentInventory].GetLimit(); i++){
				mst = (MemoryStorageType)data[bytesRead];
				bytesRead++;

				switch(mst){
					case MemoryStorageType.EMPTY:
						this.inventory[currentInventory].ForceAddStack(NULL_STACK, i); // this was null before. Hopefully nothing breaks 
						break;
					case MemoryStorageType.ITEM:
						id = NetDecoder.ReadUshort(data, bytesRead);
						bytesRead += 2;
						quantity = data[bytesRead];
						bytesRead++;
						item = ItemLoader.GetCopy(id);
						its = new ItemStack(item, quantity);
						this.inventory[currentInventory].ForceAddStack(its, i);
						break;
					case MemoryStorageType.WEAPON:
						id = NetDecoder.ReadUshort(data, bytesRead);
						bytesRead += 2;
						currentDur = NetDecoder.ReadUint(data, bytesRead);
						bytesRead += 4;
						refineLv = data[bytesRead];
						bytesRead++;
						enchant = (EnchantmentType)data[bytesRead];
						bytesRead++;

						weapon = (Weapon)ItemLoader.GetCopy(id);
						weapon.SetDurability(currentDur);
						weapon.SetExtraEffects(enchant);
						weapon.SetRefineLevel(refineLv);
						its = new ItemStack(weapon, 1);
						this.inventory[currentInventory].ForceAddStack(its, i); 
						break;
					case MemoryStorageType.ACTION:
						connectedToStack = NetDecoder.ReadBool(data, bytesRead);
						bytesRead++;
						currentCooldown = NetDecoder.ReadUshort(data, bytesRead);
						bytesRead += 2;
						totalCooldown = NetDecoder.ReadUshort(data, bytesRead);
						bytesRead += 2;
						connectedStackInventory = NetDecoder.ReadByte(data, bytesRead);
						bytesRead++;
						connectedStackSlot = NetDecoder.ReadByte(data, bytesRead);
						bytesRead++;
						ea = new EntityAction();
						ea.SetMemoryData(connectedToStack, currentCooldown, totalCooldown, connectedStackInventory, connectedStackSlot);
						break;
				}
			}
			currentInventory++;
		}

		ReloadInventory();
	}

	/*
	Turns player inventory into a serialized version in buffer
	and returns the amount of written bytes
	*/
	public int SerializeInventory(){
		ItemStack its;
		EntityAction ea;
		int bytesWritten = 0;

		for(int inventoryCode=0; inventoryCode < this.inventory.Count; inventoryCode++){
			this.buffer[bytesWritten] = this.inventory[inventoryCode].GetID();
			bytesWritten++;

			for(ushort i=0; i < this.inventory[inventoryCode].GetLimit(); i++){
				// Item Inventory
				if(this.inventory[inventoryCode].itemInventory){
					// Empty
					if(this.inventory[inventoryCode].GetSlot(i) == null){
						this.buffer[bytesWritten] = (byte)MemoryStorageType.EMPTY;
						bytesWritten++;
					}
					// Item
					else{
						its = this.inventory[inventoryCode].GetSlot(i);
						bytesWritten += its.ConvertToMemory(this.buffer, bytesWritten);
					}
				}
				// Action Inventory
				else{
					// Empty
					if(this.inventory[inventoryCode].GetPos(i) == null){
						this.buffer[bytesWritten] = (byte)MemoryStorageType.EMPTY;
						bytesWritten++;
					}
					// Action
					else{
						ea = this.inventory[inventoryCode].GetPos(i);
						bytesWritten += ea.ConvertToMemory(this.buffer, bytesWritten);
					}
				}
			}
		}

		return bytesWritten;
	}

    public void ReloadInventory(){
		for(int i=0; i < this.inventory.Count; i++){
			if(this.inventory[i].itemInventory)
				this.inventory[i].FindLastEmptySlot();
		}

        DrawStacks();
    }

    public void SendInventoryDataToServer(){
    	int inventorySize = SerializeInventory();

		NetMessage message = new NetMessage(NetCode.SENDINVENTORY);
		message.SendInventory(this.buffer, inventorySize);
		this.cl.client.Send(message);
    }

    public void SendEquipDataToServer(int inventoryCode, ItemStack oldItem, ItemStack newItem){
        if(!IsEquipmentInventory(inventoryCode))
        	return;

    	if(oldItem == null && newItem == null)
    		return;

    	NetMessage message = new NetMessage(NetCode.EQUIPITEM);

    	if(oldItem == null)
			message.EquipItem(Configurations.accountID, NULL_ITEM, newItem.GetItem());
		else if(newItem == null)
			message.EquipItem(Configurations.accountID, oldItem.GetItem(), NULL_ITEM);
		else
			message.EquipItem(Configurations.accountID, oldItem.GetItem(), newItem.GetItem());

		this.cl.client.Send(message);
    }

    public Inventory GetMainInventory(){
    	for(int i=0; i < this.inventory.Count; i++){
    		if(this.inventory[i].itemInventory && this.inventory[i].GetMainInventory())
    			return (Inventory)this.inventory[i];
    	}

    	throw new MainInventoryNotFoundException($"[PlayerInventoryManager] None of the current inventories have the main flag set. Inventory count: {this.inventory.Count}");
    }

    private bool IsEquipmentInventory(int inventoryCode){return this.inventory[inventoryCode].inventoryType == "EQUIPMENT";}

    // Draws the ItemStacks into the Inventory Screen
    private void DrawStacks(){
    	for(int i=0; i < this.inventory.Count; i++){
    		for(ushort j=0; j < this.inventory[i].GetLimit(); j++){
    			DrawSlot((byte)i, j);
    		}
    	}
    }

    // Redraws a specific slot
    public void DrawSlot(byte inventoryCode, ushort slot){
    	if(this.inventory[inventoryCode].itemInventory)
    		DrawSlotItem(inventoryCode, slot);
    	else
    		DrawSlotAction(inventoryCode, slot);
    }
    private void DrawSlotItem(byte inventoryCode, ushort slot){
    	ItemStack its = this.inventory[inventoryCode].GetSlot(slot);
    	string iconName = "";

		if(its == null){
			if(this.inventory[inventoryCode].HasInventoryIcons()){
				iconName = this.inventory[inventoryCode].GetIconName(slot);

				if(iconName != ""){
					this.slotImages[inventoryCode][slot].material.SetTexture("_Texture", InventoryLoader.GetSlotIcon(iconName));
					this.slotText[inventoryCode][slot].text = "";
					return;
				}
			}

			this.slotImages[inventoryCode][slot].material.SetTexture("_Texture", null);
			this.slotText[inventoryCode][slot].text = "";
		}
		else{
    		this.slotImages[inventoryCode][slot].material.SetTexture("_Texture", ItemLoader.GetSprite(its));

    		if(its.GetStacksize() > 1)
    			this.slotText[inventoryCode][slot].text = its.GetAmount().ToString();
    		else
    			this.slotText[inventoryCode][slot].text = "";   			
		}
    }
    private void DrawSlotAction(byte inventoryCode, ushort slot){
    	// TODO
    }

    // Activates on Left Click of a slot
    public void LeftClick(byte inventoryCode, ushort slot){
    	// If has no slot selected and not shifting
    	if(this.draggedStack == null && !MainControllerManager.shifting){
    		// Avoid null slot click
    		if(this.IsNullSlot(inventoryCode, slot))
    			return;

            string[] details; 

    		this.draggedStack = this.inventory[inventoryCode].GetSlot(slot);
    		this.inventory[inventoryCode].SetNull(slot);
    		DrawSlot(inventoryCode, slot);
    		ToggleHighlight(true, this.draggedStack);
            ResetDetails();
            this.detailsPanel.SetActive(true);

            // Finds the item selected
            Item item = this.draggedStack.GetItem();

            details = item.GetDetails();
            this.detailsName.text = details[0];

            if(item is Weapon)
                this.detailsStats.text = details[1];
            else if(item is Item)
                this.detailsDescription.text = details[1];

            this.detailsImage.material.SetTexture("_Texture", ItemLoader.GetSprite(item.GetID()));

            SendEquipDataToServer(inventoryCode, this.draggedStack, null);
            SendInventoryDataToServer();
    	}
    	// If has no slot selected and shift clicked
    	else if(this.draggedStack == null && MainControllerManager.shifting){
    		if(IsNullSlot(inventoryCode, slot))
    			return;

    		byte receivedItems;
    		byte amount;
    		List<InventoryTransaction> changes;
    		ItemStack its;
    		int targetInventory;

    		its = this.inventory[inventoryCode].GetSlot(slot);
    		amount = its.GetAmount();
    		targetInventory = GetBulkMoveTarget(inventoryCode, its);

    		if(targetInventory == -1)
    			return;

    		changes = this.inventory[targetInventory].CanFit(its);
    		receivedItems = this.inventory[targetInventory].AddStack(its, changes);

			if(receivedItems < amount)
				its.SetAmount((byte)(amount - receivedItems));
			else{
				this.inventory[inventoryCode].SetNull(slot);
				if(slot < this.inventory[inventoryCode].GetLastEmptySlot())
					this.inventory[inventoryCode].SetLastEmptySlot((short)slot);
			}

			foreach(InventoryTransaction it in changes){
				DrawSlot((byte)targetInventory, it.slotNumber);
			}

    		DrawSlot(inventoryCode, slot);

            SendEquipDataToServer(inventoryCode, this.draggedStack, null);
			SendInventoryDataToServer();
    	}
    	// If has a selected slot
    	else if(this.draggedStack != null){
    		// If can't move, ignore
    		if(!CanSwitchToInventory(this.draggedStack, inventoryCode, slot))
    			return;

    		// If items are different
    		if(!this.draggedStack.IsEqual(this.inventory[inventoryCode].GetSlot(slot))){
				ItemStack aux = this.draggedStack;
				this.draggedStack = this.inventory[inventoryCode].GetSlot(slot);
				this.inventory[inventoryCode].ForceAddStack(aux, slot);

				if(this.draggedStack == null)
					ResetSelection();
				else
					ToggleHighlight(true, this.draggedStack);

				DrawSlot(inventoryCode, slot);

	            SendEquipDataToServer(inventoryCode, this.draggedStack, aux);
				SendInventoryDataToServer();
			}
			// Stack together same ItemStacks
			else{
				this.draggedStack = this.inventory[inventoryCode].Transfer(this.draggedStack, slot);

				if(this.draggedStack == null)
					ResetSelection();
				else
					ToggleHighlight(true, this.draggedStack);

				DrawSlot(inventoryCode, slot);
				SendInventoryDataToServer();
			}
    	}
    }

    // Activates on Right Click of a slot
    public void RightClick(byte inventoryCode, ushort slot){
    	// If there's no selected slot, then split
    	if(this.draggedStack == null){
    		if(IsNullSlot(inventoryCode, slot))
    			return;

    		string[] details;

    		// If clicked stack has only a single item, grab it
    		if(this.inventory[inventoryCode].GetSlot(slot).GetAmount() == 1){
	    		// Selects slot
	    		this.draggedStack = this.inventory[inventoryCode].GetSlot(slot);
	    		this.inventory[inventoryCode].SetNull(slot);
	    		DrawSlot(inventoryCode, slot);
	    		ToggleHighlight(true, this.draggedStack);
	            ResetDetails();
	            this.detailsPanel.SetActive(true);

	            // Finds the item selected
	            Item item = this.draggedStack.GetItem();

	            details = item.GetDetails();
	            this.detailsName.text = details[0];

	            if(item is Weapon)
	                this.detailsStats.text = details[1];
	            else if(item is Item)
	                this.detailsDescription.text = details[1];

	            this.detailsImage.material.SetTexture("_Texture", ItemLoader.GetSprite(item.GetID()));

	            SendEquipDataToServer(inventoryCode, this.draggedStack, null);
    		}
    		// If stack has more than 1 item
    		else{
    			this.draggedStack = this.inventory[inventoryCode].GetSlot(slot).Split();

				DrawSlot(inventoryCode, slot);
				ToggleHighlight(true, this.draggedStack);
				ResetDetails();
				this.detailsPanel.SetActive(true);

				Item item = this.draggedStack.GetItem();
	            details = item.GetDetails();
	            this.detailsName.text = details[0];

	            if(item is Weapon)
	                this.detailsStats.text = details[1];
	            else if(item is Item)
	                this.detailsDescription.text = details[1];

	            this.detailsImage.material.SetTexture("_Texture", ItemLoader.GetSprite(item.GetID()));
    		}


    		SendInventoryDataToServer();
    	}
    	// If there is a selection and right clicks another slot
    	else{
    		// Case 1: Right clicks on empty slot
    		if(IsNullSlot(inventoryCode, slot)){
    			if(!CanSwitchToInventory(this.draggedStack, inventoryCode, slot))
    				return;

    			bool shouldBeDestroyed = this.draggedStack.Decrement();

    			// If was only holding 1 item
    			if(shouldBeDestroyed){
    				this.inventory[inventoryCode].ForceAddStack(this.draggedStack, slot);
    				this.draggedStack = null;
    				DrawSlot(inventoryCode, slot);
    				ResetSelection();
    				SendEquipDataToServer(inventoryCode, null, this.inventory[inventoryCode].GetSlot(slot));
    				SendInventoryDataToServer();
    				return;
    			}
    			else{
    				this.inventory[inventoryCode].ForceAddStack(new ItemStack(this.draggedStack.GetItem(), 1), slot);
    				DrawSlot(inventoryCode, slot);
    				ToggleHighlight(true, this.draggedStack);
    				SendEquipDataToServer(inventoryCode, null, this.inventory[inventoryCode].GetSlot(slot));
    				SendInventoryDataToServer();
    				return;
    			}
    		}
    		// Case 2: Clicks on a slot with the same item
    		else if(this.draggedStack.IsEqual(this.inventory[inventoryCode].GetSlot(slot))){
    			if(this.inventory[inventoryCode].GetSlot(slot).IsFull())
    				return;

     			if(this.draggedStack.Decrement()){
    				this.draggedStack = null;
    				ResetSelection();
    			}
    			else{
    				ToggleHighlight(true, this.draggedStack);
    			}

    			this.inventory[inventoryCode].GetSlot(slot).Increment();

    			DrawSlot(inventoryCode, slot);
    			SendInventoryDataToServer();
    		}
    		// Case 3: Clicks a slot that contains a different item
    		else if(this.draggedStack.GetID() != this.inventory[inventoryCode].GetSlot(slot).GetID()){
    			if(!CanSwitchToInventory(this.draggedStack, inventoryCode, slot))
    				return;

				ItemStack aux = this.draggedStack;
				this.draggedStack = this.inventory[inventoryCode].GetSlot(slot);
				this.inventory[inventoryCode].ForceAddStack(aux, slot);

				if(this.draggedStack == null)
					ResetSelection();
				else
					ToggleHighlight(true, this.draggedStack);

				DrawSlot(inventoryCode, slot);
				SendEquipDataToServer(inventoryCode, this.draggedStack, aux);
				SendInventoryDataToServer();
    		}
    	}
    }

    public void SetNull(int inventoryCode, byte slot){
    	this.inventory[inventoryCode].SetNull(slot);
    	DrawSlot((byte)inventoryCode, slot);
    }

    // Create a new inventory in the scroll area of the Inventory UI
    public void AddInventory(Inventory inv){
    	int inventoryCode = this.inventory.Count;

		int amountOfColumns = inv.columnCount;
		int inventorySize = inv.GetLimit();
		int amounfOfRows = Mathf.CeilToInt(inventorySize/amountOfColumns);
		Vector2 anchor = new Vector2(0.5f, 0.5f);
		Vector2 groupSizes = new Vector2(amountOfColumns * this.slotSizes.x, Mathf.CeilToInt(inventorySize/amountOfColumns) * this.slotSizes.y);

    	this.inventory.Add(inv);

		// Slots and Text
		GameObject goSlots = GameObject.Instantiate(EMPTY_OBJECT);

		goSlots.name = $"Additional Inventory {inventoryCode-2}";
		goSlots.transform.SetParent(this.scrollingAreaContent.transform);
		RectTransform slotTransform = goSlots.GetComponent<RectTransform>();
		FixTransform(slotTransform, groupSizes, this.textPivot);

		GridLayoutGroup glpSlot = goSlots.AddComponent<GridLayoutGroup>();
		glpSlot.spacing = new Vector2(4f, 4f);
		glpSlot.cellSize = this.slotSizes;
		glpSlot.childAlignment = TextAnchor.LowerCenter;
		glpSlot.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		glpSlot.constraintCount = amountOfColumns;

		this.slotImages.Add(inventoryCode, new Image[inventorySize]);
		this.slotText.Add(inventoryCode, new TextMeshProUGUI[inventorySize]);

		for(int i=0; i < inventorySize; i++){
			this.slotImages[inventoryCode][i] = CreateImageComponent(goSlots, $"Slot-{i+1}", inventoryCode, i, this.slotSizes, anchor);
			this.slotText[inventoryCode][i] = CreateTextComponent(this.slotImages[inventoryCode][i].gameObject, $"TSlot-{i+1}", this.slotSizes, this.textPivot);
		}

		SendInventoryDataToServer();
    }

    // Checks if there are more than 2 inventories with a shift-clicking capability
    private bool CanShiftMove(){
    	int counter = 0;

    	for(int i=0; i < this.inventory.Count; i++){
    		if(this.inventory[i].GetBulkMovedTo())
    			counter++;

    		if(counter >= 2)
    			return true;
    	}

    	return false;
    }

    // Returns the index of the inventory that will be the target of the bulk move
    private int GetBulkMoveTarget(int index, ItemStack its){
    	if(!CanShiftMove())
    		return -1;

    	if(this.bulkMoveAbove){
    		for(int i = 1; i < this.inventory.Count; i++){
    			if(!this.inventory[(index + i) % this.inventory.Count].IsInGlobalWhitelist(its))
    				continue;

    			if(this.inventory[(index + i) % this.inventory.Count].GetBulkMovedTo())
    				return (index + i) % this.inventory.Count;
    		}
    	}
    	else{
    		for(int i = 1; i < this.inventory.Count; i++){
    			if(!this.inventory[Mathf.Abs(index - i) % this.inventory.Count].IsInGlobalWhitelist(its))
    				continue;

    			if(this.inventory[Mathf.Abs(index - i) % this.inventory.Count].GetBulkMovedTo())
    				return Mathf.Abs(index - i) % this.inventory.Count;
    		}
    	}

    	return -1;
    }

    // Checks if it's possible to switch slots based on tag limitations
    private bool CanSwitchBetweenInventories(int indexOrigin, int indexTarget, ushort slotOrigin, ushort slotTarget, ItemStack itsOrigin, ItemStack itsTarget){
		if(!this.inventory[indexOrigin].IsInGlobalWhitelist(itsTarget) || !this.inventory[indexOrigin].IsInLocalWhitelist(itsTarget, slotOrigin))
			return false;
		if(!this.inventory[indexTarget].IsInGlobalWhitelist(itsOrigin) || !this.inventory[indexTarget].IsInLocalWhitelist(itsOrigin, slotTarget))
			return false;

    	return true;
    }

    // To be used with the draggedItem ItemStack
    private bool CanSwitchToInventory(ItemStack its, int indexInventory, ushort slot){
    	if(this.inventory[indexInventory].IsInGlobalWhitelist(its) && this.inventory[indexInventory].IsInLocalWhitelist(its, slot))
    		return true;
    	return false;
    }

    // Gets the inventory index based on slot number
    private int GetInventoryIndex(byte slot){
    	int sum = 0;

    	for(int index = 0; index < this.inventory.Count; index++){
    		sum += this.inventory[index].GetLimit();
    		if(slot < sum){
    			return index;
    		}
    	}

    	throw new SlotOutOfRangeException($"[PlayerInventoryManager] Slot {slot} is out of range. Total limit is {sum}");
    }

    // Returns true if slot is null
    private bool IsNullSlot(byte inventoryCode, ushort slot){
    	if(this.inventory[inventoryCode].GetSlot(slot) == null)
    		return true;

		return false;
    }

    // Resets selection
    public void ResetSelection(){
    	this.ToggleHighlight(false, null);
    	this.draggedStack = null;
        this.detailsPanel.SetActive(false);
        this.ResetDetails();
	}

    // Resets text in details panel
    private void ResetDetails(){
        this.detailsName.text = "";
        this.detailsDescription.text = "";
        this.detailsStats.text = "";
        this.detailsImage.material.SetTexture("_Texture", null);
    }

	// Toggles selection highlighting
	private void ToggleHighlight(bool b, ItemStack its){
		this.dragOverlay.gameObject.SetActive(b);
		this.dragStacksize.gameObject.SetActive(b);

		if(b){
			this.dragOverlay.material.SetTexture("_Texture", ItemLoader.GetSprite(its));

			if(its.GetAmount() > 1)
				this.dragStacksize.text = its.GetAmount().ToString();
			else
				this.dragStacksize.text = ""; 
		}
		else{
			this.dragOverlay.material.SetTexture("_Texture", null);
			this.dragStacksize.text = ""; 
		}
	}

	// ------------------------- Inventory Generation --------------------------------

	// Creates the default player inventories
	private void StartInventory(){
		this.inventory.Add(InventoryLoader.GetActionInventory("ACTION_HOTBAR"));
		this.inventory.Add(InventoryLoader.GetInventory("HOTBAR"));
		this.inventory.Add(InventoryLoader.GetInventory("PLAYER"));
		this.inventory.Add(InventoryLoader.GetInventory("EQUIPMENT"));
	}

	// Creates Action hotbar UI
	private void CreateActionHotbar(GameObject parent){
		int amountOfColumns = InventoryLoader.GetActionColumnCount("ACTION_HOTBAR");

		GameObject goSlots = GameObject.Instantiate(EMPTY_OBJECT);
		Vector2 groupSizes = new Vector2(amountOfColumns * this.slotSizes.x, this.slotSizes.y);
		Vector2 anchorMain = new Vector2(0.5f, 0f);
		Vector2 anchorSec = new Vector2(0.5f, 0.5f);

		int inventorySize = InventoryLoader.GetInventorySize("ACTION_HOTBAR");

		goSlots.name = "ActionHotbarSlots";
		goSlots.transform.SetParent(parent.transform);
		RectTransform slotTransform = goSlots.GetComponent<RectTransform>();
		FixTransform(slotTransform, groupSizes, anchorMain, addX:-48, addY:20);

		HorizontalLayoutGroup hlpSlot = goSlots.AddComponent<HorizontalLayoutGroup>();
		hlpSlot.spacing = 4f;
		hlpSlot.childForceExpandHeight = true;
		hlpSlot.childForceExpandWidth = true;
		hlpSlot.childAlignment = TextAnchor.MiddleCenter;
		hlpSlot.childControlHeight = false;
		hlpSlot.childControlWidth = false;

		this.slotImages.Add(0, new Image[inventorySize]);
		this.slotText.Add(0, new TextMeshProUGUI[inventorySize]);

		for(int i=0; i < inventorySize; i++){
			this.slotImages[0][i] = CreateImageComponent(goSlots, $"Slot-{i+1}", 0, i, this.slotSizes, anchorSec);
			this.slotText[0][i] = CreateTextComponent(this.slotImages[0][i].gameObject, $"TSlot-{i+1}", this.slotSizes, this.textPivot);
		}
	}

	// Creates hotbar UI
	private void CreateHotbarInventory(GameObject parent){
		int amountOfColumns = InventoryLoader.GetColumnCount("HOTBAR");

		GameObject goSlots = GameObject.Instantiate(EMPTY_OBJECT);
		Vector2 groupSizes = new Vector2(amountOfColumns * this.slotSizes.x, this.slotSizes.y);
		Vector2 anchorMain = new Vector2(0.5f, 0f);
		Vector2 anchorSec = new Vector2(0.5f, 0.5f);

		int inventorySize = InventoryLoader.GetInventorySize("HOTBAR");

		goSlots.name = "HotbarSlots";
		goSlots.transform.SetParent(parent.transform);
		RectTransform slotTransform = goSlots.GetComponent<RectTransform>();
		FixTransform(slotTransform, groupSizes, anchorMain, addX:-48, addY:(int)(20 + this.slotSizes.y + 10));


		HorizontalLayoutGroup hlpSlot = goSlots.AddComponent<HorizontalLayoutGroup>();
		hlpSlot.spacing = 4f;
		hlpSlot.childForceExpandHeight = true;
		hlpSlot.childForceExpandWidth = true;
		hlpSlot.childAlignment = TextAnchor.MiddleCenter;
		hlpSlot.childControlHeight = false;
		hlpSlot.childControlWidth = false;

		this.slotImages.Add(1, new Image[inventorySize]);
		this.slotText.Add(1, new TextMeshProUGUI[inventorySize]);

		for(int i=0; i < inventorySize; i++){
			this.slotImages[1][i] = CreateImageComponent(goSlots, $"Slot-{i+1}", 1, i, this.slotSizes, anchorSec);
			this.slotText[1][i] = CreateTextComponent(this.slotImages[1][i].gameObject, $"TSlot-{i+1}", this.slotSizes, this.textPivot);
		}
	}

	// Creates Main Bag UI
	private void CreatePlayerBagInventory(GameObject parent){
		int amountOfColumns = InventoryLoader.GetColumnCount("PLAYER");
		int inventorySize = InventoryLoader.GetInventorySize("PLAYER");
		int amounfOfRows = Mathf.CeilToInt(inventorySize/amountOfColumns);


		Vector2 scrollAreaSizes = new Vector2(850f, 820f);
		Vector2 groupSizes = new Vector2(amountOfColumns * this.slotSizes.x, amounfOfRows * this.slotSizes.y);
		Vector2 anchorMain = new Vector2(0.5f, 0f);
		Vector2 anchorSec = new Vector2(0.5f, 0.5f);

		// Scroll Object
		GameObject scrollObject = GameObject.Instantiate(EMPTY_OBJECT);
		scrollObject.name = "ScrollArea";
		scrollObject.transform.SetParent(parent.transform);
		RectTransform rectTransf = scrollObject.GetComponent<RectTransform>();
		FixTransform(rectTransf, scrollAreaSizes, anchorMain, addY:200);
		ScrollRectNoDrag scroll = scrollObject.AddComponent<ScrollRectNoDrag>();
		scroll.movementType = ScrollRect.MovementType.Clamped;
		scroll.scrollSensitivity = 100f;
		scroll.viewport = rectTransf;
		Image transparentImage = scrollObject.AddComponent<Image>();
		transparentImage.color = new Color(1f,1f,1f,0f);
		RectMask2D rm = scrollObject.AddComponent<RectMask2D>();
		rm.padding = new Vector4(0f,36f,0f,0f);
		rm.softness = new Vector2Int(0,30);
		scroll.horizontal = false;

		// Content
		GameObject contentObject = GameObject.Instantiate(EMPTY_OBJECT);
		contentObject.name = "Content";
		contentObject.transform.SetParent(scrollObject.transform);
		this.scrollingAreaContent = contentObject;
		RectTransform rectTransform = contentObject.GetComponent<RectTransform>();
		FixTransform(rectTransform, scrollAreaSizes, anchorMain);
		scroll.content = contentObject.GetComponent<RectTransform>();

		VerticalLayoutGroup vlg = contentObject.AddComponent<VerticalLayoutGroup>();
		vlg.spacing = 80;
		vlg.childForceExpandHeight = false;
		vlg.childForceExpandWidth = false;
		vlg.childAlignment = TextAnchor.LowerCenter;
		vlg.childControlHeight = false;
		vlg.childControlWidth = false;
		vlg.reverseArrangement = true;
		vlg.padding = new RectOffset(0, 0, 54, 54);

		ContentSizeFitter csf = contentObject.AddComponent<ContentSizeFitter>();
		csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		// Slots and Text
		GameObject goSlots = GameObject.Instantiate(EMPTY_OBJECT);

		goSlots.name = "MainInventorySlots";
		goSlots.transform.SetParent(contentObject.transform);
		RectTransform slotTransform = goSlots.GetComponent<RectTransform>();
		FixTransform(slotTransform, groupSizes, this.textPivot, addY:50);

		GridLayoutGroup glpSlot = goSlots.AddComponent<GridLayoutGroup>();
		glpSlot.spacing = new Vector2(4f, 4f);
		glpSlot.cellSize = this.slotSizes;
		glpSlot.childAlignment = TextAnchor.LowerCenter;
		glpSlot.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		glpSlot.constraintCount = amountOfColumns;

		this.slotImages.Add(2, new Image[inventorySize]);
		this.slotText.Add(2, new TextMeshProUGUI[inventorySize]);

		for(int i=0; i < inventorySize; i++){
			this.slotImages[2][i] = CreateImageComponent(goSlots, $"Slot-{i+1}", 2, i, this.slotSizes, anchorSec);
			this.slotText[2][i] = CreateTextComponent(this.slotImages[2][i].gameObject, $"TSlot-{i+1}", this.slotSizes, this.textPivot);
		}
	}

	// Creates Equipment UI
	private void CreateEquipmentInventory(GameObject parent){
		GameObject goSlots = GameObject.Instantiate(EMPTY_OBJECT);
		GameObject goTexts = GameObject.Instantiate(EMPTY_OBJECT);
		Vector2 groupSizes = new Vector2(96f, 96f);
		Vector2 anchorSec = new Vector2(0f, 0.5f);

		int inventorySize = InventoryLoader.GetInventorySize("EQUIPMENT");

		goSlots.name = "EquipmentSlots";
		goSlots.transform.SetParent(parent.transform);
		RectTransform slotTransform = goSlots.GetComponent<RectTransform>();
		FixTransform(slotTransform, groupSizes, anchorSec, addX:400);

		this.slotImages.Add(3, new Image[inventorySize]);
		this.slotText.Add(3, new TextMeshProUGUI[inventorySize]);

		for(int i=0; i < inventorySize; i++){
			this.slotImages[3][i] = CreateImageComponent(goSlots, $"Slot-{i+1}", 3, i, this.slotSizes, anchorSec);
			this.slotText[3][i] = CreateTextComponent(this.slotImages[3][i].gameObject, $"TSlot-{i+1}", this.slotSizes, this.textPivot);

			this.slotImages[3][i].gameObject.transform.localPosition = new Vector3(0, (-100 * (i-1)), 0f);
			this.slotText[3][i].gameObject.transform.localPosition = new Vector3(0, (-100 * (i-1)), 0f);
		}
	}

	private void CreateDragSlot(GameObject parent){
		GameObject go = GameObject.Instantiate(EMPTY_OBJECT);
		Vector2 anchor = new Vector2(0.5f, 0.5f);

		go.name = "DragBase";
		go.transform.SetParent(parent.transform);
		RectTransform transf = go.GetComponent<RectTransform>();
		FixTransform(transf, this.slotSizes, anchor);


		this.dragOverlay = CreateImageComponent(go, "DragSlot", this.slotSizes, anchor);
		this.dragOverlay.material = Instantiate(this.itemIconMaterial);
		this.dragOverlay.material.SetTexture("_Texture", null);
		this.dragStacksize = CreateTextComponent(go, "DragStacksize", this.slotSizes, anchor);
		this.dragStacksize.gameObject.AddComponent<MouseFollowerUI>();

		this.dragOverlay.gameObject.SetActive(false);
		this.dragStacksize.gameObject.SetActive(false);
	}


	// ------------------------- Component Generation ------------------------------------------

	// Creates an Image component using Inventory's default style
	private Image CreateImageComponent(GameObject parent, string goName, int inventoryCode, int slot, Vector2 size, Vector2 anchor){
		GameObject go = GameObject.Instantiate(EMPTY_OBJECT);
		go.name = goName;
		go.transform.SetParent(parent.transform);
		RectTransform transf = go.GetComponent<RectTransform>();
		FixTransform(transf, size, anchor);


		Image img = go.AddComponent<Image>();
		img.raycastTarget = true;
		img.material = Instantiate(this.itemIconMaterial);
		img.material.name = $"Slot-{slot+1}";
		img.material.SetTexture("_Texture", null);

		InventoryButton button = go.AddComponent<InventoryButton>();
		button.inventoryCode = (byte)inventoryCode;
		button.slot = (ushort)slot;
		button.SetController(this);

		return img;
	}

	// Creates an Image that follows the cursor using Inventory's default style
	private Image CreateImageComponent(GameObject parent, string goName, Vector2 size, Vector2 anchor){
		GameObject go = GameObject.Instantiate(EMPTY_OBJECT);
		go.name = goName;
		go.transform.SetParent(parent.transform);
		RectTransform transf = go.GetComponent<RectTransform>();
		FixTransform(transf, size, anchor);

		Image img = go.AddComponent<Image>();
		img.raycastTarget = false;
		img.material = Instantiate(this.itemIconMaterial);
		img.material.SetTexture("_Texture", null);

		go.AddComponent<MouseFollowerUI>();

		return img;
	}

	// Creates a TextMeshProUI component using Inventory's default style
	private TextMeshProUGUI CreateTextComponent(GameObject parent, string goName, Vector2 size, Vector2 anchor){
		GameObject go = GameObject.Instantiate(EMPTY_OBJECT);
		go.name = goName;
		go.transform.SetParent(parent.transform);
		RectTransform transf = go.GetComponent<RectTransform>();
		FixTransform(transf, size, anchor);

		TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();

		tmp.font = this.liberationSans;
		tmp.fontSize = 24;
		tmp.raycastTarget = false;
		tmp.alignment = TextAlignmentOptions.BottomRight;

		return tmp;
	}

	private void FixTransform(RectTransform rect, Vector2 size, Vector2 anchor, int addX = 0, int addY = 0, bool special = false){
		if(!special){
			rect.anchorMin = anchor;
			rect.anchorMax = anchor;
			rect.pivot = anchor;
		}
		else{
			rect.anchorMin = new Vector2(0.5f, 0f);
			rect.anchorMax = new Vector2(0.5f, 1f);
			rect.pivot = new Vector2(0.5f, 0.5f);
		}
		rect.anchoredPosition = Vector2.zero;
		rect.localScale = Vector3.one;
		rect.localRotation = Quaternion.identity;
		rect.sizeDelta = size;
		rect.localPosition = new Vector3(rect.localPosition.x + addX, rect.localPosition.y + addY, 0f);
	}
}
