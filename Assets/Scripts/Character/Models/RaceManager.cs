using UnityEngine;

public static class RaceManager{
	public static RaceSettings human;
	public static RaceSettings elf;
	public static RaceSettings dwarf;
	public static RaceSettings orc;
	public static RaceSettings halfling;
	public static RaceSettings dragonling;
	public static RaceSettings undead;

	private static readonly Gradient COMMON = new Gradient(new Color(.21f, .18f, .16f), new Color(.92f, .85f, .82f)); 


	static RaceManager(){
		SetHuman();
		SetElf();
		SetDwarf();
		SetOrc();
		SetHalfling();
		SetDragonling();
		SetUndead();
	}

	private static void SetHuman(){human = new RaceSettings(new Vector3(1f,1f,1f), 
		COMMON, 
		new Gradient(new Color(.23f, .20f, .15f), new Color(.83f, .78f, .70f)),
		new Gradient(new Color(.09f, .07f, .06f), new Color(.35f, .33f, .32f))
		);}

	private static void SetElf(){elf = new RaceSettings(new Vector3(1f,1f,1f),
		COMMON, 
		new Gradient(new Color(.51f,.49f,.32f), new Color(.91f,.90f,.77f)),
		new Gradient(new Color(.47f,.58f,.56f), new Color(.84f,.92f,.91f))
		);}

	private static void SetDwarf(){dwarf = new RaceSettings(new Vector3(1f,.75f,1f),
		COMMON,
		new Gradient(new Color(.35f, .28f, .26f), new Color(.93f, .88f, .86f)),
		new Gradient(new Color(.49f, .66f, .64f), new Color(.80f, .90f, .90f))
		);}

	private static void SetOrc(){orc = new RaceSettings(new Vector3(1f,1f,1f),
		new Gradient(new Color(.28f, .34f, .30f), new Color(.79f, .86f, .82f)),
		new Gradient(new Color(.25f, .26f, .28f), new Color(.79f, .81f, .86f)),
		COMMON
		);}

	private static void SetHalfling(){halfling = new RaceSettings(new Vector3(.88f,.67f,.88f),
		COMMON,
		new Gradient(new Color(.51f,.49f,.32f), new Color(.91f,.90f,.77f)),
		new Gradient(new Color(.49f,.39f,.32f), new Color(1f,.93f,.89f))
		);}

	private static void SetDragonling(){dragonling = new RaceSettings(new Vector3(1f,1f,1f),
		new Gradient(new Color(.28f,.13f,.11f), new Color(.71f,.36f,.33f)),
		new Gradient(new Color(.19f,.31f,.23f), new Color(.52f,.79f,.61f)),
		new Gradient(new Color(.19f,.24f,.33f), new Color(.58f,.69f,.85f))
		);}

	private static void SetUndead(){undead = new RaceSettings(new Vector3(1f,1f,1f),
		new Gradient(new Color(.4f,.4f,.4f), new Color(1f,1f,1f)),
		new Gradient(new Color(.29f,.34f,.31f), new Color(.71f,.88f,.79f)),
		new Gradient(new Color(.27f,.25f,.23f), new Color(.72f,.66f,.6f))
		);}

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