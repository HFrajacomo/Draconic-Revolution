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
        this.itemLoader = new ItemLoader(true);

        this.itemLoader.Load();

        this.itemLoader.RunPostDeserializationRoutine();

        Debug.Log(ItemLoader.GetItem("BASE_Sand"));
        Debug.Log(ItemLoader.GetCopy("BASE_Sand"));
    }
	
}