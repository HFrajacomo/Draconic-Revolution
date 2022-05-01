using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public class BlockEncyclopediaECS
{
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
	public static NativeArray<bool> objectNeedRotation;
	public static NativeArray<bool> blockWashable;
	public static NativeArray<bool> objectWashable;
	public static NativeArray<bool> blockAffectLight;
	public static NativeArray<bool> objectAffectLight;
	public static NativeArray<byte> blockLuminosity;
	public static NativeArray<byte> objectLuminosity;

	public BlockEncyclopediaECS(int amountBlocks, int amountObjects){ 
		BlockEncyclopediaECS.blockTransparent = new NativeArray<byte>(amountBlocks, Allocator.Persistent);
		BlockEncyclopediaECS.objectTransparent = new NativeArray<byte>(amountObjects, Allocator.Persistent);
		BlockEncyclopediaECS.blockSeamless = new NativeArray<bool>(amountBlocks, Allocator.Persistent);
		BlockEncyclopediaECS.objectSeamless = new NativeArray<bool>(amountObjects, Allocator.Persistent);
		BlockEncyclopediaECS.blockLoad = new NativeArray<bool>(amountBlocks, Allocator.Persistent);
		BlockEncyclopediaECS.objectLoad = new NativeArray<bool>(amountObjects, Allocator.Persistent);
		BlockEncyclopediaECS.blockInvisible = new NativeArray<bool>(amountBlocks, Allocator.Persistent);
		BlockEncyclopediaECS.objectInvisible = new NativeArray<bool>(amountObjects, Allocator.Persistent);
		BlockEncyclopediaECS.blockMaterial = new NativeArray<ShaderIndex>(amountBlocks, Allocator.Persistent);
		BlockEncyclopediaECS.objectMaterial = new NativeArray<ShaderIndex>(amountObjects, Allocator.Persistent);
		BlockEncyclopediaECS.blockTiles = new NativeArray<int3>(amountBlocks, Allocator.Persistent);
		BlockEncyclopediaECS.objectScaling = new NativeArray<Vector3>(amountObjects, Allocator.Persistent);		
		BlockEncyclopediaECS.objectNeedRotation = new NativeArray<bool>(amountObjects, Allocator.Persistent);
		BlockEncyclopediaECS.blockWashable = new NativeArray<bool>(amountBlocks, Allocator.Persistent);
		BlockEncyclopediaECS.objectWashable = new NativeArray<bool>(amountObjects, Allocator.Persistent);
		BlockEncyclopediaECS.blockAffectLight = new NativeArray<bool>(amountBlocks, Allocator.Persistent);
		BlockEncyclopediaECS.objectAffectLight = new NativeArray<bool>(amountObjects, Allocator.Persistent);
		BlockEncyclopediaECS.blockLuminosity = new NativeArray<byte>(amountBlocks, Allocator.Persistent);
		BlockEncyclopediaECS.objectLuminosity = new NativeArray<byte>(amountObjects, Allocator.Persistent);
	}

	public static void Destroy(){
		blockTransparent.Dispose();
		objectTransparent.Dispose();
		blockSeamless.Dispose();
		objectSeamless.Dispose();
		blockLoad.Dispose();
		objectLoad.Dispose();
		blockInvisible.Dispose();
		objectInvisible.Dispose();
		blockMaterial.Dispose();
		objectMaterial.Dispose();
		blockTiles.Dispose();
		objectScaling.Dispose();
		objectNeedRotation.Dispose();
		blockWashable.Dispose();
		objectWashable.Dispose();
		blockAffectLight.Dispose();
		objectAffectLight.Dispose();
		blockLuminosity.Dispose();
		objectLuminosity.Dispose();
	}
}
