using System;
using UnityEngine;
using Unity.Mathematics;

/*
Client-side Floating Origin
	The Server, the network messages and the save files always use absolute world coordinates.
	The client places every GameObject at (absolute - offset), so the player stays near (0,0,0) and float positions keep their precision
	The offset only exists on X and Z (Y is bounded) and is always a multiple of REBASE_DISTANCE, which is a multiple of the chunk width,
	so block and chunk coordinates convert exactly.
	The offset is always zero on the Server, which turns every conversion into the identity there

	Any code that turns absolute coordinates into a Unity position, or reads a Unity position as absolute coordinates, must convert through this class
*/
public static class WorldOrigin {
	// The world is shifted whenever the player gets this far from the current origin on X or Z
	public static readonly int REBASE_DISTANCE = 1024;
	private static readonly int SHADER_ORIGIN_ID = Shader.PropertyToID("_WorldOrigin");

	private static int offsetX = 0;
	private static int offsetZ = 0;

	public static int GetOffsetX(){return offsetX;}
	public static int GetOffsetZ(){return offsetZ;}
	public static Vector3 GetOffset(){return new Vector3(offsetX, 0f, offsetZ);}

	public static void Reset(){
		Set(0, 0);
	}

	public static void Set(int x, int z){
		offsetX = x;
		offsetZ = z;
		Shader.SetGlobalVector(SHADER_ORIGIN_ID, new Vector4(offsetX, 0f, offsetZ, 0f));
	}

	// Origin that puts an absolute XZ position within REBASE_DISTANCE/2 of the Unity origin
	public static int GetOriginFor(double absolute){
		return (int)(Math.Round(absolute / REBASE_DISTANCE, MidpointRounding.AwayFromZero) * REBASE_DISTANCE);
	}

	// Absolute -> Unity
	public static Vector3 ToUnity(Vector3 absolute){
		return new Vector3((float)((double)absolute.x - offsetX), absolute.y, (float)((double)absolute.z - offsetZ));
	}

	public static float3 ToUnity(float3 absolute){
		return new float3((float)((double)absolute.x - offsetX), absolute.y, (float)((double)absolute.z - offsetZ));
	}

	public static Vector3 ToUnity(float x, float y, float z){
		return new Vector3((float)((double)x - offsetX), y, (float)((double)z - offsetZ));
	}

	// Unity -> Absolute. Precision is lost far from the world origin, so block math should use ToCastCoord instead
	public static Vector3 ToAbsolute(Vector3 unity){
		return new Vector3((float)((double)unity.x + offsetX), unity.y, (float)((double)unity.z + offsetZ));
	}

	/*
	Block coordinates of a Unity position
		Rounds in double precision with the same rule as CastCoord(Vector3) (half away from zero) applied to the absolute value,
		so the result is the same block that the absolute coordinates would give, at any distance
	*/
	public static CastCoord ToCastCoord(Vector3 unity){
		double x = Math.Round((double)unity.x + offsetX, MidpointRounding.AwayFromZero);
		double z = Math.Round((double)unity.z + offsetZ, MidpointRounding.AwayFromZero);

		return new CastCoord(new Vector3((float)x, unity.y, (float)z));
	}
}
