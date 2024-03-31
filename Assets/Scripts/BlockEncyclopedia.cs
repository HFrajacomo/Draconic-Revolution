using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class BlockEncyclopedia : MonoBehaviour
{
    
	public Blocks[] blocks = new Blocks[1];
	public BlocklikeObject[] objects = new BlocklikeObject[1];
    public bool isClient;

    public void OnDestroy(){
        this.blocks = null;
        this.objects = null;
    }

    // Start is called before the first frame update
    public void Awake()
    {
        /*

    	// Loads all blocks
        for(int i=0;i<Blocks.blockCount;i++){
        	blocks[i] = Blocks.Block(i);
            BlockEncyclopediaECS.blockHP[i] = blocks[i].maxHP;
            BlockEncyclopediaECS.blockSolid[i] = blocks[i].solid;
            BlockEncyclopediaECS.blockTransparent[i] = blocks[i].transparent;
            BlockEncyclopediaECS.blockSeamless[i] = blocks[i].seamless;
            BlockEncyclopediaECS.blockLoad[i] = blocks[i].hasLoadEvent;
            BlockEncyclopediaECS.blockInvisible[i] = blocks[i].invisible;
            BlockEncyclopediaECS.blockMaterial[i] = blocks[i].shaderIndex;
            BlockEncyclopediaECS.blockTiles[i] = new int3(blocks[i].tileTop, blocks[i].tileBottom, blocks[i].tileSide);
            BlockEncyclopediaECS.blockWashable[i] = blocks[i].washable;
            BlockEncyclopediaECS.blockAffectLight[i] = blocks[i].affectLight;
            BlockEncyclopediaECS.blockLuminosity[i] = blocks[i].luminosity;
            BlockEncyclopediaECS.blockDrawRegardless[i] = blocks[i].drawRegardless;
        }

        // Loads all object meshes
        for(int i=0;i<BlocklikeObject.objectCount;i++){
        	objects[i] = BlocklikeObject.Create(i, isClient);
            BlockEncyclopediaECS.objectHP[i] = objects[i].maxHP;
            BlockEncyclopediaECS.objectSolid[i] = objects[i].solid;
            BlockEncyclopediaECS.objectTransparent[i] = objects[i].transparent;
            BlockEncyclopediaECS.objectSeamless[i] = objects[i].seamless;
            BlockEncyclopediaECS.objectLoad[i] = objects[i].hasLoadEvent;
            BlockEncyclopediaECS.objectInvisible[i] = objects[i].invisible;
            BlockEncyclopediaECS.objectMaterial[i] = objects[i].shaderIndex;
            BlockEncyclopediaECS.objectScaling[i] = objects[i].scaling;
            BlockEncyclopediaECS.hitboxScaling[i] = objects[i].hitboxScaling;
            BlockEncyclopediaECS.objectNeedRotation[i] = objects[i].needsRotation;
            BlockEncyclopediaECS.objectWashable[i] = objects[i].washable;
            BlockEncyclopediaECS.objectAffectLight[i] = objects[i].affectLight;
            BlockEncyclopediaECS.objectLuminosity[i] = objects[i].luminosity;
        }

        */
    }

    public void OnApplicationQuit(){
        BlockEncyclopediaECS.Destroy();
        Compression.Destroy();
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

    // Gets transparency value from block
    public bool CheckTransparent(ushort? code){
        if(code == null)
            return false;

        if(code <= ushort.MaxValue/2)
            return blocks[(ushort)code].transparent;
        else
            return objects[ushort.MaxValue - (ushort)code].transparent;
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

    // Get the damage received by a given block/object
    public int GetDamageReceived(ushort block, ushort blockDamage){
        if(block <= ushort.MaxValue/2)
            return this.blocks[block].CalculateDamage(blockDamage);
        else
            return this.objects[ushort.MaxValue - block].CalculateDamage(blockDamage);
    }
}
