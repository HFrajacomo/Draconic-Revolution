using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;

[BurstCompile]
public struct BuildChunkJob : IJob{
	[ReadOnly]
	public bool load;
	[ReadOnly]
	public ChunkPos pos;

	[ReadOnly]
	public NativeArray<ushort> data; // Voxeldata
	[ReadOnly]
	public NativeArray<ushort> state; // VoxelMetadata.state
	[ReadOnly]
	public NativeArray<byte> lightdata;
	[ReadOnly]
	public NativeArray<byte> renderMap;

	// OnLoad Event Trigger List
	public NativeList<int3> loadOutList;
	public NativeList<int3> loadAssetList;

	// Rendering Primitives
	public NativeList<Vector3> verts;
	public NativeList<Vector2> UVs;
	public NativeList<Vector2> lightUV;
	public NativeList<Vector3> normals;

	// Render Thread Triangles
	public NativeList<int> normalTris;
	public NativeList<int> specularTris;
	public NativeList<int> liquidTris;
	public NativeList<int> leavesTris;
	public NativeList<int> iceTris;

	// Cache
	public NativeArray<Vector3> cacheCubeVert;
	public NativeArray<Vector2> cacheCubeUV;
	public NativeArray<Vector3> cacheCubeNormal;

	// Block Encyclopedia Data
	[ReadOnly]
	public NativeArray<byte> blockTransparent;
	[ReadOnly]
	public NativeArray<byte> objectTransparent;
	[ReadOnly]
	public NativeArray<bool> blockSeamless;
	[ReadOnly]
	public NativeArray<bool> objectSeamless;
	[ReadOnly]
	public NativeArray<bool> blockLoad;
	[ReadOnly]
	public NativeArray<bool> objectLoad;
	[ReadOnly]
	public NativeArray<bool> blockInvisible;
	[ReadOnly]
	public NativeArray<bool> objectInvisible;
	[ReadOnly]
	public NativeArray<ShaderIndex> blockMaterial;
	[ReadOnly]
	public NativeArray<ShaderIndex> objectMaterial;
	[ReadOnly]
	public NativeArray<int3> blockTiles;
	[ReadOnly]
	public NativeArray<bool> blockWashable;
	[ReadOnly]
	public NativeArray<bool> objectWashable;
	[ReadOnly]
	public NativeArray<bool> blockDrawRegardless;


