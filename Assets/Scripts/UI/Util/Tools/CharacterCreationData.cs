using UnityEngine;

public static class CharacterCreationData {
	// CharacterSheet
	private static CharacterSheet charSheet;

	// Basic
	private static string name;
	private static Race? race;
	private static Religion? religion;
	private static Alignment? alignment;

	// Model Appearance
	private static bool isMale;
	private static ushort clothes;
	private static ushort legs;
	private static ushort hats;
	private static ushort boots;
	private static ushort face;
	private static byte skinPreset;
	private static float skinColor;
	private static Color skin;

	// Colors
	private static Color clothesColor1;
	private static Color clothesColor2;
	private static Color clothesColor3;
	private static Color legsColor1;
	private static Color legsColor2;
	private static Color legsColor3;
	private static Color hatsColor1;
	private static Color hatsColor2;
	private static Color hatsColor3;
	private static Color bootsColor1;
	private static Color bootsColor2;
	private static Color bootsColor3;
	private static Color faceColor1;
	private static Color faceColor2;
	private static Color faceColor3;

	// General Attributes
	private static short[] strength = new short[]{0,0,0};
	private static short[] precision = new short[]{0,0,0};
	private static short[] vitality = new short[]{0,0,0};
	private static short[] evasion = new short[]{0,0,0};
	private static short[] magic = new short[]{0,0,0};
	private static short[] charisma = new short[]{0,0,0};
	private static short[] fireRes = new short[]{0,0,0};
	private static short[] iceRes = new short[]{0,0,0};
	private static short[] lightningRes = new short[]{0,0,0};
	private static short[] poisonRes = new short[]{0,0,0};
	private static short[] curseRes = new short[]{0,0,0};
	private static short[] speed = new short[]{0,0,0};

	// Skills
	private static SkillType primarySkill;
	private static SkillType secondarySkill;
	private static readonly SkillType[] SKILLS = new SkillType[]{SkillType.ALCHEMY, SkillType.BLOODMANCY, SkillType.CRAFTING, SkillType.COMBAT,
		SkillType.CONSTRUCTION, SkillType.COOKING, SkillType.ENCHANTING, SkillType.FARMING, SkillType.FISHING, SkillType.LEADERSHIP, SkillType.MINING,
		SkillType.MOUNTING, SkillType.MUSICALITY, SkillType.NATURALISM, SkillType.SMITHING, SkillType.SORCERY, SkillType.THIEVERY, SkillType.TECHNOLOGY,
		SkillType.THAUMATURGY, SkillType.TRANSMUTING, SkillType.WITCHCRAFT};

	// Metadata
	private static string remainingPoints;
	private static readonly Item NULL_ITEM = new Null_Item();


	public static void Reset(){
		name = null;
		religion = null;
		alignment = null;
		race = null;
		isMale = true;
		clothes = ushort.MaxValue;
		legs = ushort.MaxValue;
		hats = ushort.MaxValue;
		boots = ushort.MaxValue;
		remainingPoints = "10/10";
	}

	public static void ResetAttributes(){
		strength = new short[]{0,0,0};
		precision = new short[]{0,0,0};
		vitality = new short[]{0,0,0};
		evasion = new short[]{0,0,0};
		magic = new short[]{0,0,0};
		charisma = new short[]{0,0,0};
		fireRes = new short[]{0,0,0};
		iceRes = new short[]{0,0,0};
		lightningRes = new short[]{0,0,0};
		poisonRes = new short[]{0,0,0};
		curseRes = new short[]{0,0,0};
		speed = new short[]{0,0,0};
	}

