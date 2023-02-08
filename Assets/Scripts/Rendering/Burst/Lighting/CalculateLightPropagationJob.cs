using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;


[BurstCompile]
public struct CalculateLightPropagationJob : IJob{
	public NativeArray<byte> lightMap1;
	public NativeArray<byte> lightMap2;
	public NativeArray<byte> shadowMap1;
	public NativeArray<byte> shadowMap2;

	public NativeList<int3> bfsq1; // Breadth-first search queue
	public NativeList<int3> bfsq2;
	public NativeHashSet<int3> visited1;
	public NativeHashSet<int3> visited2;
	public NativeList<int3> bfsqe1;
	public NativeList<int3> bfsqe2;
	public NativeHashSet<int3> visitede1;
	public NativeHashSet<int3> visitede2;
	public NativeList<int4> aux;
	public NativeHashSet<int4> hashAux;

	public NativeArray<byte> changed; // [0] = Update current Chunk after the neighbor, [1] = Update neighbor Chunk, [2] = Update neighbor with lights, [3] = Xm,Xp,Zm,Zp flags of chunk of neighbor to calculate borders

	[ReadOnly]
	public int chunkWidth;
	[ReadOnly]
	public int chunkDepth;
	[ReadOnly]
	public byte borderCode; // 0 = xm, 1 = xp, 2 = zm, 3 = zp


	public void Execute(){
		int index1, index2;

		// Processing Shadows
		// xm
		if(borderCode == 0){
			for(int y=0; y < chunkDepth; y++){
				for(int z=0; z < chunkWidth; z++){
					index1 = y*chunkWidth+z;
					index2 = (chunkWidth-1)*chunkDepth*chunkWidth+y*chunkWidth+z;

					ProcessShadowCode(shadowMap1[index1] & 0x0F, shadowMap2[index2] & 0x0F, index1, index2, borderCode);
					ProcessShadowCode(shadowMap1[index1] >> 4, shadowMap2[index2] >> 4, index1, index2, borderCode, extraLight:true);
				}
			}
		}
		// xp
		else if(borderCode == 1){
			for(int y=0; y < chunkDepth; y++){
				for(int z=0; z < chunkWidth; z++){
					index1 = (chunkWidth-1)*chunkDepth*chunkWidth+y*chunkWidth+z;
					index2 = y*chunkWidth+z;

					ProcessShadowCode(shadowMap1[index1] & 0x0F, shadowMap2[index2] & 0x0F, index1, index2, borderCode);
					ProcessShadowCode(shadowMap1[index1] >> 4, shadowMap2[index2] >> 4, index1, index2, borderCode, extraLight:true);
				}
			}			
		}
		// zm
		else if(borderCode == 2){
			for(int y=0; y < chunkDepth; y++){
				for(int x=0; x < chunkWidth; x++){
					index1 = x*chunkDepth*chunkWidth+y*chunkWidth;
					index2 = x*chunkDepth*chunkWidth+y*chunkWidth+(chunkWidth-1);

					ProcessShadowCode(shadowMap1[index1] & 0x0F, shadowMap2[index2] & 0x0F, index1, index2, borderCode);
					ProcessShadowCode(shadowMap1[index1] >> 4, shadowMap2[index2] >> 4, index1, index2, borderCode, extraLight:true);
				}
			}
		}
		// zp
		else if(borderCode == 3){
			for(int y=0; y < chunkDepth; y++){
				for(int x=0; x < chunkWidth; x++){
					index1 = x*chunkDepth*chunkWidth+y*chunkWidth+(chunkWidth-1);
					index2 = x*chunkDepth*chunkWidth+y*chunkWidth;

					ProcessShadowCode(shadowMap1[index1] & 0x0F, shadowMap2[index2] & 0x0F, index1, index2, borderCode);
					ProcessShadowCode(shadowMap1[index1] >> 4, shadowMap2[index2] >> 4, index1, index2, borderCode, extraLight:true);
				}
			}			
		}

		int3 current;

		// CURRENT CHUNK LIGHT PROPAG =====================================
		int bfsq1Size = bfsq1.Length;

		while(bfsq1Size > 0){
			current = bfsq1[0];

			if(visited1.Contains(current)){
				bfsq1.RemoveAt(0);
				bfsq1Size = bfsq1.Length;
				continue;
			}

			ScanSurroundings(current, (byte)(lightMap1[GetIndex(current)] & 0x0F), true, 1, lightMap1, lightMap2, shadowMap1, shadowMap2, bfsq1, bfsq2, visited1, visited2, borderCode);
			WriteLightUpdateFlag(current, borderCode);

			visited1.Add(current);
			bfsq1.RemoveAt(0);
			bfsq1Size = bfsq1.Length;
		}

		// NEIGHBOR CHUNK LIGHT PROPAG ====================================
		int bfsq2Size = bfsq2.Length;

		while(bfsq2Size > 0){
			changed[1] = 1;

			current = bfsq2[0];

			if(visited2.Contains(current)){
				bfsq2.RemoveAt(0);
				bfsq2Size = bfsq2.Length;
				continue;
			}

			ScanSurroundings(current, (byte)(lightMap2[GetIndex(current)] & 0x0F), false, 2, lightMap2, lightMap1, shadowMap2, shadowMap1, bfsq2, bfsq1, visited2, visited1, borderCode);
			WriteLightUpdateFlag(current, borderCode);

			visited2.Add(current);
			bfsq2.RemoveAt(0);
			bfsq2Size = bfsq2.Length;
		}

		// CURRENT CHUNK EXTRA LIGHT PROPAG =====================================
		int bfsqe1Size = bfsqe1.Length;

		while(bfsqe1Size > 0){
			current = bfsqe1[0];

			if(visitede1.Contains(current)){
				bfsqe1.RemoveAt(0);
				bfsqe1Size = bfsqe1.Length;
				continue;
			}

			ScanSurroundings(current, (byte)(lightMap1[GetIndex(current)] >> 4), true, 1, lightMap1, lightMap2, shadowMap1, shadowMap2, bfsqe1, bfsqe2, visitede1, visitede2, borderCode, extraLight:true);
			WriteLightUpdateFlag(current, borderCode);

			visitede1.Add(current);
			bfsqe1.RemoveAt(0);
			bfsqe1Size = bfsqe1.Length;
		}

		// NEIGHBOR CHUNK EXTRA LIGHT PROPAG ====================================
		int bfsqe2Size = bfsqe2.Length;

		while(bfsqe2Size > 0){

			current = bfsqe2[0];

			if(visitede2.Contains(current)){
				bfsqe2.RemoveAt(0);
				bfsqe2Size = bfsqe2.Length;
				continue;
			}

			ScanSurroundings(current, (byte)(lightMap2[GetIndex(current)] >> 4), false, 2, lightMap2, lightMap1, shadowMap2, shadowMap1, bfsqe2, bfsqe1, visitede2, visitede1, borderCode, extraLight:true);
			WriteLightUpdateFlag(current, borderCode);

			visitede2.Add(current);
			bfsqe2.RemoveAt(0);
			bfsqe2Size = bfsqe2.Length;
		}
	}

