using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;


[BurstCompile]
public struct GenerateHeightMapJob : IJob{
	[ReadOnly]
	public NativeArray<ushort> data;
	[ReadOnly]
	public NativeArray<bool> blockInvisible;
	[ReadOnly]
	public NativeArray<bool> objectInvisible;
	[ReadOnly]
	public NativeArray<bool> blockAffectLight;
	[ReadOnly]
	public NativeArray<bool> objectAffectLight;

	public NativeArray<byte> heightMap;
	public NativeArray<byte> renderMap;
	public NativeArray<bool> ceilingMap;

	public void Execute(){
		ushort blockCode;
		bool found, foundRender;

		for(int x=0; x < Chunk.chunkWidth; x++){
	    	for(int z=0; z < Chunk.chunkWidth; z++){
	    		found = false;
	    		foundRender = false;
	    		for(int y=Chunk.chunkDepth-1; y >= 0; y--){
	    			blockCode = this.data[x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z];

	    			// If is a block
	    			if(blockCode <= ushort.MaxValue/2){
	    				if(!blockInvisible[blockCode] && !foundRender){
	    					if(y < Chunk.chunkDepth-1)
	    						this.renderMap[x*Chunk.chunkWidth+z] = (byte)(y+1);
	    					else
	    						this.renderMap[x*Chunk.chunkWidth+z] = (byte)(Chunk.chunkDepth-1);
	    					foundRender = true;
	    				}

	    				if(blockAffectLight[blockCode]){
	    					this.heightMap[x*Chunk.chunkWidth+z] = (byte)y;
	    					this.ceilingMap[x*Chunk.chunkWidth+z] = true;
	    					found = true;
	    					break;
	    				}
	    			}
	    			// If it's an object
	    			else{
	    				if(!objectInvisible[ushort.MaxValue - blockCode] && !foundRender){
	    					if(y < Chunk.chunkDepth-1)
	    						this.renderMap[x*Chunk.chunkWidth+z] = (byte)(y+1);
	    					else
	    						this.renderMap[x*Chunk.chunkWidth+z] = (byte)(Chunk.chunkDepth-1);
	    					foundRender = true;
	    				}

	    				if(objectAffectLight[ushort.MaxValue - blockCode]){
	    					this.heightMap[x*Chunk.chunkWidth+z] = (byte)y;
	    					this.ceilingMap[x*Chunk.chunkWidth+z] = true;
	    					found = true;
	    					break;
	    				}		
	    			}
	    		}

	    		if(!foundRender){
	    			this.renderMap[x*Chunk.chunkWidth+z] = 0;
	    		}
	    		if(!found){
	    			this.heightMap[x*Chunk.chunkWidth+z] = 0;
	    			this.ceilingMap[x*Chunk.chunkWidth+z] = false;
	    		}
	    	}
		}

		FixRenderMap();
	}

	// Remaps renderMap correctly
	private void FixRenderMap(){
		byte biggest;

		for(int x=0; x < Chunk.chunkWidth; x++){
			for(int z=0; z < Chunk.chunkWidth; z++){
				biggest = 0;

				if(x > 0)
					if(this.renderMap[(x-1)*Chunk.chunkWidth+z] > biggest)
						biggest = this.renderMap[(x-1)*Chunk.chunkWidth+z];
				if(x < Chunk.chunkWidth-1)
					if(this.renderMap[(x+1)*Chunk.chunkWidth+z] > biggest)
						biggest = this.renderMap[(x+1)*Chunk.chunkWidth+z];
				if(z > 0)
					if(this.renderMap[x*Chunk.chunkWidth+(z-1)] > biggest)
						biggest = this.renderMap[x*Chunk.chunkWidth+(z-1)];
				if(z < Chunk.chunkWidth-1)
					if(this.renderMap[x*Chunk.chunkWidth+(z+1)] > biggest)
						biggest = this.renderMap[x*Chunk.chunkWidth+(z+1)];

				if(this.renderMap[x*Chunk.chunkWidth+z] > biggest)
					biggest = this.renderMap[x*Chunk.chunkWidth+z];

				this.renderMap[x*Chunk.chunkWidth+z] = biggest;
			}
		}
	}
}