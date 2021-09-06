using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;

public class InventoryUIPlayer : MonoBehaviour
{
	// Inventory data and draw info
	private Inventory inv1;
	private Inventory inv2;
	[SerializeField]
	public Image[] invButton;
	[SerializeField]
	public Text[] invText;
	[SerializeField]
	public Image[] hbButton;
	[SerializeField]
	public Text[] hbText;
	public SpriteAtlas iconAtlas;

	// Inventory Logic
	private byte selectedInventory = byte.MaxValue;
	private ushort selectedSlot = byte.MaxValue;

	// Color constants
	private readonly Color WHITE = new Color(1f,1f,1f,1f);
	private readonly Color RED = new Color(1f, 0.5f, 0.5f, 1f);

    public void OpenInventory(){
		this.inv1 = InventoryStaticMessage.playerInventory;
		this.inv2 = InventoryStaticMessage.GetInventory();

		this.DrawStacks();
    }

    // Draws the ItemStacks into the Inventory Screen
    private void DrawStacks(){
    	ItemStack its;

    	// Player Inventory
    	for(ushort i=0; i < this.inv1.GetLimit(); i++){
    		its = this.inv1.GetSlot(i);

    		if(its == null)
    			continue;

    		this.invButton[i].sprite = this.iconAtlas.GetSprite(its.GetItemIconName());

    		if(its.GetStacksize() > 1)
    			this.invText[i].text = its.GetAmount().ToString();
    	}

    	for(ushort i=0; i < this.inv2.GetLimit(); i++){
    		its = this.inv2.GetSlot(i);

    		if(its == null)
    			continue;

    		this.hbButton[i].sprite = this.iconAtlas.GetSprite(its.GetItemIconName());

    		if(its.GetStacksize() > 1)
    			this.hbText[i].text = its.GetAmount().ToString();
    	}
    }

    // Redraws a specific slot
    private void DrawSlot(byte inventoryCode, ushort slot){
    	ItemStack its;

    	if(inventoryCode == 0){
    		its = this.inv1.GetSlot(slot);

    		if(its == null){
    			this.invButton[slot].sprite = null;
    			this.invText[slot].text = "";
    		}
    		else{
	    		this.invButton[slot].sprite = this.iconAtlas.GetSprite(its.GetItemIconName());

	    		if(its.GetStacksize() > 1)
	    			this.invText[slot].text = its.GetAmount().ToString();    			
    		}
    	}
    	else{
    		its = this.inv2.GetSlot(slot);

    		if(its == null){
    			this.hbButton[slot].sprite = null;
    			this.hbText[slot].text = "";
    		}
    		else{
	    		this.hbButton[slot].sprite = this.iconAtlas.GetSprite(its.GetItemIconName());

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

    		// Selects slot
    		this.selectedInventory = inventoryCode;
    		this.selectedSlot = slot;
    		this.ToggleHighlight(true);
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
    			if(inv1.AddFromSplit(inv1.GetSlot(slot), slot, out newSlot)){
    				this.DrawSlot(inventoryCode, slot);
    				this.DrawSlot(inventoryCode, newSlot);
    			}
    		}
    		else{
    			if(inv2.AddFromSplit(inv2.GetSlot(slot), slot, out newSlot)){
    				this.DrawSlot(inventoryCode, slot);
    				this.DrawSlot(inventoryCode, newSlot);
    			}    			
    		}
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

    // Resets selection
    public void ResetSelection(){
    	this.ToggleHighlight(false);
		this.selectedSlot = byte.MaxValue;
		this.selectedInventory = byte.MaxValue;
	}

	// Toggles selection highlighting
	private void ToggleHighlight(bool b){
		if(this.selectedInventory == byte.MaxValue)
			return;

		if(this.selectedInventory == 0){
			if(b)
				invButton[this.selectedSlot].color = this.RED;
			else
				invButton[this.selectedSlot].color = this.WHITE;
		}
		else{
			if(b)
				hbButton[this.selectedSlot].color = this.RED;
			else
				hbButton[this.selectedSlot].color = this.WHITE;
		}
	}
}