	/*
	BFS into removing neighbor's directional shadow
	*/
	public void RemoveDirectionFromChunk(NativeArray<byte> selectedLightMap, NativeArray<byte> selectedShadowMap, byte currentShadow, int3 pos, byte borderCode, bool extraLight=false){
		int index = GetIndex(pos);
		int4 current;
		byte thisShadow, thisLight;
		bool firstIteration = true;
		int numIterations = 0;

		if(currentShadow < 7)
			return;

		if(!extraLight)
			aux.Add(new int4(pos, (selectedLightMap[index] & 0x0F)+1));
		else
			aux.Add(new int4(pos, (selectedLightMap[index] >> 4)+1));

		int auxSize = aux.Length;

		while(auxSize > 0){
			current = aux[0];
			index = GetIndex(current.xyz);

			if(hashAux.Contains(current)){
				aux.RemoveAt(0);
				auxSize = aux.Length;
				continue;
			}

			if(!extraLight){
				thisLight = (byte)(selectedLightMap[index] & 0x0F);
				thisShadow = (byte)(selectedShadowMap[index] & 0x0F);
			}
			else{
				thisLight = (byte)(selectedLightMap[index] >> 4);
				thisShadow = (byte)(selectedShadowMap[index] >> 4);
			}

			if(thisShadow == currentShadow){
				if(thisLight == current.w-1 && thisLight > 0){
					numIterations++;

					if(!extraLight){
						selectedLightMap[index] = (byte)(selectedLightMap[index] & 0xF0);
						selectedShadowMap[index] = (byte)((selectedShadowMap[index] & 0xF0) | 1);
					}
					else{
						selectedLightMap[index] = (byte)(selectedLightMap[index] & 0x0F);
						selectedShadowMap[index] = (byte)((selectedShadowMap[index] & 0x0F) | 16);
					}

					if(firstIteration){
						if(current.x == 0 && borderCode != 1)
							changed[3] = (byte)(changed[3] | 1);
						if(current.x == chunkWidth-1 && borderCode != 0)
							changed[3] = (byte)(changed[3] | 2);
						if(current.z == 0 && borderCode != 3)
							changed[3] = (byte)(changed[3] | 4);
						if(current.z == chunkWidth-1 && borderCode != 2)
							changed[3] = (byte)(changed[3] | 8);
					}
					else{
						if(current.x == 0)
							changed[3] = (byte)(changed[3] | 1);
						if(current.x == chunkWidth-1)
							changed[3] = (byte)(changed[3] | 2);
						if(current.z == 0)
							changed[3] = (byte)(changed[3] | 4);
						if(current.z == chunkWidth-1)
							changed[3] = (byte)(changed[3] | 8);						
					}

					if(current.x > 0)
						aux.Add(new int4(current.x-1, current.y, current.z, current.w-1));
					if(current.x < chunkWidth-1)
						aux.Add(new int4(current.x+1, current.y, current.z, current.w-1));
					if(current.z > 0)
						aux.Add(new int4(current.x, current.y, current.z-1, current.w-1));
					if(current.z < chunkWidth-1)
						aux.Add(new int4(current.x, current.y, current.z+1, current.w-1));
					if(current.y > 0)
						aux.Add(new int4(current.x, current.y-1, current.z, current.w-1));
					if(current.y < chunkDepth-1)
						aux.Add(new int4(current.x, current.y+1, current.z, current.w-1));

					hashAux.Add(current);

				}
			}

			aux.RemoveAt(0);
			auxSize = aux.Length;
			firstIteration = false;
		}

		if(numIterations > 0){
			changed[2] = 1;
			changed[0] = 1;
		}

		hashAux.Clear();
		aux.Clear();
	}

