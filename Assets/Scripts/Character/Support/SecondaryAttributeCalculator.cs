public static class SecondaryAttributeCalculator{

	public static ushort CalculateHealth(short vitality){
		if(vitality <= 0)
			return 20;
		else
			return (ushort)(vitality*20 + 20);
	}

	public static ushort CalculatePoise(short vitality){
		if(vitality <= 1)
			return 1;
		else
			return (ushort)vitality;
	}

	public static ushort CalculateMana(short magic, byte sorceryLevel){
		return (ushort)(magic + sorceryLevel);
	}

	public static ushort CalculatePower(byte witchcraft){
		// TODO: CHANGE THIS FORMULA ONCE WITCHCRAFT IS FULLY IMPLEMENTED SINCE
		// 		 ESSENTIALISTS SHOULD HAVE MORE POWER
		return (ushort)(5+(int)(witchcraft/4));
	}

	public static ushort CalculateSanity(short magic, ushort totalLevel){
		// TODO: CHANGE THIS FORMULA ONCE WITCHCRAFT IS FULLY IMPLEMENTED SINCE
		// 		 MENTALISTS SHOULD HAVE MORE SANITY
		return (ushort)(magic*2+totalLevel);
	}

	public static ushort CalculateEquipmentWeight(short vitality){
		if(vitality < 0)
			return 0;
		return (ushort)(vitality*2);
	}
}