	public static void SetName(string n){name = n;}
	public static string GetName(){return name;}
	public static void SetRace(Race r){race = r;}
	public static Race GetRace(){return (Race)race;}
	public static void SetMale(bool flag){isMale = flag;}
	public static bool GetMale(){return isMale;}
	public static void SetSkinPreset(byte b){skinPreset = b;}
	public static byte GetSkinPreset(){return skinPreset;}
	public static void SetSkinColorLerp(float f){skinColor = f;}
	public static Color GetSkin(){return skin;}
	public static void SetSkin(Color c){skin = c;}
	public static float GetSkinColorLerp(){return skinColor;}
	public static void SetClothesColor1(Color c){clothesColor1 = c;}
	public static void SetClothesColor2(Color c){clothesColor2 = c;}
	public static void SetClothesColor3(Color c){clothesColor3 = c;}
	public static void SetLegsColor1(Color c){legsColor1 = c;}
	public static void SetLegsColor2(Color c){legsColor2 = c;}
	public static void SetLegsColor3(Color c){legsColor3 = c;}
	public static void SetBootsColor1(Color c){bootsColor1 = c;}
	public static void SetBootsColor2(Color c){bootsColor2 = c;}
	public static void SetBootsColor3(Color c){bootsColor3 = c;}
	public static void SetHatsColor1(Color c){hatsColor1 = c;}
	public static void SetHatsColor2(Color c){hatsColor2 = c;}
	public static void SetHatsColor3(Color c){hatsColor3 = c;}
	public static void SetFaceColor1(Color c){faceColor1 = c;}
	public static void SetFaceColor2(Color c){faceColor2 = c;}
	public static void SetFaceColor3(Color c){faceColor3 = c;}
	public static void SetPrimarySkill(SkillType s){primarySkill = s;}
	public static void SetSecondarySkill(SkillType s){secondarySkill = s;}
	public static void SetAlignment(Alignment a){alignment = a;}
	public static void SetReligion(Religion r){religion = r;}
	public static void SetRemainingPoints(string p){remainingPoints = p;}

	public static CharacterSheet GetCharacterSheet(){return charSheet;}
	public static Color GetClothesColor1(){return clothesColor1;}
	public static Color GetClothesColor2(){return clothesColor2;}
	public static Color GetClothesColor3(){return clothesColor3;}
	public static Color GetLegsColor1(){return legsColor1;}
	public static Color GetLegsColor2(){return legsColor2;}
	public static Color GetLegsColor3(){return legsColor3;}
	public static Color GetBootsColor1(){return bootsColor1;}
	public static Color GetBootsColor2(){return bootsColor2;}
	public static Color GetBootsColor3(){return bootsColor3;}
	public static Color GetHatsColor1(){return hatsColor1;}
	public static Color GetHatsColor2(){return hatsColor2;}
	public static Color GetHatsColor3(){return hatsColor3;}
	public static Color GetFaceColor1(){return faceColor1;}
	public static Color GetFaceColor2(){return faceColor2;}
	public static Color GetFaceColor3(){return faceColor3;}
	public static SkillType GetPrimarySkill(){return primarySkill;}
	public static SkillType GetSecondarySkill(){return secondarySkill;}
	public static Alignment GetAlignment(){return (Alignment)alignment;}
	public static Religion GetReligion(){return (Religion)religion;}
	public static string GetRemainingPoints(){
		if(remainingPoints == null)
			return "10/10";
		return remainingPoints;
	}

	public static short GetAttribute(AttributeName at){
		switch(at){
			case AttributeName.STRENGTH:
				return Sum(strength);
			case AttributeName.PRECISION:
				return Sum(precision);
			case AttributeName.VITALITY:
				return Sum(vitality);
			case AttributeName.EVASION:
				return Sum(evasion);
			case AttributeName.MAGIC:
				return Sum(magic);
			case AttributeName.CHARISMA:
				return Sum(charisma);
			case AttributeName.FIRE_RESISTANCE:
				return Sum(fireRes);
			case AttributeName.ICE_RESISTANCE:
				return Sum(iceRes);
			case AttributeName.LIGHTNING_RESISTANCE:
				return Sum(lightningRes);
			case AttributeName.POISON_RESISTANCE:
				return Sum(poisonRes);
			case AttributeName.CURSE_RESISTANCE:
				return Sum(curseRes);
			case AttributeName.SPEED:
				return Sum(speed);
			default:
				return 0;
		}
	}

	public static short GetAttributeNoBonus(AttributeName at){
		switch(at){
			case AttributeName.STRENGTH:
				return SumNoBonus(strength);
			case AttributeName.PRECISION:
				return SumNoBonus(precision);
			case AttributeName.VITALITY:
				return SumNoBonus(vitality);
			case AttributeName.EVASION:
				return SumNoBonus(evasion);
			case AttributeName.MAGIC:
				return SumNoBonus(magic);
			case AttributeName.CHARISMA:
				return SumNoBonus(charisma);
			case AttributeName.FIRE_RESISTANCE:
				return SumNoBonus(fireRes);
			case AttributeName.ICE_RESISTANCE:
				return SumNoBonus(iceRes);
			case AttributeName.LIGHTNING_RESISTANCE:
				return SumNoBonus(lightningRes);
			case AttributeName.POISON_RESISTANCE:
				return SumNoBonus(poisonRes);
			case AttributeName.CURSE_RESISTANCE:
				return SumNoBonus(curseRes);
			case AttributeName.SPEED:
				return SumNoBonus(speed);
			default:
				return 0;
		}
	}

