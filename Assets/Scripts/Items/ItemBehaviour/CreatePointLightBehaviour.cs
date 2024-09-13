using System;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering.HighDefinition;

[Serializable]
public class CreatePointLightBehaviour : ItemBehaviour{
	public float voxelLightIntensity;
	public float lightComponentIntensity;
	public float lightRange;
	public Color lightColor;
	public bool realisticLight;

	public override void OnHoldPlayer(ChunkLoader cl, ItemStack its, ulong playerCode){
		Light lightComponent = cl.playerSheetController.GetLight();
		HDAdditionalLightData light = cl.playerSheetController.GetLightData();

		light.color = this.lightColor;
		light.range = this.lightRange;
		lightComponent.intensity = this.lightComponentIntensity;

		cl.playerSheetController.Enable(this.realisticLight);

		cl.playerSheetController.SetVoxelLightIntensity(this.voxelLightIntensity);
		cl.voxelLightHandler.Add(new EntityID(EntityType.PLAYER, playerCode), cl.playerPositionHandler.playerTransform.position, this.voxelLightIntensity, priority:true);
	}

	public override void OnUnholdPlayer(ChunkLoader cl, ItemStack its, ulong playerCode){
		cl.playerSheetController.Disable(this.realisticLight);
		cl.voxelLightHandler.Remove(new EntityID(EntityType.PLAYER, playerCode));
	}
}