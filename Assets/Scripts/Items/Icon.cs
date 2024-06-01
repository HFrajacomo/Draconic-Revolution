using UnityEngine;

public static class Icon
{
	private static Vector2[] UV = new Vector2[4];

	public static Vector2[] GetItemEntityUV(){
		// Main Face
		UV[0] = new Vector2(0f, 0f);
		UV[1] = new Vector2(0f, 1f);
		UV[2] = new Vector2(1f, 1f);
		UV[3] = new Vector2(1f, 0f);

		return Icon.UV;
	}
}
