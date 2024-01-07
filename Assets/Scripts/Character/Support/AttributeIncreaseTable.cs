using System.Collections.Generic;

public static class AttributeIncreaseTable{
	private static Dictionary<SkillType, Dictionary<AttributeName, sbyte>> dict;

	static AttributeIncreaseTable(){
		dict = new Dictionary<SkillType, Dictionary<AttributeName, sbyte>>();

		SetAlchemy();
		SetCrafting();
		SetFarming();
		SetWitchcraft();
		SetConstruction();
		SetCombat();
		SetCooking();
		SetEnchanting();
		SetSorcery();
		SetSmithing();
		SetThievery();
		SetLeadership();
		SetMining();
		SetMounting();
		SetMusicality();
		SetNaturalism();
		SetFishing();
		SetTechnology();
		SetTransmuting();
		SetThaumaturgy();
		SetBloodmancy();
	}

	public static Dictionary<AttributeName, sbyte> GetTenDict(SkillType t){return dict[t];}
	public static sbyte GetAttributeIncrease(SkillType t, AttributeName n){
		if(!dict[t].ContainsKey(n))
			return 0;

		return dict[t][n];
	}

	private static void SetAlchemy(){
		dict.Add(SkillType.ALCHEMY, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.ALCHEMY].Add(AttributeName.PRECISION, 2);
		dict[SkillType.ALCHEMY].Add(AttributeName.MAGIC, 1);
		dict[SkillType.ALCHEMY].Add(AttributeName.POISON_RESISTANCE, 2);
		dict[SkillType.ALCHEMY].Add(AttributeName.FIRE_RESISTANCE, 1);
		dict[SkillType.ALCHEMY].Add(AttributeName.CURSE_RESISTANCE, 1);
	}

	private static void SetCrafting(){
		dict.Add(SkillType.CRAFTING, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.CRAFTING].Add(AttributeName.PRECISION, 2);
		dict[SkillType.CRAFTING].Add(AttributeName.VITALITY, 1);
		dict[SkillType.CRAFTING].Add(AttributeName.STRENGTH, 2);
		dict[SkillType.CRAFTING].Add(AttributeName.CHARISMA, 2);
	}

	private static void SetFarming(){
		dict.Add(SkillType.FARMING, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.FARMING].Add(AttributeName.EVASION, 1);
		dict[SkillType.FARMING].Add(AttributeName.VITALITY, 1);
		dict[SkillType.FARMING].Add(AttributeName.STRENGTH, 1);
		dict[SkillType.FARMING].Add(AttributeName.ICE_RESISTANCE, 1);
		dict[SkillType.FARMING].Add(AttributeName.POISON_RESISTANCE, 2);
		dict[SkillType.FARMING].Add(AttributeName.SPEED, 1);
	}

	private static void SetWitchcraft(){
		dict.Add(SkillType.WITCHCRAFT, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.WITCHCRAFT].Add(AttributeName.MAGIC, 3);
		dict[SkillType.WITCHCRAFT].Add(AttributeName.PRECISION, 2);
		dict[SkillType.WITCHCRAFT].Add(AttributeName.CURSE_RESISTANCE, 2);
	}

	private static void SetConstruction(){
		dict.Add(SkillType.CONSTRUCTION, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.CONSTRUCTION].Add(AttributeName.VITALITY, 2);
		dict[SkillType.CONSTRUCTION].Add(AttributeName.STRENGTH, 2);
		dict[SkillType.CONSTRUCTION].Add(AttributeName.PRECISION, 1);
		dict[SkillType.CONSTRUCTION].Add(AttributeName.ICE_RESISTANCE, 1);
		dict[SkillType.CONSTRUCTION].Add(AttributeName.LIGHTNING_RESISTANCE, 1);
	}

	private static void SetCombat(){
		dict.Add(SkillType.COMBAT, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.COMBAT].Add(AttributeName.EVASION, 1);
		dict[SkillType.COMBAT].Add(AttributeName.VITALITY, 2);
		dict[SkillType.COMBAT].Add(AttributeName.STRENGTH, 2);
		dict[SkillType.COMBAT].Add(AttributeName.CHARISMA, 2);
	}

