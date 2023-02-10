using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

/*
Takes the ShadowMap and turns it into a progressive lightmap
*/
[BurstCompile]
public struct CalculateLightMapJob : IJob{
	public NativeArray<byte> lightMap;
	public NativeArray<byte> shadowMap;
	public NativeArray<int4> lightSources;
	public NativeList<int3> bfsq; // Breadth-first search queue
	public NativeList<int4> bfsqExtra;
	public NativeList<byte5> bfsqDir;
	public NativeHashSet<int3> visited;
	public NativeArray<byte> changed;
	public NativeList<byte5> directionalList;

	[ReadOnly]
	public NativeArray<byte> memoryLightMap;
	[ReadOnly]
	public NativeArray<byte> heightMap;
	[ReadOnly]
	public int chunkWidth;
	[ReadOnly]
	public int chunkDepth;
	[ReadOnly]
	public ChunkPos cpos;


	public void Execute(){
		int3 current;
		int4 currentExtra;
		byte5 currentDirectional;
		int bfsqSize;

		/***************************************
		Natural Light
		***************************************/
		DetectSunlight();
		bfsqSize = bfsq.Length;	
		
		// Iterates through queue
		while(bfsqSize > 0){
			current = bfsq[0];

			if(visited.Contains(current)){
				bfsq.RemoveAt(0);
				bfsqSize = bfsq.Length;	
				continue;
			}

			ScanSurroundings(current, (byte)(lightMap[GetIndex(current)] & 0x0F), true);

			visited.Add(current);
			bfsq.RemoveAt(0);
			bfsqSize = bfsq.Length;	
		}

		DetectDirectionals();
		QuickSort(isLightSource:false);

		int currentLevel = 15;
		bool searchedCurrentLevel = false;
		bool initiateSearch = false;
		bool initiateExtraLightSearch = false;
		int lastIndex = directionalList.Length - 1;
		int index = 0;
		bfsqSize = 0;

		if(directionalList.Length > 0){
			while(bfsqSize > 0 || !initiateSearch || lastIndex >= 0){
				initiateSearch = true;
 
				if(bfsqDir.Length > 0 && currentLevel == -1){
					searchedCurrentLevel = true;
					currentLevel = bfsqDir[0].w;
				}
				else if(bfsqDir.Length == 0 && lastIndex >= 0){
					searchedCurrentLevel = false;
					currentLevel = directionalList[lastIndex].w;
				}
				else if(bfsqDir.Length == 0 && lastIndex == -1){
					break;
				}

				if(!searchedCurrentLevel){
					for(int i=lastIndex; i >= -1; i--){
						if(i == -1){
							searchedCurrentLevel = true;
							currentLevel = -1;
							lastIndex = -1;
							break;
						}

						if(directionalList[i].w == currentLevel){
							index = GetIndex(directionalList[i].GetCoords());

							if((lightMap[index] & 0x0F) <= directionalList[i].w){
								bfsqDir.Add(directionalList[i]);
								lightMap[index] = (byte)((lightMap[index] & 0xF0) + directionalList[i].w);
								shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + directionalList[i].k);
							}
						}
						else{
							searchedCurrentLevel = true;
							currentLevel = directionalList[i].w;
							lastIndex = i;
							break;
						}
					}
				}

				if(bfsqDir.Length == 0){
					continue;
				}

				currentDirectional = bfsqDir[0];
				bfsqDir.RemoveAt(0);

				if(currentDirectional.w == currentLevel && lastIndex >= 0)
					searchedCurrentLevel = false;

				ScanDirectionals(currentDirectional.GetCoords(), currentDirectional.w, true, currentDirectional.k);

				bfsqSize = bfsqDir.Length;
			}
		}

		visited.Clear();
		directionalList.Clear();
		bfsqDir.Clear();

		/***************************************
		Extra Lights
		***************************************/

		QuickSort();
		
