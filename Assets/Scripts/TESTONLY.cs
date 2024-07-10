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
        Debug.Log(Vector2.Angle(Vector2.up, new Vector2(1,0)));
        Debug.Log(Vector2.Angle(Vector2.up, new Vector2(0, -1)));
        Debug.Log(Vector2.Angle(Vector2.up, new Vector2(-.2f,1)));
    }
	
}