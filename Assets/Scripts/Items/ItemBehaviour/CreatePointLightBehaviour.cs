using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class CreatePointLightBehaviour : ItemBehaviour{
	public float voxelLightIntensity;
	public float lightComponentIntensity;
	public float lightRange;
	public Color lightColor;

	public override void OnHoldPlayer(ChunkLoader cl, ItemStack its, ulong playerCode){
		Light light = cl.client.entityHandler.GetPointLight(EntityType.PLAYER, playerCode);

		if(light == null)
			return;

		light.color = this.lightColor;
		light.intensity = this.lightComponentIntensity;
		light.range = this.lightRange;

		light.enabled = true;

		cl.voxelLightHandler.Add(new EntityID(EntityType.PLAYER, playerCode), cl.playerPositionHandler.playerTransform.position, this.voxelLightIntensity, priority:true);
	}

	public override void OnUnholdPlayer(ChunkLoader cl, ItemStack its, ulong playerCode){
		Light light = cl.client.entityHandler.GetPointLight(EntityType.PLAYER, playerCode);

		if(light == null)
			return;

		light.enabled = false;

		cl.voxelLightHandler.Remove(new EntityID(EntityType.PLAYER, playerCode));
	}
}