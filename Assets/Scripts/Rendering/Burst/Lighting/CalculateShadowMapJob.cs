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
	public NativeArray<bool> skyFromAbove; // Columns (x*chunkWidth+z) where direct sunlight enters the top of the chunk. See SunExitMask
	[ReadOnly]
	public ChunkPos cpos;

	/**
	 * Creates the ShadowMap of the chunk. Direct sunlight only enters the columns where it leaves the bottom of the chunk above,
	 * so it goes through every layer a shaft crosses
	 */
	public void Execute(){
		bool isTransparent;
		ushort blockCode;
		byte luminosity;
		int index, column;

		for(int z=0; z < chunkWidth; z++){
			for(int x=0; x < chunkWidth; x++){
				column = x*chunkWidth+z;

				for(int y=chunkDepth-1; y >= 0; y--){
					index = x*chunkWidth*chunkDepth+y*chunkWidth+z;
					blockCode = data[index];

					if(blockCode <= ushort.MaxValue/2){
						luminosity = blockLuminosity[blockCode];
						isTransparent = isTransparentBlock[blockCode];
					}
					else{
						luminosity = objectLuminosity[ushort.MaxValue - blockCode];
						isTransparent = isTransparentObj[ushort.MaxValue - blockCode];
					}

					// In direct sunlight
					if(skyFromAbove[column] && (y > heightMap[column] || !ceilingMap[column])){
						shadowMap[index] = 18;
						lightMap[index] = 15;
					}
					else if(isTransparent){
						if(!KeepsDirectional(x, y, z, shadowMap[index] & 0x0F)){
							shadowMap[index] = (byte)((shadowMap[index] & 0xF0) | 1);
							lightMap[index] = (byte)(lightMap[index] & 0xF0);
						}
						if(!KeepsDirectional(x, y, z, shadowMap[index] >> 4)){
							shadowMap[index] = (byte)((shadowMap[index] & 0x0F) | 16);
							lightMap[index] = (byte)(lightMap[index] & 0x0F);
						}
					}
					else{
						shadowMap[index] = 0;
						lightMap[index] = 0;
					}

					// Gets lightsource
					if((luminosity & 0x0F) > 0 && states[index] <= (luminosity >> 4)){
						lightSources.Add(new int4(x, y, z, luminosity & 0x0F));

						if((luminosity & 0x0F) == 15)
							lightMap[index] = (byte)((lightMap[index] & 0x0F) | (15 << 4));
					}
				}
			}
		}
	}

	/*
	Directional ShadowCodes (7+) hold the light received from neighbor chunks, and are kept between calculations.
	The horizontal borders keep every direction, while the top and bottom only keep the light received from the chunk above (11) and below (12)
	*/
	private bool KeepsDirectional(int x, int y, int z, int shadow){
		if(shadow < 7)
			return false;
		if(x == 0 || x == chunkWidth-1 || z == 0 || z == chunkWidth-1)
			return true;
		if(y == chunkDepth-1 && cpos.y < Chunk.chunkMaxY)
			return shadow == 11;
		if(y == 0 && cpos.y > 0)
			return shadow == 12;

		return false;
	}

	public int GetIndex(int3 c){
		return c.x*chunkWidth*chunkDepth+c.y*chunkWidth+c.z;
	}
}