	public static void SetAttribute(AttributeName at, int i, short val){
		switch(at){
			case AttributeName.STRENGTH:
				strength[i] = val;
				return;
			case AttributeName.PRECISION:
				precision[i] = val;
				return;
			case AttributeName.VITALITY:
				vitality[i] = val;
				return;
			case AttributeName.EVASION:
				evasion[i] = val;
				return;
			case AttributeName.MAGIC:
				magic[i] = val;
				return;
			case AttributeName.CHARISMA:
				charisma[i] = val;
				return;
			case AttributeName.FIRE_RESISTANCE:
				fireRes[i] = val;
				return;
			case AttributeName.ICE_RESISTANCE:
				iceRes[i] = val;
				return;
			case AttributeName.LIGHTNING_RESISTANCE:
				lightningRes[i] = val;
				return;
			case AttributeName.POISON_RESISTANCE:
				poisonRes[i] = val;
				return;
			case AttributeName.CURSE_RESISTANCE:
				curseRes[i] = val;
				return;
			case AttributeName.SPEED:
				speed[i] = val;
				return;
			default:
				return;
		}
	}

	public static void AddAttribute(AttributeName at, int i, short val){
		switch(at){
			case AttributeName.STRENGTH:
				strength[i] += val;
				return;
			case AttributeName.PRECISION:
				precision[i] += val;
				return;
			case AttributeName.VITALITY:
				vitality[i] += val;
				return;
			case AttributeName.EVASION:
				evasion[i] += val;
				return;
			case AttributeName.MAGIC:
				magic[i] += val;
				return;
			case AttributeName.CHARISMA:
				charisma[i] += val;
				return;
			case AttributeName.FIRE_RESISTANCE:
				fireRes[i] += val;
				return;
			case AttributeName.ICE_RESISTANCE:
				iceRes[i] += val;
				return;
			case AttributeName.LIGHTNING_RESISTANCE:
				lightningRes[i] += val;
				return;
			case AttributeName.POISON_RESISTANCE:
				poisonRes[i] += val;
				return;
			case AttributeName.CURSE_RESISTANCE:
				curseRes[i] += val;
				return;
			case AttributeName.SPEED:
				speed[i] += val;
				return;
			default:
				return;
		}
	}

	
	public static void SetBodyPart(ModelType type, ushort code){
		switch(type){
			case ModelType.CLOTHES:
				clothes = code;
				return;
			case ModelType.LEGS:
				legs = code;
				return;
			case ModelType.FOOTGEAR:
				boots = code;
				return;
			case ModelType.HEADGEAR:
				hats = code;
				return;
			case ModelType.FACE:
				face = code;
				return;
			default:
				return;
		}
	}

	public static ushort GetBodyPart(ModelType type){
		switch(type){
			case ModelType.CLOTHES:
				return clothes;
			case ModelType.LEGS:
				return legs;
			case ModelType.FOOTGEAR:
				return boots;
			case ModelType.HEADGEAR:
				return hats;
			case ModelType.FACE:
				return face;
			default:
				return clothes;
		}
	}

