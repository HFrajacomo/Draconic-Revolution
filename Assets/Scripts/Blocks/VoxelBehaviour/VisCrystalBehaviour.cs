using System;
using UnityEngine;

using Random = System.Random;

public class VisCrystalBehaviour : VoxelBehaviour{
	public string blockName;

	private ushort blockID;
	private static readonly Random rng = new Random((int)DateTime.Now.Ticks);
	private ushort[] possibleCodes = new ushort[6];
	private int possibilities = 0;

	public override void PostDeserializationSetup(bool isClient){
		//this.blockID = Get(blockName); 
	}

	public override int OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ChunkLoader_Server cl){
		CastCoord coord = new CastCoord(pos, blockX, blockY, blockZ);

		if(facing == -1){
			if(!FindAndPlaceCrystal(coord, cl))
				DeleteCrystal(coord, cl);
		}
		else{
			if(CanBePlacedFacing(facing, coord, cl)){
				PlaceCrystal(facing, coord, cl);
			}
		}

		SaveAndSendChunk(coord, cl);

		return 1;
	}

	public int GetRandomDirection(){
		return rng.Next(1, 8);
	}

	// Returns true if placement was successful and false if it was not placed and destroyed the SpawnCrystal
	public bool FindAndPlaceCrystal(CastCoord coord, ChunkLoader_Server cl){
		ChunkPos pos = coord.GetChunkPos();

		this.possibilities = 0;

		if(coord.blockX > 0){
			if(GetCorrectPlacement(cl.chunks[pos].data.GetCell(coord.blockX-1, coord.blockY, coord.blockZ), cl)){
				this.possibleCodes[possibilities] = 0;
				this.possibilities++;
			}
		}
		if(coord.blockX < Chunk.chunkWidth - 1){
			if(GetCorrectPlacement(cl.chunks[pos].data.GetCell(coord.blockX+1, coord.blockY, coord.blockZ), cl)){
				this.possibleCodes[possibilities] = 1;
				this.possibilities++;
			}
		}
		if(coord.blockY > 0){
			if(GetCorrectPlacement(cl.chunks[pos].data.GetCell(coord.blockX, coord.blockY-1, coord.blockZ), cl)){
				this.possibleCodes[possibilities] = 4;
				this.possibilities++;
			}
		}
		if(coord.blockY < Chunk.chunkDepth - 1){
			if(GetCorrectPlacement(cl.chunks[pos].data.GetCell(coord.blockX, coord.blockY+1, coord.blockZ), cl)){
				this.possibleCodes[possibilities] = 5;
				this.possibilities++;
			}
		}
		if(coord.blockZ > 0){
			if(GetCorrectPlacement(cl.chunks[pos].data.GetCell(coord.blockX, coord.blockY, coord.blockZ-1), cl)){
				this.possibleCodes[possibilities] = 2;
				this.possibilities++;
			}
		}
		if(coord.blockZ < Chunk.chunkWidth - 1){
			if(GetCorrectPlacement(cl.chunks[pos].data.GetCell(coord.blockX, coord.blockY, coord.blockZ+1), cl)){
				this.possibleCodes[possibilities] = 3;
				this.possibilities++;
			}
		}

		if(possibilities == 0){
			return false;
		}

		int index = rng.Next(0, this.possibilities);
		ushort newCode = (ushort)(cl.chunks[coord.GetChunkPos()].data.GetCell(coord.blockX, coord.blockY, coord.blockZ) + rng.Next(1,8));

		cl.chunks[coord.GetChunkPos()].data.SetCell(coord.blockX, coord.blockY, coord.blockZ, newCode);
		cl.chunks[pos].metadata.SetState(coord.blockX, coord.blockY, coord.blockZ, possibleCodes[index]);

		cl.server.RegisterChunkToSend(pos);

		return true;
	}

	public void SaveAndSendChunk(CastCoord coord, ChunkLoader_Server cl){
		ChunkPos pos = coord.GetChunkPos();
		NetMessage message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
		message.DirectBlockUpdate(BUDCode.PLACE, pos, coord.blockX, coord.blockY, coord.blockZ, -1, blockID, cl.chunks[pos].metadata.GetState(coord.blockX, coord.blockY, coord.blockZ), cl.chunks[pos].metadata.GetHP(coord.blockX, coord.blockY, coord.blockZ));
		cl.server.SendToClients(pos, message);				
		cl.budscheduler.ScheduleSave(pos);
	}

	public void DeleteCrystal(CastCoord coord, ChunkLoader_Server cl){
		ChunkPos pos = coord.GetChunkPos();
		cl.chunks[pos].data.SetCell(coord.blockX, coord.blockY, coord.blockZ, 0);
		cl.chunks[pos].metadata.Reset(coord.blockX, coord.blockY, coord.blockZ);
	}

	private bool GetCorrectPlacement(ushort blockCode, ChunkLoader_Server cl){
		return cl.blockBook.CheckSolid(blockCode) && !cl.blockBook.CheckTransparent(blockCode);
	}

	public bool CanBePlacedFacing(int facing, CastCoord coord, ChunkLoader_Server cl){
		if(facing == 0 && coord.blockX < Chunk.chunkWidth-1)
			return GetCorrectPlacement(cl.chunks[coord.GetChunkPos()].data.GetCell(coord.blockX+1, coord.blockY, coord.blockZ), cl);
		else if(facing == 1 && coord.blockZ > 0)
			return GetCorrectPlacement(cl.chunks[coord.GetChunkPos()].data.GetCell(coord.blockX, coord.blockY, coord.blockZ-1), cl);
		else if(facing == 2 && coord.blockX > 0)
			return GetCorrectPlacement(cl.chunks[coord.GetChunkPos()].data.GetCell(coord.blockX-1, coord.blockY, coord.blockZ), cl);
		else if(facing == 3 && coord.blockZ < Chunk.chunkWidth-1)
			return GetCorrectPlacement(cl.chunks[coord.GetChunkPos()].data.GetCell(coord.blockX, coord.blockY, coord.blockZ+1), cl);
		else if(facing == 4 && coord.blockY > 0)
			return GetCorrectPlacement(cl.chunks[coord.GetChunkPos()].data.GetCell(coord.blockX, coord.blockY-1, coord.blockZ), cl);
		else if(facing == 5 && coord.blockY < Chunk.chunkDepth-1)
			return GetCorrectPlacement(cl.chunks[coord.GetChunkPos()].data.GetCell(coord.blockX, coord.blockY+1, coord.blockZ), cl);

		cl.chunks[coord.GetChunkPos()].data.SetCell(coord.blockX, coord.blockY, coord.blockZ, this.blockID);
		cl.chunks[coord.GetChunkPos()].metadata.SetState(coord.blockX, coord.blockY, coord.blockZ, 4);
		return false;
	}

	public void PlaceCrystal(int facing, CastCoord coord, ChunkLoader_Server cl){
		ChunkPos pos = coord.GetChunkPos();

		cl.chunks[pos].data.SetCell(coord.blockX, coord.blockY, coord.blockZ, this.blockID);

		if(facing == 0)
			cl.chunks[pos].metadata.SetState(coord.blockX, coord.blockY, coord.blockZ, 1);
		else if(facing == 1)
			cl.chunks[pos].metadata.SetState(coord.blockX, coord.blockY, coord.blockZ, 2);
		else if(facing == 2)
			cl.chunks[pos].metadata.SetState(coord.blockX, coord.blockY, coord.blockZ, 0);
		else if(facing == 3)
			cl.chunks[pos].metadata.SetState(coord.blockX, coord.blockY, coord.blockZ, 3);
		else
			cl.chunks[pos].metadata.SetState(coord.blockX, coord.blockY, coord.blockZ, (ushort)facing);
	}
}