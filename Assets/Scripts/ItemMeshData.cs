using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ItemMeshData
{
	// 16 Verts
	public static readonly Vector3[] vertices = {
		// First face
		new Vector3(-.3f, .1f, -.5f),
		new Vector3(-.5f, .1f, -.3f),
		new Vector3(-.5f, .1f, .3f),
		new Vector3(-.3f, .1f, .5f),
		new Vector3(.3f, .1f, .5f),
		new Vector3(.5f, .1f, .3f),
		new Vector3(.5f, .1f, -.3f),
		new Vector3(.3f, .1f, -.5f),
		// Second face
		new Vector3(-.3f, -.1f, -.5f),
		new Vector3(-.5f, -.1f, -.3f),
		new Vector3(-.5f, -.1f, .3f),
		new Vector3(-.3f, -.1f, .5f),
		new Vector3(.3f, -.1f, .5f),
		new Vector3(.5f, -.1f, .3f),
		new Vector3(.5f, -.1f, -.3f),
		new Vector3(.3f, -.1f, -.5f),
		// Material Border
		new Vector3(-.5f, .1f, -.5f),
		new Vector3(-.5f, .1f, .5f),
		new Vector3(.5f, .1f, .5f),
		new Vector3(.5f, .1f, -.5f),
		new Vector3(-.5f, -.1f, -.5f),
		new Vector3(-.5f, -.1f, .5f),
		new Vector3(.5f, -.1f, .5f),
		new Vector3(.5f, -.1f, -.5f),	
	}; 

	public static readonly int[] imageTris = {
		// First face
		0,1,2,0,2,3,7,0,3,7,3,4,6,7,4,5,6,4,
		// Second face
		8,9,10,8,10,11,15,8,11,15,11,12,14,15,12,13,14,12,
		// Side conectors
		//16,17,21,16,20,21,17,18,22,17,21,22,18,19,23,18,22,23,19,16,20,19,23,20
	};

	public static readonly int[] materialTris = {
		// Dented
		0,16,1,2,17,3,4,18,5,6,19,7,8,20,9,10,21,11,12,22,13,14,23,15,
		// Sides
		16,17,21,16,21,20,17,18,22,17,22,21,18,19,23,18,23,22,19,16,20,19,20,23
	};
}
