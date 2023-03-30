using UnityEngine;

public static class Icon
{
	public static readonly ushort iconAtlasX = 16;
	public static readonly ushort iconAtlasY = 2;

	public static readonly float offsetX = 1f/Icon.iconAtlasX;
	public static readonly float offsetY = 1f/Icon.iconAtlasY;

	private static Vector2[] UV = new Vector2[24];
	private static bool firstRun = true;

	public static Vector2[] GetItemEntityUV(uint iconID){
		uint x = (uint)(iconID%Icon.iconAtlasX);
		uint y = (uint)(iconID/Icon.iconAtlasX);

		float initPosX = Icon.offsetX * x;
		float initPosY = Icon.offsetY * (iconAtlasY - y - 1);
		float endPosX = Icon.offsetX * (uint)(x+1);
		float endPosY = Icon.offsetY * (uint)(iconAtlasY - y);

		// Main Face
		UV[0] = new Vector2(initPosX, initPosY);
		UV[1] = new Vector2(initPosX, endPosY);
		UV[2] = new Vector2(endPosX, endPosY);
		UV[3] = new Vector2(endPosX, initPosY);
		UV[4] = new Vector2(initPosX, initPosY);
		UV[5] = new Vector2(initPosX, endPosY);
		UV[6] = new Vector2(endPosX, endPosY);
		UV[7] = new Vector2(endPosX, initPosY);

		if(firstRun){
			// Sides
			UV[8] = new Vector2(0f, 0f);
			UV[9] = new Vector2(0f, 1f);
			UV[10] = new Vector2(1f, 1f);
			UV[11] = new Vector2(1f, 0f);
			UV[12] = new Vector2(0f, 0f);
			UV[13] = new Vector2(0f, 1f);
			UV[14] = new Vector2(1f, 1f);
			UV[15] = new Vector2(1f, 0f);
			UV[16] = new Vector2(0f, 0f);
			UV[17] = new Vector2(0f, 1f);
			UV[18] = new Vector2(1f, 1f);
			UV[19] = new Vector2(1f, 0f);
			UV[20] = new Vector2(0f, 0f);
			UV[21] = new Vector2(0f, 1f);
			UV[22] = new Vector2(1f, 1f);
			UV[23] = new Vector2(1f, 0f);	
		}

		Icon.firstRun = false;

		return Icon.UV;
	}
}
