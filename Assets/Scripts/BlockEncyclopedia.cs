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
        	objects[i].LoadMesh();
        }
    }
}
