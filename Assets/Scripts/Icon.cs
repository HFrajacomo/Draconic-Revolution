using UnityEngine;

public static class Icon
{
	public static readonly ushort iconAtlasX = 16;
	public static readonly ushort iconAtlasY = 2;

	public static readonly float offsetX = 1f/Icon.iconAtlasX;
	public static readonly float offsetY = 1f/Icon.iconAtlasY;

	private static readonly float itemOffsetX = Icon.offsetX/5f;
	private static readonly float itemOffsetY = Icon.offsetY/5f;

	public static Vector2[] GetItemEntityUV(uint iconID){
		uint x = (uint)(iconID%Icon.iconAtlasX);
		uint y = (uint)((iconID - x)/Icon.iconAtlasY);

		float initPosX = Icon.offsetX * x;
		float initPosY = Icon.offsetY * y;
		float endPosX = Icon.offsetX * (uint)(x+1);
		float endPosY = Icon.offsetY * (uint)(y+1);


		Vector2[] UV = new Vector2[24];

		// Main Face
		UV[0] = new Vector2(initPosX + Icon.itemOffsetX, initPosY);
		UV[1] = new Vector2(initPosX, initPosY + Icon.itemOffsetY);
		UV[2] = new Vector2(initPosX, endPosY - Icon.itemOffsetY);
		UV[3] = new Vector2(initPosX + Icon.itemOffsetX, endPosY);
		UV[4] = new Vector2(endPosX - Icon.itemOffsetX, endPosY);
		UV[5] = new Vector2(endPosX, endPosY - Icon.itemOffsetY);
		UV[6] = new Vector2(endPosX, initPosY + Icon.itemOffsetY);
		UV[7] = new Vector2(endPosX - Icon.itemOffsetX, initPosY);
		// Back Face
		UV[8] = new Vector2(initPosX + Icon.itemOffsetX, initPosY);
		UV[9] = new Vector2(initPosX, initPosY + Icon.itemOffsetY);
		UV[10] = new Vector2(initPosX, endPosY - Icon.itemOffsetY);
		UV[11] = new Vector2(initPosX + Icon.itemOffsetX, endPosY);
		UV[12] = new Vector2(endPosX - Icon.itemOffsetX, endPosY);
		UV[13] = new Vector2(endPosX, endPosY - Icon.itemOffsetY);
		UV[14] = new Vector2(endPosX, initPosY + Icon.itemOffsetY);
		UV[15] = new Vector2(endPosX - Icon.itemOffsetX, initPosY);
		// Sides
		UV[16] = new Vector2(1f, 1f);
		UV[17] = new Vector2(1f, 0f);
		UV[18] = new Vector2(1f, 1f);
		UV[19] = new Vector2(1f, 0f);
		UV[20] = new Vector2(0f, 1f);
		UV[21] = new Vector2(0f, 0f);
		UV[22] = new Vector2(0f, 1f);
		UV[23] = new Vector2(0f, 0f);	

		return UV;
	}
}
