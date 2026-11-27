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

	// Materials are indexed by ShaderIndex. The ones after the chunk mesh's submeshes (like Animated Objects) are drawn by other meshes
	public Material GetAnimatedObjectsMaterial(){
		Material[] materials = this.rend.sharedMaterials;

		if(materials.Length <= (int)ShaderIndex.ANIMATED_OBJECTS)
			return null;

		return materials[(int)ShaderIndex.ANIMATED_OBJECTS];
	}

	// Materials of the chunk mesh, one per submesh. Extra materials would draw its last submesh again
	public Material[] GetChunkMaterials(){
		Material[] materials = this.rend.sharedMaterials;

		if(materials.Length > WriteChunkMeshJob.SUBMESH_COUNT)
			System.Array.Resize(ref materials, WriteChunkMeshJob.SUBMESH_COUNT);

		return materials;
	}

	void OnDestroy(){
		this.rend = null;
		this.filter = null;
	}
}
