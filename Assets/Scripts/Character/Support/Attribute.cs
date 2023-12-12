
/**
 * baseAtt: the base attribute the character has naturally
 * equipped: the additional bonus it gains from armor
 * effect: the additional bonus it gains from enhancements (e.g. music/leadership shouts, etc.)
 * multiplier: the multiplier of all the additional bonuses (e.g. like battle style perks, potions)
 */
public class Attribute{
	private short baseAtt; 
	private short equipped; // Not saved
	private short effect; // Not saved
	private float multiplier;

	public Attribute(){}
	public Attribute(short baseAtt){
		this.baseAtt = baseAtt;
	}
	public Attribute(short b, short e){
		this.baseAtt = b;
		this.equipped = e;
	}
	public Attribute(short b, float m){
		this.baseAtt = b;
		this.multiplier = m;
	}
	public Attribute(short b, short e, float m){
		this.baseAtt = b;
		this.equipped = e;
		this.multiplier = m;
	}

	public short GetBase(){return this.baseAtt;}
	public short GetEquipped(){return this.equipped;}
	public short GetEffect(){return this.effect;}
	public float GetMultiplier(){return this.multiplier;}

	public void AddBase(short add){this.baseAtt += add;}
	public void AddEquipped(short add){this.equipped += add;}
	public void AddEffect(short add){this.effect += add;}
	public void AddMultiplier(float add){this.multiplier += add;}

	public void HardSetBase(short val){this.baseAtt = val;}
	public void HardSetEquipped(short val){this.equipped = val;}
	public void HardSetEffect(short val){this.effect = val;}
	public void HardSetMultiplier(float val){this.multiplier = val;}

	// Gets the calculates attribute
	public int GetFinal(){return (int)((this.baseAtt + this.equipped + this.effect)*multiplier);}
}