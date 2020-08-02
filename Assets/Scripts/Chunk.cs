using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{

	//public VoxelRender render;
	public VoxelData data;
	public int startX;
	public int startZ;
	public static int chunkWidth = 16;
	public static int chunkDepth = 3;

	public void Start(){
		GenerateRandomChunk(); 
		BuildChunk();
	}

	public Chunk(int xstart, int zstart){
		startX = xstart;
		startZ = zstart;
		//render = GameObject.Find("Voxel Renderer").GetComponent<VoxelRender>();		

		//GenerateRandomChunk();
		//BuildChunk(); 

	}

	public void GenerateRandomChunk(){
		int[,,] voxdata = new int[chunkWidth,chunkDepth,chunkWidth];
		int rnd;
		for(int x=0; x < chunkWidth;x++){
			for(int y=0; y < chunkDepth; y++){
				for(int z=0; z < chunkWidth; z++){
					rnd = Random.Range(0,4);
					if(rnd == 0)
						voxdata[x,y,z] = 0;
					else
						voxdata[x,y,z] = 1;
				}
			}
		}
		data = new VoxelData(voxdata);
	}

	public void BuildChunk(){
		Mesh mesh = new Mesh();

    	List<Vector3> vertices = new List<Vector3>();
    	List<int> triangles = new List<int>();		

    	for(int x=0; x<data.Width; x++){
    		for(int y=0; y<data.Height; y++){
    			for(int z=0; z<data.Depth; z++){
    				// Block Check
	    			if(data.GetCell(x,y,z) == 0){
	    				continue;
	    			}
	    			//Make Cube
			    	for(int i=0; i<6; i++){
			    		// Air Check
			    		if(data.GetNeighbor(x, y, z, (Direction)i) == 0){
			    			// Make Face
			     			//MakeFace((Direction)i, cubeScale, cubePos);
					    	vertices.AddRange(CubeMeshData.faceVertices(i, 0.5f, new Vector3(x,y,z)));
					    	int vCount = vertices.Count;

					    	triangles.Add(vCount -4);
					    	triangles.Add(vCount -4 +1);
					    	triangles.Add(vCount -4 +2);
					    	triangles.Add(vCount -4);
					    	triangles.Add(vCount -4 +2);
					    	triangles.Add(vCount -4 +3);
					    	// Make Face end
			    		}

			    	}
			    	// Make Cube End

	    			//MakeCube(0.5f, new Vector3((float)x, (float)y, (float)z), x, y, z, data);
	    		}
	    	}
    	}

    	mesh.Clear();
    	mesh.vertices = vertices.ToArray();
    	mesh.triangles = triangles.ToArray();
    	mesh.RecalculateNormals();
    	GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }
    /*
    void MakeCube(float cubeScale, Vector3 cubePos, int x, int y, int z, VoxelData data){
    	for(int i=0; i<6; i++){
    		if(data.GetNeighbor(x, y, z, (Direction)i) == 0){
     			MakeFace((Direction)i, cubeScale, cubePos);
    		}

    	}
    }

    void MakeFace(Direction dir, float faceScale, Vector3 facePos){
    	vertices.AddRange(CubeMeshData.faceVertices(dir, faceScale, facePos));
    	int vCount = vertices.Count;

    	triangles.Add(vCount -4);
    	triangles.Add(vCount -4 +1);
    	triangles.Add(vCount -4 +2);
    	triangles.Add(vCount -4);
    	triangles.Add(vCount -4 +2);
    	triangles.Add(vCount -4 +3);

    }
	
    void UpdateMesh(){

    }
    */
}
