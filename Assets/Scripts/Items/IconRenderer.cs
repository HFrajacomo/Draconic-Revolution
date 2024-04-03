using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(MeshRenderer), typeof(MeshFilter))]
public class IconRenderer : MonoBehaviour
{
	public MeshRenderer rend;
	public MeshFilter filter;

	void OnDestroy(){
		this.rend = null;
		this.filter = null;
	}
}
