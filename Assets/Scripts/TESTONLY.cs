using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class TESTONLY : MonoBehaviour
{   

    public void Start(){
        World.SetWorldSeed(845);
        WorldGenerator gen = new WorldGenerator(845);
        gen.PrintMap();

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

}
