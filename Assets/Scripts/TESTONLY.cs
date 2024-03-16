using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TESTONLY : MonoBehaviour
{
	void Start(){
        string json = Resources.Load<TextAsset>("vox").ToString();

        Blocks b = VoxelDeserializer.DeserializeBlock(json);
    }
}