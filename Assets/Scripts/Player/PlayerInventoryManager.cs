using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInventoryManager : MonoBehaviour {
    // Unity Reference
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

	// Inventory Logic
	private byte selectedInventory = byte.MaxValue;
	private ushort selectedSlot = byte.MaxValue;

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
		foreach(Image img in equipButton){
			img.material = Instantiate(this.itemIconMaterial);
			img.material.name = $"Equipment-{i}";
			img.material.SetTexture("_Texture", null);
			i++;
		}

		this.detailsImage.material = Instantiate(this.itemIconMaterial);
		this.background.material = Instantiate(this.backgroundMaterial);

		if(this.inventory.Count == 0)
			StartInventory();
	}

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

    public void ReloadInventory(){
		for(int i=0; i < this.inventory.Count; i++){
			this.inventory[i].FindLastEmptySlot();
		}

        DrawStacks();
    }

    public void SendInventoryDataToServer(){
    	int inventorySize = InventorySerializer.SerializePlayerInventory(this.inventory);

		NetMessage message = new NetMessage(NetCode.SENDINVENTORY);
		message.SendInventory(InventorySerializer.buffer, inventorySize);
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
    		}    
    	}
    }

    // Activates on Left Click of a slot
    public void LeftClick(byte inventoryCode, ushort slot){
    	// If has no slot selected and not shifting
    	if(this.selectedSlot == byte.MaxValue && !MainControllerManager.shifting){
    		// Avoid null slot click
    		if(this.IsNullSlot(inventoryCode, slot))
    			return;

            string[] details; 

    		// Selects slot
    		this.selectedInventory = inventoryCode;
    		this.selectedSlot = slot;
    		ToggleHighlight(true);
            ResetDetails();
            this.detailsPanel.SetActive(true);

            // Finds the item selected
            Item item = this.inventory[inventoryCode].GetSlot(this.selectedSlot).GetItem();

            details = item.GetDetails();
            this.detailsName.text = details[0];

            if(item is Weapon)
                this.detailsStats.text = details[1];
            else if(item is Item)
                this.detailsDescription.text = details[1];

            this.detailsImage.material.SetTexture("_Texture", ItemLoader.GetSprite(item.GetID()));
    	}
    	// If has no slot selected and shift clicked
    	else if(this.selectedSlot == byte.MaxValue && MainControllerManager.shifting){
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
    	else{
    		if(this.selectedInventory == inventoryCode && this.selectedSlot == slot){
    			ResetSelection();
    			return;
    		}

    		if(!CanSwitchBetweenInventories(this.selectedInventory, inventoryCode, this.selectedSlot, slot, this.inventory[this.selectedInventory].GetSlot(this.selectedSlot), this.inventory[inventoryCode].GetSlot(slot))){
				ResetSelection();
				return;
    		}

    		Inventory.SwitchSlots(this.inventory[this.selectedInventory], this.selectedSlot, this.inventory[inventoryCode], slot);

    		this.DrawSlot(this.selectedInventory, this.selectedSlot);
    		this.DrawSlot(inventoryCode, slot);
    		this.ResetSelection();

    		SendInventoryDataToServer();
    	}
    }

    // Activates on Right Click of a slot
    public void RightClick(byte inventoryCode, ushort slot){
    	// If there's no selected slot, then split
    	if(this.selectedSlot == byte.MaxValue){
    		if(this.IsNullSlot(inventoryCode, slot))
    			return;

    		ushort newSlot;

    		if(!this.inventory[inventoryCode].IsFull()){
    			if(this.inventory[inventoryCode].AddFromSplit(this.inventory[inventoryCode].GetSlot(slot).Split(), slot, out newSlot)){
    				this.DrawSlot(inventoryCode, slot);
    				this.DrawSlot(inventoryCode, newSlot);
    			}
    		}

    		SendInventoryDataToServer();
    	}
    	// If there's a selection, then unselect
    	else{
    		this.ResetSelection();
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
    	this.ToggleHighlight(false);
		this.selectedSlot = byte.MaxValue;
		this.selectedInventory = byte.MaxValue;
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
	private void ToggleHighlight(bool b){
		if(this.selectedInventory == byte.MaxValue)
			return;

		if(this.selectedInventory == 1){
			if(b)
				invButton[this.selectedSlot].material.SetFloat("_IsClicked", 1);
			else
				invButton[this.selectedSlot].material.SetFloat("_IsClicked", 0);
		}
		else if(this.selectedInventory == 0){
			if(b)
				hbButton[this.selectedSlot].material.SetFloat("_IsClicked", 1);
			else
				hbButton[this.selectedSlot].material.SetFloat("_IsClicked", 0);
		}
		else{
			if(b)
				equipButton[this.selectedSlot].material.SetFloat("_IsClicked", 1);
			else
				equipButton[this.selectedSlot].material.SetFloat("_IsClicked", 0);
		}
	}

	private void StartInventory(){
		this.inventory.Add(InventoryLoader.GetInventory(InventoryType.HOTBAR));
		this.inventory.Add(InventoryLoader.GetInventory(InventoryType.PLAYER));
		this.inventory.Add(InventoryLoader.GetInventory(InventoryType.EQUIPMENT));
	}
}