	// Builds the chunk mesh data excluding the X- and Z- chunk border
	public void Execute(){
		ushort thisBlock;
		ushort neighborBlock;
		ushort neighborState;
		ushort thisState;
		bool isBlock;
		bool isTransparent;
		int ii;
		int3 c;

		for(int x=0; x<Chunk.chunkWidth; x++){
			for(int z=0; z<Chunk.chunkWidth; z++){
				for(int y=renderMap[x*Chunk.chunkWidth+z]; y >= 0; y--){
					thisBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					thisState = state[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					isBlock = thisBlock <= ushort.MaxValue/2;

	    			// Runs OnLoad event
	    			if(load){
	    				// If is a block
		    			if(isBlock){
		    				if(blockLoad[thisBlock] && !blockSeamless[thisBlock]){
		    					loadOutList.Add(new int3(x,y,z));
		    				}
		    			}
		    			// If Asset
		    			else{
		    				if(objectLoad[ushort.MaxValue-thisBlock] && !objectSeamless[ushort.MaxValue-thisBlock]){
		    					loadOutList.Add(new int3(x,y,z));
		    				}
		    			}
		    		}

		    		// Object
		    		if(!isBlock){
		    			LoadMesh(x, y, z, -1, thisBlock, load, cacheCubeVert, cacheCubeUV, cacheCubeNormal);
		    		}

		    		// Transparency
		    		if(isBlock){
		    			isTransparent = blockTransparent[thisBlock] == 1 || blockDrawRegardless[thisBlock];
		    		}
		    		else{
		    			isTransparent = objectTransparent[ushort.MaxValue - thisBlock] == 1;
		    		}

		    		if(isTransparent){
				    	for(int i=0; i<6; i++){
				    		neighborBlock = GetNeighbor(x, y, z, i);
				    		neighborState = GetNeighborState(x, y, z, i);
				    		c = GetCoords(x, y, z, i);
				    		isBlock = neighborBlock <= ushort.MaxValue/2;
				    		ii = InvertDir(i);

				    		if(neighborBlock == 0)
				    			continue;
				    		
				    		// Chunk Border and floor culling here! ----------	
			    			// If Corner
				    		if(c.x >= Chunk.chunkWidth || c.x < 0 || c.z >= Chunk.chunkWidth || c.z < 0)
				    			break;

			    			if((c.x == 0 || c.x == Chunk.chunkWidth-1) && (c.z == 0 || c.z == Chunk.chunkWidth-1))
			    				continue;

				    		if((c.x == 0 && (ii != 1)) || (c.z == 0 && (ii != 0)))
				    			continue;

				    		if((c.x == Chunk.chunkWidth-1 && (ii != 3)) || (c.z == Chunk.chunkWidth-1 && (ii != 2)))
				    			continue;

				    		if(c.y == 0 && ii == 5)
				    			continue;

				    		if(c.y == Chunk.chunkDepth-1 && ii == 4){
				    			continue;
				    		}

				    		if(!isBlock)
				    			continue;


				    		////////// -----------------------------------

							// Handles Liquid chunks
			    			if(blockSeamless[neighborBlock]){
				    			if(CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
				    				continue;
				    			}
			    			}

						    LoadMesh(c.x, c.y, c.z, ii, neighborBlock, load, cacheCubeVert, cacheCubeUV, cacheCubeNormal);
					    } // faces loop
					}
	    		} // y loop
	    	} // z loop
	    } // x loop
    }

    private int InvertDir(int i){
    	if(i == 0)
    		return 2;
    	if(i == 1)
    		return 3;
    	if(i == 2)
    		return 0;
    	if(i == 3)
    		return 1;
    	if(i == 4)
    		return 5;
    	if(i == 5)
    		return 4;
    	return 0;
    }

    // Gets neighbor element
	private ushort GetNeighbor(int x, int y, int z, int dir){
		int3 neighborCoord = new int3(x, y, z) + VoxelData.offsets[dir];
		
		if(neighborCoord.x < 0 || neighborCoord.x >= Chunk.chunkWidth || neighborCoord.z < 0 || neighborCoord.z >= Chunk.chunkWidth || neighborCoord.y < 0 || neighborCoord.y >= Chunk.chunkDepth){
			return 0;
		}

		return data[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z];
	}

	private int3 GetCoords(int x, int y, int z, int dir){
		return new int3(x, y, z) + VoxelData.offsets[dir];
	}

    // Gets neighbor state
	private ushort GetNeighborState(int x, int y, int z, int dir){
		int3 neighborCoord = new int3(x, y, z) + VoxelData.offsets[dir];
		
		if(neighborCoord.x < 0 || neighborCoord.x >= Chunk.chunkWidth || neighborCoord.z < 0 || neighborCoord.z >= Chunk.chunkWidth || neighborCoord.y < 0 || neighborCoord.y >= Chunk.chunkDepth){
			return 0;
		} 

		return state[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z];
	}

	// Gets neighbor light level
	private int GetNeighborLight(int x, int y, int z, int dir, bool isNatural=true){
		int3 coord = new int3(x, y, z) + VoxelData.offsets[dir];

		if(coord.y >= Chunk.chunkDepth || coord.y < 0){
			if(isNatural)
				return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
			else
				return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
		}

		if(isNatural){
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
		}
		else
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] >> 4;
	}

