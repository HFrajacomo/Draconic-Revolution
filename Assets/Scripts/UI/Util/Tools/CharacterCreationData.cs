public static class CharacterCreationData {
	private static Race race;
	private static bool isMale;
	private static ushort clothes;
	private static ushort legs;
	private static ushort hats;
	private static ushort boots;

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

	public static void SetRace(Race r){race = r;}
	public static Race GetRace(){return race;}
	public static void SetMale(bool flag){isMale = flag;}
	public static bool GetMale(){return isMale;}

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
}