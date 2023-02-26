using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;

[BurstCompile]
public struct BuildBorderJob : IJob{
	[ReadOnly]
	public bool reload;
	[ReadOnly]
	public NativeArray<ushort> data;
	[ReadOnly]
	public NativeArray<ushort> metadata;
	[ReadOnly]
	public NativeArray<byte> lightdata;
	[ReadOnly]
	public NativeArray<ushort> neighbordata;
	[ReadOnly]
	public NativeArray<byte> neighborlight;
	[ReadOnly]
	public bool zP, zM, xP, xM;
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
		byte skipDir;
		int3 thisCoord;
		int3 neighborCoord;
		int3 newChunkPos;
		bool isFacingBorder;
		byte chunkDir; // 0 = Z+, 1 = X+, 2 = Z-, 3 = X-

		// X- Side
		if(xM){
			skipDir = 1;
			chunkDir = 3;
				
			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int z=0; z<Chunk.chunkWidth; z++){
					for(int i=0; i < 6; i++){

						if(i == skipDir)
							continue;

						if((z == 0 || z == Chunk.chunkWidth-1) && (i == 4 || i == 5 || i == 3))
							continue;

						if((z == 0 && i == 2) || (z == Chunk.chunkWidth-1 && i == 0))
							continue;

						thisBlock = data[y*Chunk.chunkWidth+z];
						thisCoord = new int3(0, y, z);

						if(i == 3){
							neighborBlock = neighbordata[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
							neighborCoord = new int3(Chunk.chunkWidth-1, y, z);
							newChunkPos = new int3(-1, 0, Chunk.chunkWidth-1);
							isFacingBorder = true;
						}
						else if(i == 4 && y < Chunk.chunkDepth-1){
							neighborBlock = data[(y+1)*Chunk.chunkWidth+z];
							neighborCoord = new int3(0, y+1, z);
							newChunkPos = new int3(0, 0, 0);
							isFacingBorder = false;
						}
						else if(i == 5 && y > 0){
							neighborBlock = data[(y-1)*Chunk.chunkWidth+z];
							neighborCoord = new int3(0, y-1, z);
							newChunkPos = new int3(0, 0, 0);
							isFacingBorder = false;
						}
						else if(i == 0 && z < Chunk.chunkWidth-1){
							neighborBlock = data[y*Chunk.chunkWidth+(z+1)];
							neighborCoord = new int3(0, y, z+1);
							newChunkPos = new int3(0,0,0);
							isFacingBorder = false;
						}
						else if(i == 2 && z > 0){
							neighborBlock = data[y*Chunk.chunkWidth+(z-1)];
							neighborCoord = new int3(0, y, z-1);
							newChunkPos = new int3(0,0,0);
							isFacingBorder = false;
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
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+z));
								}

								if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
							else{
								if(objectSeamless[ushort.MaxValue-neighborBlock]){
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z+newChunkPos.y)*Chunk.chunkWidth+z));
								}

								if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
						}

						if(thisBlock == 0)
							continue;

						if(CheckPlacement(neighborBlock)){
							LoadMesh(0, y, z, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir, isFacingBorder:isFacingBorder);
						}
					}
				}
			}
			return;
		}
		// X+ Side
		else if(xP){
			skipDir = 3;
			chunkDir = 1;

			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int z=0; z<Chunk.chunkWidth; z++){
					for(int i=0; i < 6; i++){

						if(i == skipDir)
							continue;

						if((z == 0 || z == Chunk.chunkWidth-1) && (i == 4 || i == 5 || i == 1))
							continue;

						if((z == 0 && i == 2) || (z == Chunk.chunkWidth-1 && i == 0))
							continue;

						thisBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];
						thisCoord = new int3(Chunk.chunkWidth-1, y, z);

						if(i == 1){
							neighborBlock = neighbordata[y*Chunk.chunkWidth+z];
							neighborCoord = new int3(0, y, z);
							newChunkPos = new int3(1, 0, 0);
							isFacingBorder = true;
						}
						else if(i == 4 && y < Chunk.chunkDepth-1){
							neighborBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+(y+1)*Chunk.chunkWidth+z];
							neighborCoord = new int3(Chunk.chunkWidth-1, y+1, z);
							newChunkPos = new int3(0, 0, Chunk.chunkWidth-1);
							isFacingBorder = false;
						}
						else if(i == 5 && y > 0){
							neighborBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+z];
							neighborCoord = new int3(Chunk.chunkWidth-1, y-1, z);
							newChunkPos = new int3(0, 0, Chunk.chunkWidth-1);
							isFacingBorder = false;
						}
						else if(i == 0 && z < Chunk.chunkWidth-1){
							neighborBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(z+1)];
							neighborCoord = new int3(Chunk.chunkWidth-1, y, z+1);
							newChunkPos = new int3(0,0, Chunk.chunkWidth-1);
							isFacingBorder = false;
						}
						else if(i == 2 && z > 0){
							neighborBlock = data[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(z-1)];
							neighborCoord = new int3(Chunk.chunkWidth-1, y, z-1);
							newChunkPos = new int3(0,0, Chunk.chunkWidth-1);
							isFacingBorder = false;
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
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z + newChunkPos.y)*Chunk.chunkWidth+z));
								}

								if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
							else{
								if(objectSeamless[ushort.MaxValue-neighborBlock]){
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+(newChunkPos.z), y, (pos.z + newChunkPos.y)*Chunk.chunkWidth+z));
								}

								if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
						}

						if(thisBlock == 0)
							continue;

						if(CheckPlacement(neighborBlock)){
							LoadMesh(Chunk.chunkWidth-1, y, z, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir, isFacingBorder:isFacingBorder);
						}
					}
				}
			}
			return;
		}
		// Z- Side
		else if(zM){
			skipDir = 0;
			chunkDir = 2;

			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int x=0; x<Chunk.chunkWidth; x++){
					for(int i=0; i < 6; i++){

						if(i == skipDir)
							continue;

						if((x == 0 || x == Chunk.chunkWidth-1) && (i == 4 || i == 5 || i == 2))
							continue;

						if((x == 0 && i == 3) || (x == Chunk.chunkWidth-1 && i == 1))
							continue;

						thisBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];
						thisCoord = new int3(x, y, 0);

						if(i == 2){
							neighborBlock = neighbordata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
							neighborCoord = new int3(x, y, Chunk.chunkWidth-1);
							newChunkPos = new int3(0, -1, Chunk.chunkWidth-1);
							isFacingBorder = true;
						}
						else if(i == 4 && y < Chunk.chunkDepth-1 && (x != 0 && x != Chunk.chunkWidth-1)){
							neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y+1)*Chunk.chunkWidth];
							neighborCoord = new int3(x, y+1, 0);
							newChunkPos = new int3(0, 0, 0);
							isFacingBorder = false;
						}
						else if(i == 5 && y > 0 && (x != 0 && x != Chunk.chunkWidth-1)){
							neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth];
							neighborCoord = new int3(x, y-1, 0);
							newChunkPos = new int3(0, 0, 0);
							isFacingBorder = false;
						}
						else if(i == 1 && x < Chunk.chunkWidth-1){
							neighborBlock = data[(x+1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];
							neighborCoord = new int3(x+1, y, 0);
							newChunkPos = new int3(0, 0, 0);
							isFacingBorder = false;
						}
						else if(i == 3 && x > 0){
							neighborBlock = data[(x-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];
							neighborCoord = new int3(x-1, y, 0);
							newChunkPos = new int3(0, 0, 0);
							isFacingBorder = false;
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
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+x, y, (pos.z + newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.z)));
								}

								if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
							else{
								if(objectSeamless[ushort.MaxValue-neighborBlock]){
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+x, y, (pos.z + newChunkPos.y)*Chunk.chunkWidth+(newChunkPos.z)));
								}

								if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
						}

						if(thisBlock == 0)
							continue;

						if(CheckPlacement(neighborBlock)){
							LoadMesh(x, y, 0, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir, isFacingBorder:isFacingBorder);
						}
					}
				}
			}
			return;
		}
		// Z+ Side
		else if(zP){
			skipDir = 2;
			chunkDir = 0;

			for(int y=0; y<Chunk.chunkDepth; y++){
				for(int x=0; x<Chunk.chunkWidth; x++){
					for(int i=0; i < 6; i++){

						if(i == skipDir)
							continue;

						if((x == 0 || x == Chunk.chunkWidth-1) && (i == 4 || i == 5 || i == 0))
							continue;

						if((x == 0 && i == 3) || (x == Chunk.chunkWidth-1 && i == 1))
							continue;

						thisBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
						thisCoord = new int3(x, y, Chunk.chunkWidth-1);

						if(i == 0){
							neighborBlock = neighbordata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth];
							neighborCoord = new int3(x, y, 0);
							newChunkPos = new int3(0, 1, 0);
							isFacingBorder = true;
						}
						else if(i == 4 && y < Chunk.chunkDepth-1 && (x != 0 && x != Chunk.chunkWidth-1)){
							neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y+1)*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
							neighborCoord = new int3(x, y+1, Chunk.chunkWidth-1);
							newChunkPos = new int3(0, 0, Chunk.chunkWidth-1);
							isFacingBorder = false;
						}
						else if(i == 5 && y > 0 && (x != 0 && x != Chunk.chunkWidth-1)){
							neighborBlock = data[x*Chunk.chunkWidth*Chunk.chunkDepth+(y-1)*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
							neighborCoord = new int3(x, y-1, Chunk.chunkWidth-1);
							newChunkPos = new int3(0, 0, Chunk.chunkWidth-1);
							isFacingBorder = false;
						}
						else if(i == 1 && x < Chunk.chunkWidth-1){
							neighborBlock = data[(x+1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
							neighborCoord = new int3(x+1, y, Chunk.chunkWidth-1);
							newChunkPos = new int3(0,0, Chunk.chunkWidth-1);
							isFacingBorder = false;
						}
						else if(i == 3 && x > 0){
							neighborBlock = data[(x-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)];
							neighborCoord = new int3(x-1, y, Chunk.chunkWidth-1);
							newChunkPos = new int3(0,0, Chunk.chunkWidth-1);
							isFacingBorder = false;
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
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+x, y, (pos.z + newChunkPos.y)*Chunk.chunkWidth+newChunkPos.z));
								}

								if((blockWashable[neighborBlock] || neighborBlock == 0) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
							else{
								if(objectSeamless[ushort.MaxValue-neighborBlock]){
									toBUD.Add(new int3((pos.x + newChunkPos.x)*Chunk.chunkWidth+x, y, (pos.z + newChunkPos.y)*Chunk.chunkWidth+newChunkPos.z));
								}

								if((objectWashable[ushort.MaxValue-neighborBlock]) && !reload){
									toLoadEvent.Add(thisCoord);
								}
							}
						}

						if(thisBlock == 0)
							continue;

						if(CheckPlacement(neighborBlock)){
							LoadMesh(x, y, Chunk.chunkWidth-1, i, neighborCoord, thisBlock, true, cachedCubeVerts, cachedUVVerts, cachedCubeNormal, chunkDir, isFacingBorder:isFacingBorder);
						}
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
    private bool LoadMesh(int x, int y, int z, int dir, int3 neighborIndex, ushort blockCode, bool load, NativeArray<Vector3> cacheCubeVert, NativeArray<Vector2> cacheCubeUV, NativeArray<Vector3> cacheCubeNormal, byte chunkDir, int lookahead=0, bool isFacingBorder=true){
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

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
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

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
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

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
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

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
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

    		AddLightUV(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
    		AddLightUVExtra(cacheCubeUV, x, y, z, dir, neighborIndex, chunkDir, isFacingBorder:isFacingBorder);
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
    private void AddLightUV(NativeArray<Vector2> array, int x, int y, int z, int dir, int3 neighborIndex, byte chunkDir, bool isFacingBorder=true){
    	int maxLightLevel = 15;
    	int currentLightLevel;

    	if(isFacingBorder)
    		currentLightLevel = GetOtherLight(neighborIndex.x, neighborIndex.y, neighborIndex.z);
    	else
    		currentLightLevel = GetNeighborLight(neighborIndex.x, neighborIndex.y, neighborIndex.z);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector2(maxLightLevel, 1);
	    	array[1] = new Vector2(maxLightLevel, 1);
	    	array[2] = new Vector2(maxLightLevel, 1);
	    	array[3] = new Vector2(maxLightLevel, 1);
	    	return;
    	}

    	bool xm = true;
    	bool xp = true;
    	bool zm = true;
    	bool zp = true;
    	bool ym = true;
    	bool yp = true;

    	if(neighborIndex.x > 1 || (neighborIndex.x == 1 && dir != 3) || (neighborIndex.x == 0 && dir == 1))
    		xm = false;
    	if(neighborIndex.x < Chunk.chunkWidth-2 || (neighborIndex.x == Chunk.chunkWidth-2 && dir != 1) || (neighborIndex.x == Chunk.chunkWidth-1 && dir == 3))
    		xp = false;
    	if(neighborIndex.z > 1 || (neighborIndex.z == 1 && dir != 2) || (neighborIndex.z == 0 && dir == 0))
    		zm = false;
    	if(neighborIndex.z < Chunk.chunkWidth-2 || (neighborIndex.z == Chunk.chunkWidth-2 && dir != 0) || (neighborIndex.z == Chunk.chunkWidth-1 && dir == 2))
    		zp = false;
    	if(neighborIndex.y > 1 || (neighborIndex.y == 1 && dir != 5) || (neighborIndex.y == 0 && dir == 4))
    		ym = false;
    	if(neighborIndex.y < Chunk.chunkDepth-2 || (neighborIndex.y == Chunk.chunkDepth-2 && dir != 4) || (neighborIndex.y == Chunk.chunkDepth-1 && dir == 5))
    		yp = false;

    	CalculateLightCorners(neighborIndex, dir, array, currentLightLevel, xm, xp, zm, zp, ym, yp, chunkDir, isFacingBorder);
    }

    // Sets the secondary UV of Lightmaps
    private void AddLightUVExtra(NativeArray<Vector2> array, int x, int y, int z, int dir, int3 neighborIndex, byte chunkDir, bool isFacingBorder=true){
    	int maxLightLevel = 15;
    	int currentLightLevel;

    	if(isFacingBorder)
    		currentLightLevel = GetOtherLight(neighborIndex.x, neighborIndex.y, neighborIndex.z, isNatural:false);
    	else
    		currentLightLevel = GetNeighborLight(neighborIndex.x, neighborIndex.y, neighborIndex.z, isNatural:false);

    	// If light is full blown
    	if(currentLightLevel == maxLightLevel){
	    	array[0] = new Vector2(array[0].x, maxLightLevel);
	    	array[1] = new Vector2(array[1].x, maxLightLevel);
	    	array[2] = new Vector2(array[2].x, maxLightLevel);
	    	array[3] = new Vector2(array[3].x, maxLightLevel);
	    	return;
    	}

    	bool xm = true;
    	bool xp = true;
    	bool zm = true;
    	bool zp = true;
    	bool ym = true;
    	bool yp = true;

    	if(neighborIndex.x > 0)
    		xm = false;
    	if(neighborIndex.x < Chunk.chunkWidth-1)
    		xp = false;
    	if(neighborIndex.z > 0)
    		zm = false;
    	if(neighborIndex.z < Chunk.chunkWidth-1)
    		zp = false;
    	if(neighborIndex.y > 0 || (neighborIndex.y == 0 && dir == 4))
    		ym = false;
    	if(neighborIndex.y < Chunk.chunkDepth-1)
    		yp = false;

    	CalculateLightCornersExtra(neighborIndex, dir, array, currentLightLevel, xm, xp, zm, zp, ym, yp, chunkDir, isFacingBorder);
    }

    private void CalculateLightCorners(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp, byte chunkDir, bool isFacingBorder){
    	if(isFacingBorder){
	    	// North
	    	if(dir == 0)
	    		SetCorner(array, pos, currentLightLevel, 1, 4, 3, 5, xm, xp, zm, zp, ym, yp, 0);
	    	// East
	    	else if(dir == 1)
	    		SetCorner(array, pos, currentLightLevel, 2, 4, 0, 5, xm, xp, zm, zp, ym, yp, 1);
	    	// South
	     	else if(dir == 2)
	    		SetCorner(array, pos, currentLightLevel, 3, 4, 1, 5, xm, xp, zm, zp, ym, yp, 2);
	    	// West
	      	else if(dir == 3)
	    		SetCorner(array, pos, currentLightLevel, 0, 4, 2, 5, xm, xp, zm, zp, ym, yp, 3);
	      	// Up
	    	else if(dir == 4)
	    		SetCorner(array, pos, currentLightLevel, 1, 2, 3, 0, xm, xp, zm, zp, ym, yp, 4);
	    	// Down
	     	else
	    		SetCorner(array, pos, currentLightLevel, 1, 0, 3, 2, xm, xp, zm, zp, ym, yp, 5);
	    }
	    else{
	    	if(dir == 0)
	    		SetCornerBorder(array, pos, currentLightLevel, 1, 4, 3, 5, xm, xp, zm, zp, ym, yp, 0, chunkDir);
	    	// East
	    	else if(dir == 1)
	    		SetCornerBorder(array, pos, currentLightLevel, 2, 4, 0, 5, xm, xp, zm, zp, ym, yp, 1, chunkDir);
	    	// South
	     	else if(dir == 2)
	    		SetCornerBorder(array, pos, currentLightLevel, 3, 4, 1, 5, xm, xp, zm, zp, ym, yp, 2, chunkDir);
	    	// West
	      	else if(dir == 3)
	    		SetCornerBorder(array, pos, currentLightLevel, 0, 4, 2, 5, xm, xp, zm, zp, ym, yp, 3, chunkDir);
	    	else if(dir == 4)
	    		SetCornerBorder(array, pos, currentLightLevel, 1,2,3,0, xm, xp, zm, zp, ym, yp, 4, chunkDir);
	    	else if(dir == 5)
	    		SetCornerBorder(array, pos, currentLightLevel, 1,0,3,2, xm, xp, zm, zp, ym, yp, 5, chunkDir);
	    }
    }

    private void CalculateLightCornersExtra(int3 pos, int dir, NativeArray<Vector2> array, int currentLightLevel, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp, byte chunkDir, bool isFacingBorder){
    	if(isFacingBorder){
	    	// North
	    	if(dir == 0)
	    		SetCornerExtra(array, pos, currentLightLevel, 1, 4, 3, 5, xm, xp, zm, zp, ym, yp, 0);
	    	// East
	    	else if(dir == 1)
	    		SetCornerExtra(array, pos, currentLightLevel, 2, 4, 0, 5, xm, xp, zm, zp, ym, yp, 1);
	    	// South
	     	else if(dir == 2)
	    		SetCornerExtra(array, pos, currentLightLevel, 3, 4, 1, 5, xm, xp, zm, zp, ym, yp, 2);
	    	// West
	      	else if(dir == 3)
	    		SetCornerExtra(array, pos, currentLightLevel, 0, 4, 2, 5, xm, xp, zm, zp, ym, yp, 3);
	      	// Up
	    	else if(dir == 4)
	    		SetCornerExtra(array, pos, currentLightLevel, 1, 2, 3, 0, xm, xp, zm, zp, ym, yp, 4);
	    	// Down
	     	else
	    		SetCornerExtra(array, pos, currentLightLevel, 1, 0, 3, 2, xm, xp, zm, zp, ym, yp, 5);
	    }
	    else{
	    	if(dir == 0)
	    		SetCornerBorderExtra(array, pos, currentLightLevel, 1, 4, 3, 5, xm, xp, zm, zp, ym, yp, 0, chunkDir);
	    	// East
	    	else if(dir == 1)
	    		SetCornerBorderExtra(array, pos, currentLightLevel, 2, 4, 0, 5, xm, xp, zm, zp, ym, yp, 1, chunkDir);
	    	// South
	     	else if(dir == 2)
	    		SetCornerBorderExtra(array, pos, currentLightLevel, 3, 4, 1, 5, xm, xp, zm, zp, ym, yp, 2, chunkDir);
	    	// West
	      	else if(dir == 3)
	    		SetCornerBorderExtra(array, pos, currentLightLevel, 0, 4, 2, 5, xm, xp, zm, zp, ym, yp, 3, chunkDir);
	    	else if(dir == 4)
	    		SetCornerBorderExtra(array, pos, currentLightLevel, 1,2,3,0, xm, xp, zm, zp, ym, yp, 4, chunkDir);
	    	else if(dir == 5)
	    		SetCornerBorderExtra(array, pos, currentLightLevel, 1,0,3,2, xm, xp, zm, zp, ym, yp, 5, chunkDir);
	    }
    }

    private bool CheckBorder(int dir, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp){
    	if(xm && dir == 3)
    		return false;
    	else if(xp && dir == 1)
    		return false;
    	else if(zm && dir == 2)
    		return false;
    	else if(zp && dir == 0)
    		return false;
    	else if(ym && dir == 5)
    		return false;
    	else if(yp && dir == 4)
    		return false;
    	else
    		return true;
    }

    private bool CheckTransient(int facing, bool xm, bool zm, bool xp, bool zp){
    	if((facing == 0 || facing == 2) && (xm || xp))
    		return true;
    	if((facing == 1 || facing == 3) && (zm || zp))
    		return true;
    	if((facing == 4 || facing == 5) && (xm || zm || xp || zp))
    		return true;
    	return false;
    }

    private int GetVertexLight(int current, int l1, int l2, int l3, int l4, int l5, int l6, int l7, int l8){
    	int val = 0;

    	// Populate outer values
    	val += (Max(current, l1, l2, l5) << 24);
    	val += (Max(current, l2, l3, l6) << 16);
    	val += (Max(current, l3, l4, l7) << 8);
    	val += (Max(current, l4, l1, l8));

    	return val;
    }

    private int ProcessTransient(int facing, bool xm, bool zm, bool xp, bool zp, int currentLight, int l1, int l2, int l3, int l4, int l5, int l6, int l7, int l8){
    	return GetVertexLight(currentLight, l1, l2, l3, l4, l5, l6, l7, l8);
    }

    private void SetCorner(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);
    	int transientValue;

    	if(xm || xp || zm || zp || ym || yp){
	    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp))
	    		light1 = GetOtherLight(pos.x, pos.y, pos.z, dir1, isNatural:true);
	    	else
	    		light1 = currentLightLevel;
	    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp))
	    		light2 = GetOtherLight(pos.x, pos.y, pos.z, dir2, isNatural:true);
	    	else
	    		light2 = currentLightLevel;
	    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp))
	    		light3 = GetOtherLight(pos.x, pos.y, pos.z, dir3, isNatural:true);
	    	else
	    		light3 = currentLightLevel;
	    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp))
	    		light4 = GetOtherLight(pos.x, pos.y, pos.z, dir4, isNatural:true);
	    	else
	    		light4 = currentLightLevel;

	    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && CheckBorder(dir2, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
	    		light5 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else{
	    		light5 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && CheckBorder(dir3, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
	    		light6 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else{
	    		light6 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && CheckBorder(dir4, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
	    		light7 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else{
	    		light7 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && CheckBorder(dir1, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
	    		light8 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else{
	    		light8 = currentLightLevel;
	    	}  	
    	}
    	else{
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
    	}
    	
		if(CheckTransient(facing, xm, zm, xp, zp)){
			transientValue = ProcessTransient(facing, xm, zm, xp, zp, currentLightLevel, light1, light2, light3, light4, light5, light6, light7, light8);
			array[0] = new Vector2(transientValue >> 24, 1);
			array[1] = new Vector2(((transientValue >> 16) & 0x000000FF), 1);
			array[2] = new Vector2(((transientValue >> 8) & 0x000000FF), 1);
			array[3] = new Vector2((transientValue & 0x000000FF), 1);
			return;
		}

		array[0] = new Vector2(Max(light1, light2, light5, currentLightLevel), 1);
		array[1] = new Vector2(Max(light2, light3, light6, currentLightLevel), 1);
		array[2] = new Vector2(Max(light3, light4, light7, currentLightLevel), 1);
		array[3] = new Vector2(Max(light4, light1, light8, currentLightLevel), 1);
    }

    private void SetCornerBorder(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp, int facing, byte chunkDir){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);
    	int transientValue;

    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp))
    		light1 = GetNeighborLight(pos.x, pos.y, pos.z, dir1, isNatural:true);
    	else
    		light1 = GetOtherLight(pos.x, pos.y, pos.z, dir1, isNatural:true, chunkDir:chunkDir, currentLight:currentLightLevel);
    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp))
    		light2 = GetNeighborLight(pos.x, pos.y, pos.z, dir2, isNatural:true);
    	else
    		light2 = GetOtherLight(pos.x, pos.y, pos.z, dir2, isNatural:true, chunkDir:chunkDir, currentLight:currentLightLevel);
    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp))
    		light3 = GetNeighborLight(pos.x, pos.y, pos.z, dir3, isNatural:true);
    	else
    		light3 = GetOtherLight(pos.x, pos.y, pos.z, dir3, isNatural:true, chunkDir:chunkDir, currentLight:currentLightLevel);
    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp))
    		light4 = GetNeighborLight(pos.x, pos.y, pos.z, dir4, isNatural:true);
    	else
    		light4 = GetOtherLight(pos.x, pos.y, pos.z, dir4, isNatural:true, chunkDir:chunkDir, currentLight:currentLightLevel);

    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && CheckBorder(dir2, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
    		light5 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    	}
    	else if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp) || CheckBorder(dir2, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && dir1 == chunkDir) || (!CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && dir2 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
	    		light5 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else
	    		light5 = currentLightLevel;
    	}
    	else{
    		light5 = currentLightLevel;
    	}

    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && CheckBorder(dir3, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
    		light6 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    	}
    	else if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp) || CheckBorder(dir3, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && dir2 == chunkDir) || (!CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && dir3 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
	    		light6 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else
	    		light6 = currentLightLevel;
    	}
    	else{
    		light6 = currentLightLevel;
    	}

    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && CheckBorder(dir4, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
    		light7 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    	}
    	else if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp) || CheckBorder(dir4, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && dir3 == chunkDir) || (!CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && dir4 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
	    		light7 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else
	    		light7 = currentLightLevel;
    	}
    	else{
    		light7 = currentLightLevel;
    	}

    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && CheckBorder(dir1, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
    		light8 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
    	}
    	else if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp) || CheckBorder(dir1, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && dir4 == chunkDir) || (!CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && dir1 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
	    		light8 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:true);
	    	}
	    	else
	    		light8 = currentLightLevel;
    	}
    	else{
    		light8 = currentLightLevel;
    	}  	

		transientValue = ProcessTransient(facing, xm, zm, xp, zp, currentLightLevel, light1, light2, light3, light4, light5, light6, light7, light8);
		array[0] = new Vector2(transientValue >> 24, 1);
		array[1] = new Vector2(((transientValue >> 16) & 0x000000FF), 1);
		array[2] = new Vector2(((transientValue >> 8) & 0x000000FF), 1);
		array[3] = new Vector2((transientValue & 0x000000FF), 1);
    }

    private void SetCornerExtra(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp, int facing){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);
    	int transientValue;

    	if(xm || xp || zm || zp || ym || yp){
	    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp))
	    		light1 = GetOtherLight(pos.x, pos.y, pos.z, dir1, isNatural:false);
	    	else
	    		light1 = currentLightLevel;
	    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp))
	    		light2 = GetOtherLight(pos.x, pos.y, pos.z, dir2, isNatural:false);
	    	else
	    		light2 = currentLightLevel;
	    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp))
	    		light3 = GetOtherLight(pos.x, pos.y, pos.z, dir3, isNatural:false);
	    	else
	    		light3 = currentLightLevel;
	    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp))
	    		light4 = GetOtherLight(pos.x, pos.y, pos.z, dir4, isNatural:false);
	    	else
	    		light4 = currentLightLevel;

	    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && CheckBorder(dir2, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
	    		light5 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light5 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && CheckBorder(dir3, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
	    		light6 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light6 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && CheckBorder(dir4, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
	    		light7 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light7 = currentLightLevel;
	    	}

	    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && CheckBorder(dir1, xm, xp, zm, zp, ym, yp)){
	    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
	    		light8 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else{
	    		light8 = currentLightLevel;
	    	}  	
    	}
    	else{
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
    	}

		if(CheckTransient(facing, xm, zm, xp, zp)){
			transientValue = ProcessTransient(facing, xm, zm, xp, zp, currentLightLevel, light1, light2, light3, light4, light5, light6, light7, light8);
			array[0] = new Vector2(array[0].x, transientValue >> 24);
			array[1] = new Vector2(array[1].x, ((transientValue >> 16) & 0x000000FF));
			array[2] = new Vector2(array[2].x, ((transientValue >> 8) & 0x000000FF));
			array[3] = new Vector2(array[3].x, (transientValue & 0x000000FF));
			return;
		}

		array[0] = new Vector2(array[0].x, Max(light1, light2, light5, currentLightLevel));
		array[1] = new Vector2(array[1].x, Max(light2, light3, light6, currentLightLevel));
		array[2] = new Vector2(array[2].x, Max(light3, light4, light7, currentLightLevel));
		array[3] = new Vector2(array[3].x, Max(light4, light1, light8, currentLightLevel));
    }

    private void SetCornerBorderExtra(NativeArray<Vector2> array, int3 pos, int currentLightLevel, int dir1, int dir2, int dir3, int dir4, bool xm, bool xp, bool zm, bool zp, bool ym, bool yp, int facing, byte chunkDir){
    	int light1, light2, light3, light4, light5, light6, light7, light8;
    	int3 diagonal = new int3(0,0,0);
    	int transientValue;

    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp))
    		light1 = GetNeighborLight(pos.x, pos.y, pos.z, dir1, isNatural:false);
    	else
    		light1 = GetOtherLight(pos.x, pos.y, pos.z, dir1, isNatural:false, chunkDir:chunkDir, currentLight:currentLightLevel);
    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp))
    		light2 = GetNeighborLight(pos.x, pos.y, pos.z, dir2, isNatural:false);
    	else
    		light2 = GetOtherLight(pos.x, pos.y, pos.z, dir2, isNatural:false, chunkDir:chunkDir, currentLight:currentLightLevel);
    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp))
    		light3 = GetNeighborLight(pos.x, pos.y, pos.z, dir3, isNatural:false);
    	else
    		light3 = GetOtherLight(pos.x, pos.y, pos.z, dir3, isNatural:false, chunkDir:chunkDir, currentLight:currentLightLevel);
    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp))
    		light4 = GetNeighborLight(pos.x, pos.y, pos.z, dir4, isNatural:false);
    	else
    		light4 = GetOtherLight(pos.x, pos.y, pos.z, dir4, isNatural:false, chunkDir:chunkDir, currentLight:currentLightLevel);

    	if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && CheckBorder(dir2, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
    		light5 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    	}
    	else if(CheckBorder(dir1, xm, xp, zm, zp, ym, yp) || CheckBorder(dir2, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && dir1 == chunkDir) || (!CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && dir2 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir1] + VoxelData.offsets[dir2];
	    		light5 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else
	    		light5 = currentLightLevel;
    	}
    	else{
    		light5 = currentLightLevel;
    	}

    	if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && CheckBorder(dir3, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
    		light6 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    	}
    	else if(CheckBorder(dir2, xm, xp, zm, zp, ym, yp) || CheckBorder(dir3, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir2, xm, xp, zm, zp, ym, yp) && dir2 == chunkDir) || (!CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && dir3 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir2] + VoxelData.offsets[dir3];
	    		light6 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else
	    		light6 = currentLightLevel;
    	}
    	else{
    		light6 = currentLightLevel;
    	}

    	if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && CheckBorder(dir4, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
    		light7 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    	}
    	else if(CheckBorder(dir3, xm, xp, zm, zp, ym, yp) || CheckBorder(dir4, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir3, xm, xp, zm, zp, ym, yp) && dir3 == chunkDir) || (!CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && dir4 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir3] + VoxelData.offsets[dir4];
	    		light7 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else
	    		light7 = currentLightLevel;
    	}
    	else{
    		light7 = currentLightLevel;
    	}

    	if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && CheckBorder(dir1, xm, xp, zm, zp, ym, yp)){
    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
    		light8 = GetNeighborLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
    	}
    	else if(CheckBorder(dir4, xm, xp, zm, zp, ym, yp) || CheckBorder(dir1, xm, xp, zm, zp, ym, yp)){
    		if((!CheckBorder(dir4, xm, xp, zm, zp, ym, yp) && dir4 == chunkDir) || (!CheckBorder(dir1, xm, xp, zm, zp, ym, yp) && dir1 == chunkDir)){
	    		diagonal = VoxelData.offsets[dir4] + VoxelData.offsets[dir1];
	    		light8 = GetOtherLight(pos.x, pos.y, pos.z, diagonal, isNatural:false);
	    	}
	    	else
	    		light8 = currentLightLevel;
    	}
    	else{
    		light8 = currentLightLevel;
    	}  	

		transientValue = ProcessTransient(facing, xm, zm, xp, zp, currentLightLevel, light1, light2, light3, light4, light5, light6, light7, light8);
		array[0] = new Vector2(array[0].x, transientValue >> 24);
		array[1] = new Vector2(array[1].x, ((transientValue >> 16) & 0x000000FF));
		array[2] = new Vector2(array[2].x, ((transientValue >> 8) & 0x000000FF));
		array[3] = new Vector2(array[3].x, (transientValue & 0x000000FF));
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

	// Gets neighbor light level
	private int GetNeighborLight(int x, int y, int z, int dir, bool isNatural=true){
		int3 neighborCoord = new int3(x, y, z) + VoxelData.offsets[dir];

		if(neighborCoord.x >= Chunk.chunkWidth)
			neighborCoord.x -= Chunk.chunkWidth;
		if(neighborCoord.x < 0)
			neighborCoord.x += Chunk.chunkWidth;
		if(neighborCoord.z >= Chunk.chunkWidth)
			neighborCoord.z -= Chunk.chunkWidth;
		if(neighborCoord.z < 0)
			neighborCoord.z += Chunk.chunkWidth;

		if(isNatural)
			return lightdata[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z] & 0x0F;
		else
			return lightdata[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z] >> 4;
	}

	// Gets neighbor light level
	private int GetNeighborLight(int x, int y, int z, int3 dir, bool isNatural=true){
		int3 coord = new int3(x, y, z) + dir;
		if(isNatural)
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
		else
			return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] >> 4;
	}

	// Gets neighbor light level
	private int GetNeighborLight(int x, int y, int z, bool isNatural=true){
		if(isNatural)
			return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
		else
			return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
	}

	// Gets neighbor light level
	private int GetOtherLight(int x, int y, int z, int dir, bool isNatural=true, byte chunkDir=5, int currentLight=0){
		int3 neighborCoord = new int3(x, y, z) + VoxelData.offsets[dir];

		if(neighborCoord.x >= Chunk.chunkWidth)
			if(chunkDir == 1)
				neighborCoord.x -= Chunk.chunkWidth;
			else
				return currentLight;

		if(neighborCoord.x < 0)
			if(chunkDir == 3)
				neighborCoord.x += Chunk.chunkWidth;
			else
				return currentLight;

		if(neighborCoord.z >= Chunk.chunkWidth)
			if(chunkDir == 0)
				neighborCoord.z -= Chunk.chunkWidth;
			else
				return currentLight;

		if(neighborCoord.z < 0)
			if(chunkDir == 2)
				neighborCoord.z += Chunk.chunkWidth;
			else
				return currentLight;

		if(neighborCoord.y >= Chunk.chunkDepth || neighborCoord.y < 0)
			return currentLight;

		if(isNatural)
			return neighborlight[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z] & 0x0F;
		else
			return neighborlight[neighborCoord.x*Chunk.chunkWidth*Chunk.chunkDepth+neighborCoord.y*Chunk.chunkWidth+neighborCoord.z] >> 4;
	}

	// Gets neighbor light level
	private int GetOtherLight(int x, int y, int z, bool isNatural=true){
		if(y < 0){
			y++; 
			
			if(isNatural)
				return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
			else
				return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;

		}
		else if(y >= Chunk.chunkDepth){
			y--;

			if(isNatural)
				return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
			else
				return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
		}


		if(isNatural)
			return neighborlight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
		else
			return neighborlight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
	}

	// Gets neighbor light level
	private int GetOtherLight(int x, int y, int z, int3 dir, bool isNatural=true){
		int3 coord = new int3(x, y, z) + dir;

		if(coord.x >= Chunk.chunkWidth)
			coord.x -= Chunk.chunkWidth;
		if(coord.x < 0)
			coord.x += Chunk.chunkWidth;
		if(coord.z >= Chunk.chunkWidth)
			coord.z -= Chunk.chunkWidth;
		if(coord.z < 0)
			coord.z += Chunk.chunkWidth;

		if(coord.y < 0 || coord.y >= Chunk.chunkDepth){
			if(isNatural)
				return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] & 0x0F;
			else
				return lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z] >> 4;
		}

		if(isNatural)
			return neighborlight[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
		else
			return neighborlight[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] >> 4;
	}

	// Gets neighbor light level
	private int GetOtherLight(int3 coord, bool isNatural=true){
		if(coord.y < 0){
			coord = new int3(coord.x, coord.y+1, coord.z); 
			
			if(isNatural)
				return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
			else
				return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] >> 4;

		}
		else if(coord.y >= Chunk.chunkDepth){
			coord = new int3(coord.x, coord.y-1, coord.z);

			if(isNatural)
				return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
			else
				return lightdata[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] >> 4;
		}

		if(isNatural)
			return neighborlight[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] & 0x0F;
		else
			return neighborlight[coord.x*Chunk.chunkWidth*Chunk.chunkDepth+coord.y*Chunk.chunkWidth+coord.z] >> 4;
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