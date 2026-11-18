using System;
using UnityEngine;

/*
Plays a sound on the voxel whenever it's interacted with. The sound is heard by every Client that has the chunk loaded.
The Server sends SFXPLAY on interaction, and the Clients play the sound in OnSFXPlay. Non-looping sounds are removed once they finish.

Must be used in onInteract and onSFXPlay. Other behaviours of the voxel in onSFXPlay also receive the message
*/
[Serializable]
public class VX_InteractSoundBehaviour : VoxelBehaviour{
	public string sound;
	public bool randomPitch = true; // Plays each sound with a random pitch between SFXLoader.MIN_RANDOM_PITCH and MAX_RANDOM_PITCH


	// Sounds are only loaded by Clients, which load them before voxels run this
	public override void PostDeserializationSetup(bool isClient){
		if(string.IsNullOrEmpty(this.sound))
			throw new DeserializationErrorException("[VX_InteractSoundBehaviour] No sound was given");
		if(isClient && !AudioLoader.IsSound(this.sound))
			throw new DeserializationErrorException($"[VX_InteractSoundBehaviour] Sound '{this.sound}' isn't in SOUNDS_LIST");
	}

	public override int OnInteract(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){
		Chunk chunk = cl.GetChunk(pos);

		NetMessage message = new NetMessage(NetCode.SFXPLAY);
		message.SFXPlay(pos, x, y, z, chunk.data.GetCell(x, y, z), chunk.metadata.GetState(x, y, z));
		cl.server.SendToClients(pos, message);

		return 0;
	}

	public override int OnSFXPlay(ChunkPos pos, int x, int y, int z, ushort state, ChunkLoader cl){
		cl.sfx.LoadBlockSFX(this.sound, pos, x, y, z, this.randomPitch ? SFXLoader.GetRandomPitch() : 1f);
		return 1;
	}
}
