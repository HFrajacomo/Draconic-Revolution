using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockEncyclopedia : MonoBehaviour
{
	public Blocks[] blocks = new Blocks[Blocks.blockCount];

    // Start is called before the first frame update
    void Start()
    {
        for(int i=0;i<Blocks.blockCount;i++){
        	blocks[i] = Blocks.Block(i);
        }
    }
}
