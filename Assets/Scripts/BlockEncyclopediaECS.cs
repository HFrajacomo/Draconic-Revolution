using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public static class BlockEncyclopediaECS
{
	private static bool IS_INITIALIZED = false;
	public static NativeArray<ushort> blockHP;
	public static NativeArray<ushort> objectHP;
	public static NativeArray<bool> blockSolid;
	public static NativeArray<bool> objectSolid;
	public static NativeArray<bool> blockTransparent;
	public static NativeArray<bool> objectTransparent;
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
	public static NativeArray<bool> blockAffectLight;
	public static NativeArray<bool> objectAffectLight;
	public static NativeArray<byte> blockLuminosity;
	public static NativeArray<byte> objectLuminosity;
	public static NativeArray<bool> blockDrawRegardless;
	public static NativeArray<int2> atlasSize;

	static BlockEncyclopediaECS(){ 
		InitializeNativeStructures();
	}

	public static bool IsInitialized(){return IS_INITIALIZED;}

	public static void InitializeNativeStructures(){
		if(IS_INITIALIZED){return;}

		BlockEncyclopediaECS.blockHP = new NativeArray<ushort>(VoxelLoader.GetAmountOfBlocks(), Allocator.Persistent);
		BlockEncyclopediaECS.objectHP = new NativeArray<ushort>(VoxelLoader.GetAmountOfObjects(), Allocator.Persistent);
		BlockEncyclopediaECS.blockSolid = new NativeArray<bool>(VoxelLoader.GetAmountOfBlocks(), Allocator.Persistent);
		BlockEncyclopediaECS.objectSolid = new NativeArray<bool>(VoxelLoader.GetAmountOfObjects(), Allocator.Persistent);
		BlockEncyclopediaECS.blockTransparent = new NativeArray<bool>(VoxelLoader.GetAmountOfBlocks(), Allocator.Persistent);
		BlockEncyclopediaECS.objectTransparent = new NativeArray<bool>(VoxelLoader.GetAmountOfObjects(), Allocator.Persistent);
		BlockEncyclopediaECS.blockSeamless = new NativeArray<bool>(VoxelLoader.GetAmountOfBlocks(), Allocator.Persistent);
		BlockEncyclopediaECS.objectSeamless = new NativeArray<bool>(VoxelLoader.GetAmountOfObjects(), Allocator.Persistent);
		BlockEncyclopediaECS.blockLoad = new NativeArray<bool>(VoxelLoader.GetAmountOfBlocks(), Allocator.Persistent);
		BlockEncyclopediaECS.objectLoad = new NativeArray<bool>(VoxelLoader.GetAmountOfObjects(), Allocator.Persistent);
		BlockEncyclopediaECS.blockInvisible = new NativeArray<bool>(VoxelLoader.GetAmountOfBlocks(), Allocator.Persistent);
		BlockEncyclopediaECS.objectInvisible = new NativeArray<bool>(VoxelLoader.GetAmountOfObjects(), Allocator.Persistent);
		BlockEncyclopediaECS.blockMaterial = new NativeArray<ShaderIndex>(VoxelLoader.GetAmountOfBlocks(), Allocator.Persistent);
		BlockEncyclopediaECS.objectMaterial = new NativeArray<ShaderIndex>(VoxelLoader.GetAmountOfObjects(), Allocator.Persistent);
		BlockEncyclopediaECS.blockTiles = new NativeArray<int3>(VoxelLoader.GetAmountOfBlocks(), Allocator.Persistent);
		BlockEncyclopediaECS.objectScaling = new NativeArray<Vector3>(VoxelLoader.GetAmountOfObjects(), Allocator.Persistent);
		BlockEncyclopediaECS.hitboxScaling = new NativeArray<Vector3>(VoxelLoader.GetAmountOfObjects(), Allocator.Persistent);		
		BlockEncyclopediaECS.objectNeedRotation = new NativeArray<bool>(VoxelLoader.GetAmountOfObjects(), Allocator.Persistent);
		BlockEncyclopediaECS.blockWashable = new NativeArray<bool>(VoxelLoader.GetAmountOfBlocks(), Allocator.Persistent);
		BlockEncyclopediaECS.objectWashable = new NativeArray<bool>(VoxelLoader.GetAmountOfObjects(), Allocator.Persistent);
		BlockEncyclopediaECS.blockLuminosity = new NativeArray<byte>(VoxelLoader.GetAmountOfBlocks(), Allocator.Persistent);
		BlockEncyclopediaECS.objectLuminosity = new NativeArray<byte>(VoxelLoader.GetAmountOfObjects(), Allocator.Persistent);
		BlockEncyclopediaECS.blockDrawRegardless = new NativeArray<bool>(VoxelLoader.GetAmountOfBlocks(), Allocator.Persistent);
		BlockEncyclopediaECS.blockAffectLight = new NativeArray<bool>(VoxelLoader.GetAmountOfBlocks(), Allocator.Persistent);
		BlockEncyclopediaECS.objectAffectLight = new NativeArray<bool>(VoxelLoader.GetAmountOfObjects(), Allocator.Persistent);
		BlockEncyclopediaECS.atlasSize = new NativeArray<int2>(Enum.GetValues(typeof(ShaderIndex)).Length, Allocator.Persistent);
		IS_INITIALIZED = true;
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
		BlockEncyclopediaECS.blockDrawRegardless.Dispose();
		BlockEncyclopediaECS.blockAffectLight.Dispose();
		BlockEncyclopediaECS.objectAffectLight.Dispose();
		BlockEncyclopediaECS.atlasSize.Dispose();
		IS_INITIALIZED = false;
	}
}
