using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TESTONLY : MonoBehaviour
{
    public VoxelLoader voxelLoader;

	void Awake(){
        this.voxelLoader = new VoxelLoader(true);
        this.voxelLoader.Load();
        VoxelLoader.Destroy();
    }
}