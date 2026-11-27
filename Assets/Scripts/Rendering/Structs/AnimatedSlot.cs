using System.Runtime.InteropServices;
using UnityEngine;

/*
Slot of an Animated Object in its chunk (see ChunkAnimatedObjects), read by AnimatedObjects.hlsl. Field order must follow AnimatedObjectSlot in the shader
row0 to row2: affine transform from the model to the chunk (scaling, state rotation and position of the voxel). All zero hides the slot
animation: x = global animation (AnimatedObjectTables) or -1 for the rest pose, y = start time (Time.time),
           z = normalized time the animation started at, w = packed light and damage decal (PackLight)
*/
[StructLayout(LayoutKind.Sequential)]
public struct AnimatedSlot{
	public const int SIZE = 64;

	public Vector4 row0;
	public Vector4 row1;
	public Vector4 row2;
	public Vector4 animation;

	// Same values as the Light UV of Objects in the chunk mesh: natural light (bits 0-3), extra light (4-7), dampness (8)
	// and the damage decal code (9-11, see DamageDecal). Read by AnimatedObjectVertex in AnimatedObjects.hlsl
	public static int PackLight(int natural, int extra, int dampness, int decal=0){
		return (natural & 0x0F) | ((extra & 0x0F) << 4) | ((dampness & 0x01) << 8) | ((decal & 0x07) << 9);
	}
}
