using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class TESTONLY : MonoBehaviour
{   
    /*
    ushort[] uArray = new ushort[16*16*256];

    void Start(){
        Array.Fill<ushort>(uArray, 1);

    }

    void Update(){
        NativeArray<ushort> testArray;
        NativeArray<ushort> testFastArray;

        testArray = new NativeArray<ushort>(uArray, Allocator.TempJob);
        testFastArray = NativeTools.CopyToNative(uArray);

        ushort[] output = testArray.ToArray();
        ushort[] normalTest = NativeTools.CopyToManaged(testFastArray);

        testArray.Dispose();
        testFastArray.Dispose();

        print("done");       
    }
    */





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
