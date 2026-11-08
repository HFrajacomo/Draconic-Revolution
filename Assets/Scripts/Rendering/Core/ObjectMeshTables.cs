using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

/*
Mesh data of every Object, concatenated into read-only native tables that are built once and shared by every Chunk build.
Tables are indexed by object index (ushort.MaxValue - blockCode). Object i spans [offset[i], offset[i+1]) of its data
*/
public static class ObjectMeshTables{
	private static bool isCreated = false;

	// Render Mesh
	public static NativeArray<Vector3> verts;
	public static NativeArray<Vector3> uvs;
	public static NativeArray<Vector3> normals;
	public static NativeArray<Vector4> tangents;
	public static NativeArray<int> tris;
	public static NativeArray<int> vertOffset;
	public static NativeArray<int> trisOffset;

	// Hitbox Mesh
	public static NativeArray<Vector3> hitboxVerts;
	public static NativeArray<Vector3> hitboxNormals;
	public static NativeArray<int> hitboxTris;
	public static NativeArray<int> hitboxVertOffset;
	public static NativeArray<int> hitboxTrisOffset;

	// Offset and rotation per state of objects that need rotation. Object i spans [stateOffset[i], stateOffset[i+1])
	public static NativeArray<int> stateOffset;
	public static NativeArray<Vector3> stateTranslation;
	public static NativeArray<int2> stateRotation;

	public static bool IsCreated(){return isCreated;}

	// Must be called after the Object meshes were loaded by their VX_ModelIdentityBehaviour (Client only)
	public static void Build(){
		if(isCreated)
			return;

		int amount = VoxelLoader.GetAmountOfObjects();

		List<Vector3> v = new List<Vector3>();
		List<Vector3> uv = new List<Vector3>();
		List<Vector3> n = new List<Vector3>();
		List<Vector4> t = new List<Vector4>();
		List<int> tri = new List<int>();
		List<Vector3> hv = new List<Vector3>();
		List<Vector3> hn = new List<Vector3>();
		List<int> htri = new List<int>();
		List<Vector3> translation = new List<Vector3>();
		List<int2> rotation = new List<int2>();

		int[] vOff = new int[amount+1];
		int[] tOff = new int[amount+1];
		int[] hvOff = new int[amount+1];
		int[] htOff = new int[amount+1];
		int[] sOff = new int[amount+1];

		BlocklikeObject obj;
		MeshData meshData;

		for(int i=0; i < amount; i++){
			obj = VoxelLoader.GetObject((ushort)(ushort.MaxValue - i));

			vOff[i] = v.Count;
			tOff[i] = tri.Count;
			hvOff[i] = hv.Count;
			htOff[i] = htri.Count;
			sOff[i] = translation.Count;

			if(obj.HasMeshData()){
				meshData = obj.GetMeshData();

				meshData.GetVertices(v);
				meshData.GetUVs(uv);
				meshData.GetNormals(n);
				meshData.GetTangents(t);
				tri.AddRange(meshData.GetTriangles());

				// Every attribute must have one entry per vertex
				Fit(uv, v.Count, Vector3.zero);
				Fit(n, v.Count, Vector3.up);
				Fit(t, v.Count, new Vector4(1, 0, 0, 1));

				meshData.GetHitboxVertices(hv);
				meshData.GetHitboxNormals(hn);
				htri.AddRange(meshData.GetHitboxTriangles());

				Fit(hn, hv.Count, Vector3.up);
			}

			if(obj.needsRotation){
				for(ushort state=0; state < obj.maximumRotationScaleState; state++){
					translation.Add(obj.GetOffsetVector(state));
					rotation.Add(obj.GetRotationValue(state));
				}
			}
		}

		vOff[amount] = v.Count;
		tOff[amount] = tri.Count;
		hvOff[amount] = hv.Count;
		htOff[amount] = htri.Count;
		sOff[amount] = translation.Count;

		verts = new NativeArray<Vector3>(v.ToArray(), Allocator.Persistent);
		uvs = new NativeArray<Vector3>(uv.ToArray(), Allocator.Persistent);
		normals = new NativeArray<Vector3>(n.ToArray(), Allocator.Persistent);
		tangents = new NativeArray<Vector4>(t.ToArray(), Allocator.Persistent);
		tris = new NativeArray<int>(tri.ToArray(), Allocator.Persistent);
		vertOffset = new NativeArray<int>(vOff, Allocator.Persistent);
		trisOffset = new NativeArray<int>(tOff, Allocator.Persistent);

		hitboxVerts = new NativeArray<Vector3>(hv.ToArray(), Allocator.Persistent);
		hitboxNormals = new NativeArray<Vector3>(hn.ToArray(), Allocator.Persistent);
		hitboxTris = new NativeArray<int>(htri.ToArray(), Allocator.Persistent);
		hitboxVertOffset = new NativeArray<int>(hvOff, Allocator.Persistent);
		hitboxTrisOffset = new NativeArray<int>(htOff, Allocator.Persistent);

		stateOffset = new NativeArray<int>(sOff, Allocator.Persistent);
		stateTranslation = new NativeArray<Vector3>(translation.ToArray(), Allocator.Persistent);
		stateRotation = new NativeArray<int2>(rotation.ToArray(), Allocator.Persistent);

		isCreated = true;
	}

	public static void Destroy(){
		if(!isCreated)
			return;

		verts.Dispose();
		uvs.Dispose();
		normals.Dispose();
		tangents.Dispose();
		tris.Dispose();
		vertOffset.Dispose();
		trisOffset.Dispose();

		hitboxVerts.Dispose();
		hitboxNormals.Dispose();
		hitboxTris.Dispose();
		hitboxVertOffset.Dispose();
		hitboxTrisOffset.Dispose();

		stateOffset.Dispose();
		stateTranslation.Dispose();
		stateRotation.Dispose();

		isCreated = false;
	}

	// Pads or trims a list so it has exactly the given size
	private static void Fit<T>(List<T> list, int size, T padding){
		if(list.Count > size)
			list.RemoveRange(size, list.Count - size);

		while(list.Count < size)
			list.Add(padding);
	}
}
