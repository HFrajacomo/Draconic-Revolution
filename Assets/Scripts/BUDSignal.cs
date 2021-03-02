public struct BUDSignal{
	public BUDCode type;
	public int x;
	public int y;
	public int z;
	public int budX;
	public int budY;
	public int budZ;
	public int facing;

	public BUDSignal(BUDCode t, int x, int y, int z, int bX, int bY, int bZ, int facing=-1){
		this.type = t;
		this.x = x;
		this.y = y;
		this.z = z;
		this.budX = bX;
		this.budY = bY;
		this.budZ = bZ;
		this.facing = facing;
	}

    public override string ToString(){
        return "(" + x + ", " + y + ", " + z + ")";
    }

	public bool Equals(BUDSignal b){
		if(this.x == b.x && this.y == b.y && this.z == b.z)
			return true;
		return false;
	}
}

public enum BUDCode{
	SETSTATE,
	BREAK,
	PLACE,
	LOAD,
	DECAY,
	CHANGE
}