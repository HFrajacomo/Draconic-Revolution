public static class AlignmentTools{
	
	public static bool IsEvil(Alignment a){
		return a == Alignment.CHAOTIC_EVIL || a == Alignment.NEUTRAL_EVIL || a == Alignment.LAWFUL_EVIL;
	} 

	public static bool IsNeutralVertical(Alignment a){
		return a == Alignment.NEUTRAL_EVIL || a == Alignment.TRUE_NEUTRAL || a == Alignment.NEUTRAL_GOOD;
	} 

	public static bool IsGood(Alignment a){
		return a == Alignment.LAWFUL_GOOD || a == Alignment.NEUTRAL_GOOD || a == Alignment.CHAOTIC_GOOD;
	} 

	public static bool IsLawful(Alignment a){
		return a == Alignment.LAWFUL_GOOD || a == Alignment.LAWFUL_NEUTRAL || a == Alignment.LAWFUL_EVIL;
	} 

	public static bool IsChaotic(Alignment a){
		return a == Alignment.CHAOTIC_GOOD || a == Alignment.CHAOTIC_NEUTRAL || a == Alignment.CHAOTIC_EVIL;
	}

	public static bool IsNeutralHorizontal(Alignment a){
		return a == Alignment.LAWFUL_NEUTRAL || a == Alignment.TRUE_NEUTRAL || a == Alignment.CHAOTIC_NEUTRAL;
	}
}