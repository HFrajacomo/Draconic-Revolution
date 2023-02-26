using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;

[BurstCompile]
public struct BuildCornerJob : IJob{
	[ReadOnly]
	public bool reload;
	[ReadOnly]
	public NativeArray<ushort> data;
	[ReadOnly]
	public NativeArray<ushort> metadata;
	[ReadOnly]
	public NativeArray<byte> lightdata;
	[ReadOnly]
	public NativeArray<ushort> xsidedata;
	[ReadOnly]
	public NativeArray<byte> xsidelight;
	[ReadOnly]
	public NativeArray<ushort> zsidedata;
	[ReadOnly]
	public NativeArray<byte> zsidelight;
	[ReadOnly]
	public NativeArray<ushort> cornerdata;
	[ReadOnly]
	public NativeArray<byte> cornerlight;
	[ReadOnly]
	public bool xmzm, xmzp, xpzm, xpzp;
	[ReadOnly]
	public ChunkPos pos;

	// Border Update
	public NativeList<int3> toLoadEvent;
	public NativeList<int3> toBUD;

	// Rendering Primitives
	public NativeList<Vector3> verts;
	public NativeList<Vector2> uvs;
	public NativeList<Vector2> lightUV;
	public NativeList<Vector3> normals;
	public NativeList<Vector4> tangents;

	// Render Thread Triangles
	public NativeList<int> normalTris;
	public NativeList<int> specularTris;
	public NativeList<int> liquidTris;
	public NativeList<int> leavesTris;
	public NativeList<int> iceTris;

	// Cached
	public NativeArray<Vector3> cachedCubeVerts;
	public NativeArray<Vector2> cachedUVVerts;
	public NativeArray<Vector3> cachedCubeNormal;
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


