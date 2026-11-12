using System;
using UnityEngine;
using Unity.Mathematics;

/*
Aura Crystals have their base attached to the side of a full block (solid and not transparent)
	Generated crystals pick their support among the full blocks around them, and are turned into fallbackBlock if there's none
	Placed crystals need a full block in the face they were placed on
	Crystals break when their support stops being a full block

STATES (side of the crystal where its supporting block is):
	0 = X-
	1 = X+
	2 = Z-
	3 = Z+
	4 = Y- (floor)
	5 = Y+ (ceiling)
*/

[Serializable]
public class VX_AuraCrystalBehaviour : VoxelBehaviour{
	public string blockName;
	public string fallbackBlock;

	private ushort blockID;
	private ushort fallbackBlockID;
	private VoxelReader_Server reader = new VoxelReader_Server();

	// Position of the supporting block relative to the crystal, indexed by state
	private static readonly int3[] STATE_SUPPORT_OFFSETS = {
		new int3(-1,0,0),
		new int3(1,0,0),
		new int3(0,0,-1),
		new int3(0,0,1),
		new int3(0,-1,0),
		new int3(0,1,0)
	};

	// State of a crystal placed by a player, indexed by the facing given by the player's raycast (CastCoord operator-)
	private static readonly ushort[] FACING_TO_STATE = {1, 2, 0, 3, 4, 5};


	public override void PostDeserializationSetup(bool isClient){
		this.blockID = VoxelLoader.GetBlockID(blockName);
		this.fallbackBlockID = VoxelLoader.GetBlockID(fallbackBlock);
	}

	/*
	Only neighbors inside the chunk are considered, so the result never depends on which chunks are loaded
	The support is picked uniformly among the valid ones, using the world position and seed
	*/
	public override void OnGenerate(ChunkPos pos, int x, int y, int z, ushort[] blocks, ushort[] states, ushort[] hps, int worldSeed){
		int index = x*Chunk.chunkWidth*Chunk.chunkDepth + y*Chunk.chunkWidth + z;
		int amount = 0;
		int pick;

		for(int state=0; state < STATE_SUPPORT_OFFSETS.Length; state++){
			if(HasSupportInChunk(blocks, x, y, z, state))
				amount++;
		}

		if(amount == 0){
			blocks[index] = this.fallbackBlockID;
			states[index] = 0;
			hps[index] = ushort.MaxValue;
			return;
		}

		pick = (int)(math.hash(new int4(VoxelReader_Server.ToWorld(pos, x, y, z), worldSeed)) % (uint)amount);

		for(int state=0; state < STATE_SUPPORT_OFFSETS.Length; state++){
			if(!HasSupportInChunk(blocks, x, y, z, state))
				continue;

			if(pick == 0){
				states[index] = (ushort)state;
				return;
			}

			pick--;
		}
	}

	public override bool PlacementRule(ChunkPos pos, int x, int y, int z, int direction, ChunkLoader_Server cl){
		ushort support, supportState;

		if(direction < 0 || direction >= FACING_TO_STATE.Length)
			return false;

		this.reader.Begin(cl);

		if(!this.reader.TryRead(VoxelReader_Server.ToWorld(pos, x, y, z) + STATE_SUPPORT_OFFSETS[FACING_TO_STATE[direction]], out support, out supportState))
			return false;

		return IsFullBlock(support);
	}

	// PlacementRule already guarantees a valid facing
	public override int OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ChunkLoader_Server cl){
		Chunk chunk = cl.GetChunk(pos);

		chunk.metadata.SetState(blockX, blockY, blockZ, FACING_TO_STATE[facing]);

		cl.server.SendBlockUpdate(BUDCode.PLACE, pos, blockX, blockY, blockZ, this.blockID, chunk.metadata.GetState(blockX, blockY, blockZ), chunk.metadata.GetHP(blockX, blockY, blockZ));
		cl.budscheduler.ScheduleSave(pos);

		return 1;
	}

	// Breaks the crystal if its support was broken or changed into something that isn't a full block
	public override void OnBlockUpdate(BUDCode type, int x, int y, int z, int budX, int budY, int budZ, int facing, ChunkLoader_Server cl){
		int3 world = new int3(x, y, z);
		int3 supportPos;
		ushort block, state, support, supportState;

		if(type != BUDCode.BREAK && type != BUDCode.CHANGE)
			return;

		this.reader.Begin(cl);

		if(!this.reader.TryRead(world, out block, out state))
			return;
		if(block != this.blockID || state >= STATE_SUPPORT_OFFSETS.Length)
			return;

		supportPos = world + STATE_SUPPORT_OFFSETS[state];

		if(!supportPos.Equals(new int3(budX, budY, budZ)))
			return;

		// Unloaded supports are kept, so crystals never break because of what the server can't see
		if(!this.reader.TryRead(supportPos, out support, out supportState) || IsFullBlock(support))
			return;

		BreakCrystal(world, cl);
	}

	private void BreakCrystal(int3 world, ChunkLoader_Server cl){
		int3 local;
		ChunkPos pos = VoxelReader_Server.ToChunkPos(world, out local);
		Chunk chunk = cl.GetChunk(pos);

		chunk.data.SetCell(local.x, local.y, local.z, 0);
		chunk.metadata.Reset(local.x, local.y, local.z);

		VoxelLoader.GetObject(this.blockID).OnBreak(pos, local.x, local.y, local.z, cl);
		EmitBlockUpdate(BUDCode.BREAK, world.x, world.y, world.z, 0, cl);

		cl.server.SendBlockUpdate(BUDCode.BREAK, pos, local.x, local.y, local.z, 0, ushort.MaxValue, ushort.MaxValue);
		cl.server.entityHandler.SetRefreshVision(EntityType.DROP, pos);
		cl.budscheduler.ScheduleSave(pos);
	}

	private static bool HasSupportInChunk(ushort[] blocks, int x, int y, int z, int state){
		int3 support = new int3(x, y, z) + STATE_SUPPORT_OFFSETS[state];

		if(support.x < 0 || support.x >= Chunk.chunkWidth || support.z < 0 || support.z >= Chunk.chunkWidth || support.y < 0 || support.y >= Chunk.chunkDepth)
			return false;

		return IsFullBlock(blocks[support.x*Chunk.chunkWidth*Chunk.chunkDepth + support.y*Chunk.chunkWidth + support.z]);
	}

	private static bool IsFullBlock(ushort code){
		if(code == (ushort)(ushort.MaxValue/2))
			return false;

		return VoxelLoader.CheckSolid(code) && !VoxelLoader.CheckTransparent(code);
	}
}
