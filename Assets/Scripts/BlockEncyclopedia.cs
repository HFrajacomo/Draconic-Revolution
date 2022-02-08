using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class BlockEncyclopedia : MonoBehaviour
{
	public Blocks[] blocks = new Blocks[Blocks.blockCount];
	public BlocklikeObject[] objects = new BlocklikeObject[BlocklikeObject.objectCount];
    public BlockEncyclopediaECS data;
    public bool isClient;

    // Start is called before the first frame update
    public void Awake()
    {
        data = new BlockEncyclopediaECS(Blocks.blockCount, BlocklikeObject.objectCount);

    	// Loads all blocks
        for(int i=0;i<Blocks.blockCount;i++){
        	blocks[i] = Blocks.Block(i);
            BlockEncyclopediaECS.blockTransparent[i] = blocks[i].transparent;
            BlockEncyclopediaECS.blockLiquid[i] = blocks[i].liquid;
            BlockEncyclopediaECS.blockLoad[i] = blocks[i].hasLoadEvent;
            BlockEncyclopediaECS.blockInvisible[i] = blocks[i].invisible;
            BlockEncyclopediaECS.blockMaterial[i] = blocks[i].materialIndex;
            BlockEncyclopediaECS.blockTiles[i] = new int3(blocks[i].tileTop, blocks[i].tileBottom, blocks[i].tileSide);
            BlockEncyclopediaECS.blockWashable[i] = blocks[i].washable;
            BlockEncyclopediaECS.blockAffectLight[i] = blocks[i].affectLight;
            BlockEncyclopediaECS.blockLuminosity[i] = blocks[i].luminosity;
        }

        // Loads all object meshes
        for(int i=0;i<BlocklikeObject.objectCount;i++){
        	objects[i] = BlocklikeObject.Create(i, isClient);
            BlockEncyclopediaECS.objectTransparent[i] = objects[i].transparent;
            BlockEncyclopediaECS.objectLiquid[i] = objects[i].liquid;
            BlockEncyclopediaECS.objectLoad[i] = objects[i].hasLoadEvent;
            BlockEncyclopediaECS.objectInvisible[i] = objects[i].invisible;
            BlockEncyclopediaECS.objectMaterial[i] = objects[i].materialIndex;
            BlockEncyclopediaECS.objectScaling[i] = objects[i].scaling;
            BlockEncyclopediaECS.objectNeedRotation[i] = objects[i].needsRotation;
            BlockEncyclopediaECS.objectWashable[i] = objects[i].washable;
            BlockEncyclopediaECS.objectAffectLight[i] = objects[i].affectLight;
            BlockEncyclopediaECS.objectLuminosity[i] = objects[i].luminosity;
        }
    }


    // Gets customBreak value from block
    public bool CheckCustomBreak(ushort blockCode){
      if(blockCode <= ushort.MaxValue/2)
        return blocks[blockCode].customBreak;
      else
        return objects[ushort.MaxValue - blockCode].customBreak;
    }

    // Gets customPlace value from block
    public bool CheckCustomPlace(ushort blockCode){
      if(blockCode <= ushort.MaxValue/2)
        return blocks[blockCode].customPlace;
      else
        return objects[ushort.MaxValue - blockCode].customPlace;
    }

    // Gets solid value from block
    public bool CheckSolid(ushort? code){
        if(code == null)
            return false;

        if(code <= ushort.MaxValue/2)
            return blocks[(ushort)code].solid;
        else
            return objects[ushort.MaxValue - (ushort)code].solid;
    }

    // Gets washable value from block
    public bool CheckWashable(ushort code){
        if(code <= ushort.MaxValue/2)
            return blocks[code].washable;
        else
            return objects[ushort.MaxValue - code].washable;
    }

    // Gets washable value from block
    public bool CheckLiquid(ushort? code){
        if(code == null)
            return false;
            
        if(code <= ushort.MaxValue/2)
            return blocks[(ushort)code].liquid;
        else
            return objects[ushort.MaxValue - (ushort)code].liquid;
    }

    // Gets name of given code
    public string CheckName(ushort code){
        if(code <= ushort.MaxValue/2)
            return blocks[code].name;
        else
            return objects[ushort.MaxValue - code].name;
    }

    // Gets affectLight from given code
    public bool CheckAffectLight(ushort code){
        if(code <= ushort.MaxValue/2)
            return blocks[code].affectLight;
        else
            return objects[ushort.MaxValue - code].affectLight;
    }

}