	public void Execute(){
		ushort thisBlock;
		ushort neighborBlock;
		byte chunkDir;
		int3 thisCoord;
		int3 neighborCoord;
		int4 newChunkPos; // 1st and 2nd are ChunkPos offset and 3rd and 4th are in-Chunk coords

		// XMZM
		if(xmzm){
			chunkDir = 5;
			for(int y=1; y<Chunk.chunkDepth-1; y++){
				for(int i=0; i < 6; i++){
					int x = 0;
					int z = 0;

					if(i == 0 || i == 1)
						continue;

					thisBlock = data[y*Chunk.chunkWidth];
					thisCoord = new int3(0, y, 0);

					if(i == 3){
						neighborBlock = xsidedata[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
						neighborCoord = new int3(Chunk.chunkWidth-1, y, z);
						newChunkPos = new int4(-1, 0, Chunk.chunkWidth-1, z);
					}
					else if(i == 2){
						neighborBlock = zsidedata[y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
						neighborCoord = new int3(x, y, Chunk.chunkWidth-1);
						newChunkPos = new int4(0, -1, x, Chunk.chunkWidth-1);
					}
					else if(i == 4 && y < Chunk.chunkDepth-1){
						neighborBlock = data[(y+1)*Chunk.chunkWidth];
						neighborCoord = new int3(0, y+1, z);
						newChunkPos = new int4(0, 0, x, z);
					}
					else if(i == 5 && y > 0){
						neighborBlock = data[(y-1)*Chunk.chunkWidth];
						neighborCoord = new int3(0, y-1, z);
						newChunkPos = new int4(0, 0, x, z);
					}
					else{
						continue;
					}

					if(thisBlock == 0 && neighborBlock == 0)
						continue;

					if(CheckSeams(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockSeamless[neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
						else{
							if(objectSeamless[ushort.MaxValue-neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
					}

					if(thisBlock == 0)
						continue;

					if(CheckPlacement(neighborBlock)){
						LoadMesh(x, y, z, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir);
					}
				}
			}
			return;
		}
		// XPZM
		else if(xpzm){
			chunkDir = 4;

			for(int y=1; y<Chunk.chunkDepth-1; y++){
				for(int i=0; i < 6; i++){
					int x = Chunk.chunkWidth-1;
					int z = 0;

					if(i == 0 || i == 3)
						continue;

					thisBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					thisCoord = new int3(Chunk.chunkWidth-1, y, z);

					if(i == 1){
						neighborBlock = xsidedata[y*Chunk.chunkWidth+z];
						neighborCoord = new int3(0, y, z);
						newChunkPos = new int4(1, 0, 0, z);
					}
					else if(i == 2){
						neighborBlock = zsidedata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
						neighborCoord = new int3(x, y, Chunk.chunkWidth-1);
						newChunkPos = new int4(0, -1, x, Chunk.chunkWidth-1);
					}
					else if(i == 4 && y < Chunk.chunkDepth-1){
						neighborBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+(y+1)*Chunk.chunkWidth+z];
						neighborCoord = new int3(x, y+1, z);
						newChunkPos = new int4(0, 0, x, z);
					}
					else if(i == 5 && y > 0){
						neighborBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+z];
						neighborCoord = new int3(x, y-1, z);
						newChunkPos = new int4(0, 0, x, z);
					}
					else{
						continue;
					}

					if(thisBlock == 0 && neighborBlock == 0)
						continue;

					if(CheckSeams(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockSeamless[neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
						else{
							if(objectSeamless[ushort.MaxValue-neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
					}

					if(thisBlock == 0)
						continue;

					if(CheckPlacement(neighborBlock)){
						LoadMesh(x, y, z, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir);
					}
				}
			}
			return;
		}
		// XMZP Side
		else if(xmzp){
			chunkDir = 6;

			for(int y=1; y<Chunk.chunkDepth-1; y++){
				for(int i=0; i < 6; i++){
					int x = 0;
					int z = Chunk.chunkWidth-1;

					if(i == 1 || i == 2)
						continue;

					thisBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					thisCoord = new int3(x, y, z);

					if(i == 4 && y < Chunk.chunkDepth-1){
						neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y+1)*Chunk.chunkWidth+z];
						neighborCoord = new int3(x, y+1, z);
						newChunkPos = new int4(0, 0, x, z);
					}
					else if(i == 5 && y > 0){
						neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+z];
						neighborCoord = new int3(x, y-1, z);
						newChunkPos = new int4(0, 0, x, z);
					}
					else if(i == 3){
						neighborBlock = xsidedata[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
						neighborCoord = new int3(Chunk.chunkWidth-1, y, z);
						newChunkPos = new int4(-1, 0, Chunk.chunkWidth-1, z);
					}
					else if(i == 0){
						neighborBlock = zsidedata[y*Chunk.chunkWidth];
						neighborCoord = new int3(0, y, 0);
						newChunkPos = new int4(0, 1, 0, 0);
					}
					else{
						continue;
					}

					if(thisBlock == 0 && neighborBlock == 0)
						continue;

					if(CheckSeams(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockSeamless[neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
						else{
							if(objectSeamless[ushort.MaxValue-neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
					}

					if(thisBlock == 0)
						continue;

					if(CheckPlacement(neighborBlock)){
						LoadMesh(x, y, z, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir);
					}
				}
			}
			return;
		}
		// XPZP Side
		else if(xpzp){
			chunkDir = 7;

			for(int y=1; y<Chunk.chunkDepth-1; y++){
				for(int i=0; i < 6; i++){
					int x = Chunk.chunkWidth-1;
					int z = Chunk.chunkWidth-1;

					if(i == 2 || i == 3)
						continue;

					thisBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
					thisCoord = new int3(x, y, z);

					if(i == 4 && y < Chunk.chunkDepth-1){
						neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y+1)*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
						neighborCoord = new int3(x, y+1, Chunk.chunkWidth-1);
						newChunkPos = new int4(0, 0, x, z);
					}
					else if(i == 5 && y > 0){
						neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
						neighborCoord = new int3(x, y-1, Chunk.chunkWidth-1);
						newChunkPos = new int4(0, 0, x, z);
					}
					else if(i == 1){
						neighborBlock = xsidedata[y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
						neighborCoord = new int3(0, y, Chunk.chunkWidth-1);
						newChunkPos = new int4(1,0,0,z);
					}
					else if(i == 0){
						neighborBlock = zsidedata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];
						neighborCoord = new int3(x, y, 0);
						newChunkPos = new int4(0,1,x,0);
					}
					else{
						continue;
					}

					if(thisBlock == 0 && neighborBlock == 0)
						continue;

					if(CheckSeams(thisBlock, neighborBlock)){
						continue;
					}
					else{
						if(neighborBlock <= ushort.MaxValue/2){
							if(blockSeamless[neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
						else{
							if(objectSeamless[ushort.MaxValue-neighborBlock]){
								toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.w)));
							}

							if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
								toLoadEvent.Add(thisCoord);
							}
						}
					}

					if(thisBlock == 0)
						continue;

					if(CheckPlacement(neighborBlock)){
						LoadMesh(x, y, z, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir);
					}
				}
			}
			return;
		}
	}

	// Checks if other chunk border block is a liquid and puts it on Border Update List
	private void CheckBorderUpdate(int x, int y, int z, ushort blockCode){

		if(blockCode <= ushort.MaxValue/2){
			if(blockSeamless[blockCode]){
				toLoadEvent.Add(new int3(x,y,z));
			}
		}
		else{
			if(objectSeamless[ushort.MaxValue-blockCode]){
				toLoadEvent.Add(new int3(x,y,z));
			}
		}
	} 

    // Checks if neighbor is transparent or invisible
    private bool CheckPlacement(int neighborBlock){
    	if(neighborBlock <= ushort.MaxValue/2)
    		return Boolean(blockTransparent[neighborBlock]) || blockInvisible[neighborBlock];
    	else
			return Boolean(objectTransparent[ushort.MaxValue-neighborBlock]) || objectInvisible[ushort.MaxValue-neighborBlock];
    }

    // Checks if Liquids are side by side
    private bool CheckSeams(int thisBlock, int neighborBlock){
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

    	return thisSeamless && neighborSeamless && (thisBlock == neighborBlock);
    }

    private bool Boolean(byte a){
    	if(a == 0)
    		return false;
    	return true;
    }

    // Imports Mesh data and applies it to the chunk depending on the Renderer Thread
    // Load is true when Chunk is being loaded and not reloaded
    // Returns true if loaded a blocktype mesh and false if it's an asset to be loaded later
    private bool LoadMesh(int x, int y, int z, int dir, int3 neighborIndex, ushort blockCode, bool load, NativeArray<Vector3> cacheCubeVert, NativeArray<Vector2> cacheCubeUV, NativeArray<Vector3> cacheCubeNormal, byte chunkDir, int lookahead=0){
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
    		uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
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
    		uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
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
    		VertsByState(cacheCubeVert, dir, metadata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z], new Vector3(x,y+(pos.y*Chunk.chunkDepth),z));
			verts.AddRange(cacheCubeVert);
			int vCount = verts.Length + lookahead;

			LiquidTexture(cacheCubeUV, x, z);
    		uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
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
			uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
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
			uvs.AddRange(cacheCubeUV);

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir);
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

    // Sets the secondary UV of Lightmaps
    private void AddLightUV(NativeArray<Vector2> array, int x, int y, int z, int dir, int3 neighborIndex, byte chunkDir){
    	int maxLightLevel = 15;
    	int currentLightLevel;

		currentLightLevel = GetLightBasedOnDir(dir, neighborIndex.x, neighborIndex.y, neighborIndex.z);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector2(maxLightLevel, 1);
	    	array[1] = new Vector2(maxLightLevel, 1);
	    	array[2] = new Vector2(maxLightLevel, 1);
	    	array[3] = new Vector2(maxLightLevel, 1);
	    	return;
    	}

    	SetCorner(array, neighborIndex, currentLightLevel, chunkDir, dir);
    }

    // Sets the secondary UV of Lightmaps
    private void AddLightUVExtra(NativeArray<Vector2> array, int x, int y, int z, int dir, int3 neighborIndex, byte chunkDir){
    	int maxLightLevel = 15;
    	int currentLightLevel;

		currentLightLevel = GetLightBasedOnDir(dir, neighborIndex.x, neighborIndex.y, neighborIndex.z, isNatural:false);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector2(maxLightLevel, 1);
	    	array[1] = new Vector2(maxLightLevel, 1);
	    	array[2] = new Vector2(maxLightLevel, 1);
	    	array[3] = new Vector2(maxLightLevel, 1);
	    	return;
    	}

    	SetCornerExtra(array, neighborIndex, currentLightLevel, chunkDir, dir);
    }

    private void SetCorner(NativeArray<Vector2> array, int3 pos, int currentLightLevel, byte chunkDir, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;

    	// XPZM Corner
    	if(chunkDir == 4){
    		if(facing == 4){
	    		light1 = GetLightOnX(0, pos.y, pos.z);
	    		light2 = GetLightOnZ(pos.x, pos.y, Chunk.chunkWidth-1);
	    		light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z);
	    		light4 = GetLightOnCurrent(pos.x, pos.y, pos.z+1);
	    		light5 = GetLightOnCorner(chunkDir, pos.y);
	    		light6 = GetLightOnZ(pos.x-1, pos.y, Chunk.chunkWidth-1);
	    		light7 = GetLightOnCurrent(pos.x-1, pos.y, pos.z+1);
	    		light8 = GetLightOnX(0, pos.y, pos.z+1);
	    	}
	    	else if(facing == 5){
	    		light1 = GetLightOnX(0, pos.y, pos.z);
	    		light2 = GetLightOnCurrent(pos.x, pos.y, pos.z+1);
	    		light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z);
	    		light4 = GetLightOnZ(pos.x, pos.y, Chunk.chunkWidth-1);
	    		light5 = GetLightOnX(0, pos.y, pos.z+1);
	    		light6 = GetLightOnCurrent(pos.x-1, pos.y, pos.z+1);
	    		light7 = GetLightOnZ(pos.x-1, pos.y, Chunk.chunkWidth-1);
	    		light8 = GetLightOnCorner(chunkDir, pos.y);
	    	}
	    	else if(facing == 1){
	    		light1 = GetLightOnCorner(chunkDir, pos.y);
	    		light2 = GetLightOnX(0, pos.y+1, pos.z);
	    		light3 = GetLightOnX(0, pos.y, pos.z+1);
	    		light4 = GetLightOnX(0, pos.y-1, pos.z);
	    		light5 = GetLightOnCorner(chunkDir, pos.y+1);
	    		light6 = GetLightOnX(0, pos.y+1, pos.z+1);
	    		light7 = GetLightOnX(0, pos.y-1, pos.z+1);
	    		light8 = GetLightOnCorner(chunkDir, pos.y-1);
	    	}
	    	else{ // if(facing == 2)
	    		light1 = GetLightOnZ(pos.x-1, pos.y, Chunk.chunkWidth-1);
	    		light2 = GetLightOnZ(pos.x, pos.y+1, Chunk.chunkWidth-1);
	    		light3 = GetLightOnCorner(chunkDir, pos.y);
	    		light4 = GetLightOnZ(pos.x, pos.y-1, Chunk.chunkWidth-1);
	    		light5 = GetLightOnZ(pos.x-1, pos.y+1, Chunk.chunkWidth-1);
	    		light6 = GetLightOnCorner(chunkDir, pos.y+1);
	    		light7 = GetLightOnCorner(chunkDir, pos.y-1);
	    		light8 = GetLightOnZ(pos.x-1, pos.y-1, Chunk.chunkWidth-1);
	    	}
    	}
    	// XMZM Corner
    	else if(chunkDir == 5){
    		if(facing == 4){
    			light1 = GetLightOnCurrent(1, pos.y, pos.z);
    			light2 = GetLightOnZ(0, pos.y, Chunk.chunkWidth-1);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z);
    			light4 = GetLightOnCurrent(0, pos.y, pos.z+1);
    			light5 = GetLightOnZ(1, pos.y, Chunk.chunkWidth-1);
    			light6 = GetLightOnCorner(chunkDir, pos.y);
    			light7 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z+1);
    			light8 = GetLightOnCurrent(1, pos.y, 1);
    		}
    		else if(facing == 5){
    			light1 = GetLightOnCurrent(1, pos.y, pos.z);
    			light2 = GetLightOnCurrent(0, pos.y, pos.z+1);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z);
    			light4 = GetLightOnZ(0, pos.y, Chunk.chunkWidth-1);
    			light5 = GetLightOnCurrent(1, pos.y, 1);
    			light6 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z+1);
    			light7 = GetLightOnCorner(chunkDir, pos.y);
    			light8 = GetLightOnZ(1, pos.y, Chunk.chunkWidth-1);
    		}
    		else if(facing == 2){
    			light1 = GetLightOnCorner(chunkDir, pos.y);
    			light2 = GetLightOnZ(pos.x, pos.y+1, Chunk.chunkWidth-1);
    			light3 = GetLightOnZ(pos.x+1, pos.y, Chunk.chunkWidth-1);
    			light4 = GetLightOnZ(pos.x, pos.y-1, Chunk.chunkWidth-1);
    			light5 = GetLightOnCorner(chunkDir, pos.y+1);
    			light6 = GetLightOnZ(pos.x+1, pos.y+1, Chunk.chunkWidth-1);
    			light7 = GetLightOnZ(pos.x+1, pos.y-1, Chunk.chunkWidth-1);
    			light8 = GetLightOnCorner(chunkDir, pos.y-1);
    		}
    		else{ // if(facing == 3)
    			light1 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z+1);
    			light2 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z);
    			light3 = GetLightOnCorner(chunkDir, pos.y);
    			light4 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z);
    			light5 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z+1);
    			light6 = GetLightOnCorner(chunkDir, pos.y+1);
    			light7 = GetLightOnCorner(chunkDir, pos.y-1);
    			light8 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z+1);
    		}
    	}
    	// XMZP Corner
    	else if(chunkDir == 6){
    		if(facing == 4){
    			light1 = GetLightOnCurrent(pos.x+1, pos.y, pos.z);
    			light2 = GetLightOnCurrent(pos.x, pos.y, pos.z-1);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z);
    			light4 = GetLightOnZ(pos.x, pos.y, 0);
    			light5 = GetLightOnCurrent(pos.x+1, pos.y, pos.z-1);
    			light6 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z-1);
    			light7 = GetLightOnCorner(chunkDir, pos.y);
    			light8 = GetLightOnZ(pos.x+1, pos.y, 0);
    		}
    		else if(facing == 5){
    			light1 = GetLightOnCurrent(pos.x+1, pos.y, pos.z);
    			light2 = GetLightOnZ(pos.x, pos.y, 0);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z);
    			light4 = GetLightOnCurrent(pos.x, pos.y, pos.z-1);
    			light5 = GetLightOnZ(pos.x+1, pos.y, 0);
    			light6 = GetLightOnCorner(chunkDir, pos.y);
    			light7 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z-1);
    			light8 = GetLightOnCurrent(pos.x+1, pos.y, pos.z-1);
    		}
    		else if(facing == 0){
    			light1 = GetLightOnZ(pos.x+1, pos.y, 0);
    			light2 = GetLightOnZ(pos.x, pos.y+1, 0);
    			light3 = GetLightOnCorner(chunkDir, pos.y);
    			light4 = GetLightOnZ(pos.x, pos.y-1, 0);
    			light5 = GetLightOnZ(pos.x+1, pos.y+1, 0);
    			light6 = GetLightOnCorner(chunkDir, pos.y+1);
    			light7 = GetLightOnCorner(chunkDir, pos.y-1);
    			light8 = GetLightOnZ(pos.x+1, pos.y-1, 0);
    		}
    		else{ // if(facing == 3)
    			light1 = GetLightOnCorner(chunkDir, pos.y);
    			light2 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z-1);
    			light4 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z);
    			light5 = GetLightOnCorner(chunkDir, pos.y+1);
    			light6 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z-1);
    			light7 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z-1);
    			light8 = GetLightOnCorner(chunkDir, pos.y-1);
    		}
    	}
    	// XPZP Corner
    	else{
    		if(facing == 4){
    			light1 = GetLightOnX(0, pos.y, pos.z);
    			light2 = GetLightOnCurrent(pos.x, pos.y, pos.z-1);
    			light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z);
    			light4 = GetLightOnZ(pos.x, pos.y, 0);
    			light5 = GetLightOnX(0, pos.y, pos.z-1);
    			light6 = GetLightOnCurrent(pos.x-1, pos.y, pos.z-1);
    			light7 = GetLightOnZ(pos.x-1, pos.y, 0);
    			light8 = GetLightOnCorner(chunkDir, pos.y);
    		}
    		else if(facing == 5){
    			light1 = GetLightOnX(0, pos.y, pos.z);
    			light2 = GetLightOnZ(pos.x, pos.y, 0);
    			light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z);
    			light4 = GetLightOnCurrent(pos.x, pos.y, pos.z-1);
    			light5 = GetLightOnCorner(chunkDir, pos.y);
    			light6 = GetLightOnZ(pos.x-1, pos.y, 0);
    			light7 = GetLightOnCurrent(pos.x-1, pos.y, pos.z-1);
    			light8 = GetLightOnX(0, pos.y, pos.z-1);
    		}
    		else if(facing == 0){
    			light1 = GetLightOnCorner(chunkDir, pos.y);
    			light2 = GetLightOnZ(pos.x, pos.y+1, 0);
    			light3 = GetLightOnZ(pos.x-1, pos.y, 0);
    			light4 = GetLightOnZ(pos.x, pos.y-1, 0);
    			light5 = GetLightOnCorner(chunkDir, pos.y+1);
    			light6 = GetLightOnZ(pos.x-1, pos.y+1, 0);
    			light7 = GetLightOnZ(pos.x-1, pos.y-1, 0);
    			light8 = GetLightOnCorner(chunkDir, pos.y-1);
    		}
    		else{ // if(facing == 1)
    			light1 = GetLightOnX(0, pos.y, pos.z-1);
    			light2 = GetLightOnX(0, pos.y+1, pos.z);
    			light3 = GetLightOnCorner(chunkDir, pos.y);
    			light4 = GetLightOnX(0, pos.y-1, pos.z);
    			light5 = GetLightOnX(0, pos.y+1, pos.z-1);
    			light6 = GetLightOnCorner(chunkDir, pos.y+1);
    			light7 = GetLightOnCorner(chunkDir, pos.y-1);
    			light8 = GetLightOnX(0, pos.y-1, pos.z-1);
    		}
    	}


		array[0] = new Vector2(Max(light1, light2, light5, currentLightLevel), 1);
		array[1] = new Vector2(Max(light2, light3, light6, currentLightLevel), 1);
		array[2] = new Vector2(Max(light3, light4, light7, currentLightLevel), 1);
		array[3] = new Vector2(Max(light4, light1, light8, currentLightLevel), 1);
    }

    private void SetCornerExtra(NativeArray<Vector2> array, int3 pos, int currentLightLevel, byte chunkDir, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;

    	// XPZM Corner
    	if(chunkDir == 4){
    		if(facing == 4){
	    		light1 = GetLightOnX(0, pos.y, pos.z, isNatural:false);
	    		light2 = GetLightOnZ(pos.x, pos.y, Chunk.chunkWidth-1, isNatural:false);
	    		light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z, isNatural:false);
	    		light4 = GetLightOnCurrent(pos.x, pos.y, pos.z+1, isNatural:false);
	    		light5 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
	    		light6 = GetLightOnZ(pos.x-1, pos.y, Chunk.chunkWidth-1, isNatural:false);
	    		light7 = GetLightOnCurrent(pos.x-1, pos.y, pos.z+1, isNatural:false);
	    		light8 = GetLightOnX(0, pos.y, pos.z+1, isNatural:false);
	    	}
	    	else if(facing == 5){
	    		light1 = GetLightOnX(0, pos.y, pos.z, isNatural:false);
	    		light2 = GetLightOnCurrent(pos.x, pos.y, pos.z+1, isNatural:false);
	    		light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z, isNatural:false);
	    		light4 = GetLightOnZ(pos.x, pos.y, Chunk.chunkWidth-1, isNatural:false);
	    		light5 = GetLightOnX(0, pos.y, pos.z+1, isNatural:false);
	    		light6 = GetLightOnCurrent(pos.x-1, pos.y, pos.z+1, isNatural:false);
	    		light7 = GetLightOnZ(pos.x-1, pos.y, Chunk.chunkWidth-1, isNatural:false);
	    		light8 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
	    	}
	    	else if(facing == 1){
	    		light1 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
	    		light2 = GetLightOnX(0, pos.y+1, pos.z, isNatural:false);
	    		light3 = GetLightOnX(0, pos.y, pos.z+1, isNatural:false);
	    		light4 = GetLightOnX(0, pos.y-1, pos.z, isNatural:false);
	    		light5 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
	    		light6 = GetLightOnX(0, pos.y+1, pos.z+1, isNatural:false);
	    		light7 = GetLightOnX(0, pos.y-1, pos.z+1, isNatural:false);
	    		light8 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
	    	}
	    	else{ // if(facing == 2)
	    		light1 = GetLightOnZ(pos.x-1, pos.y, Chunk.chunkWidth-1, isNatural:false);
	    		light2 = GetLightOnZ(pos.x, pos.y+1, Chunk.chunkWidth-1, isNatural:false);
	    		light3 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
	    		light4 = GetLightOnZ(pos.x, pos.y-1, Chunk.chunkWidth-1, isNatural:false);
	    		light5 = GetLightOnZ(pos.x-1, pos.y+1, Chunk.chunkWidth-1, isNatural:false);
	    		light6 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
	    		light7 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
	    		light8 = GetLightOnZ(pos.x-1, pos.y-1, Chunk.chunkWidth-1, isNatural:false);
	    	}
    	}
    	// XMZM Corner
    	else if(chunkDir == 5){
    		if(facing == 4){
    			light1 = GetLightOnCurrent(1, pos.y, pos.z, isNatural:false);
    			light2 = GetLightOnZ(0, pos.y, Chunk.chunkWidth-1, isNatural:false);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z, isNatural:false);
    			light4 = GetLightOnCurrent(0, pos.y, pos.z+1, isNatural:false);
    			light5 = GetLightOnZ(1, pos.y, Chunk.chunkWidth-1, isNatural:false);
    			light6 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light7 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z+1, isNatural:false);
    			light8 = GetLightOnCurrent(1, pos.y, 1, isNatural:false);
    		}
    		else if(facing == 5){
    			light1 = GetLightOnCurrent(1, pos.y, pos.z, isNatural:false);
    			light2 = GetLightOnCurrent(0, pos.y, pos.z+1, isNatural:false);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z, isNatural:false);
    			light4 = GetLightOnZ(0, pos.y, Chunk.chunkWidth-1, isNatural:false);
    			light5 = GetLightOnCurrent(1, pos.y, 1, isNatural:false);
    			light6 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z+1, isNatural:false);
    			light7 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light8 = GetLightOnZ(1, pos.y, Chunk.chunkWidth-1, isNatural:false);
    		}
    		else if(facing == 2){
    			light1 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light2 = GetLightOnZ(pos.x, pos.y+1, Chunk.chunkWidth-1, isNatural:false);
    			light3 = GetLightOnZ(pos.x+1, pos.y, Chunk.chunkWidth-1, isNatural:false);
    			light4 = GetLightOnZ(pos.x, pos.y-1, Chunk.chunkWidth-1, isNatural:false);
    			light5 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
    			light6 = GetLightOnZ(pos.x+1, pos.y+1, Chunk.chunkWidth-1, isNatural:false);
    			light7 = GetLightOnZ(pos.x+1, pos.y-1, Chunk.chunkWidth-1, isNatural:false);
    			light8 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
    		}
    		else{ // if(facing == 3)
    			light1 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z+1, isNatural:false);
    			light2 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z, isNatural:false);
    			light3 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light4 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z, isNatural:false);
    			light5 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z+1, isNatural:false);
    			light6 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
    			light7 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
    			light8 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z+1, isNatural:false);
    		}
    	}
    	// XMZP Corner
    	else if(chunkDir == 6){
    		if(facing == 4){
    			light1 = GetLightOnCurrent(pos.x+1, pos.y, pos.z, isNatural:false);
    			light2 = GetLightOnCurrent(pos.x, pos.y, pos.z-1, isNatural:false);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z, isNatural:false);
    			light4 = GetLightOnZ(pos.x, pos.y, 0, isNatural:false);
    			light5 = GetLightOnCurrent(pos.x+1, pos.y, pos.z-1, isNatural:false);
    			light6 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z-1, isNatural:false);
    			light7 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light8 = GetLightOnZ(pos.x+1, pos.y, 0, isNatural:false);
    		}
    		else if(facing == 5){
    			light1 = GetLightOnCurrent(pos.x+1, pos.y, pos.z, isNatural:false);
    			light2 = GetLightOnZ(pos.x, pos.y, 0, isNatural:false);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z, isNatural:false);
    			light4 = GetLightOnCurrent(pos.x, pos.y, pos.z-1, isNatural:false);
    			light5 = GetLightOnZ(pos.x+1, pos.y, 0, isNatural:false);
    			light6 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light7 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z-1, isNatural:false);
    			light8 = GetLightOnCurrent(pos.x+1, pos.y, pos.z-1, isNatural:false);
    		}
    		else if(facing == 0){
    			light1 = GetLightOnZ(pos.x+1, pos.y, 0, isNatural:false);
    			light2 = GetLightOnZ(pos.x, pos.y+1, 0, isNatural:false);
    			light3 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light4 = GetLightOnZ(pos.x, pos.y-1, 0, isNatural:false);
    			light5 = GetLightOnZ(pos.x+1, pos.y+1, 0, isNatural:false);
    			light6 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
    			light7 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
    			light8 = GetLightOnZ(pos.x+1, pos.y-1, 0, isNatural:false);
    		}
    		else{ // if(facing == 3)
    			light1 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light2 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z, isNatural:false);
    			light3 = GetLightOnX(Chunk.chunkWidth-1, pos.y, pos.z-1, isNatural:false);
    			light4 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z, isNatural:false);
    			light5 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
    			light6 = GetLightOnX(Chunk.chunkWidth-1, pos.y+1, pos.z-1, isNatural:false);
    			light7 = GetLightOnX(Chunk.chunkWidth-1, pos.y-1, pos.z-1, isNatural:false);
    			light8 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
    		}
    	}
    	// XPZP Corner
    	else{
    		if(facing == 4){
    			light1 = GetLightOnX(0, pos.y, pos.z, isNatural:false);
    			light2 = GetLightOnCurrent(pos.x, pos.y, pos.z-1, isNatural:false);
    			light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z, isNatural:false);
    			light4 = GetLightOnZ(pos.x, pos.y, 0, isNatural:false);
    			light5 = GetLightOnX(0, pos.y, pos.z-1, isNatural:false);
    			light6 = GetLightOnCurrent(pos.x-1, pos.y, pos.z-1, isNatural:false);
    			light7 = GetLightOnZ(pos.x-1, pos.y, 0, isNatural:false);
    			light8 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    		}
    		else if(facing == 5){
    			light1 = GetLightOnX(0, pos.y, pos.z, isNatural:false);
    			light2 = GetLightOnZ(pos.x, pos.y, 0, isNatural:false);
    			light3 = GetLightOnCurrent(pos.x-1, pos.y, pos.z, isNatural:false);
    			light4 = GetLightOnCurrent(pos.x, pos.y, pos.z-1, isNatural:false);
    			light5 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light6 = GetLightOnZ(pos.x-1, pos.y, 0, isNatural:false);
    			light7 = GetLightOnCurrent(pos.x-1, pos.y, pos.z-1, isNatural:false);
    			light8 = GetLightOnX(0, pos.y, pos.z-1, isNatural:false);
    		}
    		else if(facing == 0){
    			light1 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light2 = GetLightOnZ(pos.x, pos.y+1, 0, isNatural:false);
    			light3 = GetLightOnZ(pos.x-1, pos.y, 0, isNatural:false);
    			light4 = GetLightOnZ(pos.x, pos.y-1, 0, isNatural:false);
    			light5 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
    			light6 = GetLightOnZ(pos.x-1, pos.y+1, 0, isNatural:false);
    			light7 = GetLightOnZ(pos.x-1, pos.y-1, 0, isNatural:false);
    			light8 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
    		}
    		else{ // if(facing == 1)
    			light1 = GetLightOnX(0, pos.y, pos.z-1, isNatural:false);
    			light2 = GetLightOnX(0, pos.y+1, pos.z, isNatural:false);
    			light3 = GetLightOnCorner(chunkDir, pos.y, isNatural:false);
    			light4 = GetLightOnX(0, pos.y-1, pos.z, isNatural:false);
    			light5 = GetLightOnX(0, pos.y+1, pos.z-1, isNatural:false);
    			light6 = GetLightOnCorner(chunkDir, pos.y+1, isNatural:false);
    			light7 = GetLightOnCorner(chunkDir, pos.y-1, isNatural:false);
    			light8 = GetLightOnX(0, pos.y-1, pos.z-1, isNatural:false);
    		}
    	}


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

	// Gets the light of maybe neighbors by looking into the dir used to get current XYZ
	private int GetLightBasedOnDir(int dir, int x, int y, int z, bool isNatural=true){
		// Temporary
		if(x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z > ushort.MaxValue || x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z < 0)
			return 15;

		if(isNatural){
			if(dir == 4 || dir == 5)
				return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
			else if(dir == 0 || dir == 2)
				return zsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
			else if(dir == 1 || dir == 3)
				return xsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
		}
		else{
			if(dir == 4 || dir == 5)
				return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
			else if(dir == 0 || dir == 2)
				return zsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
			else if(dir == 1 || dir == 3)
				return xsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;			
		}

		return 0;
	}

	private int GetLightOnCurrent(int x, int y, int z, bool isNatural=true){
		// Temporary
		if(x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z > ushort.MaxValue || x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z < 0)
			return 15;

		if(isNatural)
			return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
		else
			return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
	}

	private int GetLightOnX(int x, int y, int z, bool isNatural=true){
		// Temporary
		if(x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z > ushort.MaxValue || x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z < 0)
			return 15;

		if(x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z < 0)
			return 0;

		if(isNatural)
			return xsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
		else
			return xsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
	}

	private int GetLightOnZ(int x, int y, int z, bool isNatural=true){
		// Temporary
		if(x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z > ushort.MaxValue || x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z < 0)
			return 15;

		if(x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z < 0)
			return 0;

		if(isNatural)
			return zsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
		else
			return zsidelight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
	}

	private int GetLightOnCorner(byte chunkDir, int y, bool isNatural=true){
		// Temporary
		if(y >= Chunk.chunkDepth || y < 0)
			return 15;

		if(isNatural){
			if(chunkDir == 4)
				return cornerlight[(y)*Chunk.chunkWidth+(Chunk.chunkWidth-1)] & 0x0F;
			else if(chunkDir == 5)
				return cornerlight[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+(y)*Chunk.chunkWidth+(Chunk.chunkWidth-1)] & 0x0F;
			else if(chunkDir == 6)
				return cornerlight[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+(y)*Chunk.chunkWidth] & 0x0F;
			else if(chunkDir == 7)
				return cornerlight[(y)*Chunk.chunkWidth] & 0x0F;
		}
		else{
			if(chunkDir == 4)
				return cornerlight[y*Chunk.chunkWidth+(Chunk.chunkWidth-1)] >> 4;
			else if(chunkDir == 5)
				return cornerlight[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)] >> 4;
			else if(chunkDir == 6)
				return cornerlight[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth] >> 4;
			else if(chunkDir == 7)
				return cornerlight[y*Chunk.chunkWidth] >> 4;
		}

		return 0;
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
	public void faceVertices(NativeArray<Vector3> fv, int dir, float scale, Vector3 pos)
	{
		for (int i = 0; i < fv.Length; i++)
		{
			fv[i] = (CubeMeshData.vertices[CubeMeshData.faceTriangles[dir*4+i]] * scale) + pos;
		}
	}

	// Gets the vertices of a given state in a liquid
	public void VertsByState(NativeArray<Vector3> fv, int dir, ushort s, Vector3 pos, float scale=0.5f){
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
			tangent = new Vector4(1, 0, 0, -1);
		else if(dir == 1)
			tangent = new Vector4(0, 0, 1, -1);
		else if(dir == 2)
			tangent = new Vector4(-1, 0, 0, -1);
		else if(dir == 3)
			tangent = new Vector4(0, 0, -1, -1);
		else if(dir == 4)
			tangent = new Vector4(0, 0, 1, -1);
		else
			tangent = new Vector4(0, 0, -1, -1);

		tangents[0] = tangent;
		tangents[1] = tangent;
		tangents[2] = tangent;
		tangents[3] = tangent;
	}
}