using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TESTONLY : MonoBehaviour
{   
	void Start(){
		float t = 1.23456f;

		Debug.Log((float)Mathf.Floor(t * 100) / 100);
	}
}
