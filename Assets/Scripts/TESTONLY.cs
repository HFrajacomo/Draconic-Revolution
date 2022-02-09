using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class TESTONLY : MonoBehaviour
{   


    private void PrintAll(ushort[] a){
        string output = "";
        foreach(ushort u in a)
            output += u.ToString() + " ";
        print(output);
    }

    private void PrintAll(NativeArray<ushort> a){
        string output = "";
        foreach(ushort u in a)
            output += u.ToString() + " ";
        print(output);
    }

    private void PopulateArray(ushort[] a){
        for(ushort i=0; i < a.Length; i++)
            a[i] = i;
    }

    private void PopulateArray(NativeArray<ushort> a){
        for(ushort i=0; i < a.Length; i++)
            a[i] = i;
    }
}