	private static void SetCooking(){
		dict.Add(SkillType.COOKING, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.COOKING].Add(AttributeName.PRECISION, 1);
		dict[SkillType.COOKING].Add(AttributeName.VITALITY, 2);
		dict[SkillType.COOKING].Add(AttributeName.FIRE_RESISTANCE, 1);
		dict[SkillType.COOKING].Add(AttributeName.POISON_RESISTANCE, 1);
		dict[SkillType.COOKING].Add(AttributeName.CHARISMA, 2);
	}

	private static void SetEnchanting(){
		dict.Add(SkillType.ENCHANTING, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.ENCHANTING].Add(AttributeName.MAGIC, 2);
		dict[SkillType.ENCHANTING].Add(AttributeName.CURSE_RESISTANCE, 2);
		dict[SkillType.ENCHANTING].Add(AttributeName.LIGHTNING_RESISTANCE, 2);
		dict[SkillType.ENCHANTING].Add(AttributeName.CHARISMA, 1);
	}

	private static void SetSorcery(){
		dict.Add(SkillType.SORCERY, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.SORCERY].Add(AttributeName.MAGIC, 2);
		dict[SkillType.SORCERY].Add(AttributeName.EVASION, 1);
		dict[SkillType.SORCERY].Add(AttributeName.CURSE_RESISTANCE, 1);
		dict[SkillType.SORCERY].Add(AttributeName.LIGHTNING_RESISTANCE, 1);
		dict[SkillType.SORCERY].Add(AttributeName.FIRE_RESISTANCE, 1);
		dict[SkillType.SORCERY].Add(AttributeName.ICE_RESISTANCE, 1);
	}

	private static void SetSmithing(){
		dict.Add(SkillType.SMITHING, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.SMITHING].Add(AttributeName.PRECISION, 1);
		dict[SkillType.SMITHING].Add(AttributeName.STRENGTH, 3);
		dict[SkillType.SMITHING].Add(AttributeName.FIRE_RESISTANCE, 2);
		dict[SkillType.SMITHING].Add(AttributeName.CHARISMA, 1);
	}

	private static void SetThievery(){
		dict.Add(SkillType.THIEVERY, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.THIEVERY].Add(AttributeName.PRECISION, 2);
		dict[SkillType.THIEVERY].Add(AttributeName.EVASION, 3);
		dict[SkillType.THIEVERY].Add(AttributeName.POISON_RESISTANCE, 1);
		dict[SkillType.THIEVERY].Add(AttributeName.SPEED, 1);
	}

	private static void SetLeadership(){
		dict.Add(SkillType.LEADERSHIP, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.LEADERSHIP].Add(AttributeName.PRECISION, 3);
		dict[SkillType.LEADERSHIP].Add(AttributeName.EVASION, 1);
		dict[SkillType.LEADERSHIP].Add(AttributeName.CHARISMA, 3);
	}

	private static void SetMining(){
		dict.Add(SkillType.MINING, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.MINING].Add(AttributeName.STRENGTH, 3);
		dict[SkillType.MINING].Add(AttributeName.PRECISION, 1);
		dict[SkillType.MINING].Add(AttributeName.EVASION, 1);
		dict[SkillType.MINING].Add(AttributeName.ICE_RESISTANCE, 2);
	}

	private static void SetMounting(){
		dict.Add(SkillType.MOUNTING, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.MOUNTING].Add(AttributeName.VITALITY, 2);
		dict[SkillType.MOUNTING].Add(AttributeName.PRECISION, 1);
		dict[SkillType.MOUNTING].Add(AttributeName.EVASION, 1);
		dict[SkillType.MOUNTING].Add(AttributeName.CHARISMA, 2);
		dict[SkillType.MOUNTING].Add(AttributeName.SPEED, 1);
	}

