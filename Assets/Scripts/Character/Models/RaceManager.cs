using UnityEngine;

public static class RaceManager{
	public static RaceSettings human;
	public static RaceSettings elf;
	public static RaceSettings dwarf;
	public static RaceSettings orc;
	public static RaceSettings halfling;
	public static RaceSettings dragonling;
	public static RaceSettings undead;

	static RaceManager(){
		SetHuman();
		SetElf();
		SetDwarf();
		SetOrc();
		SetHalfling();
		SetDragonling();
		SetUndead();
	}

	private static void SetHuman(){human = new RaceSettings(new Vector3(1f,1f,1f), new Color(1f,1f,1f), new Color(0f,0f,0f));}
	private static void SetElf(){elf = new RaceSettings(new Vector3(1f,1f,1f), new Color(1f,1f,1f), new Color(0f,0f,0f));}
	private static void SetDwarf(){dwarf = new RaceSettings(new Vector3(1f,1f,1f), new Color(1f,1f,1f), new Color(0f,0f,0f));}
	private static void SetOrc(){orc = new RaceSettings(new Vector3(1f,1f,1f), new Color(1f,1f,1f), new Color(0f,0f,0f));}
	private static void SetHalfling(){halfling = new RaceSettings(new Vector3(1f,1f,1f), new Color(1f,1f,1f), new Color(0f,0f,0f));}
	private static void SetDragonling(){dragonling = new RaceSettings(new Vector3(1f,1f,1f), new Color(1f,1f,1f), new Color(0f,0f,0f));}
	private static void SetUndead(){undead = new RaceSettings(new Vector3(1f,1f,1f), new Color(1f,1f,1f), new Color(0f,0f,0f));}

	public static RaceSettings GetHuman(){return human;}
	public static RaceSettings GetElf(){return elf;}
	public static RaceSettings GetDwarf(){return dwarf;}
	public static RaceSettings GetOrc(){return orc;}
	public static RaceSettings GetHalfling(){return halfling;}
	public static RaceSettings GetDragonling(){return dragonling;}
	public static RaceSettings GetUndead(){return undead;}

	public static RaceSettings GetSettings(Race r){
		switch(r){
			case Race.HUMAN:
				return human;
			case Race.ELF:
				return elf;
			case Race.DWARF:
				return dwarf;
			case Race.ORC:
				return orc;
			case Race.HALFLING:
				return halfling;
			case Race.DRAGONLING:
				return dragonling;
			case Race.UNDEAD:
				return undead;
			default:
				return human;
		}
	}
}