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
}