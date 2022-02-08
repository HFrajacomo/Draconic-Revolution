using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public class BlockEncyclopediaECS
{
	public static byte[] blockTransparent;
	public static byte[] objectTransparent;
	public static bool[] blockLiquid;
	public static bool[] objectLiquid;
	public static bool[] blockLoad;
	public static bool[] objectLoad;
	public static bool[] blockInvisible;
	public static bool[] objectInvisible;
	public static byte[] blockMaterial;
	public static byte[] objectMaterial;
	public static int3[] blockTiles; // [tileTop, tileBottom, tileSide]
	public static Vector3[] objectScaling;
	public static bool[] objectNeedRotation;
	public static bool[] blockWashable;
	public static bool[] objectWashable;
	public static bool[] blockAffectLight;
	public static bool[] objectAffectLight;
	public static byte[] blockLuminosity;
	public static byte[] objectLuminosity;

	public BlockEncyclopediaECS(int amountBlocks, int amountObjects){
		BlockEncyclopediaECS.blockTransparent = new byte[amountBlocks];
		BlockEncyclopediaECS.objectTransparent = new byte[amountObjects];
		BlockEncyclopediaECS.blockLiquid = new bool[amountBlocks];
		BlockEncyclopediaECS.objectLiquid = new bool[amountObjects];
		BlockEncyclopediaECS.blockLoad = new bool[amountBlocks];
		BlockEncyclopediaECS.objectLoad = new bool[amountObjects];
		BlockEncyclopediaECS.blockInvisible = new bool[amountBlocks];
		BlockEncyclopediaECS.objectInvisible = new bool[amountObjects];
		BlockEncyclopediaECS.blockMaterial = new byte[amountBlocks];
		BlockEncyclopediaECS.objectMaterial = new byte[amountObjects];
		BlockEncyclopediaECS.blockTiles = new int3[amountBlocks];
		BlockEncyclopediaECS.objectScaling = new Vector3[amountObjects];		
		BlockEncyclopediaECS.objectNeedRotation = new bool[amountObjects];
		BlockEncyclopediaECS.blockWashable = new bool[amountBlocks];
		BlockEncyclopediaECS.objectWashable = new bool[amountObjects];
		BlockEncyclopediaECS.blockAffectLight = new bool[amountBlocks];
		BlockEncyclopediaECS.objectAffectLight = new bool[amountObjects];
		BlockEncyclopediaECS.blockLuminosity = new byte[amountBlocks];
		BlockEncyclopediaECS.objectLuminosity = new byte[amountObjects];
	}
}
