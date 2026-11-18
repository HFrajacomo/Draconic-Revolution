using System;
using System.Collections.Generic;
using UnityEngine;

/*
Animated Objects only. Plays the listed animations of the Object in order, one per interaction, going back to the first after the last.
Interacting while an animation plays starts the next one from the normalized time the current one reached.

The voxel state below count (amount of queued animations) is the step: interactions since the default pose.
The Object rests on the last frame of animations[(default + step) % count] (GetAnimationPose), so state 0 (placed or generated Objects)
always rests on defaultAnimation. The part of the state above count belongs to other behaviours (like VX_FacePlayerBehaviour) and is kept.

Must be used in onInteract and animationPose
*/
[Serializable]
public class VX_AnimationQueueBehaviour : VoxelBehaviour{
	public List<string> animations;
	public string defaultAnimation; // Animation whose last frame is the pose of placed Objects. Empty is the last of the queue

	private byte[] animationIndices; // Index of each queued animation in the Object's model identity
	private int defaultStep; // Position of defaultAnimation in the queue
	private ushort blockCode;


	// Animation names are resolved through the model identity, which both the Client and the Server read from the Object file
	public override void PostObjectSetup(BlocklikeObject owner){
		VX_AnimatedModelIdentityBehaviour identity = owner.GetModelIdentity() as VX_AnimatedModelIdentityBehaviour;
		int index;

		if(identity == null)
			throw new DeserializationErrorException($"[VX_AnimationQueueBehaviour] Object '{owner.codename}' doesn't use VX_AnimatedModelIdentityBehaviour");
		if(this.animations == null || this.animations.Count == 0)
			throw new DeserializationErrorException($"[VX_AnimationQueueBehaviour] Object '{owner.codename}' has no animations in its queue");

		this.animationIndices = new byte[this.animations.Count];

		for(int i=0; i < this.animations.Count; i++){
			index = identity.GetAnimationIndex(this.animations[i]);

			if(index < 0)
				throw new DeserializationErrorException($"[VX_AnimationQueueBehaviour] Animation '{this.animations[i]}' of Object '{owner.codename}' isn't in its model identity");

			this.animationIndices[i] = (byte)index;
		}

		if(string.IsNullOrEmpty(this.defaultAnimation))
			this.defaultStep = this.animations.Count-1;
		else
			this.defaultStep = this.animations.IndexOf(this.defaultAnimation);

		if(this.defaultStep < 0)
			throw new DeserializationErrorException($"[VX_AnimationQueueBehaviour] defaultAnimation '{this.defaultAnimation}' of Object '{owner.codename}' isn't in its queue");

		this.blockCode = VoxelLoader.GetBlockID(owner.codename);
	}

	// Advances the queue and tells the Clients to play the next animation. The chunk is only saved: Clients don't rebuild it
	public override int OnInteract(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){
		Chunk chunk = cl.GetChunk(pos);
		ushort state = chunk.metadata.GetState(x, y, z);
		int step = (GetStep(state) + 1) % this.animationIndices.Length;
		ushort next = (ushort)(GetBase(state) + step);

		chunk.metadata.SetState(x, y, z, next);

		NetMessage message = new NetMessage(NetCode.VOXELANIMATION);
		message.VoxelAnimation(pos, x, y, z, this.blockCode, next, GetQueuedAnimation(step), true);
		cl.server.SendToClients(pos, message);

		// Clients already have the new state, so it isn't sent again with other changes of the chunk
		if(chunk.changeSet != null)
			chunk.changeSet.Forget(ChunkChangeSet.GetIndex(x, y, z));

		return 2;
	}

	public override int GetAnimationPose(ushort state){
		return GetQueuedAnimation(GetStep(state));
	}

	public int GetQueueLength(){return this.animations.Count;}

	private byte GetQueuedAnimation(int step){
		return this.animationIndices[(this.defaultStep + step) % this.animationIndices.Length];
	}

	// Unassigned states rest on the default pose
	private int GetStep(ushort state){
		if(state == ushort.MaxValue)
			return 0;

		return state % this.animationIndices.Length;
	}

	// Part of the state used by other behaviours
	private int GetBase(ushort state){
		if(state == ushort.MaxValue)
			return 0;

		return state - GetStep(state);
	}
}
