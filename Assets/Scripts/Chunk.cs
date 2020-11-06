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
	public byte needsGeneration;
	public Point4D features;
	public string lastVisitedTime;

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
	public GameObject obj;
	public BlockEncyclopedia blockBook;
	public ChunkLoader loader;
	public AssetGrid assetGrid;

	// Cache Information
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> specularTris = new List<int>();
    private List<int> liquidTris = new List<int>();
    private List<int> triangles = new List<int>();
    private List<Vector2> UVs = new List<Vector2>();
    private Mesh mesh;

	public Chunk(ChunkPos pos, ChunkRenderer r, BlockEncyclopedia be, ChunkLoader loader, bool fromMemory=false){
		this.pos = pos;
		this.needsGeneration = 0;
		this.assetGrid = new AssetGrid(this.pos);
		this.renderer = r;
		this.loader = loader;

		// Game Object Settings
		this.obj = new GameObject();
		this.obj.name = "Chunk " + pos.x + ", " + pos.z;
		this.obj.transform.SetParent(this.renderer.transform);
		this.obj.transform.position = new Vector3(pos.x * chunkWidth, 0f, pos.z * chunkWidth);


		if(fromMemory)
			this.data = new VoxelData();
		this.metadata = new VoxelMetadata();

		this.obj.AddComponent<MeshFilter>();
		this.obj.AddComponent<MeshRenderer>();
		this.obj.AddComponent<MeshCollider>();
		this.meshFilter = this.obj.GetComponent<MeshFilter>();
		this.meshCollider = this.obj.GetComponent<MeshCollider>();
		this.obj.GetComponent<MeshRenderer>().materials = this.renderer.GetComponent<MeshRenderer>().materials;
		this.blockBook = be;
		this.obj.layer = 8;
	}

	// Dummy Chunk Generation
	// CANNOT BE USED TO DRAW, ONLY TO ADD ELEMENTS AND SAVE
	public Chunk(ChunkPos pos){
		this.biomeName = "Plains";
		this.pos = pos;
		this.needsGeneration = 1;

		this.data = new VoxelData(new ushort[Chunk.chunkWidth, Chunk.chunkDepth, Chunk.chunkWidth]);

		this.metadata = new VoxelMetadata();
	}

	public void BuildOnVoxelData(VoxelData vd){
		this.data = vd;
	}

	public void BuildVoxelMetadata(VoxelMetadata vm){
		this.metadata = vm;
	}

	// Build the X- or Z- chunk border
	public void BuildSideBorder(bool reload=false, bool reloadXM=false, bool reloadXm=false, bool reloadZM=false, bool reloadZm=false){
		ushort thisBlock;
		ushort neighborBlock;
		bool skip;
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
					skip = false;
					thisBlock = data.GetCell(0,y,z);
					neighborBlock = loader.chunks[targetChunk].data.GetCell(chunkWidth-1, y, z);

					// Air handler
					if(thisBlock == 0)
						continue;

	    			// Handles Liquid chunks
	    			if(CheckLiquids(thisBlock, neighborBlock))
	    				continue;

	    			// Main Drawing Handling
		    		if(CheckPlacement(neighborBlock)){
				    	LoadMesh(0, y, z, (int)Direction.West, thisBlock, false, ref skip, lookahead:meshVertCount);
				    	if(skip)
				    		break;
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
					skip = false;
					thisBlock = data.GetCell(chunkWidth-1,y,z);
					neighborBlock = loader.chunks[targetChunk].data.GetCell(0, y, z);

					// Air handler
					if(thisBlock == 0)
						continue;

	    			// Handles Liquid chunks
	    			if(CheckLiquids(thisBlock, neighborBlock))
	    				continue;

	    			// Main Drawing Handling
		    		if(CheckPlacement(neighborBlock)){
				    	LoadMesh(chunkWidth-1, y, z, (int)Direction.East, thisBlock, false, ref skip, lookahead:meshVertCount);
				    	if(skip)
				    		break;
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
					skip = false;
					thisBlock = data.GetCell(x,y,0);
					neighborBlock = loader.chunks[targetChunk].data.GetCell(x, y, chunkWidth-1);

					// Air handler
					if(thisBlock == 0)
						continue;

	    			// Handles Liquid chunks
	    			if(CheckLiquids(thisBlock, neighborBlock))
	    				continue;

	    			// Main Drawing Handling
		    		if(CheckPlacement(neighborBlock)){
				    	LoadMesh(x, y, 0, (int)Direction.South, thisBlock, false, ref skip, lookahead:meshVertCount);
				    	if(skip)
				    		break;
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
					skip = false;
					thisBlock = data.GetCell(x,y,chunkWidth-1);
					neighborBlock = loader.chunks[targetChunk].data.GetCell(x, y, 0);

					// Air handler
					if(thisBlock == 0)
						continue;

	    			// Handles Liquid chunks
	    			if(CheckLiquids(thisBlock, neighborBlock))
	    				continue;

	    			// Main Drawing Handling
		    		if(CheckPlacement(neighborBlock)){
				    	LoadMesh(x, y, chunkWidth-1, (int)Direction.North, thisBlock, false, ref skip, lookahead:meshVertCount);
				    	if(skip)
				    		break;
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
    		specularTris.Clear();
    		liquidTris.Clear();
    		UVs.Clear();
		}
	}

	// Builds the chunk mesh data excluding the X- and Z- chunk border
	public void BuildChunk(bool load=false){
		ushort thisBlock;
		ushort neighborBlock;
		bool skip;

    	for(int x=0; x<data.GetWidth(); x++){
    		for(int y=0; y<data.GetHeight(); y++){
    			for(int z=0; z<data.GetDepth(); z++){
    				thisBlock = data.GetCell(x,y,z);
    				skip = false;

	    			// If air
	    			if(thisBlock == 0){
	    				continue;
	    			}

	    			// Runs OnLoad event
	    			if(load)
	    				// If is a block
		    			if(thisBlock <= ushort.MaxValue/2){
		    				blockBook.blocks[thisBlock].OnLoad(this.pos, x, y, z, loader);
		    			}
		    			// If Asset
		    			else{
							blockBook.objects[ushort.MaxValue - thisBlock].OnLoad(this.pos, x, y, z, loader);
		    			}

	    			// --------------------------------

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

		    			// Handles Liquid chunks
		    			if(CheckLiquids(thisBlock, neighborBlock))
		    				continue;

		    			// Main Drawing Handling
			    		if(CheckPlacement(neighborBlock)){
					    	LoadMesh(x, y, z, i, thisBlock, load, ref skip);

					    	if(skip)
					    		break;
			    		}	
				    } // faces loop
	    		} // z loop
	    	} // y loop
    	} // x loop

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

    	mesh.SetTriangles(specularTris.ToArray(), 1);
    	mesh.SetTriangles(liquidTris.ToArray(), 2);

    	mesh.uv = UVs.ToArray();
    	mesh.RecalculateNormals();

    	vertices.Clear();
    	triangles.Clear();
    	specularTris.Clear();
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
    	newTris[1].AddRange(specularTris.ToArray());
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
    	specularTris.Clear();
    	liquidTris.Clear();
    	UVs.Clear();
    }

    // Imports Mesh data and applies it to the chunk depending on the Renderer Thread
    // Load is true when Chunk is being loaded and not reloaded
    private void LoadMesh(int x, int y, int z, int dir, ushort blockCode, bool load, ref bool skip, int lookahead=0){
    	byte renderThread;

    	if(blockCode <= ushort.MaxValue/2)
    		renderThread = blockBook.blocks[blockCode].materialIndex;
    	else
    		renderThread = blockBook.objects[Convert(blockCode)].materialIndex;
    	
    	// If object is Normal Block
    	if(renderThread == 0){
			vertices.AddRange(CubeMeshData.faceVertices(dir, 0.5f, new Vector3(x,y,z)));
			int vCount = vertices.Count + lookahead;
    		UVs.AddRange(blockBook.blocks[blockCode].AddTexture((Direction)dir));
	    	triangles.Add(vCount -4);
	    	triangles.Add(vCount -4 +1);
	    	triangles.Add(vCount -4 +2);
	    	triangles.Add(vCount -4);
	    	triangles.Add(vCount -4 +2);
	    	triangles.Add(vCount -4 +3);    		
    	}

    	// If object is Specular Block
    	else if(renderThread == 1){
			vertices.AddRange(CubeMeshData.faceVertices(dir, 0.5f, new Vector3(x,y,z)));
			int vCount = vertices.Count + lookahead;
    		UVs.AddRange(blockBook.blocks[blockCode].AddTexture((Direction)dir));
	    	specularTris.Add(vCount -4);
	    	specularTris.Add(vCount -4 +1);
	    	specularTris.Add(vCount -4 +2);
	    	specularTris.Add(vCount -4);
	    	specularTris.Add(vCount -4 +2);
	    	specularTris.Add(vCount -4 +3);     		
    	}

    	// If object is Liquid
    	else if(renderThread == 2){
			vertices.AddRange(LiquidMeshData.VertsByState(dir, this.metadata.GetMetadata(x,y,z).state, new Vector3(x,y,z)));
			int vCount = vertices.Count + lookahead;
    		UVs.AddRange(blockBook.blocks[blockCode].LiquidTexture(x, z));
	    	liquidTris.Add(vCount -4);
	    	liquidTris.Add(vCount -4 +1);
	    	liquidTris.Add(vCount -4 +2);
	    	liquidTris.Add(vCount -4);
	    	liquidTris.Add(vCount -4 +2);
	    	liquidTris.Add(vCount -4 +3);	    		
    	}

    	// If object is an Asset
    	else{
    		if(load){
    			this.assetGrid.Add(x, y, z, blockCode, this.metadata.GetMetadata(x,y,z).state, loader);
    			skip = true;
    		}
    	}

    }

    // Checks if Liquids are side by side
    private bool CheckLiquids(int thisBlock, int neighborBlock){
    	bool thisLiquid;
    	bool neighborLiquid;


    	if(thisBlock <= ushort.MaxValue/2)
    		thisLiquid = blockBook.blocks[thisBlock].liquid;
    	else
    		thisLiquid = blockBook.objects[Convert(thisBlock)].liquid;

    	if(neighborBlock <= ushort.MaxValue/2)
    		neighborLiquid = blockBook.blocks[neighborBlock].liquid;
    	else
    		neighborLiquid = blockBook.objects[Convert(neighborBlock)].liquid;

    	return thisLiquid && neighborLiquid; 
    }

    // Checks if neighbor is transparent or invisible
    private bool CheckPlacement(int neighborBlock){
    	if(neighborBlock <= ushort.MaxValue/2)
    		return blockBook.blocks[neighborBlock].transparent || blockBook.blocks[neighborBlock].invisible;
    	else
			return blockBook.objects[Convert(neighborBlock)].transparent || blockBook.objects[Convert(neighborBlock)].invisible;
    }

    // Converts negative asset code to index position
    private int Convert(int code){
    	return ushort.MaxValue - code;
    }

    // Deletes all GameObject instances in AssetGrid
    public void Unload(){
    	this.assetGrid.Unload();
    }

}

/* Class that handles GameObjects in Chunk*/
public class AssetGrid{
	public Dictionary<Vector3, GameObject> grid;
	private ChunkPos pos;

	public AssetGrid(ChunkPos position){
		grid = new Dictionary<Vector3, GameObject>();
		pos = position;
	}

	// Adds a new GameObject instance to Grid
	public void Add(int x, int y, int z, ushort blockCode, ushort? state, ChunkLoader loader){
		Vector3 v = new Vector3(x,y,z);

		grid.Add(v, loader.blockBook.objects[ushort.MaxValue - blockCode].PlaceObject(this.pos, x, y, z, blockCode, loader));
	}

	// Adds and instantly draw element to Grid
	public void AddDraw(int x, int y, int z, ushort blockCode, ushort? state, ChunkLoader loader){
		Vector3 target = new Vector3(x,y,z);

		Add(x, y, z, blockCode, state, loader);
		grid[target].SetActive(true);	
	}

	// Gets the GO in Grid
	public GameObject Get(int x, int y, int z){
		return grid[new Vector3(x, y, z)];
	}

	// Removes the GO in Grid
	public void Remove(int x, int y, int z){
		Vector3 target = new Vector3(x, y, z);

		if(grid.ContainsKey(target)){
			GameObject.Destroy(grid[target]);
			grid.Remove(target);
		}
	}

	// Instantiates an element in AssetGrid
	public void Draw(int x, int y, int z){
		Vector3 target = new Vector3(x, y, z);

		if(grid.ContainsKey(target)){
			grid[target].SetActive(true);
		}
	}

	// Instantiates all elements in AssetGrid
	public void DrawAll(){
		foreach(GameObject go in grid.Values){
			go.SetActive(true);
		}
	}

	// Deletes all instantiation of GOs in AssetGrid
	public void Unload(){
		foreach(GameObject go in grid.Values){
			GameObject.Destroy(go);
		}
	}

}

