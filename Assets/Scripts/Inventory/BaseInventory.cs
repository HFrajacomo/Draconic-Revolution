using System;
using System.Collections;
using System.Collections.Generic;

public abstract class BaseInventory {
	public bool itemInventory;
	public string inventoryType;
	public ushort amountOfSlots;
	public int columnCount = 3;

	public string GetInventoryType(){return this.inventoryType;}
	public virtual byte AddStack(ItemStack its, List<InventoryTransaction> spots){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain AddStack(ItemStack, List<InventoryTransaction>)");}
	public virtual EntityAction AddStack(EntityAction ea, ushort slot){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain AddStack(EntityAction, byte)");}
	public virtual void ForceAddStack(ItemStack its, ushort slot){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain ForceAddStack(ItemStack, ushort)");}
	public virtual EntityAction ForceAddStack(EntityAction ea, ushort slot){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain ForceAddStack(EntityAction, ushort)");}
	public virtual void SetNull(ushort slot){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain SetNull(ushort)");}
	public virtual byte GetID(){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain GetID()");}
	public virtual ushort GetLimit(){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain GetLimit()");}

	#nullable enable
	public virtual ItemStack? GetSlot(ushort slot){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain GetSlot()");}
	public virtual EntityAction? GetPos(ushort slot){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain GetPos()");}
	public virtual ItemStack? Transfer(ItemStack its, ushort slot){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain Transfer(ItemStack, ushort)");}
	#nullable disable

	public virtual void FindLastEmptySlot(){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain FindLastEmptySlot()");}
	public virtual bool GetMainInventory(){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain GetMainInventory()");}
	public virtual bool HasInventoryIcons(){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain HasInventoryIcons()");}
	public virtual bool GetBulkMovedTo(){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain GetBulkMovedTo()");}
	public virtual string GetIconName(int id){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain GetIconName(int)");}
	public virtual List<InventoryTransaction> CanFit(ItemStack its){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain CanFit(ItemStack)");}
	public virtual short GetLastEmptySlot(){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain GetLastEmptySlot()");}
	public virtual void SetLastEmptySlot(short a){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain SetLastEmptySlot(short)");}
	public virtual bool IsInGlobalWhitelist(ItemStack its){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain IsInGlobalWhitelist(ItemStack)");}
	public virtual bool IsInLocalWhitelist(ItemStack its, ushort slot){throw new InventoryActionNotImplementedException($"[BaseInventory] {this.GetType()} does not contain IsInLocalWhitelist(ItemStack, ushort)");}
}