	private static void SetMusicality(){
		dict.Add(SkillType.MUSICALITY, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.MUSICALITY].Add(AttributeName.MAGIC, 2);
		dict[SkillType.MUSICALITY].Add(AttributeName.PRECISION, 2);
		dict[SkillType.MUSICALITY].Add(AttributeName.CHARISMA, 3);
	}

	private static void SetNaturalism(){
		dict.Add(SkillType.NATURALISM, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.NATURALISM].Add(AttributeName.MAGIC, 2);
		dict[SkillType.NATURALISM].Add(AttributeName.VITALITY, 2);
		dict[SkillType.NATURALISM].Add(AttributeName.CURSE_RESISTANCE, 2);
		dict[SkillType.NATURALISM].Add(AttributeName.POISON_RESISTANCE, 1);
	}

	private static void SetFishing(){
		dict.Add(SkillType.FISHING, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.FISHING].Add(AttributeName.STRENGTH, 1);
		dict[SkillType.FISHING].Add(AttributeName.VITALITY, 1);
		dict[SkillType.FISHING].Add(AttributeName.CHARISMA, 2);
		dict[SkillType.FISHING].Add(AttributeName.CURSE_RESISTANCE, 1);
		dict[SkillType.FISHING].Add(AttributeName.ICE_RESISTANCE, 2);
	}

	private static void SetTechnology(){
		dict.Add(SkillType.TECHNOLOGY, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.TECHNOLOGY].Add(AttributeName.PRECISION, 2);
		dict[SkillType.TECHNOLOGY].Add(AttributeName.EVASION, 2);
		dict[SkillType.TECHNOLOGY].Add(AttributeName.CHARISMA, 1);
		dict[SkillType.TECHNOLOGY].Add(AttributeName.LIGHTNING_RESISTANCE, 2);
	}

	private static void SetTransmuting(){
		dict.Add(SkillType.TRANSMUTING, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.TRANSMUTING].Add(AttributeName.MAGIC, 3);
		dict[SkillType.TRANSMUTING].Add(AttributeName.STRENGTH, 1);
		dict[SkillType.TRANSMUTING].Add(AttributeName.CHARISMA, -1);
		dict[SkillType.TRANSMUTING].Add(AttributeName.VITALITY, 1);
		dict[SkillType.TRANSMUTING].Add(AttributeName.EVASION, 2);
		dict[SkillType.TRANSMUTING].Add(AttributeName.CURSE_RESISTANCE, 1);
	}

	private static void SetThaumaturgy(){
		dict.Add(SkillType.THAUMATURGY, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.THAUMATURGY].Add(AttributeName.MAGIC, 4);
		dict[SkillType.THAUMATURGY].Add(AttributeName.PRECISION, 2);
		dict[SkillType.THAUMATURGY].Add(AttributeName.CHARISMA, -2);
		dict[SkillType.THAUMATURGY].Add(AttributeName.FIRE_RESISTANCE, 1);
		dict[SkillType.THAUMATURGY].Add(AttributeName.ICE_RESISTANCE, 1);
		dict[SkillType.THAUMATURGY].Add(AttributeName.LIGHTNING_RESISTANCE, 1);
	}

	private static void SetBloodmancy(){
		dict.Add(SkillType.BLOODMANCY, new Dictionary<AttributeName, sbyte>());
		dict[SkillType.BLOODMANCY].Add(AttributeName.MAGIC, 2);
		dict[SkillType.BLOODMANCY].Add(AttributeName.VITALITY, 2);
		dict[SkillType.BLOODMANCY].Add(AttributeName.PRECISION, 1);
		dict[SkillType.BLOODMANCY].Add(AttributeName.CHARISMA, -3);
		dict[SkillType.BLOODMANCY].Add(AttributeName.FIRE_RESISTANCE, 1);
		dict[SkillType.BLOODMANCY].Add(AttributeName.POISON_RESISTANCE, 3);
		dict[SkillType.BLOODMANCY].Add(AttributeName.STRENGTH, 1);
	}
}