﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class TESTONLY : MonoBehaviour
{
	public Client client;
	
    // Start is called before the first frame update
    void Start()
    {
    	print(Mathf.FloorToInt(-1/32f));
    }
}
