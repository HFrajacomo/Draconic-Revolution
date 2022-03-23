using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class TESTONLY : MonoBehaviour
{   

    public void Start(){
        World.SetWorldSeed(845);
        //Perlin.FillImage();
        World.DrawSplines();

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

}
