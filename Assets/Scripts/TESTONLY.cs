using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;


public class TESTONLY : MonoBehaviour {
	void Start(){
		string text = "[test, rogerio, alkasdhu]";

		List<string> allElements = JsonFormatter.StringToList(text);

		foreach(string element in allElements){
			Debug.Log(element);
		}
	}
}