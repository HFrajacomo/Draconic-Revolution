using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torch_Object : BlocklikeObject
{
	public GameObject fireVFX;

	public Torch_Object(){
		this.name = "Torch";
		this.solid = false;
		this.transparent = true;
		this.invisible = false;
		this.liquid = false;
		this.prefabName = "Torch_Object";
		this.centeringOffset = new Vector3(0f,-0.3f,0f);
		this.scaling = new Vector3(1f, 2f, 1f);

		this.fireVFX = GameObject.Find("----- PrefabVFX -----/FireVFX");
	}

	public override int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){
		ushort? state = cl.chunks[pos].metadata.GetMetadata(blockX,blockY,blockZ).state;

		if(state == null)
			return 0;
		else if(state == 1){
			ControlFire(pos, blockX, blockY, blockZ, state);
			cl.chunks[pos].metadata.GetMetadata(blockX,blockY,blockZ).state = 0;
		}
		else if(state == 0){
			ControlFire(pos, blockX, blockY, blockZ, state);
			cl.chunks[pos].metadata.GetMetadata(blockX,blockY,blockZ).state = 1;
		}

		return 0;
	}

	public override int OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){
		GameObject fire = GameObject.Instantiate(this.fireVFX, new Vector3(pos.x*Chunk.chunkWidth + blockX, blockY + 0.2f, pos.z*Chunk.chunkWidth + blockZ), Quaternion.identity);
		fire.name = BuildVFXName(pos, blockX, blockY, blockZ);
		this.vfx.Add(pos, fire, active:true);
		
		cl.chunks[pos].metadata.GetMetadata(blockX,blockY,blockZ).state = 1;

		return 0;
	}

	public override int OnBreak(ChunkPos pos, int x, int y, int z, ChunkLoader cl){
		string name = BuildVFXName(pos,x,y,z);
		GameObject.Destroy(this.vfx.data[pos][name]);
		this.vfx.Remove(pos, name);

		return 0;
	}

	// Handles Fire VFX turning on and off
	private void ControlFire(ChunkPos pos, int x, int y, int z, ushort? state){
		// Turns off fire when it's on
		if(state == 1)
			this.vfx.data[pos][BuildVFXName(pos, x, y, z)].SetActive(false);
		// Turns on fire when it's off
		else if(state == 0)
			this.vfx.data[pos][BuildVFXName(pos, x, y, z)].SetActive(true);
	}	

	// Builds name for current Torch VFX
	private string BuildVFXName(ChunkPos pos, int x, int y, int z){
		return "Torch (" + pos.x + ", " + pos.z + ") [" + x + ", " + y + ", " + z + "]";

	}

}
