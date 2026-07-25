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

    private bool bulkMoveAbove = true; // If inventory shift-move should be done upwards or downwards

	// Inventory data and draw info
	private List<Inventory> inventory = new List<Inventory>();
	private ItemStack draggedStack;
	private byte[] buffer = new byte[30000];

	// Temporary
	[SerializeField]
	public Image[] invButton;
	[SerializeField]
	public TextMeshProUGUI[] invText;
	[SerializeField]
	public Image[] hbButton;
	[SerializeField]
	public TextMeshProUGUI[] hbText;
	[SerializeField]
	public Image[] equipButton;
	[SerializeField]
	public TextMeshProUGUI[] equipText;

	// Drag and Drop overlay
	public Image dragOverlay;
	public TextMeshProUGUI dragStacksize;

	// Color constants
	private readonly Color WHITE = new Color(1f,1f,1f,1f);
	private readonly Color RED = new Color(1f, 0.5f, 0.5f, 1f);

	void Awake(){
		int i = 0;

		foreach(Image img in invButton){
			img.material = Instantiate(this.itemIconMaterial);
			img.material.name = $"Slot-{i}";
			img.material.SetTexture("_Texture", null);
			i++;
		}
		i = 0;
		foreach(Image img in hbButton){
			img.material = Instantiate(this.itemIconMaterial);
			img.material.name = $"Hotbar-{i}";
			img.material.SetTexture("_Texture", null);
			i++;
		}
		i = 0;
		foreach(Image img in equipButton){
			img.material = Instantiate(this.itemIconMaterial);
			img.material.name = $"Equipment-{i}";
			img.material.SetTexture("_Texture", null);
			i++;
		}

		this.dragOverlay.material = Instantiate(this.itemIconMaterial);
		this.dragOverlay.material.SetTexture("_Texture", null);

		this.detailsImage.material = Instantiate(this.itemIconMaterial);
		this.background.material = Instantiate(this.backgroundMaterial);

		if(this.inventory.Count == 0)
			StartInventory();
	}

	void OnDisable(){
		if(this.draggedStack != null){
			this.mainControllerManager.DropItem(this.draggedStack);
			ResetSelection();
		}
	}

	// Creates inventories based on byte array
	public void LoadFromBytes(byte[] data, int init){
		// Control variables
		int bytesRead = init;
		int currentInventory = 0;
		InventoryType type;
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

		if(this.inventory.Count == 0)
			StartInventory();

		while(bytesRead < data.Length){
			type = (InventoryType)data[bytesRead];
			bytesRead++;

			if(type != this.inventory[currentInventory].GetInventoryType()){
				if(this.inventory.Count < currentInventory)
					this.inventory[currentInventory] = InventoryLoader.GetInventory(type);
				else
					this.inventory.Add(InventoryLoader.GetInventory(type));
			}

			for(ushort i=0; i < this.inventory[currentInventory].GetLimit(); i++){
				mst = (MemoryStorageType)data[bytesRead];
				bytesRead++;

				switch(mst){
					case MemoryStorageType.EMPTY:
						this.inventory[currentInventory].SetSlot(i, null);
						break;
					case MemoryStorageType.ITEM:
						id = NetDecoder.ReadUshort(data, bytesRead);
						bytesRead += 2;
						quantity = data[bytesRead];
						bytesRead++;
						item = ItemLoader.GetCopy(id);
						its = new ItemStack(item, quantity);
						this.inventory[currentInventory].SetSlot(i, its);
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
						this.inventory[currentInventory].SetSlot(i, its); 
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
		int bytesWritten = 0;

		for(int inventoryCode=0; inventoryCode < this.inventory.Count; inventoryCode++){
			this.buffer[bytesWritten] = (byte)this.inventory[inventoryCode].GetInventoryType();
			bytesWritten++;

			for(ushort i=0; i < this.inventory[inventoryCode].GetLimit(); i++){
				if(this.inventory[inventoryCode].GetSlot(i) == null){
					this.buffer[bytesWritten] = (byte)MemoryStorageType.EMPTY;
					bytesWritten++;
				}
				else{
					its = this.inventory[inventoryCode].GetSlot(i);
					bytesWritten += its.ConvertToMemory(this.buffer, bytesWritten);
				}
			}
		}

		return bytesWritten;
	}

    public void ReloadInventory(){
		for(int i=0; i < this.inventory.Count; i++){
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

    public Inventory GetMainInventory(){
    	for(int i=0; i < this.inventory.Count; i++){
    		if(this.inventory[i].mainInventory)
    			return this.inventory[i];
    	}

    	throw new MainInventoryNotFoundException($"[PlayerInventoryManager] None of the current inventories have the main flag set. Inventory count: {this.inventory.Count}");
    }

    // Draws the ItemStacks into the Inventory Screen
    private void DrawStacks(){
    	ItemStack its;

    	// Inventory
    	for(ushort i=0; i < this.inventory[1].GetLimit(); i++){
    		its = this.inventory[1].GetSlot(i);

    		if(its == null)
    			continue;

    		this.invButton[i].material.SetTexture("_Texture", ItemLoader.GetSprite(its));

    		if(its.GetStacksize() > 1)
    			this.invText[i].text = its.GetAmount().ToString();
    	}

    	// Hotbar
    	for(ushort i=0; i < this.inventory[0].GetLimit(); i++){
    		its = this.inventory[0].GetSlot(i);

    		if(its == null)
    			continue;

    		this.hbButton[i].material.SetTexture("_Texture", ItemLoader.GetSprite(its));

    		if(its.GetStacksize() > 1)
    			this.hbText[i].text = its.GetAmount().ToString();
    	}

    	// Equipment
    	for(ushort i=0; i < this.inventory[2].GetLimit(); i++){
    		its = this.inventory[2].GetSlot(i);

    		if(its == null)
    			continue;

    		this.equipButton[i].material.SetTexture("_Texture", ItemLoader.GetSprite(its));

    		if(its.GetStacksize() > 1)
    			this.equipText[i].text = its.GetAmount().ToString();
    	}
    }

    // Redraws a specific slot
    public void DrawSlot(byte inventoryCode, ushort slot){
    	ItemStack its = this.inventory[inventoryCode].GetSlot(slot);

    	if(inventoryCode == 1){
    		if(its == null){
    			this.invButton[slot].material.SetTexture("_Texture", null);
    			this.invText[slot].text = "";
    		}
    		else{
	    		this.invButton[slot].material.SetTexture("_Texture", ItemLoader.GetSprite(its));

	    		if(its.GetStacksize() > 1)
	    			this.invText[slot].text = its.GetAmount().ToString();
	    		else
	    			this.hbText[slot].text = "";   			
    		}
    	}
    	else if(inventoryCode == 0){
    		if(its == null){
    			this.hbButton[slot].material.SetTexture("_Texture", null);
    			this.hbText[slot].text = "";
    		}
    		else{
	    		this.hbButton[slot].material.SetTexture("_Texture", ItemLoader.GetSprite(its));

	    		if(its.GetStacksize() > 1)
	    			this.hbText[slot].text = its.GetAmount().ToString();
	    		else
	    			this.hbText[slot].text = "";			
    		}    		
    	}
    	else{
    		if(its == null){
    			this.equipButton[slot].material.SetTexture("_Texture", null);
    			this.equipText[slot].text = "";
    		}
    		else{
	    		this.equipButton[slot].material.SetTexture("_Texture", ItemLoader.GetSprite(its));

	    		if(its.GetStacksize() > 1)
	    			this.equipText[slot].text = its.GetAmount().ToString();
	    		else
	    			this.hbText[slot].text = ""; 			
    		}    
    	}
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
				this.inventory[inventoryCode].RemoveFromRecords(its.GetID());
			}

			foreach(InventoryTransaction it in changes){
				DrawSlot((byte)targetInventory, it.slotNumber);
			}

    		DrawSlot(inventoryCode, slot);
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
				this.inventory[inventoryCode].SetSlot(slot, aux);

				if(this.draggedStack == null)
					ResetSelection();
				else
					ToggleHighlight(true, this.draggedStack);

				DrawSlot(inventoryCode, slot);
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
    		/*
    		Case 1: Clicks an empty slot
    		Case 2: Clicks a slot that contains the same item
    		Case 3: Clicks a slot that contains a different item
    		*/
    		// Case 1: Right clicks on empty slot
    		if(IsNullSlot(inventoryCode, slot)){
    			if(!CanSwitchToInventory(this.draggedStack, inventoryCode, slot))
    				return;

    			bool shouldBeDestroyed = this.draggedStack.Decrement();

    			// If was only holding 1 item
    			if(shouldBeDestroyed){
    				this.inventory[inventoryCode].SetSlot(slot, this.draggedStack);
    				this.draggedStack = null;
    				DrawSlot(inventoryCode, slot);
    				ResetSelection();
    				SendInventoryDataToServer();
    				return;
    			}
    			else{
    				this.inventory[inventoryCode].SetSlot(slot, new ItemStack(this.draggedStack.GetItem(), 1));
    				DrawSlot(inventoryCode, slot);
    				ToggleHighlight(true, this.draggedStack);
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
				this.inventory[inventoryCode].SetSlot(slot, aux);

				if(this.draggedStack == null)
					ResetSelection();
				else
					ToggleHighlight(true, this.draggedStack);

				DrawSlot(inventoryCode, slot);
				SendInventoryDataToServer();
    		}
    	}
    }

    // Checks if there are more than 2 inventories with a shift-clicking capability
    private bool CanShiftMove(){
    	int counter = 0;

    	for(int i=0; i < this.inventory.Count; i++){
    		if(this.inventory[i].bulkMovedTo)
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

    			if(this.inventory[(index + i) % this.inventory.Count].bulkMovedTo)
    				return (index + i) % this.inventory.Count;
    		}
    	}
    	else{
    		for(int i = 1; i < this.inventory.Count; i++){
    			if(!this.inventory[Mathf.Abs(index - i) % this.inventory.Count].IsInGlobalWhitelist(its))
    				continue;

    			if(this.inventory[Mathf.Abs(index - i) % this.inventory.Count].bulkMovedTo)
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

	private void StartInventory(){
		this.inventory.Add(InventoryLoader.GetInventory(InventoryType.HOTBAR));
		this.inventory.Add(InventoryLoader.GetInventory(InventoryType.PLAYER));
		this.inventory.Add(InventoryLoader.GetInventory(InventoryType.EQUIPMENT));
	}
}
