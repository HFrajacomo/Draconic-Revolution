using UnityEngine;

public static class CharacterCreationData {
	// Basic
	private static string name;
	private static Race race;
	private static Religion religion;
	private static Alignment alignment;

	// Model Appearance
	private static bool isMale;
	private static ushort clothes;
	private static ushort legs;
	private static ushort hats;
	private static ushort boots;
	private static byte skinPreset;
	private static float skinColor;

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

	// Metadata
	private static string remainingPoints;


	public static void Reset(){
		race = Race.HUMAN;
		isMale = true;
		clothes = ushort.MaxValue;
		legs = ushort.MaxValue;
		hats = ushort.MaxValue;
		boots = ushort.MaxValue;
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
	public static Race GetRace(){return race;}
	public static void SetMale(bool flag){isMale = flag;}
	public static bool GetMale(){return isMale;}
	public static void SetSkinPreset(byte b){skinPreset = b;}
	public static byte GetSkinPreset(){return skinPreset;}
	public static void SetSkinColorLerp(float f){skinColor = f;}
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
	public static void SetPrimarySkill(SkillType s){primarySkill = s;}
	public static void SetSecondarySkill(SkillType s){secondarySkill = s;}
	public static void SetAlignment(Alignment a){alignment = a;}
	public static void SetReligion(Religion r){religion = r;}
	public static void SetRemainingPoints(string p){remainingPoints = p;}

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
	public static SkillType GetPrimarySkill(){return primarySkill;}
	public static SkillType GetSecondarySkill(){return secondarySkill;}
	public static Alignment GetAlignment(){return alignment;}
	public static Religion GetReligion(){return religion;}
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
			default:
				return;
		}
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
}