	// Gets neighbor light level
	private int GetNeighborLight(int x, int y, int z, int3 dir, bool isNatural=true){
		int3 coord = new int3(x, y, z) + dir;

		if(coord.y >= Chunk.chunkDepth || coord.y < 0){
			if(isNatural)
				return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
			else
				return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
		}

		if(isNatural)
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
		else
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] >> 4;
	}

    // Checks if neighbor is transparent or invisible
    private bool CheckPlacement(int neighborBlock){
    	if(neighborBlock <= ushort.MaxValue/2)
    		return (Boolean(blockTransparent[neighborBlock]) || blockInvisible[neighborBlock]);
    	else
			return (Boolean(objectTransparent[ushort.MaxValue-neighborBlock]) || objectInvisible[ushort.MaxValue-neighborBlock]);
    }

    // Checks if seamlesses are side by side
    private bool CheckSeams(int thisBlock, int neighborBlock, ushort thisState, ushort neighborState){
    	bool thisSeamless;
    	bool neighborSeamless;


    	if(thisBlock <= ushort.MaxValue/2)
    		thisSeamless = blockSeamless[thisBlock];
    	else
    		thisSeamless = objectSeamless[ushort.MaxValue-thisBlock];

    	if(neighborBlock <= ushort.MaxValue/2)
    		neighborSeamless = blockSeamless[neighborBlock];
    	else
    		neighborSeamless = objectSeamless[ushort.MaxValue-neighborBlock];

    	return thisSeamless && neighborSeamless && (thisBlock == neighborBlock) && thisState == neighborState;
    }

    private bool Boolean(byte a){
    	if(a == 0)
    		return false;
    	return true;
    }


    // Imports Mesh data and applies it to the chunk depending on the Renderer Thread
    // Load is true when Chunk is being loaded and not reloaded
    // Returns true if loaded a blocktype mesh and false if it's an asset to be loaded later
    private bool LoadMesh(int x, int y, int z, int dir, ushort blockCode, bool load, NativeArray<Vector3> cacheCubeVert, NativeArray<Vector2> cacheCubeUV, NativeArray<Vector3> cacheCubeNormal, int lookahead=0){
    	ShaderIndex renderThread;

    	if(blockCode <= ushort.MaxValue/2)
    		renderThread = blockMaterial[blockCode];
    	else
    		renderThread = objectMaterial[ushort.MaxValue-blockCode];
    	
    	// If object is Normal Block
    	if(renderThread == ShaderIndex.OPAQUE){
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
    		UVs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);
    		
	    	normalTris.Add(vCount -4);
	    	normalTris.Add(vCount -4 +1);
	    	normalTris.Add(vCount -4 +2);
	    	normalTris.Add(vCount -4);
	    	normalTris.Add(vCount -4 +2);
	    	normalTris.Add(vCount -4 +3); 
	    	
	    	return true;
    	}

    	// If object is Specular Block
    	else if(renderThread == ShaderIndex.SPECULAR){
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
    		UVs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

	    	specularTris.Add(vCount -4);
	    	specularTris.Add(vCount -4 +1);
	    	specularTris.Add(vCount -4 +2);
	    	specularTris.Add(vCount -4);
	    	specularTris.Add(vCount -4 +2);
	    	specularTris.Add(vCount -4 +3);
	    	
	    	return true;   		
    	}

    	// If object is Liquid
    	else if(renderThread == ShaderIndex.WATER){
    		VertsByState(cacheCubeVert, dir, state[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z], new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			LiquidTexture(cacheCubeUV, x, z);
    		UVs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);
    		
	    	liquidTris.Add(vCount -4);
	    	liquidTris.Add(vCount -4 +1);
	    	liquidTris.Add(vCount -4 +2);
	    	liquidTris.Add(vCount -4);
	    	liquidTris.Add(vCount -4 +2);
	    	liquidTris.Add(vCount -4 +3);

	    	return true;    		
    	}

    	// If object is Leaves
    	else if(renderThread == ShaderIndex.LEAVES){
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
			UVs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

	    	leavesTris.Add(vCount -4);
	    	leavesTris.Add(vCount -4 +1);
	    	leavesTris.Add(vCount -4 +2);
	    	leavesTris.Add(vCount -4);
	    	leavesTris.Add(vCount -4 +2);
	    	leavesTris.Add(vCount -4 +3);

	    	return true;
    	}

    	// If object is Ice
    	else if(renderThread == ShaderIndex.ICE){
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
			UVs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

	    	iceTris.Add(vCount -4);
	    	iceTris.Add(vCount -4 +1);
	    	iceTris.Add(vCount -4 +2);
	    	iceTris.Add(vCount -4);
	    	iceTris.Add(vCount -4 +2);
	    	iceTris.Add(vCount -4 +3);

	    	return true;
    	}

    	// If object is an Asset
    	else{
			loadAssetList.Add(new int3(x,y,z));
    		return false;
    	}
    }

    // Sets the secondary UV of Lightmaps
    private void AddLightUV(NativeArray<Vector2> array, int x, int y, int z, int dir){
    	int maxLightLevel = 15;
    	int currentLightLevel = GetNeighborLight(x, y, z, dir);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector2(maxLightLevel, 1);
	    	array[1] = new Vector2(maxLightLevel, 1);
	    	array[2] = new Vector2(maxLightLevel, 1);
	    	array[3] = new Vector2(maxLightLevel, 1);
	    	return;
    	}

    	int3 auxPos = new int3(x,y,z) + VoxelData.offsets[dir];

    	CalculateLightCorners(auxPos, dir, array, currentLightLevel);

    }

    // Sets the secondary UV of ExtraLights Lightmaps
    private void AddLightUVExtra(NativeArray<Vector2> array, int x, int y, int z, int dir){
    	int maxLightLevel = 15;
    	int currentLightLevel = GetNeighborLight(x, y, z, dir, isNatural:false);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector2(array[0].x, maxLightLevel);
	    	array[1] = new Vector2(array[1].x, maxLightLevel);
	    	array[2] = new Vector2(array[2].x, maxLightLevel);
	    	array[3] = new Vector2(array[3].x, maxLightLevel);
	    	return;
    	}

    	int3 auxPos = new int3(x,y,z) + VoxelData.offsets[dir];

    	CalculateLightCornersExtra(auxPos, dir, array, currentLightLevel);
    }

    private void CalculateLightCorners(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel){
    	// North
    	if(dir == 0)
    		SetCorner(array, pos, currentLightLevel, 1, 4, 3, 5, 0);
    	// East
    	else if(dir == 1)
    		SetCorner(array, pos, currentLightLevel, 2, 4, 0, 5, 1);
    	// South
     	else if(dir == 2)
    		SetCorner(array, pos, currentLightLevel, 3, 4, 1, 5, 2);
    	// West
      	else if(dir == 3)
    		SetCorner(array, pos, currentLightLevel, 0, 4, 2, 5, 3);
      	// Up
    	else if(dir == 4)
    		SetCorner(array, pos, currentLightLevel, 1, 2, 3, 0, 4);
    	// Down
     	else
     		SetCorner(array, pos, currentLightLevel, 1, 0, 3, 2, 5);
    }

    private void CalculateLightCornersExtra(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel){
    	// North
    	if(dir == 0)
    		SetCornerExtra(array, pos, currentLightLevel, 1, 4, 3, 5, 0);
    	// East
    	else if(dir == 1)
    		SetCornerExtra(array, pos, currentLightLevel, 2, 4, 0, 5, 1);
    	// South
     	else if(dir == 2)
    		SetCornerExtra(array, pos, currentLightLevel, 3, 4, 1, 5, 2);
    	// West
      	else if(dir == 3)
    		SetCornerExtra(array, pos, currentLightLevel, 0, 4, 2, 5, 3);
      	// Up
    	else if(dir == 4)
    		SetCornerExtra(array, pos, currentLightLevel, 1, 2, 3, 0, 4);
    	// Down
     	else
    		SetCornerExtra(array, pos, currentLightLevel, 1, 0, 3, 2, 5);
    }

    private void SetCorner(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);

		light1 = GetNeighborLight(pos.x, pos.y, pos.z, dir1, isNatural:true);
		light2 = GetNeighborLight(pos.x, pos.y, pos.z, dir2, isNatural:true);
		light3 = GetNeighborLight(pos.x, pos.y, pos.z, dir3, isNatural:true);
		light4 = GetNeighborLight(pos.x, pos.y, pos.z, dir4, isNatural:true);

		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
		light5 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
		light6 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
		light7 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
		light8 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);

		array[0] = new Vector2(Max(light1, light2, light5, currentLightLevel), 1);
		array[1] = new Vector2(Max(light2, light3, light6, currentLightLevel), 1);
		array[2] = new Vector2(Max(light3, light4, light7, currentLightLevel), 1);
		array[3] = new Vector2(Max(light4, light1, light8, currentLightLevel), 1);
    }

    private void SetCornerExtra(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);

		light1 = GetNeighborLight(pos.x, pos.y, pos.z, dir1, isNatural:false);
		light2 = GetNeighborLight(pos.x, pos.y, pos.z, dir2, isNatural:false);
		light3 = GetNeighborLight(pos.x, pos.y, pos.z, dir3, isNatural:false);
		light4 = GetNeighborLight(pos.x, pos.y, pos.z, dir4, isNatural:false);

		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
		light5 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
		light6 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
		light7 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
		light8 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);



		array[0] = new Vector2(array[0].x, Max(light1, light2, light5, currentLightLevel));
		array[1] = new Vector2(array[1].x, Max(light2, light3, light6, currentLightLevel));
		array[2] = new Vector2(array[2].x, Max(light3, light4, light7, currentLightLevel));
		array[3] = new Vector2(array[3].x, Max(light4, light1, light8, currentLightLevel));
    }

    /*
    Returns the maximum between light levels
    */
    private int Max(int a, int b, int c, int d){
    	int maximum = a;

    	if(maximum - b < 0)
    		maximum = b;
    	if(maximum - c < 0)
    		maximum = c;
    	if(maximum - d < 0)
    		maximum = d;
    	return maximum;
    }

	// Sets UV mapping for a direction
	private void AddTexture(NativeArray<Vector2> array, int dir, ushort blockCode){
		int textureID;

		if(dir == 4)
			textureID = blockTiles[blockCode].x;
		else if(dir == 5)
			textureID = blockTiles[blockCode].y;
		else
			textureID = blockTiles[blockCode].z;

		// If should use normal atlas
		if(blockMaterial[blockCode] == ShaderIndex.OPAQUE){
			float x = textureID%Blocks.atlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.atlasSizeX);
	 
			x *= 1f / Blocks.atlasSizeX;
			y *= 1f / Blocks.atlasSizeY;

			array[0] = new Vector2(x,y+(1f/Blocks.atlasSizeY));
			array[1] = new Vector2(x+(1f/Blocks.atlasSizeX),y+(1f/Blocks.atlasSizeY));
			array[2] = new Vector2(x+(1f/Blocks.atlasSizeX),y);
			array[3] = new Vector2(x,y);
		}
		// If should use transparent atlas
		else if(blockMaterial[blockCode] == ShaderIndex.SPECULAR){
			float x = textureID%Blocks.transparentAtlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.transparentAtlasSizeX);
	 
			x *= 1f / Blocks.transparentAtlasSizeX;
			y *= 1f / Blocks.transparentAtlasSizeY;

			array[0] = new Vector2(x,y+(1f/Blocks.transparentAtlasSizeY));
			array[1] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y+(1f/Blocks.transparentAtlasSizeY));
			array[2] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y);
			array[3] = new Vector2(x,y);
		}
		// If should use Leaves atlas
		else if(blockMaterial[blockCode] == ShaderIndex.LEAVES){
			float x = textureID%Blocks.transparentAtlasSizeX;
			float y = Mathf.FloorToInt(textureID/Blocks.transparentAtlasSizeX);
	 
			x *= 1f / Blocks.transparentAtlasSizeX;
			y *= 1f / Blocks.transparentAtlasSizeY;

			array[0] = new Vector2(x,y+(1f/Blocks.transparentAtlasSizeY));
			array[1] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y+(1f/Blocks.transparentAtlasSizeY));
			array[2] = new Vector2(x+(1f/Blocks.transparentAtlasSizeX),y);
			array[3] = new Vector2(x,y);
		}
	}

	// Gets UV Map for Liquid blocks
	private void LiquidTexture(NativeArray<Vector2> array, int x, int z){
		int size = Chunk.chunkWidth;
		int tileSize = 1/size;

		array[0] = new Vector2(x*tileSize,z*tileSize);
		array[1] = new Vector2(x*tileSize,(z+1)*tileSize);
		array[2] = new Vector2((x+1)*tileSize,(z+1)*tileSize);
		array[3] = new Vector2((x+1)*tileSize,z*tileSize);
	}

	// Cube Mesh Data get verts
	public static void faceVertices(NativeArray<Vector3> fv, int dir, float scale, Vector3 pos)
	{
		for (int i = 0; i < fv.Length; i++)
		{
			fv[i] = (CubeMeshData.vertices[CubeMeshData.faceTriangles[dir*4+i]] * scale) + pos;
		}
	}

	// Gets the vertices of a given state in a liquid
	
	public static void VertsByState(NativeArray<Vector3> fv, int dir, ushort s, Vector3 pos, float scale=0.5f){
        if(s == ushort.MaxValue)
            s = 0;

		if(s == 19 || s == 20 || s == 21){
		    for (int i = 0; i < fv.Length; i++)
		    {
		    	fv[i] = (LiquidMeshData.verticesOnState[LiquidMeshData.faceTriangles[dir*4+i]] * scale) + pos;
		    }
		}
		else{
		    for (int i = 0; i < fv.Length; i++)
		    {
		    	fv[i] = (LiquidMeshData.verticesOnState[((int)s*8)+ LiquidMeshData.faceTriangles[dir*4+i]] * scale) + pos;
		    }
		}
	}


	public void CalculateNormal(NativeArray<Vector3> normals, int dir){
		Vector3 normal;

		if(dir == 0)
			normal = new Vector3(0, 0, 1);
		else if(dir == 1)
			normal = new Vector3(1, 0, 0);
		else if(dir == 2)
			normal = new Vector3(0, 0, -1);
		else if(dir == 3)
			normal = new Vector3(-1, 0, 0);
		else if(dir == 4)
			normal = new Vector3(0, 1, 0);
		else
			normal = new Vector3(0, -1, 0);

		normals[0] = normal;
		normals[1] = normal;
		normals[2] = normal;
		normals[3] = normal;
	}
}
