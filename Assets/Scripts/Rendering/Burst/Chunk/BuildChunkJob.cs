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
	public int verticalCode;
	[ReadOnly]
	public bool canRain;

	[ReadOnly]
	public NativeArray<ushort> data; // Voxeldata
	[ReadOnly]
	public NativeArray<ushort> state; // VoxelMetadata.state
	[ReadOnly]
	public NativeArray<ushort> hp;
	[ReadOnly]
	public NativeArray<byte> renderMap;
	[ReadOnly]
	public NativeArray<byte> heightMap;

	// Neighbor Data
	[ReadOnly]
	public NativeArray<ushort> xmdata;
	[ReadOnly]
	public NativeArray<ushort> xpdata;
	[ReadOnly]
	public NativeArray<ushort> zmdata;
	[ReadOnly]
	public NativeArray<ushort> zpdata;
	[ReadOnly]
	public NativeArray<ushort> xmzmdata;
	[ReadOnly]
	public NativeArray<ushort> xpzmdata;
	[ReadOnly]
	public NativeArray<ushort> xmzpdata;
	[ReadOnly]
	public NativeArray<ushort> xpzpdata;

	// Neighbor Height
	[ReadOnly]
	public NativeArray<byte> xmheight;
	[ReadOnly]
	public NativeArray<byte> xpheight;
	[ReadOnly]
	public NativeArray<byte> zmheight;
	[ReadOnly]
	public NativeArray<byte> zpheight;

	// Vertical Data
	[ReadOnly]
	public NativeArray<ushort> vdata;
	[ReadOnly]
	public NativeArray<ushort> vxmdata;
	[ReadOnly]
	public NativeArray<ushort> vxpdata;
	[ReadOnly]
	public NativeArray<ushort> vzmdata;
	[ReadOnly]
	public NativeArray<ushort> vzpdata;
	[ReadOnly]
	public NativeArray<ushort> vxmzmdata;
	[ReadOnly]
	public NativeArray<ushort> vxpzmdata;
	[ReadOnly]
	public NativeArray<ushort> vxmzpdata;
	[ReadOnly]
	public NativeArray<ushort> vxpzpdata;

	// OnLoad Event Trigger List
	public NativeList<int3> loadOutList;
	public NativeList<int3> loadAssetList;
	public NativeHashSet<int3> assetCoordinates;

	// Rendering Primitives
	public NativeList<Vector3> verts;
	public NativeList<Vector2> UVs;
	public NativeList<Vector3> normals;
	public NativeList<Vector4> tangents;
	public NativeList<Vector2> dampnessdata;

	// Decals
	public NativeList<Vector3> decalVerts;
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
	public NativeArray<Vector2> cacheCubeUV;
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
	[ReadOnly]
	public NativeArray<ushort> blockHP;
	[ReadOnly]
	public NativeArray<ushort> objectHP;

	// Atlas Information
	[ReadOnly]
	public NativeArray<int2> atlasSize;


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

					isInBorder = true;
				}
				else{
					y = renderMap[x*Chunk.chunkWidth+z];

					if(y == Chunk.chunkDepth-1 && isSurfaceChunk)
						y++;
					if(verticalCode == 1 && y == Chunk.chunkDepth-1){
						y = Chunk.chunkDepth;
					}
					if(verticalCode == -1){
						yMin = -1;
					}
				}

				for(; y >= yMin; y--){
					if(verticalCode != 0){
						if(CheckCorner(x, y, z))
							continue;
						if(CheckBorder(x, y, z))
							isInBorder = true;
						else
							isInBorder = false;
					}

					if(y == Chunk.chunkDepth && isSurfaceChunk && verticalCode == 0){
						thisBlock = 0;
						thisState = 0;
						isBlock = true;
					}
					else{
						thisBlock = GetBlockData(x, y, z, isInBorder);
						thisState = GetBlockState(x, y, z, isInBorder);
						isBlock = thisBlock <= ushort.MaxValue/2;
					}

	    			// Runs OnLoad event
	    			if(load && !isInBorder){
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

		    		// Handles DrawRegardless cases
		    		if(isBlock){
			    		if(blockDrawRegardless[thisBlock]){
			    			bool isABlock;
			    			bool isAtTheBorder;
			    			int3 auxC;

			    			for(int i=0; i < 6; i++){
					    		if(x < 0)
					    			continue;

					    		if(x >= Chunk.chunkWidth)
					    			continue;

					    		if(z < 0)
					    			continue;

					    		if(z >= Chunk.chunkWidth)
					    			continue;

					    		if(y >= Chunk.chunkDepth)
					    			continue;

					    		if(y < 0)
					    			continue;

			    				auxC = GetCoords(x, y, z, i);
			    				isAtTheBorder = CheckBorder(auxC.x, auxC.y, auxC.z);
					    		neighborBlock = GetBlockData(auxC.x, auxC.y, auxC.z, isAtTheBorder);
					    		neighborState = GetBlockState(auxC.x, auxC.y, auxC.z, isAtTheBorder);
					    		isABlock = neighborBlock <= ushort.MaxValue/2;

					    		if(isABlock){
						    		if(blockSeamless[thisBlock]){
						    			if(CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
						    				continue;
						    			}
						    		}
						    	}
						    	else{
						    		if(objectSeamless[thisBlock]){
						    			if(CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
						    				continue;
						    			}
						    		}
						    	}

						    	LoadMesh(x, y, z, i, thisBlock, load, cacheCubeVert, cacheCubeUV, cacheCubeNormal, y == Chunk.chunkDepth & isSurfaceChunk);
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

				    		if(verticalCode != -1)
					    		if(c.y == 0 && ii == 5)
					    			continue;

					    	if(verticalCode != 1)
					    		if(c.y == Chunk.chunkDepth-1 && ii == 4 && !isSurfaceChunk){
					    			continue;
				    		}

				    		neighborBlock = GetBlockData(c.x, c.y, c.z, false);
				    		neighborState = GetBlockState(c.x, c.y, c.z, false);
				    		neighborHP = GetBlockHP(c.x, c.y, c.z);
				    		isBlock = neighborBlock <= ushort.MaxValue/2;

				    		if(neighborBlock == 0)
				    			continue;

				    		// Handles Objects
				    		if(!isBlock){
				    			if(!assetCoordinates.Contains(c)){
				    				assetCoordinates.Add(c);
				    				LoadMesh(c.x, c.y, c.z, ii, neighborBlock, load, cacheCubeVert, cacheCubeUV, cacheCubeNormal, y == Chunk.chunkDepth & isSurfaceChunk);
				    			}

				    			continue;
				    		}

			    			// Cuts down handling of DrawRegardless blocks here
			    			if(blockDrawRegardless[neighborBlock])
			    				continue;

							// Handles Liquid chunks
			    			if(blockSeamless[neighborBlock]){
				    			if(CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
				    				continue;
				    			}
			    			}

						    LoadMesh(c.x, c.y, c.z, ii, neighborBlock, load, cacheCubeVert, cacheCubeUV, cacheCubeNormal, y == Chunk.chunkDepth & isSurfaceChunk);

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

	// Gets the block in relation to the 9-way chunk approach
	// Corners are not considered since they don't need to be fetched for blockdata
	private ushort GetBlockData(int x, int y, int z, bool isInBorder){
		if(!isInBorder){
			return data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
		}
		else{
			if(x == -1)
				return xmdata[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
			else if(x == Chunk.chunkWidth)
				return xpdata[y*Chunk.chunkWidth+z];
			else if(z == -1)
				return zmdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
			else if(z == Chunk.chunkWidth)
				return zpdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];
			else if(y == Chunk.chunkDepth && verticalCode == 1)
				return vdata[x*Chunk.chunkWidth*Chunk.chunkDepth+z];
			else if(y == -1 && verticalCode == -1)
				return vdata[x*Chunk.chunkWidth*Chunk.chunkDepth+(Chunk.chunkDepth-1)*Chunk.chunkWidth+z];
			else{
				return 0;
			}
		}
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
		int sideCode = CheckCardinalSide(coord.x, coord.y, coord.z);

		switch(sideCode){
			case 0:
				return heightMap[coord.x*Chunk.chunkWidth+coord.z];
			case 1:
				return xpheight[coord.z];
			case 2:
				return zmheight[coord.x*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
			case 3:
				return xmheight[(Chunk.chunkWidth-1)*Chunk.chunkWidth+coord.z];
			case 4:
				return zpheight[coord.x*Chunk.chunkWidth];
			default:
				return 0;
		}
	}

	private void CalculateDampness(int x, int y, int z, int dir){
		byte dampness = ToByte(this.canRain && y > GetNeighborHeight(x, z, dir));

		// Add 1 Vector for every face vert
		this.dampnessdata.Add(new Vector2(dampness, 0));
		this.dampnessdata.Add(new Vector2(dampness, 0));
		this.dampnessdata.Add(new Vector2(dampness, 0));
		this.dampnessdata.Add(new Vector2(dampness, 0));
	}

	/*
	Return List

	0: normal
	1: xm
	2: vxm
	3: xp
	4: vxp
	5: zm
	6: vzm
	7: zp
	8: vzp
	9: xmzm
	10: vxmzm
	11: xpzm
	12: vxpzm
	13: xmzp
	14: vxmzp
	15: xpzp
	16: vxpzp
	17: topY
	18: bottomY
	*/
	private int CheckSide(int x, int y, int z){
		if(x == -1 && z == -1){
			if(y == -1 || y == Chunk.chunkDepth)
				return 10;
			else
				return 9;			
		}
		else if(x == Chunk.chunkWidth && z == -1){
			if(y == -1 || y == Chunk.chunkDepth)
				return 12;
			else
				return 11;			
		}
		else if(x == -1 && z == Chunk.chunkWidth){
			if(y == -1 || y == Chunk.chunkDepth)
				return 14;
			else
				return 13;		
		}
		else if(x == Chunk.chunkWidth && z == Chunk.chunkWidth){
			if(y == -1 || y == Chunk.chunkDepth)
				return 16;
			else
				return 15;		
		}
		else if(x == -1){
			if(y == -1 || y == Chunk.chunkDepth)
				return 2;
			else
				return 1;
		}
		else if(x == Chunk.chunkWidth){
			if(y == -1 || y == Chunk.chunkDepth)
				return 4;
			else
				return 3;
		}
		else if(z == -1){
			if(y == -1 || y == Chunk.chunkDepth)
				return 6;
			else
				return 5;			
		}
		else if(z == Chunk.chunkWidth){
			if(y == -1 || y == Chunk.chunkDepth)
				return 8;
			else
				return 7;			
		}
		else if(y == -1){
			return 18;
		}
		else if(y == Chunk.chunkDepth){
			return 17;
		}

		return 0; // own chunk
	}

	// Checks which cardinal side a neighbor is in
	private int CheckCardinalSide(int x, int y, int z){
		if(x == Chunk.chunkWidth)
			return 1;
		else if(z == -1)
			return 2;
		else if(x == -1)
			return 3;
		else if(z == Chunk.chunkWidth)
			return 4;
		return 0;
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
    // Load is true when Chunk is being loaded and not reloaded
    // Returns true if loaded a blocktype mesh and false if it's an asset to be loaded later
    private bool LoadMesh(int x, int y, int z, int dir, ushort blockCode, bool load, NativeArray<Vector3> cacheCubeVert, NativeArray<Vector2> cacheCubeUV, NativeArray<Vector3> cacheCubeNormal, bool isSurfaceBlock, int lookahead=0){
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

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

    		CalculateTangent(cacheCubeTangent, dir);
    		tangents.AddRange(cacheCubeTangent);

    		CalculateDampness(x, y, z, dir);
    		
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

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

    		CalculateTangent(cacheCubeTangent, dir);
    		tangents.AddRange(cacheCubeTangent);

    		CalculateDampness(x, y, z, dir);

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

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

    		CalculateTangent(cacheCubeTangent, dir);
    		tangents.AddRange(cacheCubeTangent);

    		CalculateDampness(x, y, z, dir);
    		    		
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

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

    		CalculateTangent(cacheCubeTangent, dir);
    		tangents.AddRange(cacheCubeTangent);

    		CalculateDampness(x, y, z, dir);
    		
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

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

    		CalculateTangent(cacheCubeTangent, dir);
    		tangents.AddRange(cacheCubeTangent);

    		CalculateDampness(x, y, z, dir);
    		
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

    		CalculateNormal(cacheCubeNormal, dir);
    		normals.AddRange(cacheCubeNormal);

    		CalculateTangent(cacheCubeTangent, dir);
    		tangents.AddRange(cacheCubeTangent);

    		CalculateDampness(x, y, z, dir);
    		    		
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
			int atlasX = atlasSize[(int)ShaderIndex.OPAQUE].x;
			int atlasY = atlasSize[(int)ShaderIndex.OPAQUE].y;
			float x = textureID%atlasX;
			float y = Mathf.FloorToInt(textureID/atlasX);

			x *= 1f / atlasX;
			y *= 1f / atlasY;

			array[0] = new Vector2(x,y+(1f/atlasY));
			array[1] = new Vector2(x+(1f/atlasX),y+(1f/atlasY));
			array[2] = new Vector2(x+(1f/atlasX),y);
			array[3] = new Vector2(x,y);
		}
		// If should use transparent atlas
		else if(blockMaterial[blockCode] == ShaderIndex.SPECULAR){
			int atlasX = atlasSize[(int)ShaderIndex.SPECULAR].x;
			int atlasY = atlasSize[(int)ShaderIndex.SPECULAR].y;
			float x = textureID%atlasX;
			float y = Mathf.FloorToInt(textureID/atlasX);
	 
			x *= 1f / atlasX;
			y *= 1f / atlasY;

			array[0] = new Vector2(x,y+(1f/atlasY));
			array[1] = new Vector2(x+(1f/atlasX),y+(1f/atlasY));
			array[2] = new Vector2(x+(1f/atlasX),y);
			array[3] = new Vector2(x,y);
		}
		// If should use Leaves atlas
		else if(blockMaterial[blockCode] == ShaderIndex.LEAVES){
			int atlasX = atlasSize[(int)ShaderIndex.LEAVES].x;
			int atlasY = atlasSize[(int)ShaderIndex.LEAVES].y;
			float x = textureID%atlasX;
			float y = Mathf.FloorToInt(textureID/atlasX);
	 
			x *= 1f / atlasX;
			y *= 1f / atlasY;

			array[0] = new Vector2(x,y+(1f/atlasY));
			array[1] = new Vector2(x+(1f/atlasX),y+(1f/atlasY));
			array[2] = new Vector2(x+(1f/atlasX),y);
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
