using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Unity.Collections;

public static class NativeTools
{

    /*
    Uses unsafe code to copy all elements in NativeArray to a normal array
    */
    public static unsafe T[] CopyToManaged<T>(NativeArray<T> array) where T: unmanaged{
        int size = array.Length;
        int byteSize = size * UnsafeUtility.SizeOf<T>();

        T[] outputArray = new T[size];
    
        void* inputBuffer = UnsafeUtility.AddressOf(ref outputArray[0]);
        void* outBuffer = array.GetUnsafePtr();

        UnsafeUtility.MemCpy(inputBuffer, outBuffer, byteSize);
        return outputArray;
    }

    /*
    Uses unsafe code to copy all elements in NativeArray to a normal array
    */
    public static unsafe NativeArray<T> CopyToNative<T>(T[] array) where T: struct{
        int size = array.Length;
        int byteSize = size * UnsafeUtility.SizeOf<T>();

        NativeArray<T> outputArray = new NativeArray<T>(size, Allocator.TempJob);
    
        void* inputBuffer = outputArray.GetUnsafePtr();
        void* outBuffer = UnsafeUtility.AddressOf(ref array[0]);

        UnsafeUtility.MemCpy(inputBuffer, outBuffer, byteSize);
        return outputArray;
    }
    public static unsafe NativeList<T> CopyToNative<T>(List<T> list) where T: struct{
        int size = list.Count;
        int byteSize = size * UnsafeUtility.SizeOf<T>();

        NativeList<T> outputList = new NativeList<T>(0, Allocator.TempJob);
    
        void* inputBuffer = outputList.GetUnsafePtr();
        void* outBuffer = UnsafeUtility.AddressOf(ref outputList);

        UnsafeUtility.MemCpy(inputBuffer, outBuffer, byteSize);
        return outputList;
    }
    public static unsafe NativeArray<T> Copy<T>(NativeArray<T> array) where T: struct{
        int size = array.Length;
        int byteSize = size* UnsafeUtility.SizeOf<T>();

        NativeArray<T> outputArray = new NativeArray<T>(size, Allocator.TempJob);

        void* inputBuffer = array.GetUnsafePtr();
        void* outBuffer = outputArray.GetUnsafePtr();

        UnsafeUtility.MemCpy(inputBuffer, outBuffer, byteSize);
        return outputArray;
    }
}
