using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public static class BlockEncyclopediaECS
{
	public static NativeArray<ushort> blockHP;
	public static NativeArray<ushort> objectHP;
	public static NativeArray<bool> blockSolid;
	public static NativeArray<bool> objectSolid;
	public static NativeArray<byte> blockTransparent;
	public static NativeArray<byte> objectTransparent;
	public static NativeArray<bool> blockSeamless;
	public static NativeArray<bool> objectSeamless;
	public static NativeArray<bool> blockLoad;
	public static NativeArray<bool> objectLoad;
	public static NativeArray<bool> blockInvisible;
	public static NativeArray<bool> objectInvisible;
	public static NativeArray<ShaderIndex> blockMaterial;
	public static NativeArray<ShaderIndex> objectMaterial;
	public static NativeArray<int3> blockTiles; // [tileTop, tileBottom, tileSide]
	public static NativeArray<Vector3> objectScaling;
	public static NativeArray<Vector3> hitboxScaling;
	public static NativeArray<bool> objectNeedRotation;
	public static NativeArray<bool> blockWashable;
	public static NativeArray<bool> objectWashable;
	public static bool[] blockAffectLight;
	public static bool[] objectAffectLight;
	public static NativeArray<byte> blockLuminosity;
	public static NativeArray<byte> objectLuminosity;
	public static NativeArray<bool> blockDrawTopRegardless;

	static BlockEncyclopediaECS(){ 
		BlockEncyclopediaECS.blockHP = new NativeArray<ushort>(Blocks.blockCount, Allocator.Persistent);
		BlockEncyclopediaECS.objectHP = new NativeArray<ushort>(BlocklikeObject.objectCount, Allocator.Persistent);
		BlockEncyclopediaECS.blockSolid = new NativeArray<bool>(Blocks.blockCount, Allocator.Persistent);
		BlockEncyclopediaECS.objectSolid = new NativeArray<bool>(BlocklikeObject.objectCount, Allocator.Persistent);
		BlockEncyclopediaECS.blockTransparent = new NativeArray<byte>(Blocks.blockCount, Allocator.Persistent);
		BlockEncyclopediaECS.objectTransparent = new NativeArray<byte>(BlocklikeObject.objectCount, Allocator.Persistent);
		BlockEncyclopediaECS.blockSeamless = new NativeArray<bool>(Blocks.blockCount, Allocator.Persistent);
		BlockEncyclopediaECS.objectSeamless = new NativeArray<bool>(BlocklikeObject.objectCount, Allocator.Persistent);
		BlockEncyclopediaECS.blockLoad = new NativeArray<bool>(Blocks.blockCount, Allocator.Persistent);
		BlockEncyclopediaECS.objectLoad = new NativeArray<bool>(BlocklikeObject.objectCount, Allocator.Persistent);
		BlockEncyclopediaECS.blockInvisible = new NativeArray<bool>(Blocks.blockCount, Allocator.Persistent);
		BlockEncyclopediaECS.objectInvisible = new NativeArray<bool>(BlocklikeObject.objectCount, Allocator.Persistent);
		BlockEncyclopediaECS.blockMaterial = new NativeArray<ShaderIndex>(Blocks.blockCount, Allocator.Persistent);
		BlockEncyclopediaECS.objectMaterial = new NativeArray<ShaderIndex>(BlocklikeObject.objectCount, Allocator.Persistent);
		BlockEncyclopediaECS.blockTiles = new NativeArray<int3>(Blocks.blockCount, Allocator.Persistent);
		BlockEncyclopediaECS.objectScaling = new NativeArray<Vector3>(BlocklikeObject.objectCount, Allocator.Persistent);
		BlockEncyclopediaECS.hitboxScaling = new NativeArray<Vector3>(BlocklikeObject.objectCount, Allocator.Persistent);		
		BlockEncyclopediaECS.objectNeedRotation = new NativeArray<bool>(BlocklikeObject.objectCount, Allocator.Persistent);
		BlockEncyclopediaECS.blockWashable = new NativeArray<bool>(Blocks.blockCount, Allocator.Persistent);
		BlockEncyclopediaECS.objectWashable = new NativeArray<bool>(BlocklikeObject.objectCount, Allocator.Persistent);
		BlockEncyclopediaECS.blockLuminosity = new NativeArray<byte>(Blocks.blockCount, Allocator.Persistent);
		BlockEncyclopediaECS.objectLuminosity = new NativeArray<byte>(BlocklikeObject.objectCount, Allocator.Persistent);
		BlockEncyclopediaECS.blockDrawTopRegardless = new NativeArray<bool>(Blocks.blockCount, Allocator.Persistent);

		BlockEncyclopediaECS.blockAffectLight = new bool[Blocks.blockCount];
		BlockEncyclopediaECS.objectAffectLight = new bool[BlocklikeObject.objectCount];
	}

	public static void Destroy(){
		BlockEncyclopediaECS.blockHP.Dispose();
		BlockEncyclopediaECS.objectHP.Dispose();
		BlockEncyclopediaECS.blockSolid.Dispose();
		BlockEncyclopediaECS.objectSolid.Dispose();
		BlockEncyclopediaECS.blockTransparent.Dispose();
		BlockEncyclopediaECS.objectTransparent.Dispose();
		BlockEncyclopediaECS.blockSeamless.Dispose();
		BlockEncyclopediaECS.objectSeamless.Dispose();
		BlockEncyclopediaECS.blockLoad.Dispose();
		BlockEncyclopediaECS.objectLoad.Dispose();
		BlockEncyclopediaECS.blockInvisible.Dispose();
		BlockEncyclopediaECS.objectInvisible.Dispose();
		BlockEncyclopediaECS.blockMaterial.Dispose();
		BlockEncyclopediaECS.objectMaterial.Dispose();
		BlockEncyclopediaECS.blockTiles.Dispose();
		BlockEncyclopediaECS.objectScaling.Dispose();
		BlockEncyclopediaECS.hitboxScaling.Dispose();
		BlockEncyclopediaECS.objectNeedRotation.Dispose();
		BlockEncyclopediaECS.blockWashable.Dispose();
		BlockEncyclopediaECS.objectWashable.Dispose();
		BlockEncyclopediaECS.blockLuminosity.Dispose();
		BlockEncyclopediaECS.objectLuminosity.Dispose();
		BlockEncyclopediaECS.blockDrawTopRegardless.Dispose();
	}
}
