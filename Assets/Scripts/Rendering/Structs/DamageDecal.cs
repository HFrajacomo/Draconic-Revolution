/*
Damage decals of Objects, drawn by the Object shaders (DamageDecal.hlsl) with the same texture and stages as the block decals

The decal code of a voxel is 0 when it isn't damaged, or its decal stage + 1 (1 to Constants.DECAL_STAGE_SIZE)
	Static Objects: Light UV (UV3) w = 1 + decal code, so undamaged vertices keep the 1 every vertex had
	Animated Objects: bits 9 to 11 of the light packed in their slot (AnimatedSlot.PackLight)
*/
public static class DamageDecal{
	// Decal code of a voxel with the given HP. Unassigned HP (0 or ushort.MaxValue) and full HP have no decal
	public static int GetCode(ushort hp, ushort maxHP){
		float hpPercentage;

		if(hp == 0 || hp == ushort.MaxValue || maxHP == 0 || hp >= maxHP)
			return 0;

		hpPercentage = (float)hp / (float)maxHP;

		for(int i=0; i < Constants.DECAL_STAGE_SIZE; i++){
			if(hpPercentage <= Constants.DECAL_STAGE_PERCENTAGE[i])
				return Constants.DECAL_STAGE_SIZE - i;
		}

		return 0;
	}
}