	public static void CreateCharacterSheet(){
		CharacterSheet sheet = new CharacterSheet();
		ClothingInfo clothesInfo, legsInfo, bootsInfo, hatsInfo, faceInfo;

		clothesInfo = new ClothingInfo(clothes, clothesColor1, clothesColor2, clothesColor3, isMale);
		legsInfo = new ClothingInfo(legs, legsColor1, legsColor2, legsColor3, isMale);
		bootsInfo = new ClothingInfo(boots, bootsColor1, bootsColor2, bootsColor3, isMale);
		hatsInfo = new ClothingInfo(hats, hatsColor1, hatsColor2, hatsColor3, isMale);
		faceInfo = new ClothingInfo(face, faceColor1, faceColor2, faceColor3, isMale);

		sheet.SetName(name);
		sheet.SetReligion((Religion)religion);
		sheet.SetAlignment((Alignment)alignment);
		sheet.SetRace((Race)race);
		sheet.SetCronology(0);
		sheet.SetGender(isMale);
		sheet.SetSpecialEffectHandler(new SpecialEffectHandler());
		sheet.SetCharacterAppearance(new CharacterAppearance((Race)race, skin, hatsInfo, clothesInfo, legsInfo, bootsInfo, faceInfo));
		sheet.SetHealth(new DepletableAttribute(SecondaryAttributeCalculator.CalculateHealth(Sum(vitality))));
		sheet.SetPoise(new DepletableAttribute(SecondaryAttributeCalculator.CalculatePoise(Sum(vitality))));
		sheet.SetMana(new DepletableAttribute(SecondaryAttributeCalculator.CalculateMana(Sum(magic), GetStartingLevel(SkillType.SORCERY))));
		sheet.SetPower(new DepletableAttribute(SecondaryAttributeCalculator.CalculatePower(GetStartingLevel(SkillType.WITCHCRAFT))));
		sheet.SetSanity(new DepletableAttribute(SecondaryAttributeCalculator.CalculateSanity(Sum(magic), 13)));
		sheet.SetProtection(new DepletableAttribute(0));
		sheet.SetEquipmentWeight(new DepletableAttribute(0, SecondaryAttributeCalculator.CalculateEquipmentWeight(Sum(vitality))));

		sheet.SetStrength(new Attribute(Sum(strength)));
		sheet.SetPrecision(new Attribute(Sum(precision)));
		sheet.SetVitality(new Attribute(Sum(vitality)));
		sheet.SetEvasion(new Attribute(Sum(evasion)));
		sheet.SetMagic(new Attribute(Sum(magic)));
		sheet.SetCharisma(new Attribute(Sum(charisma)));

		sheet.SetFireResistance(new Attribute(Sum(fireRes)));
		sheet.SetColdResistance(new Attribute(Sum(iceRes)));
		sheet.SetLightningResistance(new Attribute(Sum(lightningRes)));
		sheet.SetPoisonResistance(new Attribute(Sum(poisonRes)));
		sheet.SetCurseResistance(new Attribute(Sum(curseRes)));
		sheet.SetSpeed(new Attribute(Sum(speed)));
		sheet.SetPhysicalDefense(0);
		sheet.SetMagicalDefense(0);
		sheet.SetDamageReductionMultiplier(1f);
		sheet.SetHasBlood(race != Race.UNDEAD);
		sheet.SetIsWeaponDrawn(false);
		sheet.SetIsImortal(false);
		sheet.SetMainSkill(primarySkill);
		sheet.SetSecondarySkill(secondarySkill);

		// Skills
		foreach(SkillType type in SKILLS){
			if(type == primarySkill)
				sheet.SetSkill(type, new SkillExp(8, SkillExp.GetLevelEXP(8)));
			else if(type == secondarySkill)
				sheet.SetSkill(type, new SkillExp(5, SkillExp.GetLevelEXP(5)));
			else
				sheet.SetSkill(type, new SkillExp(1, SkillExp.GetLevelEXP(1)));
		}

		// Equipment
		sheet.SetRightHand(NULL_ITEM);
		sheet.SetLeftHand(NULL_ITEM);
		sheet.SetHelmet(NULL_ITEM);
		sheet.SetArmor(NULL_ITEM);
		sheet.SetLegs(NULL_ITEM);
		sheet.SetBoots(NULL_ITEM);
		sheet.SetRing1(NULL_ITEM);
		sheet.SetRing2(NULL_ITEM);
		sheet.SetRing3(NULL_ITEM);
		sheet.SetRing4(NULL_ITEM);
		sheet.SetAmulet(NULL_ITEM);
		sheet.SetCape(NULL_ITEM);

		charSheet = sheet;
	}

	private static short Sum(short[] a){
		int sum = 0;

		for(int i=0; i < a.Length; i++)
			sum += a[i];

		return (short)sum;
	}

	private static short SumNoBonus(short[] a){
		int sum = 0;

		sum = a[0] + a[1];

		return (short)sum;
	}

	private static byte GetStartingLevel(SkillType skill){
		if(primarySkill == skill)
			return 8;
		else if(secondarySkill == skill)
			return 5;
		return 1;
	}
}