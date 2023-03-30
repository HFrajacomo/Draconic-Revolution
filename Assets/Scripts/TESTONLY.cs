using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class TESTONLY : MonoBehaviour
{   
	public GameObject[] test = new GameObject[500];
	public GameObject prefab;

	public void Start(){
		for(int i=0; i < 500; i++){
			test[i] = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);
		}
	}

	public void Update(){
		foreach(GameObject go in test){
			go.SetActive(true);
		}
	}
}
