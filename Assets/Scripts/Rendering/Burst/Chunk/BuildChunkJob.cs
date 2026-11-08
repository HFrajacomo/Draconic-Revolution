using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

/*
Builds the block geometry of a chunk.
Reads a padded snapshot (see PaddedChunk) that contains the chunk and a 1-voxel border of its neighbors
*/
[BurstCompile]
public struct BuildChunkJob : IJob{
	[ReadOnly]
	public ChunkPos pos;
	[ReadOnly]
	public bool hasTop; // If the snapshot border above the chunk was filled by the upper chunk
	[ReadOnly]
	public bool hasBottom; // If the snapshot border below the chunk was filled by the lower chunk
	[ReadOnly]
	public bool canRain;

	// Padded Snapshot
	[ReadOnly]
	public NativeArray<ushort> blocks;
	[ReadOnly]
	public NativeArray<byte> light;
	[ReadOnly]
	public NativeArray<byte> heights;

	// Chunk-only Data
	[ReadOnly]
	public NativeArray<ushort> state; // VoxelMetadata.state
	[ReadOnly]
	public NativeArray<ushort> hp;
	[ReadOnly]
	public NativeArray<byte> renderMap;

	// Asset Loading
	public NativeList<int3> loadAssetList;
	public NativeBitArray assetCoordinates; // Indexed by voxel index

	// Collidable faces, indexed by direction*VOXEL_COUNT + voxel index. Merged into the collider by GreedyColliderJob
	public NativeBitArray colliderFaces;

	// Rendering Primitives
	public NativeList<Vector3> verts;
	public NativeList<Vector3> UVs;
	public NativeList<Vector3> lightUV;
	public NativeList<Vector3> normals;
	public NativeList<Vector4> tangents;

	// Decals
	public NativeList<Vector3> decalVerts;
	public NativeList<Vector3> decalNormals;
	public NativeList<Vector2> decalUVs;
	public NativeList<int> decalTris;

	// Render Thread Triangles
	public NativeList<int> normalTris;
	public NativeList<int> specularTris;
	public NativeList<int> liquidTris;
	public NativeList<int> leavesTris;
	public NativeList<int> iceTris;
	public NativeList<int> lavaTris;

	// Cache
	public NativeArray<Vector3> cacheCubeVert;
	public NativeArray<Vector3> cacheCubeUV;
	public NativeArray<Vector3> extraUV;
	public NativeArray<Vector3> cacheCubeNormal;
	public NativeArray<Vector4> cacheCubeTangent;

	// Block Encyclopedia Data
	[ReadOnly]
	public NativeArray<bool> blockTransparent;
	[ReadOnly]
	public NativeArray<bool> objectTransparent;
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
	public NativeArray<bool> blockDrawLiquid;
	[ReadOnly]
	public NativeArray<ushort> blockHP;
	[ReadOnly]
	public NativeArray<ushort> objectHP;


