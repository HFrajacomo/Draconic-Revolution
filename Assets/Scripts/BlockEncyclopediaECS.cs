using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public class BlockEncyclopediaECS
{
	public static byte[] blockTransparent;
	public static byte[] objectTransparent;
	public static bool[] blockSeamless;
	public static bool[] objectSeamless;
	public static bool[] blockLoad;
	public static bool[] objectLoad;
	public static bool[] blockInvisible;
	public static bool[] objectInvisible;
	public static ShaderIndex[] blockShader;
	public static ShaderIndex[] objectShader;
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
		BlockEncyclopediaECS.blockSeamless = new bool[amountBlocks];
		BlockEncyclopediaECS.objectSeamless = new bool[amountObjects];
		BlockEncyclopediaECS.blockLoad = new bool[amountBlocks];
		BlockEncyclopediaECS.objectLoad = new bool[amountObjects];
		BlockEncyclopediaECS.blockInvisible = new bool[amountBlocks];
		BlockEncyclopediaECS.objectInvisible = new bool[amountObjects];
		BlockEncyclopediaECS.blockShader = new ShaderIndex[amountBlocks];
		BlockEncyclopediaECS.objectShader = new ShaderIndex[amountObjects];
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
