using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TESTONLY : MonoBehaviour
{
	
    // Start is called before the first frame update
    void Start()
    {
    	print(Math.Round(-0.6f, MidpointRounding.AwayFromZero));
    }
}

public class test{
	List<int> rola = new List<int>();
	List<string> rolinha = new List<string>();

	public test(){
		this.rola.Add(1);
		this.rolinha.Add("aa");
	}
}