	public void Execute(){
		ushort thisBlock;
		ushort neighborBlock;
		ushort neighborState;
		ushort neighborHP;
		ushort thisState;
		bool isBlock;
		bool isTransparent;
		int ii;
		int y;
		bool isInBorder;
		bool isSurfaceChunk;
		int3 c;
		int decalCode;
		int yMin = 0;

		isSurfaceChunk = pos.y == Chunk.chunkMaxY;

		for(int x=-1; x<=Chunk.chunkWidth; x++){
			for(int z=-1; z<=Chunk.chunkWidth; z++){
				isInBorder = false;
				yMin = 0;

				if(x == -1 || z == -1 || x == Chunk.chunkWidth || z == Chunk.chunkWidth){
					if(CheckCorner(x, z))
						continue;

					if(x == -1)
						y = renderMap[z];
					else if(x == Chunk.chunkWidth)
						y = renderMap[(Chunk.chunkWidth-1)*Chunk.chunkWidth+z];
					else if(z == -1)
						y = renderMap[x*Chunk.chunkWidth];
					else
						y = renderMap[x*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
				}
				else{
					y = renderMap[x*Chunk.chunkWidth+z];

					if(y == Chunk.chunkDepth-1 && isSurfaceChunk)
						y++;
					if(hasTop && y == Chunk.chunkDepth-1){
						y = Chunk.chunkDepth;
					}
					if(hasBottom){
						yMin = -1;
					}
				}

				for(; y >= yMin; y--){
					isInBorder = CheckBorder(x, y, z);

					if(y == Chunk.chunkDepth && isSurfaceChunk && !hasTop){
						thisBlock = 0;
						thisState = 0;
						isBlock = true;
					}
					else{
						thisBlock = GetBlockData(x, y, z);
						thisState = GetBlockState(x, y, z, isInBorder);
						isBlock = thisBlock <= ushort.MaxValue/2;
					}

		    		// Handles DrawLiquid cases
		    		if(isBlock){
			    		if(blockDrawLiquid[thisBlock] && !isInBorder){
			    			bool isABlock;
			    			bool isAtTheBorder;
			    			int3 auxC;
			    			int i = 4;

				    		if(y >= Chunk.chunkDepth)
				    			continue;

				    		if(y < 0)
				    			continue;

		    				auxC = GetCoords(x, y, z, i);
		    				isAtTheBorder = CheckBorder(auxC.x, auxC.y, auxC.z);
				    		neighborBlock = GetBlockData(auxC.x, auxC.y, auxC.z);
				    		neighborState = GetBlockState(auxC.x, auxC.y, auxC.z, isAtTheBorder);
				    		isABlock = neighborBlock <= ushort.MaxValue/2;

				    		if(isABlock){
					    		if(blockSeamless[thisBlock]){
					    			if(!CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
					    				LoadMesh(x, y, z, i, thisBlock, cacheCubeVert, cacheCubeUV, cacheCubeNormal, y == Chunk.chunkDepth & isSurfaceChunk);
					    			}
					    		}
					    	}
					    	else{
					    		if(objectSeamless[thisBlock]){
					    			if(!CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
					    				LoadMesh(x, y, z, i, thisBlock, cacheCubeVert, cacheCubeUV, cacheCubeNormal, y == Chunk.chunkDepth & isSurfaceChunk);
					    			}
					    		}
					    	}
			    		}
		    		}

		    		// Transparency
		    		if(isBlock){
		    			isTransparent = blockTransparent[thisBlock];
		    		}
		    		else{
		    			isTransparent = objectTransparent[ushort.MaxValue - thisBlock];
		    		}

		    		if(isTransparent){
				    	for(int i=0; i<6; i++){
				    		if(x == -1 && i != 1)
				    			continue;

				    		if(x == Chunk.chunkWidth && i != 3)
				    			continue;

				    		if(z == -1 && i != 0)
				    			continue;

				    		if(z == Chunk.chunkWidth && i != 2)
				    			continue;

				    		if(y == Chunk.chunkDepth && i != 5)
				    			continue;

				    		if(y == -1 && i != 4)
				    			continue;

				    		c = GetCoords(x, y, z, i);
				    		ii = InvertDir(i);

				    		if(CheckBorder(c.x, c.y, c.z))
				    			continue;

				    		if(!hasBottom)
					    		if(c.y == 0 && ii == 5)
					    			continue;

					    	if(!hasTop)
					    		if(c.y == Chunk.chunkDepth-1 && ii == 4 && !isSurfaceChunk){
					    			continue;
				    		}

				    		neighborBlock = GetBlockData(c.x, c.y, c.z);
				    		neighborState = GetBlockState(c.x, c.y, c.z, false);
				    		neighborHP = GetBlockHP(c.x, c.y, c.z);
				    		isBlock = neighborBlock <= ushort.MaxValue/2;

				    		if(neighborBlock == 0)
				    			continue;

				    		// Handles Objects
				    		if(!isBlock){
				    			int assetIndex = c.x*Chunk.chunkWidth*Chunk.chunkDepth+c.y*Chunk.chunkWidth+c.z;

				    			if(!assetCoordinates.IsSet(assetIndex)){
				    				assetCoordinates.Set(assetIndex, true);
				    				LoadMesh(c.x, c.y, c.z, ii, neighborBlock, cacheCubeVert, cacheCubeUV, cacheCubeNormal, y == Chunk.chunkDepth & isSurfaceChunk);
				    			}

				    			continue;
				    		}

			    			// Cuts down handling of DrawLiquid blocks here
			    			if(blockDrawLiquid[neighborBlock] && i == 4)
			    				continue;

							// Handles Liquid chunks
			    			if(blockSeamless[neighborBlock]){
				    			if(CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
				    				continue;
				    			}
			    			}

						    LoadMesh(c.x, c.y, c.z, ii, neighborBlock, cacheCubeVert, cacheCubeUV, cacheCubeNormal, y == Chunk.chunkDepth & isSurfaceChunk);

				    		if(neighborBlock <= ushort.MaxValue/2){
				    			if(neighborHP == 0 || neighborHP == ushort.MaxValue || neighborHP == blockHP[neighborBlock])
				    				continue;
				    		}
				    		else{
				    			if(neighborHP == 0 || neighborHP == ushort.MaxValue || neighborHP == objectHP[neighborBlock])
				    				continue;			    			
				    		}

				    		decalCode = GetDecalStage(neighborBlock, neighborHP);

			    			if(decalCode >= 0)
			    				BuildDecal(c.x, c.y, c.z, ii, decalCode);
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

    private bool CheckCorner(int x, int z){
    	return ((x == -1 || x == Chunk.chunkWidth) && (z == -1 || z == Chunk.chunkWidth));
    }

    private bool CheckCorner(int x, int y, int z){
    	if(y != -1 && y != Chunk.chunkDepth)
    		return ((x == -1 || x == Chunk.chunkWidth) && (z == -1 || z == Chunk.chunkWidth));
    	else
    		return (x == -1 || x == Chunk.chunkWidth || z == -1 || z == Chunk.chunkWidth);
    }

    private bool CheckBorder(int x, int y, int z){
    	return ((x == -1 || x == Chunk.chunkWidth) || (z == -1 || z == Chunk.chunkWidth)) || (y == -1 || y == Chunk.chunkDepth);
    }

	private int3 GetCoords(int x, int y, int z, int dir){
		return new int3(x, y, z) + VoxelData.offsets[dir];
	}

	// Gets the block from the padded snapshot. Borders of missing vertical neighbors are filled with air
	private ushort GetBlockData(int x, int y, int z){
		return blocks[PaddedChunk.Index(x, y, z)];
	}

	private ushort GetBlockState(int x, int y, int z, bool isInBorder){
		if(!isInBorder)
			return state[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
		else
			return 0;
	}

	private ushort GetBlockHP(int x, int y, int z){
		return hp[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];	
	}

	public int GetDecalStage(ushort block, ushort hp){
		float hpPercentage;

		if(block <= ushort.MaxValue/2)
			hpPercentage = (float)hp / (float)blockHP[block];
		else
			hpPercentage = (float)hp / (float)objectHP[ushort.MaxValue - block];

		if(hpPercentage > 1f)
			hpPercentage = 1f;

	    for(int i=0; i < Constants.DECAL_STAGE_SIZE; i++){
			if(hpPercentage <= Constants.DECAL_STAGE_PERCENTAGE[i])
				return (Constants.DECAL_STAGE_SIZE - 1) - i;
		}

		return -1;
	}

	// Gets neighbor height
	private byte GetNeighborHeight(int x, int z, int dir){
		// Y value doesn't matter here
		int3 coord = new int3(x, 0, z) + VoxelData.offsets[dir];

		return heights[PaddedChunk.MapIndex(coord.x, coord.z)];
	}

	// Gets neighbor light level
	private int GetNeighborLight(int x, int y, int z, int dir, bool isNatural=true){
		return GetNeighborLight(x, y, z, VoxelData.offsets[dir], isNatural:isNatural);
	}

	// Gets neighbor light level. Light outside of the snapshot border of missing vertical neighbors is 0
	private int GetNeighborLight(int x, int y, int z, int3 dir, bool isNatural=true){
		int3 coord = new int3(x, y, z) + dir;
		byte value = light[PaddedChunk.Index(coord.x, coord.y, coord.z)];

		if(isNatural)
			return value & 0x0F;
		return value >> 4;
	}

    // Checks if neighbor is transparent or invisible
    private bool CheckPlacement(int neighborBlock){
    	if(neighborBlock <= ushort.MaxValue/2)
    		return blockTransparent[neighborBlock] || blockInvisible[neighborBlock];
    	else
			return objectTransparent[ushort.MaxValue-neighborBlock] || objectInvisible[ushort.MaxValue-neighborBlock];
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

	public Vector3 GetDecalPosition(float x, float y, float z, int dir){
		Vector3 normal;

		if(dir == 0)
			normal = new Vector3(0, 0, Constants.DECAL_OFFSET);
		else if(dir == 1)
			normal = new Vector3(Constants.DECAL_OFFSET, 0, 0);
		else if(dir == 2)
			normal = new Vector3(0, 0, -Constants.DECAL_OFFSET);
		else if(dir == 3)
			normal = new Vector3(-Constants.DECAL_OFFSET, 0, 0);
		else if(dir == 4)
			normal = new Vector3(0, Constants.DECAL_OFFSET, 0);
		else
			normal = new Vector3(0, -Constants.DECAL_OFFSET, 0);

		return new Vector3(x + normal.x, y + normal.y, z + normal.z);
	}

	public void BuildDecal(int x, int y, int z, int dir, int decal){
		faceVertices(cacheCubeVert, dir, 0.5f, GetDecalPosition(x, y+(this.pos.y*Chunk.chunkDepth), z, dir));
		decalVerts.AddRange(cacheCubeVert);
		int vCount = decalVerts.Length;

		CalculateNormal(cacheCubeNormal, dir);
		decalNormals.AddRange(cacheCubeNormal);

		FillUV(decal);
		
    	decalTris.Add(vCount -4);
    	decalTris.Add(vCount -4 +1);
    	decalTris.Add(vCount -4 +2);
    	decalTris.Add(vCount -4);
    	decalTris.Add(vCount -4 +2);
    	decalTris.Add(vCount -4 +3); 
	}

	public void FillUV(int decal){
		float xSize = 1 / (float)Constants.DECAL_STAGE_SIZE;
		float xMin = (float)decal * xSize;

		decalUVs.Add(new Vector2(xMin, 0));
		decalUVs.Add(new Vector2(xMin, 1));
		decalUVs.Add(new Vector2(xMin + xSize, 1));
		decalUVs.Add(new Vector2(xMin + xSize, 0));
	}


    // Imports Mesh data and applies it to the chunk depending on the Renderer Thread
    // Returns true if loaded a blocktype mesh and false if it's an asset to be loaded later
    private bool LoadMesh(int x, int y, int z, int dir, ushort blockCode, NativeArray<Vector3> cacheCubeVert, NativeArray<Vector3> cacheCubeUV, NativeArray<Vector3> cacheCubeNormal, bool isSurfaceBlock, int lookahead=0){
    	ShaderIndex renderThread;

    	if(blockCode <= ushort.MaxValue/2)
    		renderThread = blockMaterial[blockCode];
    	else
    		renderThread = objectMaterial[ushort.MaxValue-blockCode];
    	
    	// If object is Normal Block
    	if(renderThread == ShaderIndex.OPAQUE){
    		MarkColliderFace(x, y, z, dir);
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
    		UVs.AddRange(cacheCubeUV);

    		AddLightUV(extraUV, x, y, z, dir, fullLight:isSurfaceBlock);
    		AddLightUVExtra(extraUV, x, y, z, dir);
    		lightUV.AddRange(extraUV);

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

    		AddLightUV(extraUV, x, y, z, dir, fullLight:isSurfaceBlock);
    		AddLightUVExtra(extraUV, x, y, z, dir);
    		lightUV.AddRange(extraUV);

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

    		AddLightUV(extraUV, x, y, z, dir, fullLight:isSurfaceBlock);
    		AddLightUVExtra(extraUV, x, y, z, dir);
    		lightUV.AddRange(extraUV);

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

    		AddLightUV(extraUV, x, y, z, dir, fullLight:isSurfaceBlock);
    		AddLightUVExtra(extraUV, x, y, z, dir);
    		lightUV.AddRange(extraUV);

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
    		MarkColliderFace(x, y, z, dir);
    		faceVertices(cacheCubeVert, dir, 0.5f, new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			AddTexture(cacheCubeUV, dir, blockCode);
			UVs.AddRange(cacheCubeUV);

    		AddLightUV(extraUV, x, y, z, dir, fullLight:isSurfaceBlock);
    		AddLightUVExtra(extraUV, x, y, z, dir);
    		lightUV.AddRange(extraUV);

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

    	// If object is Lava
    	else if(renderThread == ShaderIndex.LAVA){
    		VertsByState(cacheCubeVert, dir, state[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z], new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			LiquidTexture(cacheCubeUV, x, z);
    		UVs.AddRange(cacheCubeUV);

    		AddLightUV(extraUV, x, y, z, dir, fullLight:isSurfaceBlock);
    		AddLightUVExtra(extraUV, x, y, z, dir);
    		lightUV.AddRange(extraUV);

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

    		CalculateTangent(cacheCubeTangent, dir);
    		tangents.AddRange(cacheCubeTangent);
    		    		
	    	lavaTris.Add(vCount -4);
	    	lavaTris.Add(vCount -4 +1);
	    	lavaTris.Add(vCount -4 +2);
	    	lavaTris.Add(vCount -4);
	    	lavaTris.Add(vCount -4 +2);
	    	lavaTris.Add(vCount -4 +3);

	    	return true;    		
    	}

    	// If object is an Asset
    	else{
			loadAssetList.Add(new int3(x,y,z));
    		return false;
    	}
    }

    private void MarkColliderFace(int x, int y, int z, int dir){
    	colliderFaces.Set(dir*Chunk.chunkWidth*Chunk.chunkWidth*Chunk.chunkDepth + x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z, true);
    }

    // Sets the secondary UV of Lightmaps and Dampness
    private void AddLightUV(NativeArray<Vector3> array, int x, int y, int z, int dir, bool fullLight=false){
    	int maxLightLevel = 15;
    	int currentLightLevel = GetNeighborLight(x, y, z, dir);
    	byte height = GetNeighborHeight(x, z, dir);

    	// Check value for Dampness
    	byte dampness = ToByte(this.canRain && y > height);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel || fullLight){
	    	array[0] = new Vector3(maxLightLevel, 1, dampness);
	    	array[1] = new Vector3(maxLightLevel, 1, dampness);
	    	array[2] = new Vector3(maxLightLevel, 1, dampness);
	    	array[3] = new Vector3(maxLightLevel, 1, dampness);
	    	return;
    	}

    	int3 auxPos = new int3(x,y,z) + VoxelData.offsets[dir];

    	CalculateLightCorners(auxPos, dir, array, currentLightLevel, dampness);
    }

    // Sets the secondary UV of ExtraLights Lightmaps and Dampness
    private void AddLightUVExtra(NativeArray<Vector3> array, int x, int y, int z, int dir){
    	int maxLightLevel = 15;
    	int currentLightLevel = GetNeighborLight(x, y, z, dir, isNatural:false);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector3(array[0].x, maxLightLevel, array[0].z);
	    	array[1] = new Vector3(array[1].x, maxLightLevel, array[1].z);
	    	array[2] = new Vector3(array[2].x, maxLightLevel, array[2].z);
	    	array[3] = new Vector3(array[3].x, maxLightLevel, array[3].z);
	    	return;
    	}

    	int3 auxPos = new int3(x,y,z) + VoxelData.offsets[dir];

    	CalculateLightCornersExtra(auxPos, dir, array, currentLightLevel);
    }

    private void CalculateLightCorners(int3 pos, int dir, NativeArray<Vector3> array, int currentLightLevel, byte dampness){
    	// North
    	if(dir == 0)
    		SetCorner(array, pos, currentLightLevel, 1, 4, 3, 5, 0, dampness);
    	// East
    	else if(dir == 1)
    		SetCorner(array, pos, currentLightLevel, 2, 4, 0, 5, 1, dampness);
    	// South
     	else if(dir == 2)
    		SetCorner(array, pos, currentLightLevel, 3, 4, 1, 5, 2, dampness);
    	// West
      	else if(dir == 3)
    		SetCorner(array, pos, currentLightLevel, 0, 4, 2, 5, 3, dampness);
      	// Up
    	else if(dir == 4)
    		SetCorner(array, pos, currentLightLevel, 1, 2, 3, 0, 4, dampness);
    	// Down
     	else
     		SetCorner(array, pos, currentLightLevel, 1, 0, 3, 2, 5, dampness);
    }

    private void CalculateLightCornersExtra(int3 pos, int dir, NativeArray<Vector3> array, int currentLightLevel){
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

    private void SetCorner(NativeArray<Vector3> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, int facing, byte dampness){
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

		array[0] = new Vector3(Max(light1, light2, light5, currentLightLevel), 1, dampness);
		array[1] = new Vector3(Max(light2, light3, light6, currentLightLevel), 1, dampness);
		array[2] = new Vector3(Max(light3, light4, light7, currentLightLevel), 1, dampness);
		array[3] = new Vector3(Max(light4, light1, light8, currentLightLevel), 1, dampness);
    }

    private void SetCornerExtra(NativeArray<Vector3> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, int facing){
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



		array[0] = new Vector3(array[0].x, Max(light1, light2, light5, currentLightLevel), array[0].z);
		array[1] = new Vector3(array[1].x, Max(light2, light3, light6, currentLightLevel), array[1].z);
		array[2] = new Vector3(array[2].x, Max(light3, light4, light7, currentLightLevel), array[2].z);
		array[3] = new Vector3(array[3].x, Max(light4, light1, light8, currentLightLevel), array[3].z);
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
	private void AddTexture(NativeArray<Vector3> array, int dir, ushort blockCode){
		int textureID;

		if(dir == 4)
			textureID = blockTiles[blockCode].x;
		else if(dir == 5)
			textureID = blockTiles[blockCode].y;
		else
			textureID = blockTiles[blockCode].z;

		array[0] = new Vector3(0, 1, textureID);
		array[1] = new Vector3(1, 1, textureID);
		array[2] = new Vector3(1, 0, textureID);
		array[3] = new Vector3(0, 0, textureID);
	}

	// Gets UV Map for Liquid blocks
	private void LiquidTexture(NativeArray<Vector3> array, int x, int z){
		int size = Chunk.chunkWidth;
		float tileSize = 1f/size;

		array[0] = new Vector3(x*tileSize,z*tileSize, 0);
		array[1] = new Vector3(x*tileSize,(z+1)*tileSize, 0);
		array[2] = new Vector3((x+1)*tileSize,(z+1)*tileSize, 0);
		array[3] = new Vector3((x+1)*tileSize,z*tileSize, 0);
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

	public void CalculateTangent(NativeArray<Vector4> tangents, int dir){
		Vector4 tangent;

		if(dir == 0)
			tangent = new Vector4(-1, 0, 0, -1);
		else if(dir == 1)
			tangent = new Vector4(0, 0, 1, -1);
		else if(dir == 2)
			tangent = new Vector4(1, 0, 0, -1);
		else if(dir == 3)
			tangent = new Vector4(0, 0, -1, -1);
		else if(dir == 4)
			tangent = new Vector4(-1, 0, 0, -1);
		else
			tangent = new Vector4(-1, 0, 0, -1);

		tangents[0] = tangent;
		tangents[1] = tangent;
		tangents[2] = tangent;
		tangents[3] = tangent;
	}

	public byte ToByte(bool b){
		if(b)
			return 1;
		return 0;
	}
}
