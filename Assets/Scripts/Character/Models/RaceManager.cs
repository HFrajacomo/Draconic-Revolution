using UnityEngine;

public static class RaceManager{
	public static RaceSettings human;
	public static RaceSettings elf;
	public static RaceSettings dwarf;
	public static RaceSettings orc;
	public static RaceSettings halfling;
	public static RaceSettings dragonling;
	public static RaceSettings undead;

	private static readonly Gradient COMMON = new Gradient(new Color(0.24f, 0.11f, 0.09f), new Color(0.93f, 0.71f, 0.60f));
	private static readonly Gradient MAGICAL = new Gradient(new Color(0.65f, 0.87f, 0.85f), new Color(0.90f, 1.00f, 0.97f));


	static RaceManager(){
		SetHuman();
		SetElf();
		SetDwarf();
		SetOrc();
		SetHalfling();
		SetDragonling();
		SetUndead();
	}

	private static void SetHuman(){human = new RaceSettings(Race.HUMAN, new Vector3(1f,1f,1f), 
		COMMON,
		new Gradient(new Color(0.60f, 0.43f, 0.25f), new Color(0.98f, 0.91f, 0.81f)),
		new Gradient(new Color(0.19f, 0.13f, 0.13f), new Color(0.50f, 0.37f, 0.30f))
		);}

	private static void SetElf(){elf = new RaceSettings(Race.ELF, new Vector3(1f,1f,1f),
		COMMON, 
		new Gradient(new Color(0.73f, 0.76f, 0.51f), new Color(0.91f, 0.93f, 0.79f)),
		MAGICAL
		);}

	private static void SetDwarf(){dwarf = new RaceSettings(Race.DWARF, new Vector3(1f,.75f,1f),
		COMMON,
		MAGICAL,
		new Gradient(new Color(0.64f, 0.37f, 0.34f), new Color(0.93f, 0.78f, 0.78f))
		);}

	private static void SetOrc(){orc = new RaceSettings(Race.ORC, new Vector3(1f,1f,1f),
		new Gradient(new Color(0.38f, 0.44f, 0.35f), new Color(0.70f, 0.75f, 0.56f)),
		new Gradient(new Color(0.35f, 0.38f, 0.44f), new Color(0.56f, 0.66f, 0.75f)),
		COMMON
		);}

	private static void SetHalfling(){halfling = new RaceSettings(Race.HALFLING, new Vector3(.88f,.67f,.88f),
		COMMON,
		new Gradient(new Color(0.60f, 0.43f, 0.25f), new Color(0.98f, 0.91f, 0.81f)),
		new Gradient(new Color(.49f,.39f,.32f), new Color(1f,.93f,.89f))
		);}

	private static void SetDragonling(){dragonling = new RaceSettings(Race.DRAGONLING, new Vector3(1f,1f,1f),
		new Gradient(new Color(0.43f, 0.15f, 0.15f), new Color(0.98f, 0.35f, 0.35f)),
		new Gradient(new Color(0.34f, 0.56f, 0.19f), new Color(0.55f, 0.89f, 0.53f)),
		new Gradient(new Color(0.19f, 0.33f, 0.56f), new Color(0.53f, 0.66f, 0.89f))
		);}

	private static void SetUndead(){undead = new RaceSettings(Race.UNDEAD, new Vector3(1f,1f,1f),
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