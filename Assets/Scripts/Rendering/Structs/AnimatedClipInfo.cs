using System.Runtime.InteropServices;
using UnityEngine;

/*
An animation of AnimatedObjectTables, read by AnimatedObjects.hlsl. Field order must follow AnimatedObjectClip in the shader
data: x = first row in _AnimatedObjectFrames, y = frame count, z = part count, w = length in seconds
flags: x = 1 if it loops
*/
[StructLayout(LayoutKind.Sequential)]
public struct AnimatedClipInfo{
	public const int SIZE = 32;

	public Vector4 data;
	public Vector4 flags;

	public AnimatedClipInfo(int firstRow, AnimatedClipData clip){
		this.data = new Vector4(firstRow, clip.frameCount, clip.partCount, clip.length);
		this.flags = new Vector4(clip.loop ? 1f : 0f, 0f, 0f, 0f);
	}
}
