public static class CharacterCreationData {
	private static Race race;
	private static bool isMale;
	private static ushort clothes;
	private static ushort legs;
	private static ushort hats;
	private static ushort boots;

	public static void SetRace(Race r){race = r;}
	public static Race GetRace(){return race;}
	public static void SetMale(bool flag){isMale = flag;}
	public static bool GetMale(){return isMale;}
	
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
}