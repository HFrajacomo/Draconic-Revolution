using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public struct CalculateShadowMapJob : IJob{
	public NativeArray<byte> shadowMap;
	public NativeArray<byte> lightMap;
	public NativeList<int4> lightSources;
	[ReadOnly]
	public NativeArray<byte> heightMap;
	[ReadOnly]
	public NativeArray<ushort> data;
	[ReadOnly]
	public NativeArray<ushort> states;
	[ReadOnly]
	public int chunkWidth;
	[ReadOnly]
	public int chunkDepth;
	[ReadOnly]
	public NativeArray<bool> isTransparentBlock;
	[ReadOnly]
	public NativeArray<bool> isTransparentObj;
	[ReadOnly]
	public NativeArray<byte> blockLuminosity;
	[ReadOnly]
	public NativeArray<byte> objectLuminosity;
	[ReadOnly]
	public NativeArray<bool> ceilingMap;
	[ReadOnly]
	public NativeArray<bool> neighborCeilingMap;
	[ReadOnly]
	public bool isStandalone;
	[ReadOnly]
	public ChunkPos cpos;

	public void Execute(){
		if(isStandalone){
			CreateStandaloneShadowMap();
		}
		else{
			CreateLayeredShadowMap();
		}
	}

	/**
	 * Creates a ShadowMap for the current chunk considering the above chunk and how sunlight passes through it
	 */
	public void CreateLayeredShadowMap(){
		bool isBlock;
		ushort blockCode;
		int index;
		bool hasCeiling;

		for(int z=0; z < Chunk.chunkWidth; z++){
			for(int x=0; x < Chunk.chunkWidth; x++){
				hasCeiling = neighborCeilingMap[x*Chunk.chunkWidth+z];

				for(int y=chunkDepth-1; y >= 0; y--){
					index = x*chunkWidth*chunkDepth+y*chunkWidth+z;
					blockCode = data[index];
					isBlock = blockCode <= ushort.MaxValue/2;

					// If is above heightMap
					if((y > heightMap[x*chunkWidth+z] && !hasCeiling) || (!ceilingMap[x*chunkWidth+z] && heightMap[x*chunkWidth+z] == 0)){
						shadowMap[index] = 18;
						lightMap[index] = (byte)((lightMap[index] & 0xF0) | 15);

						// Gets lightsource
						if(isBlock){
							if((blockLuminosity[blockCode] & 0x0F) > 0 && states[index] <= (blockLuminosity[blockCode] >> 4)){
								lightSources.Add(new int4(x, y, z, (blockLuminosity[blockCode] & 0x0F)));

								if((blockLuminosity[blockCode] & 0x0F) == 15){
									lightMap[index] = (byte)((lightMap[index] & 0x0F) | (15 << 4));
								}
							}
						}
						else{
							if((objectLuminosity[ushort.MaxValue - blockCode] & 0x0F) > 0 && states[index] <= (objectLuminosity[ushort.MaxValue - blockCode] >> 4)){
								lightSources.Add(new int4(x, y, z, objectLuminosity[ushort.MaxValue - blockCode] & 0x0F));

								if((objectLuminosity[ushort.MaxValue - blockCode] & 0x0F) == 15){
									lightMap[index] = (byte)((lightMap[index] & 0x0F) | (15 << 4));
								}						
							}
						}

						continue;
					}

					// If is transparent
					if(isBlock){
						if(isTransparentBlock[blockCode]){
							if(CheckBorder(x, y, z) == 0){
								shadowMap[index] = 17;
								lightMap[index] = 0;
							}
							
							else{
								if((shadowMap[index] & 0x0F) < 7){
									shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | 1);
									lightMap[index] = (byte)((lightMap[index] & 0xF0));
								}
								if((shadowMap[index] >> 4) < 7){
									shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | 16);
									lightMap[index] = (byte)((lightMap[index] & 0x0F));
								}
							}
						}
						else{
							shadowMap[index] = 0;
							lightMap[index] = 0;
						}

						if((blockLuminosity[blockCode] & 0x0F) > 0 && states[index] <= (blockLuminosity[blockCode] >> 4)){
							lightSources.Add(new int4(x, y, z, (blockLuminosity[blockCode] & 0x0F)));

							if((blockLuminosity[blockCode] & 0x0F) == 15){
								lightMap[index] = (byte)((lightMap[index] & 0x0F) | (15 << 4));
							}	
						}
					}
					else{
						if(isTransparentObj[ushort.MaxValue - blockCode]){
							if(CheckBorder(x, y, z) == 0){
								shadowMap[index] = 17;
								lightMap[index] = 0;
							}
							else{
								if((shadowMap[index] & 0x0F) < 7){
									shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | 1);
									lightMap[index] = (byte)((lightMap[index] & 0xF0));
								}
								if((shadowMap[index] >> 4) < 7){
									shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | 16);
									lightMap[index] = (byte)((lightMap[index] & 0x0F));
								}
							}
						}
						else{
							shadowMap[index] = 0;
							lightMap[index] = 0;
						}

						if((objectLuminosity[ushort.MaxValue - blockCode] & 0x0F) > 0 && states[index] <= (objectLuminosity[ushort.MaxValue - blockCode] >> 4)){
							lightSources.Add(new int4(x, y, z, (objectLuminosity[ushort.MaxValue - blockCode] & 0x0F)));

							if((objectLuminosity[ushort.MaxValue - blockCode] & 0x0F) == 15){
								lightMap[index] = (byte)((lightMap[index] & 0x0F) | (15 << 4));
							}
						}
					}					
				}
			}
		}		
	}

	/**
	 * Creates a ShadowMap for the current chunk not considering the Y Level above chunk
	 */
	public void CreateStandaloneShadowMap(){
		bool isBlock;
		ushort blockCode;
		int index;

		for(int z=0; z < chunkWidth; z++){
			for(int x=0; x < chunkWidth; x++){
				for(int y=chunkDepth-1; y >= 0; y--){
					index = x*chunkWidth*chunkDepth+y*chunkWidth+z;
					blockCode = data[index];
					isBlock = blockCode <= ushort.MaxValue/2;

					// If is above heightMap
					if((y > heightMap[x*chunkWidth+z] || !ceilingMap[x*chunkWidth+z])){
						shadowMap[index] = 18;

						// Gets lightsource
						if(isBlock){
							if((blockLuminosity[blockCode] & 0x0F) > 0 && states[index] <= (blockLuminosity[blockCode] >> 4)){
								lightSources.Add(new int4(x, y, z, (blockLuminosity[blockCode] & 0x0F)));

								if((blockLuminosity[blockCode] & 0x0F) == 15){
									lightMap[index] = (byte)((lightMap[index] & 0x0F) | (15 << 4));
								}
							}
						}
						else{
							if((objectLuminosity[ushort.MaxValue - blockCode] & 0x0F) > 0 && states[index] <= (objectLuminosity[ushort.MaxValue - blockCode] >> 4)){
								lightSources.Add(new int4(x, y, z, objectLuminosity[ushort.MaxValue - blockCode] & 0x0F));							

								if((objectLuminosity[ushort.MaxValue - blockCode] & 0x0F) == 15){
									lightMap[index] = (byte)((lightMap[index] & 0x0F) | (15 << 4));
								}
							}
						}

						if(blockCode == 0)
							lightMap[index] = 15;
						else
							lightMap[index] = 0;

						continue;
					}

					// If is transparent
					if(isBlock){
						if(isTransparentBlock[blockCode]){
							if(CheckBorder(x, y, z) == 0){
								shadowMap[index] = 17;
								lightMap[index] = 0;
							}
							else{
								if((shadowMap[index] & 0x0F) < 7){
									shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | 1);
									lightMap[index] = (byte)((lightMap[index] & 0xF0));
								}
								if((shadowMap[index] >> 4) < 7){
									shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | 16);
									lightMap[index] = (byte)((lightMap[index] & 0x0F));
								}
							}
						}
						else{
							shadowMap[index] = 0;
							lightMap[index] = 0;
						}

						if((blockLuminosity[blockCode] & 0x0F) > 0 && states[index] <= (blockLuminosity[blockCode] >> 4)){
							lightSources.Add(new int4(x, y, z, (blockLuminosity[blockCode] & 0x0F)));

							if((blockLuminosity[blockCode] & 0x0F) == 15){
								lightMap[index] = (byte)((lightMap[index] & 0x0F) | (15 << 4));
							}
						}
					}
					else{
						if(isTransparentObj[ushort.MaxValue - blockCode]){
							if(CheckBorder(x, y, z) == 0){
								shadowMap[index] = 17;
								lightMap[index] = 0;
							}
							else{
								if((shadowMap[index] & 0x0F) < 7){
									shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | 1);
									lightMap[index] = (byte)((lightMap[index] & 0xF0));
								}
								if((shadowMap[index] >> 4) < 7){
									shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | 16);
									lightMap[index] = (byte)((lightMap[index] & 0x0F));
								}
							}
						}
						else{
							shadowMap[index] = 0;
							lightMap[index] = 0;
						}

						if((objectLuminosity[ushort.MaxValue - blockCode] & 0x0F) > 0 && states[index] <= (objectLuminosity[ushort.MaxValue - blockCode] >> 4)){
							lightSources.Add(new int4(x, y, z, (objectLuminosity[ushort.MaxValue - blockCode] & 0x0F)));			

							if((objectLuminosity[ushort.MaxValue - blockCode] & 0x0F) == 15){
								lightMap[index] = (byte)((lightMap[index] & 0x0F) | (15 << 4));
							}
						}
					}
				}
			}
		}
	}

	public int GetIndex(int3 c){
		return c.x*chunkWidth*chunkDepth+c.y*chunkWidth+c.z;
	}

	public byte CheckBorder(int x, int y, int z){
		if(x == 0)
			return 1;
		else if(x == chunkWidth-1)
			return 2;
		else if(z == 0)
			return 4;
		else if(z == chunkWidth-1)
			return 8;
		else
			return 0;
	}
}