using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockEncyclopedia : MonoBehaviour
{
	public Blocks[] blocks = new Blocks[Blocks.blockCount];
	public BlocklikeObject[] objects = new BlocklikeObject[BlocklikeObject.objectCount];

    // Start is called before the first frame update
    void Start()
    {
    	// Loads all blocks
        for(int i=0;i<Blocks.blockCount;i++){
        	blocks[i] = Blocks.Block(i);
        }

        // Loads all object meshes
        for(int i=0;i<BlocklikeObject.objectCount;i++){
        	objects[i] = BlocklikeObject.Create(i);
        }
    }

    // Gets customBreak value from block
    public bool CheckCustomBreak(int blockCode){
      if(blockCode >= 0)
        return blocks[blockCode].customBreak;
      else
        return objects[(blockCode*-1)-1].customBreak;
    }

    // Gets customPlace value from block
    public bool CheckCustomPlace(int blockCode){
      if(blockCode >= 0)
        return blocks[blockCode].customPlace;
      else
        return objects[(blockCode*-1)-1].customPlace;
    }

    // Gets solid value from block
    public bool CheckSolid(int? code){
        if(code == null)
            return false;

        if(code >= 0)
            return blocks[(int)code].solid;
        else
            return objects[((int)code*-1)-1].solid;
    }

    // Gets washable value from block
    public bool CheckWashable(int code){
        if(code >= 0)
            return blocks[code].washable;
        else
            return objects[(code*-1)-1].washable;
    }

}
