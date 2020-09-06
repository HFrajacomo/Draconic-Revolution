using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LiquidMeshData
{
	public static Vector3[,] verticesOnState = {

		// State 0 (Still 3)
		{new Vector3(1, 1, 1),
    	new Vector3(-1, 1, 1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 1, -1),
    	new Vector3(1, 1, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

		// State 1 (Still 2)
		{new Vector3(1, 1, 1),
    	new Vector3(0, 1, 1),
    	new Vector3(0, 0, 1),
    	new Vector3(1, 0, 1),
    	new Vector3(0, 1, 0),
    	new Vector3(1, 1, 0),
    	new Vector3(1, 0, 0),
    	new Vector3(0, 0, 0)},

		// State 2 (Still 1)
		{new Vector3(0.5f, 0.5f, 0.5f),
    	new Vector3(0, 0.5f, 0.5f),
    	new Vector3(0, 0, 0.5f),
    	new Vector3(0.5f, 0, 0.5f),
    	new Vector3(0, 0.5f, 0),
    	new Vector3(0.5f, 0.5f, 0),
    	new Vector3(0.5f, 0, 0),
    	new Vector3(0, 0, 0)},

    	// State 3 (N2)
    	{new Vector3(1,0,1),
    	new Vector3(-1,0,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 1, -1),
    	new Vector3(1, 1, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 4 (NE2)
    	{new Vector3(1,0,1),
    	new Vector3(-1,1,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 1, -1),
    	new Vector3(1, 1, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 5 (E2)
    	{new Vector3(1,0,1),
    	new Vector3(-1,1,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 1, -1),
    	new Vector3(1, 0, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 6 (SE2)
    	{new Vector3(1,1,1),
    	new Vector3(-1,1,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 1, -1),
    	new Vector3(1, 0, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 7 (S2)
    	{new Vector3(1,1,1),
    	new Vector3(-1,1,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 0, -1),
    	new Vector3(1, 0, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 8 (SW2)
    	{new Vector3(1,1,1),
    	new Vector3(-1,1,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 0, -1),
    	new Vector3(1, 1, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 9 (W2)
    	{new Vector3(1,1,1),
    	new Vector3(-1,0,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 0, -1),
    	new Vector3(1, 1, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 10 (NW2)
    	{new Vector3(1,1,1),
    	new Vector3(-1,0,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 1, -1),
    	new Vector3(1, 1, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 11 (N1)
    	{new Vector3(0.5f,0,0.5f),
    	new Vector3(-0.5f,0,0.5f),
    	new Vector3(-0.5f, -0.5f, 0.5f),
    	new Vector3( 0.5f, -0.5f,  0.5f),
    	new Vector3(-0.5f, 0.5f, -0.5f),
    	new Vector3(0.5f, 0.5f, -0.5f),
    	new Vector3(0.5f, -0.5f, -0.5f),
    	new Vector3(-0.5f, -0.5f, -0.5f)},


    	// State 12 (NE1)
    	{new Vector3(0.5f,0,0.5f),
    	new Vector3(-0.5f,0.5f,0.5f),
    	new Vector3(-0.5f, -0.5f, 0.5f),
    	new Vector3( 0.5f, -0.5f,  0.5f),
    	new Vector3(-0.5f, 0.5f, -0.5f),
    	new Vector3(0.5f, 0.5, -0.5f),
    	new Vector3(0.5f, -0.5f, -0.5f),
    	new Vector3(-0.5f, -0.5f, -0.5f)},

    	// State 13 (E1)
    	{new Vector3(0.5f, 0, 0.5f),
    	new Vector3(-0.5f, 0.5, 0.5f),
    	new Vector3(-0.5f, -0.5f, 0.5f),
    	new Vector3( 0.5f, -0.5f,  0.5f),
    	new Vector3(-0.5f, 0.5f, -0.5f),
    	new Vector3(0.5f, 0, -0.5f),
    	new Vector3(0.5f, -0.5f, -0.5f),
    	new Vector3(-0.5f, -0.5f, -0.5f)},

    	// State 14 (SE1)
    	{new Vector3(0.5f, 0.5f, 0.5f),
    	new Vector3(-0.5f, 0.5, 0.5f),
    	new Vector3(-0.5f, -0.5f, 0.5f),
    	new Vector3( 0.5f, -0.5f,  0.5f),
    	new Vector3(-0.5f, 0.5f, -0.5f),
    	new Vector3(0.5f, 0, -0.5f),
    	new Vector3(0.5f, -0.5f, -0.5f),
    	new Vector3(-0.5f, -0.5f, -0.5f)},   

    	// State 15 (S1)
    	{new Vector3(0.5f, 0.5f, 0.5f),
    	new Vector3(-0.5f, 0.5, 0.5f),
    	new Vector3(-0.5f, -0.5f, 0.5f),
    	new Vector3( 0.5f, -0.5f,  0.5f),
    	new Vector3(-0.5f, 0, -0.5f),
    	new Vector3(0.5f, 0, -0.5f),
    	new Vector3(0.5f, -0.5f, -0.5f),
    	new Vector3(-0.5f, -0.5f, -0.5f)},

    	// State 16 (SW1)
    	{new Vector3(0.5f, 0.5f, 0.5f),
    	new Vector3(-0.5f, 0.5, 0.5f),
    	new Vector3(-0.5f, -0.5f, 0.5f),
    	new Vector3( 0.5f, -0.5f,  0.5f),
    	new Vector3(-0.5f, 0, -0.5f),
    	new Vector3(0.5f, 0.5f, -0.5f),
    	new Vector3(0.5f, -0.5f, -0.5f),
    	new Vector3(-0.5f, -0.5f, -0.5f)}, 

    	// State 17 (W1)
    	{new Vector3(0.5f, 0.5f, 0.5f),
    	new Vector3(-0.5f, 0, 0.5f),
    	new Vector3(-0.5f, -0.5f, 0.5f),
    	new Vector3( 0.5f, -0.5f,  0.5f),
    	new Vector3(-0.5f, 0, -0.5f),
    	new Vector3(0.5f, 0.5f, -0.5f),
    	new Vector3(0.5f, -0.5f, -0.5f),
    	new Vector3(-0.5f, -0.5f, -0.5f)},

    	// State 18 (NW1)
    	{new Vector3(0.5f, 0.5f, 0.5f),
    	new Vector3(-0.5f, 0, 0.5f),
    	new Vector3(-0.5f, -0.5f, 0.5f),
    	new Vector3( 0.5f, -0.5f,  0.5f),
    	new Vector3(-0.5f, 0.5f, -0.5f),
    	new Vector3(0.5f, 0.5f, -0.5f),
    	new Vector3(0.5f, -0.5f, -0.5f),
    	new Vector3(-0.5f, -0.5f, -0.5f)},
	
		// States 19 & 20 (Still)
		{new Vector3(1, 1, 1),
    	new Vector3(-1, 1, 1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 1, -1),
    	new Vector3(1, 1, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},
	};

	public static Vector3[] GetByState(short? s){
		if(s == null){
			return verticesOnState[0];
		}

		if(s >= 0 && s <=2){
			return verticesOnState[0];
		}
	}
}
