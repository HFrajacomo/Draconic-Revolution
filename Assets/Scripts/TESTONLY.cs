using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TESTONLY : MonoBehaviour
{
	
    // Start is called before the first frame update
    void Start()
    {
    	print((int)(-1/32f));
    	print(Mathf.FloorToInt(-1/32f));
    }
}
