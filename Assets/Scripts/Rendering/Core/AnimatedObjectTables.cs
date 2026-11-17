using System.Collections.Generic;
using UnityEngine;

/*
Client only. Animations of every Animated Object, built once when the game scene loads (like ObjectMeshTables) and kept in global GPU buffers:
	_AnimatedObjectClips: AnimatedClipInfo of every animation, indexed by global animation id
	_AnimatedObjectFrames: rows of the part transforms of every animation (AnimatedClipData.rows)
Animations of Object i have the global ids [GetGlobalClip(i, 0), GetGlobalClip(i, 0) + animation count)
*/
public static class AnimatedObjectTables{
	private static readonly int CLIPS_ID = Shader.PropertyToID("_AnimatedObjectClips");
	private static readonly int FRAMES_ID = Shader.PropertyToID("_AnimatedObjectFrames");
	private static readonly int ROW_SIZE = 16;

	private static bool isCreated = false;

	// Per Object
	private static int[] clipOffset; // -1 if not animated
	private static int[] clipCount;
	private static float[] radius;

	// Per global animation
	private static float[] clipLength;
	private static bool[] clipLoop;

	private static GraphicsBuffer clipBuffer;
	private static GraphicsBuffer frameBuffer;

	public static bool IsCreated(){return isCreated;}

	// Sets the animation buffers on a material, so its draws don't depend on the global buffers being bound
	public static void BindTo(Material material){
		if(!isCreated)
			return;

		material.SetBuffer(CLIPS_ID, clipBuffer);
		material.SetBuffer(FRAMES_ID, frameBuffer);
	}

	// Must be called after the Object models were loaded by their model identities (Client only)
	public static void Build(){
		if(isCreated)
			return;

		int amount = VoxelLoader.GetAmountOfObjects();
		List<AnimatedClipInfo> infos = new List<AnimatedClipInfo>();
		List<Vector4> rows = new List<Vector4>();
		List<float> lengths = new List<float>();
		List<bool> loops = new List<bool>();
		BlocklikeObject obj;
		VX_AnimatedModelIdentityBehaviour identity;
		AnimatedClipData clip;

		clipOffset = new int[amount];
		clipCount = new int[amount];
		radius = new float[amount];

		for(int i=0; i < amount; i++){
			obj = VoxelLoader.GetObject((ushort)(ushort.MaxValue - i));
			identity = obj.GetModelIdentity() as VX_AnimatedModelIdentityBehaviour;
			clipOffset[i] = -1;

			if(obj.shaderIndex != ShaderIndex.ANIMATED_OBJECTS || identity == null || !identity.IsAnimationLoaded())
				continue;

			clipOffset[i] = infos.Count;
			clipCount[i] = identity.GetAnimationCount();
			radius[i] = identity.GetRadius();

			for(int c=0; c < clipCount[i]; c++){
				clip = identity.GetClip(c);
				infos.Add(new AnimatedClipInfo(rows.Count, clip));
				rows.AddRange(clip.rows);
				lengths.Add(clip.length);
				loops.Add(clip.loop);
			}
		}

		clipLength = lengths.ToArray();
		clipLoop = loops.ToArray();

		// Buffers can't be empty
		clipBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Mathf.Max(1, infos.Count), AnimatedClipInfo.SIZE);
		frameBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Mathf.Max(1, rows.Count), ROW_SIZE);

		if(infos.Count > 0)
			clipBuffer.SetData(infos);
		if(rows.Count > 0)
			frameBuffer.SetData(rows);

		Shader.SetGlobalBuffer(CLIPS_ID, clipBuffer);
		Shader.SetGlobalBuffer(FRAMES_ID, frameBuffer);

		isCreated = true;
	}

	public static void Destroy(){
		if(!isCreated)
			return;

		clipBuffer.Release();
		frameBuffer.Release();
		clipBuffer = null;
		frameBuffer = null;

		isCreated = false;
	}

	// Global id of an animation of an Object (index = ushort.MaxValue - block code), or -1 if it doesn't exist
	public static int GetGlobalClip(int objectIndex, int animation){
		if(!isCreated || objectIndex < 0 || objectIndex >= clipOffset.Length)
			return -1;
		if(clipOffset[objectIndex] < 0 || animation < 0 || animation >= clipCount[objectIndex])
			return -1;

		return clipOffset[objectIndex] + animation;
	}

	public static float GetClipLength(int globalClip){return clipLength[globalClip];}
	public static bool IsClipLooping(int globalClip){return clipLoop[globalClip];}

	// Largest distance from the model origin reached in any animation, in model units
	public static float GetRadius(int objectIndex){
		if(!isCreated || objectIndex < 0 || objectIndex >= radius.Length)
			return 0f;

		return radius[objectIndex];
	}
}
