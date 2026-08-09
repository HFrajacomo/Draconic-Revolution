using System;
using System.Collections;
using System.Collections.Generic;

public abstract class ClickableSlot {
	protected bool isItemStack;
	public bool IsItemStack(){return this.isItemStack;}
	public abstract ushort GetID();
}