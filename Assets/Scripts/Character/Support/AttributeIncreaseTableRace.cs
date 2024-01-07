using System.Collections.Generic;

public static class AttributeIncreaseTableRace{
	private static Dictionary<Race, Dictionary<AttributeName, sbyte>> dict;

	static AttributeIncreaseTableRace(){
		dict = new Dictionary<Race, Dictionary<AttributeName, sbyte>>();

		SetHuman();
		SetElf();
		SetDwarf();
		SetOrc();
		SetHalfling();
		SetDragonling();
		SetUndead();
	}

	public static Dictionary<AttributeName, sbyte> GetTenDict(Race r){return dict[r];}
	public static sbyte GetAttributeIncrease(Race r, AttributeName n){
		if(!dict[r].ContainsKey(n))
			return 0;

		return dict[r][n];
	}

	private static void SetHuman(){
		dict.Add(Race.HUMAN, new Dictionary<AttributeName, sbyte>());
		dict[Race.HUMAN].Add(AttributeName.VITALITY, 2);
		dict[Race.HUMAN].Add(AttributeName.FIRE_RESISTANCE, 1);
		dict[Race.HUMAN].Add(AttributeName.ICE_RESISTANCE, 1);
		dict[Race.HUMAN].Add(AttributeName.SPEED, 10);
	}

	private static void SetElf(){
		dict.Add(Race.ELF, new Dictionary<AttributeName, sbyte>());
		dict[Race.ELF].Add(AttributeName.STRENGTH, -2);
		dict[Race.ELF].Add(AttributeName.PRECISION, 3);
		dict[Race.ELF].Add(AttributeName.CHARISMA, 3);	
		dict[Race.ELF].Add(AttributeName.SPEED, 10);
	}

	private static void SetDwarf(){
		dict.Add(Race.DWARF, new Dictionary<AttributeName, sbyte>());
		dict[Race.DWARF].Add(AttributeName.STRENGTH, 3);
		dict[Race.DWARF].Add(AttributeName.VITALITY, 3);
		dict[Race.DWARF].Add(AttributeName.CHARISMA, -2);
		dict[Race.DWARF].Add(AttributeName.SPEED, 10);	
	}

	private static void SetOrc(){
		dict.Add(Race.ORC, new Dictionary<AttributeName, sbyte>());
		dict[Race.ORC].Add(AttributeName.STRENGTH, 6);
		dict[Race.ORC].Add(AttributeName.CHARISMA, -2);
		dict[Race.ORC].Add(AttributeName.SPEED, 10);	
	}

	private static void SetHalfling(){
		dict.Add(Race.HALFLING, new Dictionary<AttributeName, sbyte>());
		dict[Race.HALFLING].Add(AttributeName.EVASION, 3);
		dict[Race.HALFLING].Add(AttributeName.SPEED, 11);	
		dict[Race.HALFLING].Add(AttributeName.VITALITY, -1);	
	}

	private static void SetDragonling(){
		dict.Add(Race.DRAGONLING, new Dictionary<AttributeName, sbyte>());
		dict[Race.DRAGONLING].Add(AttributeName.MAGIC, 2);
		dict[Race.DRAGONLING].Add(AttributeName.FIRE_RESISTANCE, 1);	
		dict[Race.DRAGONLING].Add(AttributeName.ICE_RESISTANCE, 1);	
		dict[Race.DRAGONLING].Add(AttributeName.LIGHTNING_RESISTANCE, 1);	
		dict[Race.DRAGONLING].Add(AttributeName.EVASION, -1);
		dict[Race.DRAGONLING].Add(AttributeName.SPEED, 10);	
	}

	private static void SetUndead(){
		dict.Add(Race.UNDEAD, new Dictionary<AttributeName, sbyte>());
		dict[Race.UNDEAD].Add(AttributeName.VITALITY, -10);
		dict[Race.UNDEAD].Add(AttributeName.POISON_RESISTANCE, 100);	
		dict[Race.UNDEAD].Add(AttributeName.CURSE_RESISTANCE, 100);	
		dict[Race.UNDEAD].Add(AttributeName.CHARISMA, -10);	
		dict[Race.UNDEAD].Add(AttributeName.STRENGTH, -5);
		dict[Race.UNDEAD].Add(AttributeName.SPEED, 10);
	}
}