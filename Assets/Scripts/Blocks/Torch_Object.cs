using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torch_Object : BlocklikeObject
{
	public Torch_Object(){
		this.name = "Torch";
		this.solid = false;
		this.transparent = true;
		this.invisible = false;
		this.liquid = false;

		this.prefabName = "Torch_Object";
		this.centeringOffset = new Vector3(0f,-0.35f,0f);
		this.scaling = new Vector3(1f, 3.5f, 1f);
	}

	public override int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){
		return 1;
	}
}
