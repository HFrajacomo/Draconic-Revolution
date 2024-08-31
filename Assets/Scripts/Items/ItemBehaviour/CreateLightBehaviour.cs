using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class CreateLightBehaviour : ItemBehaviour{
	public float voxelLightIntensity;
	public float lightComponentIntensity;
	public Color lightColor;

	public override void OnHoldPlayer(ChunkLoader cl, ItemStack its){
		//cl.client.entityHandler.AddPlayer()
	}
}