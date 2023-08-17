using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TESTONLY : MonoBehaviour
{   
	public Text text_;


	void Start(){
		SetText("aa");
	}

	void Update(){
		SetText("bb");
		this.text_.SetAllDirty();
	}

	private void SetText(string t){
		this.text_.text  = t;
	}
}