	// Checks if current block is bordering the neighbor chunk
	public bool CheckBorder(int3 c, byte borderCode, int side){
		if(side == 1){
			if(borderCode == 0 && c.x == 0)
				return true;
			if(borderCode == 1 && c.x == chunkWidth-1)
				return true;
			if(borderCode == 2 && c.z == 0)
				return true;
			if(borderCode == 3 && c.z == chunkWidth-1)
				return true;
		}
		else{
			if(borderCode == 0 && c.x == chunkWidth-1)
				return true;
			if(borderCode == 1 && c.x == 0)
				return true;
			if(borderCode == 2 && c.z == chunkWidth-1)
				return true;
			if(borderCode == 3 && c.z == 0)
				return true;
		}

		return false;
	}

	// Checks whether the neighbor border block to the given one was visited before
	public bool CheckVisited(int3 c, byte borderCode, int side){
		int3 originalNeighbor;

		if(side == 1){
			if(borderCode == 0)
				originalNeighbor = new int3((chunkWidth-1), c.y, c.z);
			else if(borderCode == 1)
				originalNeighbor = new int3(0, c.y, c.z);
			else if(borderCode == 2)
				originalNeighbor = new int3(c.x, c.y, (chunkWidth-1));
			else
				originalNeighbor = new int3(c.x, c.y, 0);
		}
		else{
			if(borderCode == 0)
				originalNeighbor = new int3(0, c.y, c.z);
			else if(borderCode == 1)
				originalNeighbor = new int3((chunkWidth-1), c.y, c.z);
			else if(borderCode == 2)
				originalNeighbor = new int3(c.x, c.y, 0);
			else
				originalNeighbor = new int3(c.x, c.y, (chunkWidth-1));			
		}

		if(side == 1)
			return visited2.Contains(originalNeighbor);
		else
			return visited1.Contains(originalNeighbor);
	}

	public int3 GetNeighborCoord(int3 c, byte borderCode, int side){
		int3 originalNeighbor;

		if(side == 1){
			if(borderCode == 0)
				originalNeighbor = new int3((chunkWidth-1), c.y, c.z);
			else if(borderCode == 1)
				originalNeighbor = new int3(0, c.y, c.z);
			else if(borderCode == 2)
				originalNeighbor = new int3(c.x, c.y, (chunkWidth-1));
			else
				originalNeighbor = new int3(c.x, c.y, 0);
		}
		
		else{
			if(borderCode == 0)
				originalNeighbor = new int3(0, c.y, c.z);
			else if(borderCode == 1)
				originalNeighbor = new int3((chunkWidth-1), c.y, c.z);
			else if(borderCode == 2)
				originalNeighbor = new int3(c.x, c.y, 0);
			else
				originalNeighbor = new int3(c.x, c.y, (chunkWidth-1));
		}
		

		return originalNeighbor;
	}	

	// Finds out which light processing the given shadow border must go through
	public void ProcessShadowCode(int a, int b, int index1, int index2, byte borderCode, bool extraLight=false){
		// Checking if was explored already
		int3 coord1, coord2;

		coord1 = GetCoord(index1);
		coord2 = GetCoord(index2);

		if(extraLight)
			if(visitede1.Contains(coord1) && this.visitede2.Contains(coord2))
				return;
		else
			if(visited1.Contains(coord1) && visited2.Contains(coord2))
				return;

		// Finding the order
		int shadowCode = a+b;

		if(shadowCode == 0)
			return;

		int aux;
		bool order = a <= b;

		if(b < a){
			aux = b;
			b = a;
			a = aux;
		}

		// 0-2, 0-3
		if((shadowCode == 2 || shadowCode == 3) && a == 0)
			ApplyShadowWork(1, order, index1, index2, borderCode, extraLight:extraLight);

		// 1-2, 1-3
		else if(a == 1 && (b == 2 || b == 3))
			ApplyShadowWork(2, order, index1, index2, borderCode, extraLight:extraLight);

		// 3-3
		else if(shadowCode == 6 && a == 3)
			ApplyShadowWork(3, order, index1, index2, borderCode, extraLight:extraLight);
		
		// 1-7, 1-8, 1-9, 1-10
		else if(b >= 7 && a == 1)
			ApplyShadowWork(4, order, index1, index2, borderCode, extraLight:extraLight);

		// All directionals linked to a 2 or 3 (e.g. 2-7, 3-7, 2-8, 3-8, etc.)
		else if(b >= 7 && (a == 2 || a == 3))
			ApplyShadowWork(5, order, index1, index2, borderCode, extraLight:extraLight);

		// Almost any combination of directionals
		else if(shadowCode >= 15)
			ApplyShadowWork(5, order, index1, index2, borderCode, extraLight:extraLight);

		// 2-3
		else if(a == 2 && b == 3)
			ApplyShadowWork(6, order, index1, index2, borderCode, extraLight:extraLight);

		// 0-7, 0-8, 0-9, 0-10
		else if(shadowCode >= 7 && a == 0)
			ApplyShadowWork(7, order, index1, index2, borderCode, extraLight:extraLight);
	}