		currentLevel = 15;
		searchedCurrentLevel = false;
		initiateExtraLightSearch = false;
		lastIndex = lightSources.Length - 1;
		index = 0;
		bfsqSize = 0;

		visited.Clear();

		if(lightSources.Length > 0){
			while(bfsqSize > 0 || !initiateExtraLightSearch || lastIndex >= 0){
				initiateExtraLightSearch = true;
 
				if(bfsqExtra.Length > 0 && currentLevel == -1){
					searchedCurrentLevel = true;
					currentLevel = bfsqExtra[0].w;
				}
				else if(bfsqExtra.Length == 0 && lastIndex >= 0){
					searchedCurrentLevel = false;
					currentLevel = lightSources[lastIndex].w;
				}
				else if(bfsqExtra.Length == 0 && lastIndex == -1){
					break;
				}

				if(!searchedCurrentLevel){
					for(int i=lastIndex; i >= -1; i--){
						if(i == -1){
							searchedCurrentLevel = true;
							currentLevel = -1;
							lastIndex = -1;
							break;
						}

						if(lightSources[i].w == currentLevel){
							index = GetIndex(lightSources[i].xyz);

							if((lightMap[index] >> 4) < directionalList[i].w){
								bfsqExtra.Add(lightSources[i]);
								lightMap[index] = (byte)((lightMap[index] & 0x0F) + (lightSources[i].w << 4));
								shadowMap[index] = (byte)((shadowMap[index] & 0x0F) + (3 << 4));
							}
						}
						else{
							searchedCurrentLevel = true;
							currentLevel = lightSources[i].w;
							lastIndex = i;
							break;
						}
					}
				}

				if(bfsqExtra.Length == 0){
					continue;
				}

				currentExtra = bfsqExtra[0];
				bfsqExtra.RemoveAt(0);

				if(currentExtra.w == currentLevel && lastIndex >= 0)
					searchedCurrentLevel = false;

				ScanSurroundings(currentExtra.xyz, (byte)currentExtra.w, false);

				bfsqSize = bfsqExtra.Length;
			}
		}

		directionalList.Clear();

		DetectDirectionals(extraLight:true);
		QuickSort(isLightSource:false);

		visited.Clear();

		currentLevel = 15;
		searchedCurrentLevel = false;
		initiateExtraLightSearch = false;
		lastIndex = directionalList.Length - 1;
		index = 0;
		bfsqSize = 0;

		if(directionalList.Length > 0){
			while(bfsqSize > 0 || !initiateExtraLightSearch || lastIndex >= 0){
				initiateExtraLightSearch = true;
 
				if(bfsqDir.Length > 0 && currentLevel == -1){
					searchedCurrentLevel = true;
					currentLevel = bfsqDir[0].w;
				}
				else if(bfsqDir.Length == 0 && lastIndex >= 0){
					searchedCurrentLevel = false;
					currentLevel = directionalList[lastIndex].w;
				}
				else if(bfsqDir.Length == 0 && lastIndex == -1){
					break;
				}

				if(!searchedCurrentLevel){
					for(int i=lastIndex; i >= -1; i--){
						if(i == -1){
							searchedCurrentLevel = true;
							currentLevel = -1;
							lastIndex = -1;
							break;
						}

						if(directionalList[i].w == currentLevel){
							index = GetIndex(directionalList[i].GetCoords());

							if((lightMap[index] >> 4) <= directionalList[i].w){
								bfsqDir.Add(directionalList[i]);
								lightMap[index] = (byte)((lightMap[index] & 0x0F) + (directionalList[i].w << 4));
								shadowMap[index] = (byte)((shadowMap[index] & 0x0F) + (directionalList[i].k << 4));
							}
						}
						else{
							searchedCurrentLevel = true;
							currentLevel = directionalList[i].w;
							lastIndex = i;
							break;
						}
					}
				}

				if(bfsqDir.Length == 0){
					continue;
				}

				currentDirectional = bfsqDir[0];
				bfsqDir.RemoveAt(0);

				if(currentDirectional.w == currentLevel && lastIndex >= 0)
					searchedCurrentLevel = false;

				ScanDirectionals(currentDirectional.GetCoords(), currentDirectional.w, false, currentDirectional.k);

				bfsqSize = bfsqDir.Length;
			}
		}

