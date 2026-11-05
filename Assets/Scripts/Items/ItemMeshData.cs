using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ItemMeshData
{
	public static readonly Vector3[] vertices = {
		new Vector3(-.5f, -.5f, 0f),
		new Vector3(-.5f, .5f, 0f),
		new Vector3(.5f, .5f, 0f),
		new Vector3(.5f, -.5f, 0f)
	};

	public static readonly int[] imageTris = {
		0,1,2,2,3,0
	};
}
