using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TESTONLY : MonoBehaviour
{
    public VoxelLoader voxelLoader;
    public ItemLoader itemLoader;

    void Start(){
        Debug.Log(Vector2.Angle(Vector2.down, new Vector2(0.8f,0.6f)));
    }
	
}