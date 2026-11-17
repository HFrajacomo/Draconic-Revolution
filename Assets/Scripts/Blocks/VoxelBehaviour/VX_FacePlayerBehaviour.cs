using System;
using UnityEngine;
using Unity.Mathematics;

/*
Objects only. Turns the front of the model to the player that placed it, in one of the 4 horizontal directions,
along the horizontal axis (X or Z) that points most directly at the player. Objects not placed by players face Z+.

The direction is kept in the voxel state as direction * statesPerDirection + rest (0 = Z+, 1 = X+, 2 = Z-, 3 = X-).
rest belongs to other behaviours of the Object, which must only use states below statesPerDirection and keep the part above it
(like VX_AnimationQueueBehaviour, whose queue length is its statesPerDirection). States past the 4 directions face Z+.

Must be used in onPlayerPlace and rotationValue, with the Object's needsRotation on and maximumRotationScaleState = 4 * statesPerDirection
*/
[Serializable]
public class VX_FacePlayerBehaviour : VoxelBehaviour{
	public int statesPerDirection; // States used by the other behaviours of the Object. 0 is read as 1
	public int frontYaw; // Degrees (multiple of 90) added to the rotation, for models whose front isn't Z+

	private static readonly int DIRECTION_COUNT = 4;
	// Rotation (ObjectTransformMath convention) that turns a model facing Z+ to each direction
	private static readonly int[] DIRECTION_YAW = {180, 90, 0, -90};


	public override void PostDeserializationSetup(bool isClient){
		if(this.statesPerDirection < 1)
			this.statesPerDirection = 1;
	}

	public override void PostObjectSetup(BlocklikeObject owner){
		if(!owner.needsRotation || owner.maximumRotationScaleState < DIRECTION_COUNT * this.statesPerDirection)
			throw new DeserializationErrorException($"[VX_FacePlayerBehaviour] Object '{owner.codename}' needs needsRotation and maximumRotationScaleState {DIRECTION_COUNT * this.statesPerDirection}");
	}

	// Keeps the part of the state used by other behaviours (set by onPlace, which runs before)
	public override int OnPlayerPlace(ChunkPos pos, int x, int y, int z, int facing, ulong playerID, ChunkLoader_Server cl){
		VoxelMetadata metadata = cl.GetChunk(pos).metadata;
		ushort state = metadata.GetState(x, y, z);
		int rest = (state == ushort.MaxValue) ? 0 : state % this.statesPerDirection;

		metadata.SetState(x, y, z, (ushort)(ChooseDirection(pos, x, y, z, playerID, cl) * this.statesPerDirection + rest));
		return 0;
	}

	public override int2 GetRotationValue(ushort state){
		return new int2(DIRECTION_YAW[GetDirection(state)] + this.frontYaw, 0);
	}

	private int GetDirection(ushort state){
		if(state >= DIRECTION_COUNT * this.statesPerDirection)
			return 0;

		return state / this.statesPerDirection;
	}

	// Z+ if the placer is unknown or right above the voxel
	private int ChooseDirection(ChunkPos pos, int x, int y, int z, ulong playerID, ChunkLoader_Server cl){
		PlayerData placer;

		if(!cl.regionHandler.allPlayerData.TryGetValue(playerID, out placer))
			return 0;

		CastCoord coord = new CastCoord(pos, x, y, z);
		float dx = placer.posX - coord.GetWorldX();
		float dz = placer.posZ - coord.GetWorldZ();

		if(math.abs(dx) > math.abs(dz))
			return dx > 0 ? 1 : 3;

		return dz >= 0 ? 0 : 2;
	}
}
