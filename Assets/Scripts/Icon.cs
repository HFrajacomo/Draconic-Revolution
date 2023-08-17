using UnityEngine;

public static class Icon
{
	public static readonly ushort iconAtlasX = 16;
	public static readonly ushort iconAtlasY = 2;

	public static readonly float offsetX = 1f/Icon.iconAtlasX;
	public static readonly float offsetY = 1f/Icon.iconAtlasY;

	private static Vector2[] UV = new Vector2[4];

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

		return Icon.UV;
	}
}
