using System;
using System.Collections;
using System.Collections.Generic;

public abstract class ClickableSlot {
	protected bool isItemStack;
	public bool IsItemStack(){return this.isItemStack;}
	public abstract ushort GetID();

	public static bool IsEqual(ClickableSlot a, ClickableSlot b){
		if(a == null && b == null)
			return true;

		if(a == null || b == null)
			return false;
			
		if(a.isItemStack != b.isItemStack)
			return false;

		return a.GetID() == b.GetID();
	}
}