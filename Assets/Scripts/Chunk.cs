using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{

	// Chunk Settings
	public VoxelData data;
	public VoxelMetadata metadata;
	public static int chunkWidth = 16;
	public static int chunkDepth = 100;
	public ChunkPos pos;
	public string biomeName;
	public Point4D features;

	// Draw Flags
	private bool xPlusDrawn = false;
	private bool zPlusDrawn = false;
	private bool xMinusDrawn = false;
	private bool zMinusDrawn = false;
	public bool drawMain = false;

	// Unity Settings
	public ChunkRenderer renderer;
	public MeshFilter meshFilter;
	public MeshCollider meshCollider;
	public GameObject obj = new GameObject();
	public BlockEncyclopedia blockBook;
	public ChunkLoader loader;

	// Cache Information
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> transparentTris = new List<int>();
    private List<int> liquidTris = new List<int>();
    private List<int> triangles = new List<int>();
    private List<Vector2> UVs = new List<Vector2>();
    private Mesh mesh;

	public Chunk(ChunkPos pos, ChunkRenderer r, BlockEncyclopedia be, ChunkLoader loader){
		this.pos = pos;
		this.obj.name = "Chunk " + pos.x + ", " + pos.z;
		this.renderer = r;
		this.obj.transform.SetParent(this.renderer.transform);
		this.obj.transform.position = new Vector3(pos.x * chunkWidth, 0f, pos.z * chunkWidth);
		this.loader = loader;

		this.metadata = new VoxelMetadata(); // May change when chunk loads

		this.obj.AddComponent<MeshFilter>();
		this.obj.AddComponent<MeshRenderer>();
		this.obj.AddComponent<MeshCollider>();
		this.meshFilter = this.obj.GetComponent<MeshFilter>();
		this.meshCollider = this.obj.GetComponent<MeshCollider>();
		this.obj.GetComponent<MeshRenderer>().materials = this.renderer.GetComponent<MeshRenderer>().materials;
		this.blockBook = be;
		this.obj.layer = 8;
	}

	public void BuildOnVoxelData(VoxelData vd){
		this.data = vd;
	}

	// Build the X- or Z- chunk border
	public void BuildSideBorder(bool reload=false, bool reloadXM=false, bool reloadXm=false, bool reloadZM=false, bool reloadZm=false){
		int thisBlock;
		int neighborBlock;
		int meshVertCount = this.meshFilter.sharedMesh.vertices.Length;

		if(reload){
			this.xMinusDrawn = false;
			this.xPlusDrawn = false;
			this.zMinusDrawn = false;
			this.zPlusDrawn = false;
		}
		if(reloadXM)
			this.xPlusDrawn = false;
		if(reloadXm)
			this.xMinusDrawn = false;
		if(reloadZM)
			this.zPlusDrawn = false;
		if(reloadZm)
			this.zMinusDrawn = false;


		// X- Side analysis

		ChunkPos targetChunk = new ChunkPos(this.pos.x-1, this.pos.z);

		// Stop immediately if it's a final chunk
		if(loader.chunks.ContainsKey(targetChunk) && !xMinusDrawn){

			this.xMinusDrawn = true;

			for(int y=0; y<data.GetHeight(); y++){
				for(int z=0; z<data.GetDepth(); z++){
					thisBlock = data.GetCell(0,y,z);
					neighborBlock = loader.chunks[targetChunk].data.GetCell(chunkWidth-1, y, z);

					// Air handler
					if(thisBlock == 0)
						continue;

					// Water Handler
					if(blockBook.Get(thisBlock).liquid && blockBook.Get(neighborBlock).liquid)
						continue;

					// If should write
					if(blockBook.Get(neighborBlock).transparent || blockBook.Get(neighborBlock).invisible || blockBook.Get(neighborBlock).liquid){

						// Full block handler
						if(thisBlock > 0){

			    			// Handling Liquid and non-liquid blocks
			    			if(!blockBook.blocks[thisBlock].liquid){
			    				vertices.AddRange(CubeMeshData.faceVertices(Direction.West, 0.5f, new Vector3(0,y,z)));
			    				int vCount = meshVertCount + vertices.Count;
					    		UVs.AddRange(blockBook.blocks[thisBlock].AddTexture(Direction.West));
						    	triangles.Add(vCount -4);
						    	triangles.Add(vCount -4 +1);
						    	triangles.Add(vCount -4 +2);
						    	triangles.Add(vCount -4);
						    	triangles.Add(vCount -4 +2);
						    	triangles.Add(vCount -4 +3);
			    			}
					    	else{
			    				vertices.AddRange(LiquidMeshData.VertsByState((int)Direction.West, this.metadata.GetMetadata(0,y,z).state, new Vector3(0,y,z)));
			    				int vCount = meshVertCount + vertices.Count;
					    		UVs.AddRange(blockBook.blocks[thisBlock].LiquidTexture(0, z));
						    	liquidTris.Add(vCount -4);
						    	liquidTris.Add(vCount -4 +1);
						    	liquidTris.Add(vCount -4 +2);
						    	liquidTris.Add(vCount -4);
						    	liquidTris.Add(vCount -4 +2);
						    	liquidTris.Add(vCount -4 +3);							    	
					    	}							
						}
						// If it's an asset
						else{
			    			int vCount = meshVertCount + vertices.Count;

			    			// If block has special Rotation Rules
			    			if(blockBook.objects[(thisBlock*-1)-1].needsRotation)
			    				vertices.AddRange(blockBook.objects[(thisBlock*-1)-1].ToWorldSpace(new Vector3(0,y,z), blockBook.objects[(thisBlock*-1)-1].ApplyRotation(this, 0,y,z)));
			    			else
								vertices.AddRange(blockBook.objects[(thisBlock*-1)-1].ToWorldSpace(new Vector3(0,y,z)));

			    			foreach(int tri in blockBook.objects[(thisBlock*-1)-1].mesh.triangles)
			    				transparentTris.Add(tri + vCount);

			    			UVs.AddRange(blockBook.objects[(thisBlock*-1)-1].mesh.uv);					
						}
					}
				}
			}
		}
				
			
		// X+ Side analysis

		targetChunk = new ChunkPos(this.pos.x+1, this.pos.z);

		// Stop immediately if it's a final chunk
		if(loader.chunks.ContainsKey(targetChunk) && !xPlusDrawn){

			this.xPlusDrawn = true;

			for(int y=0; y<data.GetHeight(); y++){
				for(int z=0; z<data.GetDepth(); z++){
					thisBlock = data.GetCell(chunkWidth-1,y,z);
					neighborBlock = loader.chunks[targetChunk].data.GetCell(0, y, z);

					// Air handler
					if(thisBlock == 0)
						continue;

					// Water Handler
					if(blockBook.Get(thisBlock).liquid && blockBook.Get(neighborBlock).liquid)
						continue;

					// If should write
					if(blockBook.Get(neighborBlock).transparent || blockBook.Get(neighborBlock).invisible || blockBook.Get(neighborBlock).liquid){

						// Full block handler
						if(thisBlock >= 0){

			    			// Handling Liquid and non-liquid blocks
			    			if(!blockBook.blocks[thisBlock].liquid){
				    			vertices.AddRange(CubeMeshData.faceVertices(Direction.East, 0.5f, new Vector3(chunkWidth-1,y,z)));
				    			int vCount = meshVertCount + vertices.Count;
					    		UVs.AddRange(blockBook.blocks[thisBlock].AddTexture(Direction.East));
						    	triangles.Add(vCount -4);
						    	triangles.Add(vCount -4 +1);
						    	triangles.Add(vCount -4 +2);
						    	triangles.Add(vCount -4);
						    	triangles.Add(vCount -4 +2);
						    	triangles.Add(vCount -4 +3);
			    			}
					    	else{
			    				vertices.AddRange(LiquidMeshData.VertsByState((int)Direction.East, this.metadata.GetMetadata(chunkWidth-1,y,z).state, new Vector3(chunkWidth-1,y,z)));
			    				int vCount = meshVertCount + vertices.Count;
					    		UVs.AddRange(blockBook.blocks[thisBlock].LiquidTexture(chunkWidth-1, z));
						    	liquidTris.Add(vCount -4);
						    	liquidTris.Add(vCount -4 +1);
						    	liquidTris.Add(vCount -4 +2);
						    	liquidTris.Add(vCount -4);
						    	liquidTris.Add(vCount -4 +2);
						    	liquidTris.Add(vCount -4 +3);							    	
					    	}							
						}
						// If it's an asset
						else{
			    			int vCount = meshVertCount + vertices.Count;

			    			// If block has special Rotation Rules
			    			if(blockBook.objects[(thisBlock*-1)-1].needsRotation)
			    				vertices.AddRange(blockBook.objects[(thisBlock*-1)-1].ToWorldSpace(new Vector3(chunkWidth-1,y,z), blockBook.objects[(thisBlock*-1)-1].ApplyRotation(this, chunkWidth-1,y,z)));
			    			else
								vertices.AddRange(blockBook.objects[(thisBlock*-1)-1].ToWorldSpace(new Vector3(chunkWidth-1,y,z)));

			    			foreach(int tri in blockBook.objects[(thisBlock*-1)-1].mesh.triangles)
			    				transparentTris.Add(tri + vCount);

			    			UVs.AddRange(blockBook.objects[(thisBlock*-1)-1].mesh.uv);					
						}
					}
				}
			}
		}
		

		// If the side being analyzed is the Z- Side

		targetChunk = new ChunkPos(this.pos.x, this.pos.z-1);
		// Stop immediately if it's a final chunk
		if(loader.chunks.ContainsKey(targetChunk) && !zMinusDrawn){

			this.zMinusDrawn = true;

			for(int y=0; y<data.GetHeight(); y++){
				for(int x=0; x<data.GetDepth(); x++){
					thisBlock = data.GetCell(x,y,0);
					neighborBlock = loader.chunks[targetChunk].data.GetCell(x, y, chunkWidth-1);

					// Air handler
					if(thisBlock == 0)
						continue;

					// Water Handler
					if(blockBook.Get(thisBlock).liquid && blockBook.Get(neighborBlock).liquid)
						continue;
						
					// If should write
					if(blockBook.Get(neighborBlock).transparent || blockBook.Get(neighborBlock).invisible || blockBook.Get(neighborBlock).liquid){

						// Full block handler
						if(thisBlock > 0){

			    			// Handling Liquid and non-liquid blocks
			    			if(!blockBook.blocks[thisBlock].liquid){
			    				vertices.AddRange(CubeMeshData.faceVertices(Direction.South, 0.5f, new Vector3(x,y,0)));
			    				int vCount = meshVertCount + vertices.Count;
					    		UVs.AddRange(blockBook.blocks[thisBlock].AddTexture(Direction.South));
						    	triangles.Add(vCount -4);
						    	triangles.Add(vCount -4 +1);
						    	triangles.Add(vCount -4 +2);
						    	triangles.Add(vCount -4);
						    	triangles.Add(vCount -4 +2);
						    	triangles.Add(vCount -4 +3);
			    			}
					    	else{
			    				vertices.AddRange(LiquidMeshData.VertsByState((int)Direction.South, this.metadata.GetMetadata(x,y,0).state, new Vector3(x,y,0)));
			    				int vCount = meshVertCount + vertices.Count;
					    		UVs.AddRange(blockBook.blocks[thisBlock].LiquidTexture(x, 0));
						    	liquidTris.Add(vCount -4);
						    	liquidTris.Add(vCount -4 +1);
						    	liquidTris.Add(vCount -4 +2);
						    	liquidTris.Add(vCount -4);
						    	liquidTris.Add(vCount -4 +2);
						    	liquidTris.Add(vCount -4 +3);							    	
					    	}							
						}
						// If it's an asset
						else{
			    			int vCount = meshVertCount + vertices.Count;

			    			// If block has special Rotation Rules
			    			if(blockBook.objects[(thisBlock*-1)-1].needsRotation)
			    				vertices.AddRange(blockBook.objects[(thisBlock*-1)-1].ToWorldSpace(new Vector3(x,y,0), blockBook.objects[(thisBlock*-1)-1].ApplyRotation(this, x,y,0)));
			    			else
								vertices.AddRange(blockBook.objects[(thisBlock*-1)-1].ToWorldSpace(new Vector3(x,y,0)));

			    			foreach(int tri in blockBook.objects[(thisBlock*-1)-1].mesh.triangles)
			    				transparentTris.Add(tri + vCount);

			    			UVs.AddRange(blockBook.objects[(thisBlock*-1)-1].mesh.uv);					
						}
					}
				}
			}
		}	

		// Z+ Side Analysis

		targetChunk = new ChunkPos(this.pos.x, this.pos.z+1);

		// Stop immediately if it's a final chunk
		if(loader.chunks.ContainsKey(targetChunk) && !zPlusDrawn){

			this.zPlusDrawn = true;

			for(int y=0; y<data.GetHeight(); y++){
				for(int x=0; x<data.GetDepth(); x++){
					thisBlock = data.GetCell(x,y,chunkWidth-1);
					neighborBlock = loader.chunks[targetChunk].data.GetCell(x, y, 0);

					// Air handler
					if(thisBlock == 0)
						continue;

					// Water Handler
					if(blockBook.Get(thisBlock).liquid && blockBook.Get(neighborBlock).liquid)
						continue;
						
					// If should write
					if(blockBook.Get(neighborBlock).transparent || blockBook.Get(neighborBlock).invisible || blockBook.Get(neighborBlock).liquid){

						// Full block handler
						if(thisBlock > 0){

			    			// Handling Liquid and non-liquid blocks
			    			if(!blockBook.blocks[thisBlock].liquid){
			    				vertices.AddRange(CubeMeshData.faceVertices(Direction.North, 0.5f, new Vector3(x,y,chunkWidth-1)));
			    				int vCount = meshVertCount + vertices.Count;
					    		UVs.AddRange(blockBook.blocks[thisBlock].AddTexture(Direction.North));
						    	triangles.Add(vCount -4);
						    	triangles.Add(vCount -4 +1);
						    	triangles.Add(vCount -4 +2);
						    	triangles.Add(vCount -4);
						    	triangles.Add(vCount -4 +2);
						    	triangles.Add(vCount -4 +3);
			    			}
					    	else{
			    				vertices.AddRange(LiquidMeshData.VertsByState((int)Direction.North, this.metadata.GetMetadata(x,y,chunkWidth-1).state, new Vector3(x,y,chunkWidth-1)));
			    				int vCount = meshVertCount + vertices.Count;
					    		UVs.AddRange(blockBook.blocks[thisBlock].LiquidTexture(x, chunkWidth-1));
						    	liquidTris.Add(vCount -4);
						    	liquidTris.Add(vCount -4 +1);
						    	liquidTris.Add(vCount -4 +2);
						    	liquidTris.Add(vCount -4);
						    	liquidTris.Add(vCount -4 +2);
						    	liquidTris.Add(vCount -4 +3);							    	
					    	}							
						}
						// If it's an asset
						else{
			    			int vCount = meshVertCount + vertices.Count;

			    			// If block has special Rotation Rules
			    			if(blockBook.objects[(thisBlock*-1)-1].needsRotation)
			    				vertices.AddRange(blockBook.objects[(thisBlock*-1)-1].ToWorldSpace(new Vector3(x,y,chunkWidth-1), blockBook.objects[(thisBlock*-1)-1].ApplyRotation(this, x,y,chunkWidth-1)));
			    			else
								vertices.AddRange(blockBook.objects[(thisBlock*-1)-1].ToWorldSpace(new Vector3(x,y,chunkWidth-1)));

			    			foreach(int tri in blockBook.objects[(thisBlock*-1)-1].mesh.triangles)
			    				transparentTris.Add(tri + vCount);

			    			UVs.AddRange(blockBook.objects[(thisBlock*-1)-1].mesh.uv);					
						}
					}
				}
			}
		}						

		// Only draw if there's something to draw
		if(vertices.Count > 0)
			AddToMesh();
		else{
			vertices.Clear();
    		triangles.Clear();
    		transparentTris.Clear();
    		liquidTris.Clear();
    		UVs.Clear();
		}
	}

	/*
	TODO: Remove x=0 and z=0 chunk side rendering and place it into a new function
		that receives the X- and Z- chunks and draws sides based on their side blocks

		Track the loading of the chunks in ChunkLoader WORKS
	*/

	// Builds the chunk mesh data excluding the X- and Z- chunk border
	public void BuildChunk(){
		int thisBlock;
		int neighborBlock;

    	for(int x=0; x<data.GetWidth(); x++){
    		for(int y=0; y<data.GetHeight(); y++){
    			for(int z=0; z<data.GetDepth(); z++){
    				thisBlock = data.GetCell(x,y,z);

    				// Special Conditions -------------

    				// If invisible block
	    			if(blockBook.Get(thisBlock).invisible){
	    				continue;
	    			}

	    			// --------------------------------

    				// If is a full block
    				if(thisBlock >= 0){

				    	for(int i=0; i<6; i++){
				    		neighborBlock = data.GetNeighbor(x, y, z, (Direction)i);

				    		
				    		// Chunk Border and floor culling here! ----------
				    		
				    		if((x == 0 && (int)Direction.West == i) || (z == 0 && (int)Direction.South == i)){
				    			continue;
				    		}
				    		if((x == chunkWidth-1 && (int)Direction.East == i) || (z == chunkWidth-1 && (int)Direction.North == i)){
				    			continue;
				    		}
				    		if(y == 0 && (int)Direction.Down == i){
				    			continue;
				    		}
				    		////////// -----------------------------------

				    		// If neighbor is a block
				    		if(neighborBlock >= 0){

				    			// Handles Liquid chunks
				    			if(blockBook.blocks[thisBlock].liquid && blockBook.blocks[neighborBlock].liquid)
				    				continue;

					    		if(blockBook.blocks[neighborBlock].transparent || blockBook.blocks[neighborBlock].invisible){

					    			// Handling Liquid and non-liquid blocks
					    			if(!blockBook.blocks[thisBlock].liquid){
					    				vertices.AddRange(CubeMeshData.faceVertices(i, 0.5f, new Vector3(x,y,z)));
					    				int vCount = vertices.Count;
							    		UVs.AddRange(blockBook.blocks[thisBlock].AddTexture((Direction)i));
								    	triangles.Add(vCount -4);
								    	triangles.Add(vCount -4 +1);
								    	triangles.Add(vCount -4 +2);
								    	triangles.Add(vCount -4);
								    	triangles.Add(vCount -4 +2);
								    	triangles.Add(vCount -4 +3);
					    			}
							    	else{
			    						vertices.AddRange(LiquidMeshData.VertsByState(i, this.metadata.GetMetadata(x,y,z).state, new Vector3(x,y,z)));
			    						int vCount = vertices.Count;
							    		UVs.AddRange(blockBook.blocks[thisBlock].LiquidTexture(x, z));
								    	liquidTris.Add(vCount -4);
								    	liquidTris.Add(vCount -4 +1);
								    	liquidTris.Add(vCount -4 +2);
								    	liquidTris.Add(vCount -4);
								    	liquidTris.Add(vCount -4 +2);
								    	liquidTris.Add(vCount -4 +3);							    	
							    	}
							    	


					    		}
					    	}
					    	// If neighbor is an asset
					    	else{
					    		neighborBlock = (neighborBlock * -1) - 1;

				    			// Handles Liquid chunks
				    			if(blockBook.blocks[thisBlock].liquid && blockBook.objects[neighborBlock].liquid)
				    				continue;

					    		if(blockBook.objects[neighborBlock].transparent || blockBook.objects[neighborBlock].invisible){

			    					// Handling Liquid and non-liquid blocks
					    			if(!blockBook.blocks[thisBlock].liquid){
							    		vertices.AddRange(CubeMeshData.faceVertices(i, 0.5f, new Vector3(x,y,z)));
							    		int vCount = vertices.Count;
								    	UVs.AddRange(blockBook.blocks[thisBlock].AddTexture((Direction)i));
								    	triangles.Add(vCount -4);
								    	triangles.Add(vCount -4 +1);
								    	triangles.Add(vCount -4 +2);
								    	triangles.Add(vCount -4);
								    	triangles.Add(vCount -4 +2);
								    	triangles.Add(vCount -4 +3);
							  		}
							  		else{
			    						vertices.AddRange(LiquidMeshData.VertsByState(i, this.metadata.GetMetadata(x,y,z).state, new Vector3(x,y,z)));
			    						int vCount = vertices.Count;
							    		UVs.AddRange(blockBook.blocks[thisBlock].LiquidTexture(x, z));
								    	liquidTris.Add(vCount -4);
								    	liquidTris.Add(vCount -4 +1);
								    	liquidTris.Add(vCount -4 +2);
								    	liquidTris.Add(vCount -4);
								    	liquidTris.Add(vCount -4 +2);
								    	liquidTris.Add(vCount -4 +3);							  			
							  		}
					    		}
					    	}					    		
					    }

				    }
				    // If is an object-type block
				    else{
				    	thisBlock = (thisBlock * -1) - 1;

				    	for(int i=0; i<6; i++){
				    		neighborBlock = data.GetNeighbor(x, y, z, (Direction)i);

				    		/*
				    		Z- and X- Chunk Border culling here
				    		*/
				    		if((x == 0 && (int)Direction.West == i) || (z == 0 && (int)Direction.South == i)){
				    			continue;
				    		}
				    		if((x == chunkWidth-1 && (int)Direction.East == i) || (z == chunkWidth-1 && (int)Direction.North == i)){
				    			continue;
				    		}

				    		// If is a full block
				    		if(neighborBlock >= 0){

				    			// Handles Liquid chunks
				    			if(blockBook.objects[thisBlock].liquid && blockBook.blocks[neighborBlock].liquid)
				    				continue;

					    		if(blockBook.blocks[neighborBlock].transparent || blockBook.blocks[neighborBlock].invisible){

					    			int vCount = vertices.Count;

					    			// If block has special Rotation Rules
					    			if(blockBook.objects[thisBlock].needsRotation)
					    				vertices.AddRange(blockBook.objects[thisBlock].ToWorldSpace(new Vector3(x,y,z), blockBook.objects[thisBlock].ApplyRotation(this, x,y,z)));
					    			else
										vertices.AddRange(blockBook.objects[thisBlock].ToWorldSpace(new Vector3(x,y,z)));

					    			foreach(int tri in blockBook.objects[thisBlock].mesh.triangles)
					    				transparentTris.Add(tri + vCount);

					    			UVs.AddRange(blockBook.objects[thisBlock].mesh.uv);
				    				break;
				    			}
				    		}

				    		// If is an object type block
				    		else{
					    		neighborBlock = (neighborBlock * -1) - 1;

				    			// Handles Liquid chunks
				    			if(blockBook.objects[thisBlock].liquid && blockBook.objects[neighborBlock].liquid)
				    				continue;

					    		if(blockBook.objects[neighborBlock].transparent || blockBook.objects[neighborBlock].invisible){
					    			int vCount = vertices.Count;

					    			// If block has special Rotation Rules
					    			if(blockBook.objects[thisBlock].needsRotation)
					    				vertices.AddRange(blockBook.objects[thisBlock].ToWorldSpace(new Vector3(x,y,z), blockBook.objects[thisBlock].ApplyRotation(this, x,y,z)));
					    			else
										vertices.AddRange(blockBook.objects[thisBlock].ToWorldSpace(new Vector3(x,y,z)));
					    			
					    			foreach(int tri in blockBook.objects[thisBlock].mesh.triangles)
					    				transparentTris.Add(tri + vCount);

					    			UVs.AddRange(blockBook.objects[thisBlock].mesh.uv);
				    				break;
				    			}				    			
				    		}
				    	}

				    }
	    		}
	    	}
    	}

		BuildMesh();
		drawMain = true;
    }

    // Builds meshes from verts, UVs and tris from different layers
    private void BuildMesh(){
    	mesh = new Mesh();
    	mesh.Clear();
    	mesh.subMeshCount = 3;

    	mesh.vertices = vertices.ToArray();
    	mesh.SetTriangles(triangles.ToArray(), 0);
    	this.meshCollider.sharedMesh = mesh;

    	mesh.SetTriangles(transparentTris.ToArray(), 1);
    	mesh.SetTriangles(liquidTris.ToArray(), 2);

    	mesh.uv = UVs.ToArray();
    	mesh.RecalculateNormals();

    	vertices.Clear();
    	triangles.Clear();
    	transparentTris.Clear();
    	liquidTris.Clear();
    	UVs.Clear();

    	this.meshFilter.sharedMesh = mesh;
    }

    // Adds verts, UVs and tris to meshes
    private void AddToMesh(){
    	List<Vector3> newVerts = new List<Vector3>();
    	List<int>[] newTris = {new List<int>(), new List<int>(), new List<int>()};
    	List<Vector2> newUVs = new List<Vector2>();
    	mesh = new Mesh();
    	mesh.subMeshCount = 3;

    	newVerts.AddRange(this.meshFilter.sharedMesh.vertices);
    	newTris[0].AddRange(this.meshFilter.sharedMesh.GetTriangles(0));
    	newTris[1].AddRange(this.meshFilter.sharedMesh.GetTriangles(1));
    	newTris[2].AddRange(this.meshFilter.sharedMesh.GetTriangles(2));
    	newUVs.AddRange(this.meshFilter.sharedMesh.uv);

    	newVerts.AddRange(vertices.ToArray());
    	newTris[0].AddRange(triangles.ToArray());
    	newTris[1].AddRange(transparentTris.ToArray());
    	newTris[2].AddRange(liquidTris.ToArray());
    	newUVs.AddRange(UVs.ToArray());

    	mesh.vertices = newVerts.ToArray();
    	mesh.SetTriangles(newTris[0].ToArray(), 0);
    	mesh.uv = newUVs.ToArray();

    	this.meshCollider.sharedMesh = mesh;

    	mesh.SetTriangles(newTris[1].ToArray(), 1);
    	mesh.SetTriangles(newTris[2].ToArray(), 2);

    	mesh.RecalculateNormals();

    	this.meshFilter.sharedMesh = mesh;

    	vertices.Clear();
    	triangles.Clear();
    	transparentTris.Clear();
    	liquidTris.Clear();
    	UVs.Clear();
    	
    }
}
