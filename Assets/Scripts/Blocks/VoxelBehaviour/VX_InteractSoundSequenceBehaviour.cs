using System;
using System.Collections.Generic;
using UnityEngine;

/*
Plays the listed sounds in order, one per interaction, going back to the first after the last. Interaction n plays sounds[(n-1) % count].

The position in the list is the amount of interactions since placement, kept in the voxel state below count (amount of sounds),
the same way VX_AnimationQueueBehaviour keeps its queue. So the sounds follow its animations when both lists have the same length:
	countInteractions false: another behaviour listed before this one in onInteract (like VX_AnimationQueueBehaviour) advances the state
	countInteractions true: this behaviour advances it, for voxels with no other behaviour doing it
The part of the state above count belongs to other behaviours (like VX_FacePlayerBehaviour) and is kept.

The Server sends SFXPLAY with the new state, and the Clients pick the same sound from it in OnSFXPlay.
Must be used in onInteract (or onContainerToggle) and onSFXPlay. Other behaviours of the voxel in onSFXPlay also receive the message
*/
[Serializable]
public class VX_InteractSoundSequenceBehaviour : VoxelBehaviour{
	public List<string> sounds;
	public bool countInteractions;
	public bool randomPitch = true; // Plays each sound with a random pitch between SFXLoader.MIN_RANDOM_PITCH and MAX_RANDOM_PITCH


	// Sounds are only loaded by Clients, which load them before voxels run this
	public override void PostDeserializationSetup(bool isClient){
		if(this.sounds == null || this.sounds.Count == 0)
			throw new DeserializationErrorException("[VX_InteractSoundSequenceBehaviour] No sounds were given");

		if(!isClient)
			return;

		foreach(string sound in this.sounds){
			if(!AudioLoader.IsSound(sound))
				throw new DeserializationErrorException($"[VX_InteractSoundSequenceBehaviour] Sound '{sound}' isn't in SOUNDS_LIST");
		}
	}

	// Objects only: checks that the sounds follow the animation queue of the Object
	// Containers run both from onContainerToggle instead of onInteract, so that list is checked too
	public override void PostObjectSetup(BlocklikeObject owner){
		CheckQueueSync(owner, owner.GetOnInteract());
		CheckQueueSync(owner, owner.GetOnContainerToggle());
	}

	private void CheckQueueSync(BlocklikeObject owner, List<VoxelBehaviour> interact){
		VX_AnimationQueueBehaviour queue;

		if(interact == null)
			return;

		int position = interact.IndexOf(this);

		if(position < 0)
			return;

		for(int i=0; i < interact.Count; i++){
			queue = interact[i] as VX_AnimationQueueBehaviour;

			if(queue == null)
				continue;

			if(this.countInteractions)
				throw new DeserializationErrorException($"[VX_InteractSoundSequenceBehaviour] Object '{owner.codename}' already counts interactions with VX_AnimationQueueBehaviour, so countInteractions must be false");
			if(i > position)
				throw new DeserializationErrorException($"[VX_InteractSoundSequenceBehaviour] Object '{owner.codename}' must list VX_AnimationQueueBehaviour before it in onInteract/onContainerToggle");
			if(queue.GetQueueLength() != this.sounds.Count)
				throw new DeserializationErrorException($"[VX_InteractSoundSequenceBehaviour] Object '{owner.codename}' has {this.sounds.Count} sounds and {queue.GetQueueLength()} queued animations");
		}
	}

	// Standalone: the new state is sent to the Clients and saved. Otherwise the behaviour that counts interactions handles it
	public override int OnInteract(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){
		Chunk chunk = cl.GetChunk(pos);
		ushort state = chunk.metadata.GetState(x, y, z);

		if(this.countInteractions){
			state = (ushort)(GetBase(state) + (GetStep(state) + 1) % this.sounds.Count);
			chunk.metadata.SetState(x, y, z, state);
		}

		NetMessage message = new NetMessage(NetCode.SFXPLAY);
		message.SFXPlay(pos, x, y, z, chunk.data.GetCell(x, y, z), state);
		cl.server.SendToClients(pos, message);

		return this.countInteractions ? 1 : 0;
	}

	// The state already counts the interaction that played the sound
	public override int OnSFXPlay(ChunkPos pos, int x, int y, int z, ushort state, ChunkLoader cl){
		cl.sfx.LoadBlockSFX(this.sounds[(GetStep(state) + this.sounds.Count - 1) % this.sounds.Count], pos, x, y, z, this.randomPitch ? SFXLoader.GetRandomPitch() : 1f);
		return 1;
	}

	// Unassigned states count no interactions
	private int GetStep(ushort state){
		if(state == ushort.MaxValue)
			return 0;

		return state % this.sounds.Count;
	}

	private int GetBase(ushort state){
		if(state == ushort.MaxValue)
			return 0;

		return state - GetStep(state);
	}
}
