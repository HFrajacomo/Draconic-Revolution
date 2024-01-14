using UnityEngine;
using System.Collections.Generic;

public static class DebugCube{
	private static Dictionary<ChunkPos, GameObject> cubes = new Dictionary<ChunkPos, GameObject>();

	public static void Create(ChunkPos p, Color col, int offset=150, int scale=3){
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = new Vector3(p.x*Chunk.chunkWidth+8, p.y*Chunk.chunkDepth+offset, p.z*Chunk.chunkWidth+8);
        cube.transform.localScale = new Vector3(scale, scale, scale);
        cube.GetComponent<Renderer>().material.color = col;

        cubes.Add(p, cube);
	}

	public static void Delete(ChunkPos p){
		GameObject.Destroy(cubes[p]);
		cubes.Remove(p);
	}

	public static void ChangeColor(ChunkPos p, Color c){
		if(cubes.ContainsKey(p))
			cubes[p].GetComponent<Renderer>().material.color = c;
		else
			Create(p, c);
	}

	public static void Clear(){
		foreach(ChunkPos pos in cubes.Keys){
			GameObject.Destroy(cubes[pos]);
		}

		cubes.Clear();
	}
}