using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;

[BurstCompile]
public struct BuildVerticalChunkJob : IJob{
	[ReadOnly]
	public ChunkPos pos;
	[ReadOnly]
	public bool isBottom;
	[ReadOnly]
	public bool isTop;

	[ReadOnly]
	public NativeArray<ushort> data; // Voxeldata
	[ReadOnly]
	public NativeArray<ushort> state; // VoxelMetadata.state
	[ReadOnly]
	public NativeArray<ushort> neighbordata;
	[ReadOnly]
	public NativeArray<ushort> neighborStates;
	[ReadOnly]
	public NativeArray<byte> lightdata;
	[ReadOnly]
	public NativeArray<byte> neighborlight;
	[ReadOnly]
	public NativeArray<byte> renderMap;

	// Rendering Primitives
	public NativeList<Vector3> verts;
	public NativeList<Vector2> UVs;
	public NativeList<Vector2> lightUV;
	public NativeList<Vector3> normals;
	public NativeList<Vector4> tangents;

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
	public NativeArray<Vector4> cacheCubeTangent;

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
		ushort neighborBlock, neighborState;
		ushort thisState;
		bool isBlock, isNeighborBlock;
		int3 neighborCoord;
		int i, y;

		if(isBottom){
			i = 5;
			y = 0;
			for(int x=1; x<Chunk.chunkWidth-1; x++){
				for(int z=1; z<Chunk.chunkWidth-1; z++){

					thisBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+z];
					thisState = state[x*Chunk.chunkWidth*Chunk.chunkDepth+z];
					neighborBlock = neighbordata[x*Chunk.chunkWidth*Chunk.chunkDepth+(Chunk.chunkDepth-1)*Chunk.chunkWidth+z];
					neighborState = neighborStates[x*Chunk.chunkWidth*Chunk.chunkDepth+(Chunk.chunkDepth-1)*Chunk.chunkWidth+z];
					neighborCoord = new int3(x, Chunk.chunkDepth-1, z);
					isBlock = thisBlock <= ushort.MaxValue/2;
		    		isNeighborBlock = neighborBlock <= ushort.MaxValue/2;

		    		// If current is invisible, ignore
		    		if(isBlock){
		    			if(blockInvisible[thisBlock]){
		    				continue;
		    			}
		    		}
		    		else{
		    			continue;
		    		}

		    		////////// -----------------------------------

					// Handles Liquid chunks
		    		if(isBlock){
		    			if(blockSeamless[neighborBlock]){
			    			if(CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
			    				continue;
			    			}
		    			}

		    		}
		    		else{
		    			if(objectSeamless[ushort.MaxValue-neighborBlock]){
			    			if(CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
			    				continue;
			    			}    				
		    			}
		    		}

		    		if(isNeighborBlock){
		    			if(Boolean(blockTransparent[neighborBlock]))
						    LoadMesh(x, y, z, i, thisBlock, neighborCoord, cacheCubeVert, cacheCubeUV, cacheCubeNormal);
		    		}
		    		else{
		    			if(Boolean(objectTransparent[ushort.MaxValue-neighborBlock]))
						    LoadMesh(x, y, z, i, thisBlock, neighborCoord, cacheCubeVert, cacheCubeUV, cacheCubeNormal);		    				
		    		}
		    	} // z loop
		    } // x loop
		}

		if(isTop){
			i = 4;
			y = Chunk.chunkDepth-1;
			for(int x=1; x<Chunk.chunkWidth-1; x++){
				for(int z=1; z<Chunk.chunkWidth-1; z++){

					thisBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					thisState = state[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					neighborBlock = neighbordata[x*Chunk.chunkWidth*Chunk.chunkDepth+z];
					neighborState = neighborStates[x*Chunk.chunkWidth*Chunk.chunkDepth+z];
					neighborCoord = new int3(x, 0, z);
					isBlock = thisBlock <= ushort.MaxValue/2;
		    		isNeighborBlock = neighborBlock <= ushort.MaxValue/2;

		    		// If current is invisible, ignore
		    		if(isBlock){
		    			if(blockInvisible[thisBlock]){
		    				continue;
		    			}
		    		}
		    		else{
		    			continue;
		    		}

		    		////////// -----------------------------------

					// Handles Liquid chunks
		    		if(isBlock){
		    			if(blockSeamless[neighborBlock]){
			    			if(CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
			    				continue;
			    			}
		    			}

		    		}
		    		else{
		    			if(objectSeamless[ushort.MaxValue-neighborBlock]){
			    			if(CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
			    				continue;
			    			}    				
		    			}
		    		}


		    		if(isNeighborBlock){
		    			if(Boolean(blockTransparent[neighborBlock]))
						    LoadMesh(x, y, z, i, thisBlock, neighborCoord, cacheCubeVert, cacheCubeUV, cacheCubeNormal);
		    		}
		    		else{
		    			if(Boolean(objectTransparent[ushort.MaxValue-neighborBlock]))
						    LoadMesh(x, y, z, i, thisBlock, neighborCoord, cacheCubeVert, cacheCubeUV, cacheCubeNormal);		    				
		    		}
		    	} // z loop
		    } // x loop			
		}
    }

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

    // Imports Mesh data and applies it to the chunk depending on the Renderer Thread
    // Returns true if loaded a blocktype mesh and false if it's an asset to be loaded later
    private bool LoadMesh(int x, int y, int z, int dir, ushort blockCode, int3 neighborIndex, NativeArray<Vector3> cacheCubeVert, NativeArray<Vector2> cacheCubeUV, NativeArray<Vector3> cacheCubeNormal, int lookahead=0){
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

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

    		CalculateTangent(cacheCubeTangent, dir);
    		tangents.AddRange(cacheCubeTangent);
    		    		
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

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

    		CalculateTangent(cacheCubeTangent, dir);
    		tangents.AddRange(cacheCubeTangent);
    		
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

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

    		CalculateTangent(cacheCubeTangent, dir);
    		tangents.AddRange(cacheCubeTangent);
    		    		
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

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

    		CalculateTangent(cacheCubeTangent, dir);
    		tangents.AddRange(cacheCubeTangent);
    		
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

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex);
    		lightUV.AddRange(cacheCubeUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

    		CalculateTangent(cacheCubeTangent, dir);
    		tangents.AddRange(cacheCubeTangent);
    		
	    	iceTris.Add(vCount -4);
	    	iceTris.Add(vCount -4 +1);
	    	iceTris.Add(vCount -4 +2);
	    	iceTris.Add(vCount -4);
	    	iceTris.Add(vCount -4 +2);
	    	iceTris.Add(vCount -4 +3);

	    	return true;
    	}

    	return false;
    }

    public void AddLightUV(NativeArray<Vector2> array, int x, int y, int z, int dir, int3 neighborIndex){
    	int maxLightLevel = 15;
    	int currentLightLevel = GetOtherLight(neighborIndex.x, neighborIndex.y, neighborIndex.z);

    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector2(maxLightLevel, 1);
	    	array[1] = new Vector2(maxLightLevel, 1);
	    	array[2] = new Vector2(maxLightLevel, 1);
	    	array[3] = new Vector2(maxLightLevel, 1);
	    	return;
    	}

    	CalculateLightCorners(neighborIndex, dir, array, currentLightLevel);
    }

    public void AddLightUVExtra(NativeArray<Vector2> array, int x, int y, int z, int dir, int3 neighborIndex){
    	int maxLightLevel = 15;
    	int currentLightLevel = GetOtherLight(neighborIndex.x, neighborIndex.y, neighborIndex.z, isNatural:false);

    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector2(array[0].x, maxLightLevel);
	    	array[1] = new Vector2(array[1].x, maxLightLevel);
	    	array[2] = new Vector2(array[2].x, maxLightLevel);
	    	array[3] = new Vector2(array[3].x, maxLightLevel);
	    	return;
    	}

    	CalculateLightCornersExtra(neighborIndex, dir, array, currentLightLevel);
    }

    private void CalculateLightCorners(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel){
      	// Up
    	if(dir == 4)
    		SetCorner(array, pos, currentLightLevel, 1, 2, 3, 0, 4);
    	// Down
     	else
    		SetCorner(array, pos, currentLightLevel, 1, 0, 3, 2, 5);
    }

    private void CalculateLightCornersExtra(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel){
      	// Up
    	if(dir == 4)
    		SetCornerExtra(array, pos, currentLightLevel, 1, 2, 3, 0, 4);
    	// Down
     	else
    		SetCornerExtra(array, pos, currentLightLevel, 1, 0, 3, 2, 5);
    }

    private void SetCorner(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);

		light1 = GetOtherLight(pos.x, pos.y, pos.z, dir1, isNatural:true);
		light2 = GetOtherLight(pos.x, pos.y, pos.z, dir2, isNatural:true);
		light3 = GetOtherLight(pos.x, pos.y, pos.z, dir3, isNatural:true);
		light4 = GetOtherLight(pos.x, pos.y, pos.z, dir4, isNatural:true);

		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
		light5 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
		light6 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
		light7 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
		light8 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);

		array[0] = new Vector2(Max(light1, light2, light5, currentLightLevel), 1);
		array[1] = new Vector2(Max(light2, light3, light6, currentLightLevel), 1);
		array[2] = new Vector2(Max(light3, light4, light7, currentLightLevel), 1);
		array[3] = new Vector2(Max(light4, light1, light8, currentLightLevel), 1);
    }

    private void SetCornerExtra(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);

		light1 = GetOtherLight(pos.x, pos.y, pos.z, dir1, isNatural:false);
		light2 = GetOtherLight(pos.x, pos.y, pos.z, dir2, isNatural:false);
		light3 = GetOtherLight(pos.x, pos.y, pos.z, dir3, isNatural:false);
		light4 = GetOtherLight(pos.x, pos.y, pos.z, dir4, isNatural:false);

		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
		light5 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
		light6 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
		light7 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
		light8 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);

		array[0] = new Vector2(array[0].x, Max(light1, light2, light5, currentLightLevel));
		array[1] = new Vector2(array[1].x, Max(light2, light3, light6, currentLightLevel));
		array[2] = new Vector2(array[2].x, Max(light3, light4, light7, currentLightLevel));
		array[3] = new Vector2(array[3].x, Max(light4, light1, light8, currentLightLevel));
    }

	private int GetOtherLight(int x, int y, int z, bool isNatural=true){
		if(isNatural)
			return neighborlight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
		else
			return neighborlight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
	}

	private int GetOtherLight(int x, int y, int z, int dir, bool isNatural=true, byte chunkDir=5, int currentLight=0){
		int3 neighborCoord = new int3(x, y, z) + VoxelData.offsets[dir];

		if(isNatural)
			return neighborlight[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z] & 0x0F;
		else
			return neighborlight[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z] >> 4;
	}

	private int GetOtherLight(int x, int y, int z, int3 dir, bool isNatural=true){
		int3 coord = new int3(x, y, z) + dir;

		if(isNatural)
			return neighborlight[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
		else
			return neighborlight[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] >> 4;
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


	public void CalculateNormal(NativeArray<Vector3> normals, int dir){
		Vector3 normal;

		if(dir == 4)
			normal = new Vector3(0, 1, 0);
		else
			normal = new Vector3(0, -1, 0);

		normals[0] = normal;
		normals[1] = normal;
		normals[2] = normal;
		normals[3] = normal;
	}

	public void CalculateTangent(NativeArray<Vector4> tangents, int dir){
		Vector4 tangent;

		if(dir == 4)
			tangent = new Vector4(0, 0, 1, -1);
		else
			tangent = new Vector4(0, 0, -1, -1);

		tangents[0] = tangent;
		tangents[1] = tangent;
		tangents[2] = tangent;
		tangents[3] = tangent;
	}

	public bool Boolean(byte b){
		return b != 0;
	}
}