		CheckBorders();
	}

	public void CheckBorders(){
		if(memoryLightMap.Length == 0){
			CheckBordersFirstLoad();
		}
		else{
			CheckBordersMemory();
		}
	}

	// Checks if chunk has empty space in neighborhood
	public void CheckBordersFirstLoad(){
		int index;

		for(int x=0; x < chunkWidth; x++){
			for(int y=0; y < chunkDepth; y++){
				for(int z=0; z < chunkWidth; z++){
					index = x*chunkWidth*chunkDepth+y*chunkWidth+z;

					if(x == 0 && (((lightMap[index] & 0x0F) > 0 && (shadowMap[index] & 0x0F) != 2) || (((lightMap[index] >> 4) > 0) && (shadowMap[index] >> 4) != 2) || (changed[0] & 1) == 1))
						changed[0] = (byte)(changed[0] | 1);
					else if(x == chunkWidth-1 && (((lightMap[index] & 0x0F) > 0 && (shadowMap[index] & 0x0F) != 2) || (((lightMap[index] >> 4) > 0) && (shadowMap[index] >> 4) != 2) || (changed[0] & 2) == 2))
						changed[0] = (byte)(changed[0] | 2);
					if(z == 0 && (((lightMap[index] & 0x0F) > 0 && (shadowMap[index] & 0x0F) != 2) || (((lightMap[index] >> 4) > 0) && (shadowMap[index] >> 4) != 2) || (changed[0] & 4) == 4))
						changed[0] = (byte)(changed[0] | 4);
					else if(z == chunkWidth-1 && (((lightMap[index] & 0x0F) > 0 && (shadowMap[index] & 0x0F) != 2) || (((lightMap[index] >> 4) > 0) && (shadowMap[index] >> 4) != 2) || (changed[0] & 8) == 8))
						changed[0] = (byte)(changed[0] | 8);

					if(x == 0 && ((lightMap[index] & 0x0F) == 0 && (shadowMap[index] & 0x0F) == 1))
						changed[0] = (byte)(changed[0] | 1);
					else if(x == chunkWidth-1 && ((lightMap[index] & 0x0F) == 0 && (shadowMap[index] & 0x0F) == 1))
						changed[0] = (byte)(changed[0] | 2);
					if(z == 0 && ((lightMap[index] & 0x0F) == 0 && (shadowMap[index] & 0x0F) == 1))
						changed[0] = (byte)(changed[0] | 4);
					else if(z == chunkWidth-1 && ((lightMap[index] & 0x0F) == 0 && (shadowMap[index] & 0x0F) == 1))
						changed[0] = (byte)(changed[0] | 8);

					if(cpos.y > 0 && y == 0 && (lightMap[index] & 0x0F) > 0 && (shadowMap[index] & 0x0F) >= 2 || (((lightMap[index] >> 4) > 0) && (shadowMap[index] >> 4) != 2))
						changed[0] = (byte)(changed[0] | 16);
					if(cpos.y < Chunk.chunkMaxY && y == Chunk.chunkDepth-1 && (lightMap[index] & 0x0F) > 0 && (shadowMap[index] & 0x0F) >= 2 || (((lightMap[index] >> 4) > 0) && (shadowMap[index] >> 4) != 2))
						changed[0] = (byte)(changed[0] | 32);
				}
			}
		}
	}

	// Checks for light value changes in borders
	public void CheckBordersMemory(){
		int x, y, z, index;

		// xm
		x = 0;
		for(y=0; y < chunkDepth; y++){
			for(z=0; z < chunkWidth; z++){
				index = y*chunkWidth+z;

				if(lightMap[index] != memoryLightMap[index]){
					changed[0] = (byte)(changed[0] | 1);
				}
			}
		}

		// xp
		x = chunkWidth-1;
		for(y=0; y < chunkDepth; y++){
			for(z=0; z < chunkWidth; z++){
				index = x*chunkWidth*chunkDepth+y*chunkWidth+z;

				if(lightMap[index] != memoryLightMap[index]){
					changed[0] = (byte)(changed[0] | 2);
				}
			}
		}

		// zm
		z = 0;
		for(y=0; y < chunkDepth; y++){
			for(x=0; x < chunkWidth; x++){
				index = x*chunkWidth*chunkDepth+y*chunkWidth;

				if(lightMap[index] != memoryLightMap[index]){
					changed[0] = (byte)(changed[0] | 4);
				}
			}
		}

		// zp
		z = chunkWidth-1;
		for(y=0; y < chunkDepth; y++){
			for(x=0; x < chunkWidth; x++){
				index = x*chunkWidth*chunkDepth+y*chunkWidth+z;

				if(lightMap[index] != memoryLightMap[index]){
					changed[0] = (byte)(changed[0] | 8);
				}
			}
		}

		// ym
		if(cpos.y > 0){
			y = 0;
			for(z=0; z < chunkWidth; z++){
				for(x=0; x < chunkWidth; x++){
					index = x*chunkWidth*chunkDepth+z;

					if(lightMap[index] != memoryLightMap[index]){
						changed[0] = (byte)(changed[0] | 16);
					}
				}
			}
		}

		// yp
		if(cpos.y < Chunk.chunkMaxY){
			y = chunkDepth-1;
			for(z=0; z < chunkWidth; z++){
				for(x=0; x < chunkWidth; x++){
					index = x*chunkWidth*chunkDepth+y*chunkWidth+z;

					if(lightMap[index] != memoryLightMap[index]){
						changed[0] = (byte)(changed[0] | 32);
					}
				}
			}
		}
	}

	public void DetectDirectionals(bool extraLight=false){
		int index;
		// xm
		for(int z=0; z < chunkWidth; z++){
			for(int y=heightMap[z]; y >= 0; y--){
				index = y*chunkWidth+z;

				if(!extraLight){
					if((shadowMap[index] & 0x0F) >= 7)
						directionalList.Add(new byte5(0, y, z, lightMap[index] & 0x0F, shadowMap[index] & 0x0F));
				}
				else{
					if((shadowMap[index] >> 4) >= 7)
						directionalList.Add(new byte5(0, y, z, lightMap[index] >> 4, shadowMap[index] >> 4));					
				}
			}
		}

		// xp
		for(int z=0; z < chunkWidth; z++){
			for(int y=heightMap[(chunkWidth-1)*chunkWidth+z]; y >= 0; y--){
				index = (chunkWidth-1)*chunkWidth*chunkDepth+y*chunkWidth+z;

				if(!extraLight){
					if((shadowMap[index] & 0x0F) >= 7)
						directionalList.Add(new byte5((chunkWidth-1), y, z, lightMap[index] & 0x0F, shadowMap[index] & 0x0F));
				}
				else{
					if((shadowMap[index] >> 4) >= 7)
						directionalList.Add(new byte5((chunkWidth-1), y, z, lightMap[index] >> 4, shadowMap[index] >> 4));	
				}
			}
		}

		// zm
		for(int x=0; x < chunkWidth; x++){
			for(int y=heightMap[x*chunkWidth]; y >= 0; y--){
				index = x*chunkWidth*chunkDepth+y*chunkWidth;

				if(!extraLight){
					if((shadowMap[index] & 0x0F) >= 7)
						directionalList.Add(new byte5(x, y, 0, lightMap[index] & 0x0F, shadowMap[index] & 0x0F));
				}
				else{
					if((shadowMap[index] >> 4) >= 7)
						directionalList.Add(new byte5(x, y, 0, lightMap[index] >> 4, shadowMap[index] >> 4));	
				}
			}
		}

		// zp
		for(int x=0; x < chunkWidth; x++){
			for(int y=heightMap[x*chunkWidth+(chunkWidth-1)]; y >= 0; y--){
				index = x*chunkWidth*chunkDepth+y*chunkWidth+(chunkWidth-1);

				if(!extraLight){
					if((shadowMap[index] & 0x0F) >= 7)
						directionalList.Add(new byte5(x, y, (chunkWidth-1), lightMap[index] & 0x0F, shadowMap[index] & 0x0F));
				}
				else{
					if((shadowMap[index] >> 4) >= 7)
						directionalList.Add(new byte5(x, y, (chunkWidth-1), lightMap[index] >> 4, shadowMap[index] >> 4));	
				}
			}
		}

		// ym
		for(int x=0; x < chunkWidth; x++){
			for(int z=0; z < chunkWidth; z++){
				index = x*chunkWidth*chunkDepth+z;

				if(!extraLight){
					if((shadowMap[index] & 0x0F) >= 7)
						directionalList.Add(new byte5(x, 0, z, lightMap[index] & 0x0F, shadowMap[index] & 0x0F));
				}
				else{
					if((shadowMap[index] >> 4) >= 7)
						directionalList.Add(new byte5(x, 0, z, lightMap[index] >> 4, shadowMap[index] >> 4));
				}
			}
		}

		// yp
		for(int x=0; x < chunkWidth; x++){
			for(int z=0; z < chunkWidth; z++){
				index = x*chunkWidth*chunkDepth+(chunkDepth-1)*chunkWidth+z;

				if(!extraLight){
					if((shadowMap[index] & 0x0F) >= 7)
						directionalList.Add(new byte5(x, chunkDepth-1, z, lightMap[index] & 0x0F, shadowMap[index] & 0x0F));
				}
				else{
					if((shadowMap[index] >> 4) >= 7)
						directionalList.Add(new byte5(x, chunkDepth-1, z, lightMap[index] >> 4, shadowMap[index] >> 4));
				}
			}
		}
	}

	// Checks the surroundings and adds light fallout
	public void ScanSurroundings(int3 c, byte currentLight, bool isNatural){
		if(currentLight == 1)
			return;

		int3 aux;
		int index;

		// East
		aux = new int3(c.x+1, c.y, c.z);

		if(aux.x < chunkWidth){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + 3);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) + (3 << 4));
						bfsqExtra.Add(new int4(aux, (currentLight-1)));
					}			
				}
			}
		}

		// West
		aux = new int3(c.x-1, c.y, c.z);

		if(aux.x >= 0){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + 3);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) + (3 << 4));
						bfsqExtra.Add(new int4(aux, (currentLight-1)));
					}					
				}
			}
		}	

		// North
		aux = new int3(c.x, c.y, c.z+1);

		if(aux.z < chunkWidth){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + 3);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) + (3 << 4));
						bfsqExtra.Add(new int4(aux, (currentLight-1)));
					}					
				}
			}
		}	

		// South
		aux = new int3(c.x, c.y, c.z-1);

		if(aux.z >= 0){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + 3);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) + (3 << 4));
						bfsqExtra.Add(new int4(aux, (currentLight-1)));
					}					
				}
			}
		}

		// Up
		aux = new int3(c.x, c.y+1, c.z);

		if(aux.y < chunkDepth){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + 3);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) + (3 << 4));
						bfsqExtra.Add(new int4(aux, (currentLight-1)));
					}					
				}
			}
		}

		// Down
		aux = new int3(c.x, c.y-1, c.z);

		if(aux.y >= 0){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) + 3);
						bfsq.Add(aux);
					}
				}
				else{
					if((lightMap[index] >> 4) < currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) + ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) + (3 << 4));
						bfsqExtra.Add(new int4(aux, (currentLight-1)));
					}					
				}
			}
		}
	}

	// Checks the surroundings and adds light fallout
	public void ScanDirectionals(int3 c, byte currentLight, bool isNatural, byte newShadow){
		if(currentLight == 0)
			return;

		int3 aux;
		int index;

		// East
		aux = new int3(c.x+1, c.y, c.z);

		if(aux.x < chunkWidth){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | newShadow);
						bfsqDir.Add(new byte5(aux, (byte)(currentLight-1), newShadow));
					}
				}
				else{
					if((lightMap[index] >> 4) <= currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) | ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | (newShadow << 4));
						bfsqDir.Add(new byte5(aux, (byte)(currentLight-1), newShadow));
					}
				}
			}
		}

		// West
		aux = new int3(c.x-1, c.y, c.z);

		if(aux.x >= 0){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | newShadow);
						bfsqDir.Add(new byte5(aux, (byte)(currentLight-1), newShadow));
					}
				}
				else{
					if((lightMap[index] >> 4) <= currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) | ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | (newShadow << 4));
						bfsqDir.Add(new byte5(aux, (byte)(currentLight-1), newShadow));
					}					
				}
			}
		}	

		// North
		aux = new int3(c.x, c.y, c.z+1);

		if(aux.z < chunkWidth){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | newShadow);
						bfsqDir.Add(new byte5(aux, (byte)(currentLight-1), newShadow));
					}
				}
				else{
					if((lightMap[index] >> 4) <= currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) | ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | (newShadow << 4));
						bfsqDir.Add(new byte5(aux, (byte)(currentLight-1), newShadow));
					}					
				}
			}
		}	

		// South
		aux = new int3(c.x, c.y, c.z-1);

		if(aux.z >= 0){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | newShadow);
						bfsqDir.Add(new byte5(aux, (byte)(currentLight-1), newShadow));
					}
				}
				else{
					if((lightMap[index] >> 4) <= currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) | ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | (newShadow << 4));
						bfsqDir.Add(new byte5(aux, (byte)(currentLight-1), newShadow));
					}					
				}
			}
		}

		// Up
		aux = new int3(c.x, c.y+1, c.z);

		if(aux.y < chunkDepth){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | newShadow);
						bfsqDir.Add(new byte5(aux, (byte)(currentLight-1), newShadow));
					}
				}
				else{
					if((lightMap[index] >> 4) <= currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) | ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | (newShadow << 4));
						bfsqDir.Add(new byte5(aux, (byte)(currentLight-1), newShadow));
					}					
				}
			}
		}

		// Down
		aux = new int3(c.x, c.y-1, c.z);

		if(aux.y >= 0){
			if(!visited.Contains(aux)){
				index = GetIndex(aux);

				if(isNatural){
					if((lightMap[index] & 0x0F) < currentLight && (shadowMap[index] & 0x0F) != 0){
						lightMap[index] = (byte)(((lightMap[index] & 0xF0) + (currentLight-1)));
						shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | newShadow);
						bfsqDir.Add(new byte5(aux, (byte)(currentLight-1), newShadow));
					}
				}
				else{
					if((lightMap[index] >> 4) <= currentLight && (shadowMap[index] >> 4) > 0){
						lightMap[index] = (byte)(((lightMap[index] & 0x0F) | ((currentLight-1) << 4)));
						shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | (newShadow << 4));
						bfsqDir.Add(new byte5(aux, (byte)(currentLight-1), newShadow));
					}
				}
			}
		}
	}

	private void QuickSort(bool isLightSource=true){
		int init = 0;
		int end;

		if(isLightSource){
			end = lightSources.Length - 1;

			if(lightSources.Length == 0)
				return;
		}
		else{
			end = directionalList.Length - 1;

			if(directionalList.Length == 0)
				return;
		}

		QuickSort(init, end, isLightSource:isLightSource);
	}

	private void QuickSort(int init, int end, bool isLightSource=true){
		if(isLightSource){
			if(init < end){
				int4 val = lightSources[init];
				int i = init +1;
				int e = end;

				while(i <= e){
					if(lightSources[i].w <= val.w)
						i++;
					else if(val.w < lightSources[e].w)
						e--;
					else{
						int4 swap = lightSources[i];
						lightSources[i] = lightSources[e];
						lightSources[e] = swap;
						i++;
						e--;
					}
				}

				lightSources[init] = lightSources[e];
				lightSources[e] = val;

				QuickSort(init, e - 1, isLightSource:isLightSource);
				QuickSort(e + 1, end, isLightSource:isLightSource);
			}
		}
		else{
			if(init < end){
				byte5 val = directionalList[init];
				int i = init +1;
				int e = end;

				while(i <= e){
					if(directionalList[i].w <= val.w)
						i++;
					else if(val.w < directionalList[e].w)
						e--;
					else{
						byte5 swap = directionalList[i];
						directionalList[i] = directionalList[e];
						directionalList[e] = swap;
						i++;
						e--;
					}
				}

				directionalList[init] = directionalList[e];
				directionalList[e] = val;

				QuickSort(init, e - 1, isLightSource:isLightSource);
				QuickSort(e + 1, end, isLightSource:isLightSource);
			}			
		}
	}

	public int GetIndex(int3 c){
		return c.x*chunkWidth*chunkDepth+c.y*chunkWidth+c.z;
	}
	public int GetIndex(int x, int y, int z){
		return x*chunkWidth*chunkDepth+y*chunkWidth+z;
	}

	// Iterates through heightMap and populates the BFS queue
	public void DetectSunlight(){
		int index;
		byte height;
		byte maxLightLevel = 15;

		for(int z=0; z < chunkWidth; z++){
			for(int x=0; x < chunkWidth; x++){

				if(heightMap[x*chunkWidth+z] >= chunkDepth-1){
					continue;
				}

				height = (byte)(heightMap[x*chunkWidth+z]+1);
				index = x*chunkWidth*chunkDepth+height*chunkWidth+z;

				if((shadowMap[index] & 0x0F) == 2){
					bfsq.Add(new int3(x, height, z));
					lightMap[index] = (byte)((lightMap[index] & 0xF0) | maxLightLevel);
					AnalyzeSunShaft(x, height, z);

					// Sets the remaining skylight above to max
					for(int y=height+1; y < chunkDepth; y++){
						index = x*chunkWidth*chunkDepth+y*chunkWidth+z;
						lightMap[index] = (byte)((lightMap[index] & 0xF0) | maxLightLevel);

						AnalyzeSunShaft(x, y, z);
					}
				}
			}
		}
	}

	// Finds if a natural light affected block should propagate
	public bool AnalyzeSunShaft(int x, int y, int z){
		bool xp = false;
		bool xm = false;
		bool zp = false;
		bool zm = false;
		byte shadow;

		// Checks borders
		if(x > 0){
			xm = true;
		}
		if(x < chunkWidth-2){
			xp = true;
		}
		if(z > 0){
			zm = true;
		}
		if(z < chunkWidth-2){
			zp = true;
		}

		if(xm){
			shadow = (byte)(shadowMap[GetIndex(x-1, y, z)] & 0x0F);
			if(shadow != 2 && shadow != 0){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}
		if(xp){
			shadow = (byte)(shadowMap[GetIndex(x+1, y, z)] & 0x0F);
			if(shadow != 2 && shadow != 0){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}
		if(zm){
			shadow = (byte)(shadowMap[GetIndex(x, y, z-1)] & 0x0F);
			if(shadow != 2 && shadow != 0){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}
		if(zp){
			shadow = (byte)(shadowMap[GetIndex(x, y, z+1)] & 0x0F);
			if(shadow != 2 && shadow != 0){
				bfsq.Add(new int3(x, y, z));
				return true;
			}
		}

		return false;
	}
}