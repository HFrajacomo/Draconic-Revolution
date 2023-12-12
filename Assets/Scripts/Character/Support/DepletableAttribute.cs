using UnityEngine;

public class DepletableAttribute{
	private ushort current;
	private ushort maximum;
	private bool CAN_GO_BELOW_ZERO = false;

	public DepletableAttribute(ushort max){
		this.current = max;
		this.maximum = max;
	}

	public DepletableAttribute(ushort cur, ushort max){
		this.current = cur;
		this.maximum = max;	
	}

	public DepletableAttribute(ushort cur, ushort max, bool belowZero){
		this.current = cur;
		this.maximum = max;	
		this.CAN_GO_BELOW_ZERO = belowZero;
	}

	// Returns true if is Maxed
	public bool Add(ushort amount){
		this.current = (ushort)Mathf.Min(this.current + amount, this.maximum);

		return this.current == this.maximum;
	}

	// Returns true if is zeroed
	public bool Subtract(ushort amount){
		this.current = (ushort)(this.current - amount);

		return this.current <= 0 && !CAN_GO_BELOW_ZERO;
	}

	// Sets the DepletableAttribute to go below zero (especially useful for Rage mode, where your HP can go below zero)
	public void SetBelowZeroFlag(bool value){
		this.CAN_GO_BELOW_ZERO = value;
	}

	public float GetFloat(){return (float)this.current/this.maximum;}

	public ushort GetCurrentValue(){return this.current;}
	public ushort GetMaximumValue(){return this.maximum;}
	public bool GetZeroFlag(){return this.CAN_GO_BELOW_ZERO;}
}