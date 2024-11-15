using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUIPlayer : MonoBehaviour
{
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

	// Inventory data and draw info
	private Inventory inv1;
	private Inventory inv2;
	[SerializeField]
	public Image[] invButton;
	[SerializeField]
	public TextMeshProUGUI[] invText;
	[SerializeField]
	public Image[] hbButton;
	[SerializeField]
	public TextMeshProUGUI[] hbText;

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

		this.detailsImage.material = Instantiate(this.itemIconMaterial);
		this.background.material = Instantiate(this.backgroundMaterial);
	}


    public void OpenInventory(Inventory inventory, Inventory hotbar){
		this.inv1 = inventory;
		this.inv2 = hotbar;

		this.DrawStacks();
        this.inv1.FindLastEmptySlot();
        this.inv2.FindLastEmptySlot();
    }

    public void ReloadInventory(){
        this.inv1.FindLastEmptySlot();
        this.inv2.FindLastEmptySlot();  

        this.DrawStacks();
    }

    public void SendInventoryDataToServer(){
    	int inventorySize = InventorySerializer.SerializePlayerInventory(this.inv2, this.inv1);

		NetMessage message = new NetMessage(NetCode.SENDINVENTORY);
		message.SendInventory(InventorySerializer.buffer, inventorySize);
		this.cl.client.Send(message);
    }

    // Draws the ItemStacks into the Inventory Screen
    private void DrawStacks(){
    	ItemStack its;

    	// Player Inventory
    	for(ushort i=0; i < this.inv1.GetLimit(); i++){
    		its = this.inv1.GetSlot(i);

    		if(its == null)
    			continue;

    		this.invButton[i].material.SetTexture("_Texture", ItemLoader.GetSprite(its));

    		if(its.GetStacksize() > 1)
    			this.invText[i].text = its.GetAmount().ToString();
    	}

    	for(ushort i=0; i < this.inv2.GetLimit(); i++){
    		its = this.inv2.GetSlot(i);

    		if(its == null)
    			continue;

    		this.hbButton[i].material.SetTexture("_Texture", ItemLoader.GetSprite(its));

    		if(its.GetStacksize() > 1)
    			this.hbText[i].text = its.GetAmount().ToString();
    	}
    }

    // Redraws a specific slot
    public void DrawSlot(byte inventoryCode, ushort slot){
    	ItemStack its;

    	if(inventoryCode == 0){
    		its = this.inv1.GetSlot(slot);

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
    	else{
    		its = this.inv2.GetSlot(slot);

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
    }

    // Activates on Left Click of a slot
    public void LeftClick(byte inventoryCode, ushort slot){
    	// If has no slot selected and not shifting
    	if(this.selectedSlot == byte.MaxValue && !MainControllerManager.shifting){
    		// Avoid null slot click
    		if(this.IsNullSlot(inventoryCode, slot))
    			return;

            Item item;
            string[] details; 

    		// Selects slot
    		this.selectedInventory = inventoryCode;
    		this.selectedSlot = slot;
    		this.ToggleHighlight(true);
            ResetDetails();
            this.detailsPanel.SetActive(true);

            // Finds the item selected
            if(inventoryCode == 0)
                item = inv1.GetSlot(this.selectedSlot).GetItem();
            else
                item = inv2.GetSlot(this.selectedSlot).GetItem();

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
    		if(this.IsNullSlot(inventoryCode, slot))
    			return;

    		byte receivedItems;
    		byte amount;
    		List<InventoryTransaction> changes;
    		ItemStack its;

    		// Sends item from inv1 to inv2
    		if(inventoryCode == 0){
    			its = inv1.GetSlot(slot);
    			amount = its.GetAmount();
    			changes = inv2.CanFit(its);
    			receivedItems = inv2.AddStack(its, changes);
    			
    			if(receivedItems < amount)
    				its.SetAmount((byte)(amount - receivedItems));
    			else{
    				inv1.SetNull(slot);
    				if(slot < inv1.GetLastEmptySlot())
    					inv1.SetLastEmptySlot((short)slot);
    				inv1.RemoveFromRecords(its.GetID());
    			}

    			foreach(InventoryTransaction it in changes){
    				this.DrawSlot(1, it.slotNumber);
    			}
    		}
    		// Sends item from inv2 to inv1
    		else{
    			its = inv2.GetSlot(slot);
    			amount = its.GetAmount();
    			changes = inv1.CanFit(its);
    			receivedItems = inv1.AddStack(its, changes);
    			
    			if(receivedItems < amount)
    				its.SetAmount((byte)(amount - receivedItems));
    			else{
    				inv2.SetNull(slot);
    				if(slot < inv2.GetLastEmptySlot())
    					inv2.SetLastEmptySlot((short)slot);
    				inv2.RemoveFromRecords(its.GetID());
    			}

    			foreach(InventoryTransaction it in changes){
    				this.DrawSlot(0, it.slotNumber);
    			}
    		}

    		this.DrawSlot(inventoryCode, slot);

			SendInventoryDataToServer();
    	}
    	// If has a selected slot
    	else{
    		if(this.selectedInventory == 0 && inventoryCode == 0)
    			Inventory.SwitchSlots(inv1, this.selectedSlot, inv1, slot);
    		else if(this.selectedInventory == 0)
    			Inventory.SwitchSlots(inv1, this.selectedSlot, inv2, slot);
    		else if(inventoryCode == 0)
    			Inventory.SwitchSlots(inv2, this.selectedSlot, inv1, slot);
    		else
    			Inventory.SwitchSlots(inv2, this.selectedSlot, inv2, slot);

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

    		if(inventoryCode == 0){
                if(!inv1.IsFull()){
        			if(inv1.AddFromSplit(inv1.GetSlot(slot).Split(), slot, out newSlot)){
        				this.DrawSlot(inventoryCode, slot);
        				this.DrawSlot(inventoryCode, newSlot);
        			}
                }
    		}
    		else{
                if(!inv2.IsFull()){
        			if(inv2.AddFromSplit(inv2.GetSlot(slot).Split(), slot, out newSlot)){
        				this.DrawSlot(inventoryCode, slot);
        				this.DrawSlot(inventoryCode, newSlot);
        			}    
                }			
    		}

    		SendInventoryDataToServer();
    	}
    	// If there's a selection, then unselect
    	else{
    		this.ResetSelection();
    	}
    }

    // Returns true if slot is null
    private bool IsNullSlot(byte inventoryCode, ushort slot){
		if(inventoryCode == 0){
			if(inv1.GetSlot(slot) == null)
				return true;
		}
		else{
			if(inv2.GetSlot(slot) == null)
				return true;
		}
		return false;
    }

    // Nulls a slot in Hotbar
    public void SetNull(ushort slot){
        inv2.SetNull(slot);
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

		if(this.selectedInventory == 0){
			if(b)
				invButton[this.selectedSlot].material.SetFloat("_IsClicked", 1);
			else
				invButton[this.selectedSlot].material.SetFloat("_IsClicked", 0);
		}
		else{
			if(b)
				hbButton[this.selectedSlot].material.SetFloat("_IsClicked", 1);
			else
				hbButton[this.selectedSlot].material.SetFloat("_IsClicked", 0);
		}
	}
}
