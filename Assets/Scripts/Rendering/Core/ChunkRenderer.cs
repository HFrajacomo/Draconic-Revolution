using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(MeshRenderer), typeof(MeshFilter))]
public class ChunkRenderer : MonoBehaviour
{
	public MeshRenderer rend;
	public MeshFilter filter;

	public Material decalMaterial;

	void Start(){
		VoxelLoader.SetAtlasTextures(this);
	}

	void OnDestroy(){
		this.rend = null;
		this.filter = null;
	}
}
