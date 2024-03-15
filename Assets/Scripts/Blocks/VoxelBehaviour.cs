using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public abstract class VoxelBehaviour {
	// Common
    public void EmitBUDTo(BUDCode type, int x, int y, int z, int tickOffset, ChunkLoader_Server cl){cl.budscheduler.ScheduleBUD(new BUDSignal(type, x, y, z, 0, 0, 0, 0), tickOffset);}
	public void EraseMetadata(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){cl.chunks[pos].metadata.Reset(x,y,z);}

    // Emit BUD Around
    public void EmitBlockUpdate(BUDCode type, int x, int y, int z, int tickOffset, ChunkLoader_Server cl){
    	CastCoord thisPos = new CastCoord(new Vector3(x, y, z));

    	CastCoord[] neighbors = {
	    	thisPos.Add(1,0,0),
	    	thisPos.Add(-1,0,0),
	    	thisPos.Add(0,1,0),
	    	thisPos.Add(0,-1,0),
	    	thisPos.Add(0,0,1),
	    	thisPos.Add(0,0,-1)
    	};

		int[] facings = {2,0,4,5,1,3};

		int faceCounter=0;

    	foreach(CastCoord c in neighbors){
			if(c.blockY < 0 || c.blockY > Chunk.chunkDepth-1){
				continue;
			}
			
	        cl.budscheduler.ScheduleBUD(new BUDSignal(type, c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), facings[faceCounter]), tickOffset);
	      
	        faceCounter++;
    	}
    }	

    // Constructor and Deserialization
	public virtual void PostDeserializationSetup(bool isClient){return;}

	// Events
	public virtual void OnBlockUpdate(BUDCode type, int x, int y, int z, int budX, int budY, int budZ, int facing, ChunkLoader_Server cl){return;}
	public virtual int OnInteract(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){return 0;}
	public virtual int OnPlace(ChunkPos pos, int x, int y, int z, int facing, ChunkLoader_Server cl){return 0;}
	public virtual int OnBreak(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){return 0;}
	public virtual int OnLoad(CastCoord coord, ChunkLoader_Server cl){return 0;}
	public virtual int OnVFXBuild(ChunkPos pos, int x, int y, int z, int facing, ushort state, ChunkLoader cl){return 0;}
	public virtual int OnVFXChange(ChunkPos pos, int x, int y, int z, int facing, ushort state, ChunkLoader cl){return 0;}
	public virtual int OnVFXBreak(ChunkPos pos, int x, int y, int z, ushort state, ChunkLoader cl){return 0;}
	public virtual int OnSFXPlay(ChunkPos pos, int x, int y, int z, ushort state, ChunkLoader cl){return 0;}
	public virtual bool PlacementRule(ChunkPos pos, int x, int y, int z, int direction, ChunkLoader_Server cl){return true;}
	public virtual Vector3 GetOffsetVector(ushort state){return Vector3.zero;}
	public virtual int2 GetRotationValue(ushort state){return new int2(0,0);}
}