	// Applies propagation of light 
	public void ApplyShadowWork(int workCode, bool normalOrder, int index1, int index2, byte borderCode, bool extraLight=false){
		// Update border UVs only
		if(workCode == 1){
			if(!normalOrder)
				changed[1] = 1;
		}

		// Propagate normally and sets the correct shadow direction
		else if(workCode == 2){
			if(normalOrder){
				if(!extraLight){
					lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) -1));
					shadowMap1[index1] = (byte)((shadowMap1[index1] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
					bfsq1.Add(GetCoord(index1));
					visited2.Add(GetCoord(index2));
				}
				else{
					lightMap1[index1] = (byte)((lightMap1[index1] & 0x0F) | ((lightMap2[index2] - 16) & 0xF0));
					shadowMap1[index1] = (byte)((shadowMap1[index1] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
					bfsqe1.Add(GetCoord(index1));
					visitede2.Add(GetCoord(index2));					
				}
			}
			else{
				if(!extraLight){
					lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) -1));
					shadowMap2[index2] = (byte)((shadowMap2[index2] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
					bfsq2.Add(GetCoord(index2));
					visited1.Add(GetCoord(index1));
				}
				else{
					lightMap2[index2] = (byte)((lightMap2[index2] & 0x0F) | ((lightMap1[index1] - 16) & 0xF0));
					shadowMap2[index2] = (byte)((shadowMap2[index2] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
					bfsqe2.Add(GetCoord(index2));
					visitede1.Add(GetCoord(index1));
				}
			}
		}

		// Find which side to propagate
		else if(workCode == 3){
			if(!extraLight){
				if((lightMap2[index2] & 0x0F) < ((lightMap1[index1] & 0x0F) -1)){
					lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) -1));
					shadowMap2[index2] = (byte)((shadowMap2[index2] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
					bfsq2.Add(GetCoord(index2));
					visited1.Add(GetCoord(index1));
				}
				else if((lightMap1[index1] & 0x0F) < ((lightMap2[index2] & 0x0F) -1)){
					lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) -1));
					shadowMap1[index1] = (byte)((shadowMap1[index1] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
					bfsq1.Add(GetCoord(index1));
					visited2.Add(GetCoord(index2));
				}
			}
			else{
				if((lightMap2[index2] & 0xF0) < ((lightMap1[index1] & 0xF0) - 16)){
					lightMap2[index2] = (byte)((lightMap2[index2] & 0x0F) | ((lightMap1[index1] - 16) & 0xF0));
					shadowMap2[index2] = (byte)((shadowMap2[index2] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
					bfsqe2.Add(GetCoord(index2));
					visitede1.Add(GetCoord(index1));
				}
				else if((lightMap1[index1] & 0xF0) < ((lightMap2[index2] & 0xF0) - 16)){
					lightMap1[index1] = (byte)((lightMap1[index1] & 0x0F) | ((lightMap2[index2] - 16) & 0xF0));
					shadowMap1[index1] = (byte)((shadowMap1[index1] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
					bfsqe1.Add(GetCoord(index1));
					visitede2.Add(GetCoord(index2));
				}
			}
		}

		// Propagate to a third chunk or dies because of lack of transmitter
		else if(workCode == 4){
			if(normalOrder){
				if(!extraLight){
					// If is the same direction, delete
					if(GetShadowDirection(borderCode, !normalOrder) == (shadowMap2[index2] & 0x0F)){
						RemoveDirectionFromChunk(lightMap2, shadowMap2, (byte)(shadowMap2[index2] & 0x0F), GetCoord(index2), borderCode, extraLight:extraLight);
					}
					// If not same direction, propagate
					else{
						// If actually can propagate some light
						if(((lightMap2[index2] & 0x0F) - 1) > 0){
							lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) - 1));
							shadowMap1[index1] = (byte)((shadowMap1[index1] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
							bfsq1.Add(GetCoord(index1));
							visited2.Add(GetCoord(index2));
						}
					}
				}
				else{
					// If is the same direction, delete
					if(GetShadowDirection(borderCode, !normalOrder) == ((shadowMap2[index2] & 0xF0) >> 4)){
						RemoveDirectionFromChunk(lightMap2, shadowMap2, (byte)((shadowMap2[index2] & 0xF0) >> 4), GetCoord(index2), borderCode, extraLight:extraLight);
					}
					// If not same direction, propagate
					else{
						// If actually can propagate some light
						if((((lightMap2[index2] & 0xF0) - 16) >> 4) > 0){
							lightMap1[index1] = (byte)((lightMap1[index1] & 0x0F) | ((lightMap2[index2] - 16) & 0xF0));
							shadowMap1[index1] = (byte)((shadowMap1[index1] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
							bfsqe1.Add(GetCoord(index1));
							visitede2.Add(GetCoord(index2));
						}
					}
				}
			}
			else{
				if(!extraLight){
					// If is the same direction, delete
					if(GetShadowDirection(borderCode, !normalOrder) == (shadowMap1[index1] & 0x0F)){
						RemoveDirectionFromChunk(lightMap1, shadowMap1, (byte)(shadowMap1[index1] & 0x0F), GetCoord(index1), borderCode, extraLight:extraLight);
					}
					// If not same direction, propagate
					else{
						// If actually can propagate some light
						if(((lightMap1[index1] & 0x0F) - 1) > 0){
							lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) - 1));
							shadowMap2[index2] = (byte)((shadowMap2[index2] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
							bfsq2.Add(GetCoord(index2));
							visited1.Add(GetCoord(index1));
						}
					}
				}
				else{
					// If is the same direction, delete
					if(GetShadowDirection(borderCode, !normalOrder) == ((shadowMap1[index1] & 0xF0) >> 4)){
						RemoveDirectionFromChunk(lightMap1, shadowMap1, (byte)((shadowMap1[index1] & 0xF0) >> 4), GetCoord(index1), borderCode, extraLight:extraLight);
					}
					// If not same direction, propagate
					else{
						// If actually can propagate some light
						if(((lightMap1[index1] - 16) >> 4) > 0){
							lightMap2[index2] = (byte)((lightMap2[index2] & 0x0F) | ((lightMap1[index1] - 16) & 0xF0));
							shadowMap2[index2] = (byte)((shadowMap2[index2] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
							bfsqe2.Add(GetCoord(index2));
							visitede1.Add(GetCoord(index1));
						}
					}
				}
			}
		}

		// If Directionals hit transmitters or directionals hit directionals
		else if(workCode == 5){
			if(normalOrder){
				if(!extraLight){
					// If not the same direction, try to expand
					if(GetShadowDirection(borderCode, !normalOrder) != (shadowMap2[index2] & 0x0F)){
						if((lightMap2[index2] & 0x0F) < (lightMap1[index1] & 0x0F) - 1){
							lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) - 1));
							shadowMap2[index2] = (byte)((shadowMap2[index2] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
							bfsq2.Add(GetCoord(index2));
						}
					}
					else{
						if(!CheckPropagatorAround(lightMap2, shadowMap2, GetCoord(index2), borderCode, extraLight:extraLight)){
							RemoveDirectionFromChunk(lightMap2, shadowMap2, (byte)(shadowMap2[index2] & 0x0F), GetCoord(index2), borderCode, extraLight:extraLight);
							lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) - 1));
							bfsq2.Add(GetCoord(index2));
							visited1.Add(GetCoord(index1));
						}
					}
				}
				else{
					// If not the same direction, try to expand
					if(GetShadowDirection(borderCode, !normalOrder) != (shadowMap2[index2] >> 4)){
						if(((lightMap2[index2] >> 4)) < (lightMap1[index1] >> 4) - 1){
							lightMap2[index2] = (byte)((lightMap2[index2] & 0x0F) | ((lightMap1[index1] - 16) & 0xF0));
							shadowMap2[index2] = (byte)((shadowMap2[index2] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
							bfsqe2.Add(GetCoord(index2));
						}
					}
					else{
						if(!CheckPropagatorAround(lightMap2, shadowMap2, GetCoord(index2), borderCode, extraLight:extraLight)){
							RemoveDirectionFromChunk(lightMap2, shadowMap2, (byte)(shadowMap2[index2] >> 4), GetCoord(index2), borderCode, extraLight:extraLight);
							lightMap2[index2] = (byte)((lightMap2[index2] & 0x0F) | ((lightMap1[index1] - 16) & 0xF0));
							bfsq2.Add(GetCoord(index2));
							visited1.Add(GetCoord(index1));
						}
						// If transmitter hits directional in border of extra light, try expand directionals
						else{
							bfsq2.Add(GetCoord(index2));
							visited1.Add(GetCoord(index1));
						} 
					}
				}
			}
			else{
				if(!extraLight){
					// If not the same direction, try to expand
					if(GetShadowDirection(borderCode, !normalOrder) != (shadowMap1[index1] & 0x0F)){
						if((lightMap1[index1] & 0x0F) < (lightMap2[index2] & 0x0F) - 1){
							lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) - 1));
							shadowMap1[index1] = (byte)((shadowMap1[index1] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
							bfsq1.Add(GetCoord(index1));
						}
					}
					else{
						if(!CheckPropagatorAround(lightMap1, shadowMap1, GetCoord(index1), borderCode, extraLight:extraLight)){
							RemoveDirectionFromChunk(lightMap1, shadowMap1, (byte)(shadowMap1[index1] & 0x0F), GetCoord(index1), borderCode, extraLight:extraLight);
							lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) - 1));
							bfsq1.Add(GetCoord(index1));
							visited2.Add(GetCoord(index2));
						}
					}
				}
				else{
					// If not the same direction, try to expand
					if(GetShadowDirection(borderCode, !normalOrder) != (shadowMap1[index1] >> 4)){
						if((lightMap1[index1] & 0xF0) < (lightMap2[index2] & 0xF0) - 16){
							lightMap1[index1] = (byte)((lightMap1[index1] & 0x0F) | ((lightMap2[index2] - 16) & 0xF0));
							shadowMap1[index1] = (byte)((shadowMap1[index1] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
							bfsqe1.Add(GetCoord(index1));
						}
					}
					else{
						if(!CheckPropagatorAround(lightMap1, shadowMap1, GetCoord(index1), borderCode, extraLight:extraLight)){
							RemoveDirectionFromChunk(lightMap1, shadowMap1, (byte)(shadowMap1[index1] >> 4), GetCoord(index1), borderCode, extraLight:extraLight);
							lightMap1[index1] = (byte)((lightMap1[index1] & 0x0F) | ((lightMap2[index2] - 16) & 0xF0));
							bfsq1.Add(GetCoord(index1));
							visited2.Add(GetCoord(index2));
						}
						// If transmitter hits directional in border of extra light, try expand directionals
						else{
							bfsq1.Add(GetCoord(index1));
							visited2.Add(GetCoord(index2));
						} 
					}			
				}
			}
		}

		// If Sunlight hits local propagation in neighbor
		else if(workCode == 6){
			if(normalOrder){
				if(!extraLight){
					if((lightMap1[index1] & 0x0F) < ((lightMap2[index2] & 0x0F) -1)){
						lightMap1[index1] = (byte)((lightMap1[index1] & 0xF0) | ((lightMap2[index2] & 0x0F) -1));
						shadowMap1[index1] = (byte)((shadowMap1[index1] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
						bfsq1.Add(GetCoord(index1));
					}	
				}
				else{
					if((lightMap1[index1] >> 4) < ((lightMap2[index2] >> 4) - 1)){
						lightMap1[index1] = (byte)((lightMap1[index1] & 0x0F) | ((lightMap2[index2] - 16) & 0xF0));
						shadowMap1[index1] = (byte)((shadowMap1[index1] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
						bfsqe1.Add(GetCoord(index1));
					}	
				}
			}
			else{
				if(!extraLight){
					if((lightMap2[index2] & 0x0F) < ((lightMap1[index1] & 0x0F) -1)){
						lightMap2[index2] = (byte)((lightMap2[index2] & 0xF0) | ((lightMap1[index1] & 0x0F) -1));
						shadowMap2[index2] = (byte)((shadowMap2[index2] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
						bfsq2.Add(GetCoord(index2));
					}	
				}
				else{
					if((lightMap2[index2] >> 4) < ((lightMap1[index1] >> 4) - 1)){
						lightMap2[index2] = (byte)((lightMap2[index2] & 0x0F) | ((lightMap1[index1] - 16) & 0xF0));
						shadowMap2[index2] = (byte)((shadowMap2[index2] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
						bfsqe2.Add(GetCoord(index2));
					}	
				}	
			}
		}

		// If a directional is besides a wall
		else if(workCode == 7){
			if(normalOrder){
				if(!extraLight){
					if(!CheckPropagatorAround(lightMap2, shadowMap2, GetCoord(index2), borderCode, extraLight:extraLight)){
						RemoveDirectionFromChunk(lightMap2, shadowMap2, (byte)(shadowMap2[index2] & 0x0F), GetCoord(index2), borderCode, extraLight:extraLight);
					}
				}
				else{
					if(!CheckPropagatorAround(lightMap2, shadowMap2, GetCoord(index2), borderCode, extraLight:extraLight)){
						RemoveDirectionFromChunk(lightMap2, shadowMap2, (byte)(shadowMap2[index2] >> 4), GetCoord(index2), borderCode, extraLight:extraLight);
					}					
				}
			}
			else{
				if(!extraLight){
					if(!CheckPropagatorAround(lightMap1, shadowMap1, GetCoord(index1), borderCode, extraLight:extraLight)){
						RemoveDirectionFromChunk(lightMap1, shadowMap1, (byte)(shadowMap1[index1] & 0x0F), GetCoord(index1), borderCode, extraLight:extraLight);
					}
				}
				else{
					if(!CheckPropagatorAround(lightMap1, shadowMap1, GetCoord(index1), borderCode, extraLight:extraLight)){
						RemoveDirectionFromChunk(lightMap1, shadowMap1, (byte)(shadowMap1[index1] >> 4), GetCoord(index1), borderCode, extraLight:extraLight);
					}					
				}
			}
		}
	}

	public byte GetShadowDirection(byte borderCode, bool normalOrder){
		if(normalOrder){
			if(borderCode == 0)
				return 8;
			if(borderCode == 1)
				return 7;
			if(borderCode == 2)
				return 10;
			else
				return 9;
		}
		else
			return (byte)(borderCode + 7);
	}

	public int3 GetCoord(int index){
		int x = index / (chunkWidth*chunkDepth);
		int y = (index/chunkWidth)%chunkDepth;
		int z = index%chunkWidth;

		return new int3(x, y, z);
	}

	// Checks if there's a voxel around in the same chunk that generated the one provided by index
	public bool CheckPropagatorAround(NativeArray<byte> lightMap, NativeArray<byte> shadowMap, int3 coord, byte borderCode, bool extraLight=false){
		int indexAbove, indexBelow, indexPlus, indexMinus;
		int index = GetIndex(coord);
		byte currentShadow, currentLight;

		if(!extraLight){
			currentShadow = (byte)(shadowMap[index] & 0x0F);
			currentLight = (byte)((lightMap[index] & 0x0F) + 1);
		}
		else{
			currentShadow = (byte)(shadowMap[index] >> 4);
			currentLight = (byte)((lightMap[index] >> 4) + 1);
		}

		indexAbove = -1;
		indexBelow = -1;
		indexPlus = -1;
		indexMinus = -1;

		if(borderCode == 0 || borderCode == 1){
			if(coord.x > 0)
				indexMinus = GetIndex(new int3(coord.x-1, coord.y, coord.z));
			if(coord.x < chunkWidth-1)
				indexPlus = GetIndex(new int3(coord.x+1, coord.y, coord.z));
			if(coord.y > 0)
				indexBelow = GetIndex(new int3(coord.x, coord.y-1, coord.z));
			if(coord.y < chunkDepth-1)
				indexAbove = GetIndex(new int3(coord.x, coord.y+1, coord.z));
		}
		else if(borderCode == 2 || borderCode == 3){
			if(coord.z > 0)
				indexMinus = GetIndex(new int3(coord.x, coord.y, coord.z-1));
			if(coord.z < chunkWidth-1)
				indexPlus = GetIndex(new int3(coord.x, coord.y, coord.z+1));
			if(coord.y > 0)
				indexBelow = GetIndex(new int3(coord.x, coord.y-1, coord.z));
			if(coord.y < chunkDepth-1)
				indexAbove = GetIndex(new int3(coord.x, coord.y+1, coord.z));
		}
		else{
			if(coord.z > 0)
				indexMinus = GetIndex(new int3(coord.x, coord.y, coord.z-1));
			if(coord.z < chunkWidth-1)
				indexPlus = GetIndex(new int3(coord.x, coord.y, coord.z+1));
			if(coord.x > 0)
				indexBelow = GetIndex(new int3(coord.x-1, coord.y, coord.z));
			if(coord.x < chunkDepth-1)
				indexAbove = GetIndex(new int3(coord.x+1, coord.y, coord.z));
		}

		if(!extraLight){
			if(indexAbove >= 0){
				if((shadowMap[indexAbove] & 0x0F) == currentShadow && (lightMap[indexAbove] & 0x0F) == currentLight)
					return true;
			}
			if(indexBelow >= 0){
				if((shadowMap[indexBelow] & 0x0F) == currentShadow && (lightMap[indexBelow] & 0x0F) == currentLight)
					return true;
			}
			if(indexPlus >= 0){
				if((shadowMap[indexPlus] & 0x0F) == currentShadow && (lightMap[indexPlus] & 0x0F) == currentLight)
					return true;
			}
			if(indexMinus >= 0){
				if((shadowMap[indexMinus] & 0x0F) == currentShadow && (lightMap[indexMinus] & 0x0F) == currentLight)
					return true;
			}
		}
		else{
			if(indexAbove >= 0){
				if((shadowMap[indexAbove] >> 4) == currentShadow && (lightMap[indexAbove] >> 4) == currentLight)
					return true;
			}
			if(indexBelow >= 0){
				if((shadowMap[indexBelow] >> 4) == currentShadow && (lightMap[indexBelow] >> 4) == currentLight)
					return true;
			}
			if(indexPlus >= 0){
				if((shadowMap[indexPlus] >> 4) == currentShadow && (lightMap[indexPlus] >> 4) == currentLight)
					return true;
			}
			if(indexMinus >= 0){
				if((shadowMap[indexMinus] >> 4) == currentShadow && (lightMap[indexMinus] >> 4) == currentLight)
					return true;
			}			
		}

		return false;
	}


	// Checks the surroundings and adds light fallout
	public void ScanSurroundings(int3 c, byte currentLight, bool normalOrder, int side, NativeArray<byte> selectedMap, NativeArray<byte> otherMap, NativeArray<byte> selectedShadow, NativeArray<byte> otherShadow, NativeList<int3> bfsq, NativeList<int3> otherBfsq, NativeHashSet<int3> visited, NativeHashSet<int3> otherVisited, byte borderCode, bool extraLight=false){
		if(currentLight == 1)
			return;

		int3 aux;
		int index;
		byte sideShadow;

		// East
		aux = new int3(c.x+1, c.y, c.z);

		if(aux.x < chunkWidth){
			index = GetIndex(aux);

			if(!extraLight){
				if((selectedMap[index] & 0x0F) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
				}					
			}
		}
		else{
			if((borderCode == 1 && side == 1) || (borderCode == 0 && side == 2)){
				if(!otherVisited.Contains(GetNeighborCoord(c, borderCode, side))){
					if(!extraLight){
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] & 0x0F);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0xF0) | currentLight-1);
							otherShadow[n] = (byte)((otherShadow[n] & 0xF0) | InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
					else{
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] >> 4);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0x0F) | ((currentLight-1) << 4));
							otherShadow[n] = (byte)((otherShadow[n] & 0x0F) | (InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)) << 4));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
				}
			}
		}

		// West
		aux = new int3(c.x-1, c.y, c.z);

		if(aux.x >= 0){
			index = GetIndex(aux);

			if(!extraLight){
				if((selectedMap[index] & 0x0F) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
				}					
			}
		}
		else{
			if((borderCode == 0 && side == 1) || (borderCode == 1 && side == 2)){
				if(!otherVisited.Contains(GetNeighborCoord(c, borderCode, side))){
					if(!extraLight){
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] & 0x0F);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0xF0) | currentLight-1);
							otherShadow[n] = (byte)((otherShadow[n] & 0xF0) | InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
					else{
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] >> 4);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0x0F) | ((currentLight-1) << 4));
							otherShadow[n] = (byte)((otherShadow[n] & 0x0F) | (InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)) << 4));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
				}
			}
		}

		// North
		aux = new int3(c.x, c.y, c.z+1);

		if(aux.z < chunkWidth){
			index = GetIndex(aux);

			if(!extraLight){
				if((selectedMap[index] & 0x0F) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
				}					
			}
		}
		else{
			if((borderCode == 3 && side == 1) || (borderCode == 4 && side == 2)){
				if(!otherVisited.Contains(GetNeighborCoord(c, borderCode, side))){
					if(!extraLight){
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] & 0x0F);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0xF0) | currentLight-1);
							otherShadow[n] = (byte)((otherShadow[n] & 0xF0) | InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
					else{
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] >> 4);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0x0F) | ((currentLight-1) << 4));
							otherShadow[n] = (byte)((otherShadow[n] & 0x0F) | (InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)) << 4));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
				}
			}
		}	

		// South
		aux = new int3(c.x, c.y, c.z-1);

		if(aux.z >= 0){
			index = GetIndex(aux);

			if(!extraLight){
				if((selectedMap[index] & 0x0F) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
				}					
			}
		}
		else{
			if((borderCode == 4 && side == 1) || (borderCode == 3 && side == 2)){
				if(!otherVisited.Contains(GetNeighborCoord(c, borderCode, side))){
					if(!extraLight){
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] & 0x0F);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0xF0) | currentLight-1);
							otherShadow[n] = (byte)((otherShadow[n] & 0xF0) | InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
					else{
						int n = GetIndex(GetNeighborCoord(c, borderCode, side));
						sideShadow = (byte)(otherShadow[n] >> 4);
						if(sideShadow == 1){
							changed[3] = (byte)(changed[3] | 128);
							otherMap[n] = (byte)((otherMap[n] & 0x0F) | ((currentLight-1) << 4));
							otherShadow[n] = (byte)((otherShadow[n] & 0x0F) | (InvertShadowDirection(GetShadowDirection(borderCode, normalOrder)) << 4));
							otherBfsq.Add(GetNeighborCoord(c, borderCode, side));
						}
					}
				}			
			}
		}	

		// Up
		aux = new int3(c.x, c.y+1, c.z);

		if(aux.y < chunkDepth){
			index = GetIndex(aux);

			if(!extraLight){
				if((selectedMap[index] & 0x0F) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
				}					
			}
		}

		// Down
		aux = new int3(c.x, c.y-1, c.z);

		if(aux.y >= 0){
			index = GetIndex(aux);

			if(!extraLight){
				if((selectedMap[index] & 0x0F) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0xF0) + (currentLight-1)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0xF0) | GetShadowDirection(borderCode, normalOrder));
				}
			}
			else{
				if((selectedMap[index] >> 4) < currentLight && selectedShadow[index] != 0){
					selectedMap[index] = (byte)(((selectedMap[index] & 0x0F) + ((currentLight-1) << 4)));
					bfsq.Add(aux);
					selectedShadow[index] = (byte)((selectedShadow[index] & 0x0F) | (GetShadowDirection(borderCode, normalOrder) << 4));
				}
			}
		}
	}

	// Adds to further light update flag if playing with directionals
	public void WriteLightUpdateFlag(int3 aux, byte borderCode){
		// zp
		if(aux.z == chunkWidth-1 && borderCode != 3)
			changed[3] = (byte)(changed[3] | (1 << 3));
		// zm
		else if(aux.z == 0 && borderCode != 4)
			changed[3] = (byte)(changed[3] | (1 << 2));

		// xp
		if(aux.x == chunkWidth-1 && borderCode != 0)
			changed[3] = (byte)(changed[3] | (1 << 1));
		// xm
		else if(aux.x == 0 && borderCode != 1)
			changed[3] = (byte)(changed[3] | (1));
	}

	public byte InvertShadowDirection(byte shadow){
		if(shadow == 7)
			return 8;
		if(shadow == 8)
			return 7;
		if(shadow == 9)
			return 10;
		if(shadow == 10)
			return 9;
		return 0;
	}

	public int GetIndex(int3 c){
		return c.x*chunkWidth*chunkDepth+c.y*chunkWidth+c.z;
	}
	public int GetIndex(int x, int y, int z){
		return x*chunkWidth*chunkDepth+y*chunkWidth+z;
	}

}