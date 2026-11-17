using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public struct VerifyPropagationJob : IJob {
	[ReadOnly]
	public NativeArray<byte> lightdata;
	[ReadOnly]
	public NativeArray<byte> shadowdata;
	[ReadOnly]
	public NativeArray<byte> xmlight;
	[ReadOnly]
	public NativeArray<byte> xmshadow;
	[ReadOnly]
	public NativeArray<byte> xplight;
	[ReadOnly]
	public NativeArray<byte> xpshadow;
	[ReadOnly]
	public NativeArray<byte> zmlight;
	[ReadOnly]
	public NativeArray<byte> zmshadow;
	[ReadOnly]
	public NativeArray<byte> zplight;
	[ReadOnly]
	public NativeArray<byte> zpshadow;
	[ReadOnly]
	public NativeArray<byte> vplight;
	[ReadOnly]
	public NativeArray<byte> vpshadow;
	[ReadOnly]
	public NativeArray<byte> vmlight;
	[ReadOnly]
	public NativeArray<byte> vmshadow;
	[ReadOnly]
	public bool bidirectional; // Also flags horizontal borders where the neighbor is the brighter one

	public NativeArray<byte> changed;

	// Directional ShadowCodes of light received from the chunk above and below (see CalculateLightPropagationJob.GetShadowDirection)
	private const byte SHADOW_FROM_ABOVE = 11;
	private const byte SHADOW_FROM_BELOW = 12;

	public void Execute(){
		byte highestLightCurrent = 0;
		byte highestLightNeighbor = 0;
		bool FOUND = false;

		// xm
		for(int y=0; y < Chunk.chunkDepth; y++){
			for(int z = 0; z < Chunk.chunkWidth; z++){
				if(ValidPropagationChecking(shadowdata[y*Chunk.chunkWidth+z], xmshadow[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z])){
					highestLightCurrent = ExtractHighestLightFactor(lightdata[y*Chunk.chunkWidth+z]);
					highestLightNeighbor = ExtractHighestLightFactor(xmlight[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z]);

					if(ShouldPropagate(highestLightCurrent, highestLightNeighbor, bidirectional)){
						changed[0] = (byte)(changed[0] | 1);
						FOUND = true;
						break;
					}
				}
			}
			if(FOUND)
				break;
		}

		// xp
		FOUND = false;
		for(int y=0; y < Chunk.chunkDepth; y++){
			for(int z = 0; z < Chunk.chunkWidth; z++){
				if(ValidPropagationChecking(shadowdata[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z], xpshadow[y*Chunk.chunkWidth+z])){
					highestLightCurrent = ExtractHighestLightFactor(lightdata[(Chunk.chunkWidth-1)*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z]);
					highestLightNeighbor = ExtractHighestLightFactor(xplight[y*Chunk.chunkWidth+z]);

					if(ShouldPropagate(highestLightCurrent, highestLightNeighbor, bidirectional)){
						changed[0] = (byte)(changed[0] | 2);
						FOUND = true;
						break;
					}
				}
			}
			if(FOUND)
				break;
		}

		// zm
		FOUND = false;
		for(int y=0; y < Chunk.chunkDepth; y++){
			for(int x = 0; x < Chunk.chunkWidth; x++){
				if(ValidPropagationChecking(shadowdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth], zmshadow[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)])){
					highestLightCurrent = ExtractHighestLightFactor(lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth]);
					highestLightNeighbor = ExtractHighestLightFactor(zmlight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)]);

					if(ShouldPropagate(highestLightCurrent, highestLightNeighbor, bidirectional)){
						changed[0] = (byte)(changed[0] | 4);
						FOUND = true;
						break;
					}
				}
			}
			if(FOUND)
				break;
		}

		// zp
		FOUND = false;
		for(int y=0; y < Chunk.chunkDepth; y++){
			for(int x = 0; x < Chunk.chunkWidth; x++){
				if(ValidPropagationChecking(shadowdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)], zpshadow[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth])){
					highestLightCurrent = ExtractHighestLightFactor(lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+(Chunk.chunkWidth-1)]);
					highestLightNeighbor = ExtractHighestLightFactor(zplight[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth]);

					if(ShouldPropagate(highestLightCurrent, highestLightNeighbor, bidirectional)){
						changed[0] = (byte)(changed[0] | 8);
						FOUND = true;
						break;
					}
				}
			}
			if(FOUND)
				break;
		}

		// ym
		if(vmshadow.Length > 0){
			FOUND = false;
			for(int z=0; z < Chunk.chunkWidth; z++){
				for(int x = 0; x < Chunk.chunkWidth; x++){
					if(ValidPropagationChecking(shadowdata[x*Chunk.chunkWidth*Chunk.chunkDepth+z], vmshadow[x*Chunk.chunkWidth*Chunk.chunkDepth+(Chunk.chunkDepth-1)*Chunk.chunkWidth+z])){
						highestLightCurrent = ExtractHighestLightFactor(lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+z]);
						highestLightNeighbor = ExtractHighestLightFactor(vmlight[x*Chunk.chunkWidth*Chunk.chunkDepth+(Chunk.chunkDepth-1)*Chunk.chunkWidth+z]);

						if(ShouldPropagate(highestLightCurrent, highestLightNeighbor, true)){
							changed[0] = (byte)(changed[0] | 16);
							FOUND = true;
							break;
						}
					}
					else if(HoldsLightFromWall(shadowdata[x*Chunk.chunkWidth*Chunk.chunkDepth+z], vmshadow[x*Chunk.chunkWidth*Chunk.chunkDepth+(Chunk.chunkDepth-1)*Chunk.chunkWidth+z], SHADOW_FROM_BELOW)){
						changed[0] = (byte)(changed[0] | 16);
						FOUND = true;
						break;
					}
				}
				if(FOUND)
					break;
			}
		}

		// yp
		if(vpshadow.Length > 0){
			FOUND = false;
			for(int z=0; z < Chunk.chunkWidth; z++){
				for(int x = 0; x < Chunk.chunkWidth; x++){
					if(ValidPropagationChecking(shadowdata[x*Chunk.chunkWidth*Chunk.chunkDepth+(Chunk.chunkDepth-1)*Chunk.chunkWidth+z], vpshadow[x*Chunk.chunkWidth*Chunk.chunkDepth+z])){
						highestLightCurrent = ExtractHighestLightFactor(lightdata[x*Chunk.chunkWidth*Chunk.chunkDepth+(Chunk.chunkDepth-1)*Chunk.chunkWidth+z]);
						highestLightNeighbor = ExtractHighestLightFactor(vplight[x*Chunk.chunkWidth*Chunk.chunkDepth+z]);

						if(ShouldPropagate(highestLightCurrent, highestLightNeighbor, true)){
							changed[0] = (byte)(changed[0] | 32);
							FOUND = true;
							break;
						}
					}
					else if(HoldsLightFromWall(shadowdata[x*Chunk.chunkWidth*Chunk.chunkDepth+(Chunk.chunkDepth-1)*Chunk.chunkWidth+z], vpshadow[x*Chunk.chunkWidth*Chunk.chunkDepth+z], SHADOW_FROM_ABOVE)){
						changed[0] = (byte)(changed[0] | 32);
						FOUND = true;
						break;
					}
				}
				if(FOUND)
					break;
			}
		}
	}

	/*
	Light received from the vertical neighbor is kept through calculations (see CalculateShadowMapJob). If the neighbor's voxel became solid,
	that light may have lost its source, so the border is propagated to remove it
	*/
	private bool HoldsLightFromWall(byte shadow, byte neighborShadow, byte direction){
		if(neighborShadow != 0)
			return false;

		return (shadow & 0x0F) == direction || (shadow >> 4) == direction;
	}

	// Light is propagated when one side is brighter than what the other could give it. Pushing only checks if the current chunk is the brighter one
	private bool ShouldPropagate(byte current, byte neighbor, bool bothWays){
		if(current > neighbor+1)
			return true;

		return bothWays && neighbor > current+1;
	}

	private byte ExtractHighestLightFactor(byte encodedLight){
		if(((encodedLight & 0xF0) >> 4) == 0){
			return (byte)(encodedLight & 0x0F);
		}
		else{
			byte natural = (byte)(encodedLight & 0x0F);
			byte extra = (byte)((encodedLight & 0xF0) >> 4);

			if(extra <= natural){
				return natural;
			}
			else{
				return extra;
			}
		}
	}

	private bool ValidPropagationChecking(byte shadowA, byte shadowB){
		if(shadowA != 0 && shadowB != 0){
			if(shadowA != 2 && shadowB != 2){
				return true;
			}
		}
		return false;
	}
}