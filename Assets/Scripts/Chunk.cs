using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{

	// Chunk Settings
	public VoxelData data;
	public static int chunkWidth = 16;
	public static int chunkDepth = 100;
	public ChunkPos pos;

	// Unity Settings
	public ChunkRenderer renderer;
	public MeshFilter meshFilter;
	public GameObject obj;

	public Chunk(ChunkPos pos, ChunkRenderer r){
		this.pos = pos;
		this.renderer = r;
		this.obj = new GameObject();
		this.obj.AddComponent<MeshFilter>();
		this.obj.AddComponent<MeshRenderer>();
		this.obj.AddComponent<MeshCollider>();
		this.obj.transform.position = new Vector3(pos.x * chunkWidth, 0f, pos.z * chunkWidth);
		this.obj.name = "Chunk " + pos.x + ", " + pos.z;
		this.obj.transform.SetParent(this.renderer.transform);
		this.obj.GetComponent<MeshRenderer>().material = this.renderer.GetComponent<MeshRenderer>().material;
	}

	public void BuildOnVoxelData(VoxelData vd){
		this.data = vd;	
	}

	public void BuildChunk(){
		Mesh mesh = new Mesh();
		Blocks thisBlock;
		Blocks neighborBlock;


    	List<Vector3> vertices = new List<Vector3>();
    	List<int> triangles = new List<int>();
    	List<Vector2> UVs = new List<Vector2>();

    	for(int x=0; x<data.GetWidth(); x++){
    		for(int y=0; y<data.GetHeight(); y++){
    			for(int z=0; z<data.GetDepth(); z++){
    				thisBlock = new Blocks(data.GetCell(x,y,z));

    				// If invisible block
	    			if(thisBlock.invisible){
	    				continue;
	    			}
	    			//Make Cube
			    	for(int i=0; i<6; i++){
			    		// Air Check
			    		neighborBlock = new Blocks(data.GetNeighbor(x, y, z, (Direction)i));
			    		if(neighborBlock.transparent || neighborBlock.invisible){
			    			// Make Face
					    	vertices.AddRange(CubeMeshData.faceVertices(i, 0.5f, new Vector3(x,y,z)));
					    	
					    	UVs.AddRange(thisBlock.AddTexture((Direction)i));
					    	
					    	int vCount = vertices.Count;

					    	triangles.Add(vCount -4);
					    	triangles.Add(vCount -4 +1);
					    	triangles.Add(vCount -4 +2);
					    	triangles.Add(vCount -4);
					    	triangles.Add(vCount -4 +2);
					    	triangles.Add(vCount -4 +3);
			    		}

			    	}

	    		}
	    	}
    	}


    	mesh.Clear();
    	mesh.vertices = vertices.ToArray();
    	mesh.triangles = triangles.ToArray();
    	mesh.uv = UVs.ToArray();
    	mesh.RecalculateNormals();

    	this.obj.GetComponent<MeshFilter>().sharedMesh = mesh;
    	this.obj.GetComponent<MeshCollider>().sharedMesh = mesh;
    }
}
