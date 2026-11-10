using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

/*
Finds the visible faces of a chunk, in parallel slices of the padded snapshot (see PaddedChunk), one per x coordinate.
Each slice writes MeshRecords to its own buffer of the records stream, with the light of every face already calculated.
MergeChunkMeshJob then turns the records into mesh data, reading the slices in order
*/
[BurstCompile]
public struct BuildChunkJob : IJobParallelFor{
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

	// Output
	public NativeStream.Writer records;

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
	public NativeArray<bool> blockWashable;
	[ReadOnly]
	public NativeArray<bool> objectWashable;
	[ReadOnly]
	public NativeArray<bool> blockDrawLiquid;
	[ReadOnly]
	public NativeArray<ushort> blockHP;
	[ReadOnly]
	public NativeArray<ushort> objectHP;


	public void Execute(int index){
		int x = index-1;
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

		records.BeginForEachIndex(index);

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

				    		// A full liquid under the same liquid has no visible top
				    		if(neighborBlock != thisBlock || !IsFullLiquidState(thisState)){
					    		if(isABlock){
						    		if(blockSeamless[thisBlock]){
						    			if(!CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
						    				WriteFace(x, y, z, i, thisBlock, y == Chunk.chunkDepth & isSurfaceChunk, false);
						    			}
						    		}
						    	}
						    	else{
						    		if(objectSeamless[thisBlock]){
						    			if(!CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
						    				WriteFace(x, y, z, i, thisBlock, y == Chunk.chunkDepth & isSurfaceChunk, false);
						    			}
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

				    		// Handles Objects. Only the first record of each Object is used (see MergeChunkMeshJob)
				    		if(!isBlock){
				    			WriteFace(c.x, c.y, c.z, ii, neighborBlock, y == Chunk.chunkDepth & isSurfaceChunk, true);
				    			continue;
				    		}

			    			// Cuts down handling of DrawLiquid blocks here
			    			if(blockDrawLiquid[neighborBlock] && i == 4)
			    				continue;

			    			// A full liquid under the same liquid has no visible top
			    			if(i == 5 && neighborBlock == thisBlock && blockDrawLiquid[neighborBlock] && IsFullLiquidState(neighborState))
			    				continue;

							// Handles Liquid chunks
			    			if(blockSeamless[neighborBlock]){
				    			if(CheckSeams(thisBlock, neighborBlock, thisState, neighborState)){
				    				continue;
				    			}
			    			}

						    WriteFace(c.x, c.y, c.z, ii, neighborBlock, y == Chunk.chunkDepth & isSurfaceChunk, false);

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
			    				WriteDecal(c.x, c.y, c.z, ii, decalCode);
					    } // faces loop
					}
	    		} // y loop
	    	} // z loop

	    records.EndForEachIndex();
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

    // Writes the record of a face. Faces of Objects become Object records, since Objects are added by PrepareAssetsJob
    private void WriteFace(int x, int y, int z, int dir, ushort blockCode, bool isSurfaceBlock, bool isObject){
    	MeshRecord record = new MeshRecord();
    	ShaderIndex renderThread;
    	int4 natural, extra;
    	byte dampness;

    	if(blockCode <= ushort.MaxValue/2)
    		renderThread = blockMaterial[blockCode];
    	else
    		renderThread = objectMaterial[ushort.MaxValue-blockCode];

    	record.x = (short)x;
    	record.y = (short)y;
    	record.z = (short)z;
    	record.dir = (byte)dir;
    	record.block = blockCode;
    	record.material = (byte)renderThread;
    	record.isObject = isObject;

    	if(renderThread == ShaderIndex.ASSETS || renderThread == ShaderIndex.ASSETS_SOLID){
    		record.kind = MeshRecord.OBJECT;
    	}
    	else{
    		record.kind = MeshRecord.FACE;
    		CalculateFaceLight(x, y, z, dir, isSurfaceBlock, out natural, out extra, out dampness);
    		record.natural = MeshRecord.Pack(natural);
    		record.extra = MeshRecord.Pack(extra);
    		record.dampness = dampness;
    	}

    	records.Write(record);
    }

    private void WriteDecal(int x, int y, int z, int dir, int decal){
    	MeshRecord record = new MeshRecord();

    	record.kind = MeshRecord.DECAL;
    	record.x = (short)x;
    	record.y = (short)y;
    	record.z = (short)z;
    	record.dir = (byte)dir;
    	record.decal = (byte)decal;

    	records.Write(record);
    }

    // Light levels of the 4 corners of a face (natural and extra light) and its dampness
    private void CalculateFaceLight(int x, int y, int z, int dir, bool fullLight, out int4 natural, out int4 extra, out byte dampness){
    	int maxLightLevel = 15;
    	int currentLightLevel = GetNeighborLight(x, y, z, dir);
    	int currentExtraLevel = GetNeighborLight(x, y, z, dir, isNatural:false);
    	byte height = GetNeighborHeight(x, z, dir);
    	int3 auxPos = new int3(x,y,z) + VoxelData.offsets[dir];

    	dampness = ToByte(this.canRain && y > height);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel || fullLight)
    		natural = new int4(maxLightLevel);
    	else
    		natural = CalculateLightCorners(auxPos, dir, currentLightLevel, true);

    	if(currentExtraLevel == maxLightLevel)
    		extra = new int4(maxLightLevel);
    	else
    		extra = CalculateLightCorners(auxPos, dir, currentExtraLevel, false);
    }

    private int4 CalculateLightCorners(int3 pos, int dir, int currentLightLevel, bool isNatural){
    	// North
    	if(dir == 0)
    		return SetCorner(pos, currentLightLevel, 1, 4, 3, 5, isNatural);
    	// East
    	else if(dir == 1)
    		return SetCorner(pos, currentLightLevel, 2, 4, 0, 5, isNatural);
    	// South
     	else if(dir == 2)
    		return SetCorner(pos, currentLightLevel, 3, 4, 1, 5, isNatural);
    	// West
      	else if(dir == 3)
    		return SetCorner(pos, currentLightLevel, 0, 4, 2, 5, isNatural);
      	// Up
    	else if(dir == 4)
    		return SetCorner(pos, currentLightLevel, 1, 2, 3, 0, isNatural);
      	// Down
     	else
     		return SetCorner(pos, currentLightLevel, 1, 0, 3, 2, isNatural);
    }

    private int4 SetCorner(int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, bool isNatural){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal;

		light1 = GetNeighborLight(pos.x, pos.y, pos.z, dir1, isNatural:isNatural);
		light2 = GetNeighborLight(pos.x, pos.y, pos.z, dir2, isNatural:isNatural);
		light3 = GetNeighborLight(pos.x, pos.y, pos.z, dir3, isNatural:isNatural);
		light4 = GetNeighborLight(pos.x, pos.y, pos.z, dir4, isNatural:isNatural);

		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
		light5 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:isNatural);
		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
		light6 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:isNatural);
		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
		light7 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:isNatural);
		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
		light8 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:isNatural);

		return new int4(
			Max(light1, light2, light5, currentLightLevel),
			Max(light2, light3, light6, currentLightLevel),
			Max(light3, light4, light7, currentLightLevel),
			Max(light4, light1, light8, currentLightLevel)
		);
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

		// Falling and Landed liquids are full blocks
		if(s >= 19 && s <= 23){
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


	// Checks if a liquid state fills the whole voxel
	public static bool IsFullLiquidState(ushort s){
		return s == 0 || s == ushort.MaxValue || (s >= 19 && s <= 23);
	}

	public byte ToByte(bool b){
		if(b)
			return 1;
		return 0;
	}
}
