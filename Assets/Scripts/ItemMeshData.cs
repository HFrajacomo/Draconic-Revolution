using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ItemMeshData
{
	// 16 Verts
	public static readonly Vector3[] vertices = {
		// Image Faces
		new Vector3(-.3f, -.3f, .05f),
		new Vector3(-.3f, .3f, .05f),
		new Vector3(.3f, .3f, .05f),
		new Vector3(.3f, -.3f, .05f),
		new Vector3(-.3f, -.3f, -.05f),
		new Vector3(-.3f, .3f, -.05f),
		new Vector3(.3f, .3f, -.05f),
		new Vector3(.3f, -.3f, -.05f),

		// Top/Bottom Rim Faces
		new Vector3(-.3f, .3f, -.05f),
		new Vector3(-.3f, .3f, .05f),
		new Vector3(.3f, .3f, .05f),
		new Vector3(.3f, .3f, -.05f),
		new Vector3(-.3f, -.3f, -.05f),		
		new Vector3(-.3f, -.3f, .05f),
		new Vector3(.3f, -.3f, .05f),
		new Vector3(.3f, -.3f, -.05f),

		// Sides Rim Faces
		new Vector3(-.3f, -.3f, -.05f),
		new Vector3(-.3f, -.3f, .05f),
		new Vector3(-.3f, .3f, .05f),
		new Vector3(-.3f, .3f, -.05f),
		new Vector3(.3f, -.3f, -.05f),
		new Vector3(.3f, -.3f, .05f),
		new Vector3(.3f, .3f, .05f),
		new Vector3(.3f, .3f, -.05f)
	}; 

	public static readonly int[] imageTris = {
		0,1,2,0,2,3,4,5,6,4,6,7
	};

	public static readonly int[] materialTris = {
		8,9,10,8,10,11,12,13,14,12,14,15,16,17,18,16,18,19,20,21,22,20,22,23
	};
}
