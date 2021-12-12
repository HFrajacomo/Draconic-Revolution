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
    	print(-1%16);
        print(-16%16);
        print(Mathf.FloorToInt(-1f/16));
        print(Mathf.FloorToInt(-16f/16));
    }
}
