using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class TESTONLY : MonoBehaviour
{
	public ChunkRenderer rend;

    // Start is called before the first frame update
    void Start()
    {
    	ItemEntity ie = new ItemEntity(ItemID.DIRTBLOCK, 0, rend);
    }
}
