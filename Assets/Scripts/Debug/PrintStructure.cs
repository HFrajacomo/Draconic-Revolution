using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class PrintStructure {
	public static void PrintDictionary<T, U>(Dictionary<T, U> dict){
		StringBuilder sb = new StringBuilder();

		foreach(T key in dict.Keys){
			sb.Append($"{{{key.ToString()} -> {dict[key].ToString()}}}\n");
		}

		Debug.LogWarning($"[PrintStructure - Dictionary]\n{sb.ToString()}");
	}

	public static void PrintByteArray(byte[] data){
        StringBuilder hex = new StringBuilder();
        foreach (byte b in data){
            hex.AppendFormat("{0:X2} ", b);
        }

        Debug.LogWarning(hex.ToString());
	}

	public static void PrintByteArray(byte[] data, int size){
        StringBuilder hex = new StringBuilder();
        for(int i=0; i < size; i++){
            hex.AppendFormat("{0:X2} ", data[i]);
        }

        Debug.LogWarning(hex.ToString());
	}
}