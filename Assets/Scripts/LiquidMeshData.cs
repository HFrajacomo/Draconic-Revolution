using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LiquidMeshData
{

	/*
	all vertices of all possible states in a liquid mesh
	*/
	public static Vector3[][] verticesOnState = {

		// State 0 (Still 3)
		new Vector3[] {new Vector3(1, 1, 1),
    	new Vector3(-1, 1, 1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 1, -1),
    	new Vector3(1, 1, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

		// State 1 (Still 2)
		new Vector3[] {new Vector3(1, 0, 1),
    	new Vector3(-1, 0, 1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 0, -1),
    	new Vector3(1, 0, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

		// State 2 (Still 1)
		new Vector3[] {new Vector3(1, -0.5f, 1),
    	new Vector3(-1, -0.5f, 1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, -0.5f, -1),
    	new Vector3(1, -0.5f, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 3 (N2)
    	new Vector3[] {new Vector3(1,0,1),
    	new Vector3(-1,0,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 1, -1),
    	new Vector3(1, 1, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 4 (NE2)
    	new Vector3[] {new Vector3(1,0,1),
    	new Vector3(-1,0,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 1, -1),
    	new Vector3(1, 0, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 5 (E2)
    	new Vector3[] {new Vector3(1,0,1),
    	new Vector3(-1,1,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 1, -1),
    	new Vector3(1, 0, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 6 (SE2)
    	new Vector3[] {new Vector3(1,0,1),
    	new Vector3(-1,1,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 0, -1),
    	new Vector3(1, 0, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 7 (S2)
    	new Vector3[] {new Vector3(1,1,1),
    	new Vector3(-1,1,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 0, -1),
    	new Vector3(1, 0, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 8 (SW2)
    	new Vector3[] {new Vector3(1,1,1),
    	new Vector3(-1,0,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 0, -1),
    	new Vector3(1, 0, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 9 (W2)
    	new Vector3[] {new Vector3(1,1,1),
    	new Vector3(-1,0,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 0, -1),
    	new Vector3(1, 1, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 10 (NW2)
        new Vector3[] {new Vector3(1,0,1),
        new Vector3(-1,0,1),
        new Vector3(-1, -1, 1),
        new Vector3(1, -1, 1),
        new Vector3(-1, 0, -1),
        new Vector3(1, 1, -1),
        new Vector3(1, -1, -1),
        new Vector3(-1, -1, -1)},

    	// State 11 (N1)
    	new Vector3[] {new Vector3(1,-0.5f,1),
    	new Vector3(-1, -0.5f,1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1,  1),
    	new Vector3(-1, 0, -1),
    	new Vector3(1, 0, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)},

    	// State 12 (NE1)
        new Vector3[] {new Vector3(1,-0.5f,1),
        new Vector3(-1, -0.5f,1),
        new Vector3(-1, -1, 1),
        new Vector3(1, -1,  1),
        new Vector3(-1, 0, -1),
        new Vector3(1, -0.5f, -1),
        new Vector3(1, -1, -1),
        new Vector3(-1, -1, -1)},

    	// State 13 (E1)
        new Vector3[] {new Vector3(1,-0.5f,1),
        new Vector3(-1, 0,1),
        new Vector3(-1, -1, 1),
        new Vector3(1, -1,  1),
        new Vector3(-1, 0, -1),
        new Vector3(1, -0.5f, -1),
        new Vector3(1, -1, -1),
        new Vector3(-1, -1, -1)},

    	// State 14 (SE1)
        new Vector3[] {new Vector3(1,-0.5f,1),
        new Vector3(-1, 0,1),
        new Vector3(-1, -1, 1),
        new Vector3(1, -1,  1),
        new Vector3(-1, -0.5f, -1),
        new Vector3(1, -0.5f, -1),
        new Vector3(1, -1, -1),
        new Vector3(-1, -1, -1)},  

    	// State 15 (S1)
        new Vector3[] {new Vector3(1,0,1),
        new Vector3(-1, 0,1),
        new Vector3(-1, -1, 1),
        new Vector3(1, -1,  1),
        new Vector3(-1, -0.5f, -1),
        new Vector3(1, -0.5f, -1),
        new Vector3(1, -1, -1),
        new Vector3(-1, -1, -1)},

    	// State 16 (SW1)
        new Vector3[] {new Vector3(1,0,1),
        new Vector3(-1, -0.5f,1),
        new Vector3(-1, -1, 1),
        new Vector3(1, -1,  1),
        new Vector3(-1, -0.5f, -1),
        new Vector3(1, -0.5f, -1),
        new Vector3(1, -1, -1),
        new Vector3(-1, -1, -1)},

    	// State 17 (W1)
        new Vector3[] {new Vector3(1,0,1),
        new Vector3(-1, -0.5f,1),
        new Vector3(-1, -1, 1),
        new Vector3(1, -1,  1),
        new Vector3(-1, -0.5f, -1),
        new Vector3(1, 0, -1),
        new Vector3(1, -1, -1),
        new Vector3(-1, -1, -1)},

    	// State 18 (NW1)
        new Vector3[] {new Vector3(1,-0.5f,1),
        new Vector3(-1, -0.5f,1),
        new Vector3(-1, -1, 1),
        new Vector3(1, -1,  1),
        new Vector3(-1, -0.5f, -1),
        new Vector3(1, 0, -1),
        new Vector3(1, -1, -1),
        new Vector3(-1, -1, -1)},
	
		// States 19 & 20 (Falling 3 and 2)
		new Vector3[] {new Vector3(1, 1, 1),
    	new Vector3(-1, 1, 1),
    	new Vector3(-1, -1, 1),
    	new Vector3(1, -1, 1),
    	new Vector3(-1, 1, -1),
    	new Vector3(1, 1, -1),
    	new Vector3(1, -1, -1),
    	new Vector3(-1, -1, -1)}
	};

	public static int[][] faceTriangles = {
	  new int[] {0,1,2,3},
	  new int[] {5,0,3,6},
	  new int[] {4,5,6,7},
	  new int[] {1,4,7,2},
	  new int[] {5,4,1,0},
	  new int[] {3,2,7,6}
	};

	// Gets the vertices of a given state in a liquid
	public static Vector3[] VertsByState(int dir, ushort s, Vector3 pos, float scale=0.5f){
		Vector3[] fv = new Vector3[4];

        if(s == ushort.MaxValue)
            s = 0;

		if(s == 19 || s == 20){
		    for (int i = 0; i < fv.Length; i++)
		    {
		      fv[i] = (verticesOnState[19][faceTriangles[dir][i]] * scale) + pos;
		    }
			return fv;
		}
		else{
		    for (int i = 0; i < fv.Length; i++)
		    {
		      fv[i] = (verticesOnState[(int)s][faceTriangles[dir][i]] * scale) + pos;
		    }
			return fv;
		}
	}
}
