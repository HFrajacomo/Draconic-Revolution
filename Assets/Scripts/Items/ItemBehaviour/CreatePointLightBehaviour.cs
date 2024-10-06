using System;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering.HighDefinition;

[Serializable]
public class CreatePointLightBehaviour : ItemBehaviour{
	public string audioName;
	public float voxelLightIntensity;
	public float lightComponentIntensity;
	public float lightRange;
	public Color lightColor;
	public bool realisticLight;

	public override void OnHoldPlayer(ChunkLoader cl, ItemStack its, ulong playerCode){
		Light lightComponent = cl.playerSheetController.GetLight();
		HDAdditionalLightData light = cl.playerSheetController.GetLightData();
		EntityID id = new EntityID(EntityType.PLAYER, playerCode);

		light.color = this.lightColor;
		light.range = this.lightRange;
		lightComponent.intensity = this.lightComponentIntensity;

		cl.playerSheetController.Enable(this.realisticLight);

		cl.playerSheetController.SetVoxelLightIntensity(this.voxelLightIntensity);
		cl.voxelLightHandler.Add(id, cl.playerPositionHandler.playerTransform.position, this.voxelLightIntensity, priority:true);
		cl.sfx.LoadEntitySFX(this.audioName, id);
	}

	public override void OnUnholdPlayer(ChunkLoader cl, ItemStack its, ulong playerCode){
		EntityID id = new EntityID(EntityType.PLAYER, playerCode);

		cl.playerSheetController.Disable(this.realisticLight);
		cl.voxelLightHandler.Remove(id);
		cl.sfx.RemoveEntitySFX(id);
	}

	public override void OnHoldClient(ChunkLoader cl, ItemStack its, ulong playerCode){
		GameObject go = cl.client.entityHandler.GetEntityObject(new EntityID(EntityType.PLAYER, playerCode));
		Light lightComponent = go.GetComponent<Light>();
		HDAdditionalLightData light = go.GetComponent<HDAdditionalLightData>();
		RealisticLight realLight = go.GetComponent<RealisticLight>();
		EntityID id = new EntityID(EntityType.PLAYER, playerCode);

		light.color = this.lightColor;
		light.range = this.lightRange;
		lightComponent.intensity = this.lightComponentIntensity;

		lightComponent.enabled = true;
		light.enabled = true;
		realLight.enabled = this.realisticLight;

		cl.voxelLightHandler.Add(id, go.transform.position, this.voxelLightIntensity);
		cl.sfx.LoadEntitySFX(this.audioName, id);
	}

	public override void OnUnholdClient(ChunkLoader cl, ItemStack its, ulong playerCode){
		GameObject go = cl.client.entityHandler.GetEntityObject(new EntityID(EntityType.PLAYER, playerCode));
		Light lightComponent = go.GetComponent<Light>();
		HDAdditionalLightData light = go.GetComponent<HDAdditionalLightData>();
		RealisticLight realLight = go.GetComponent<RealisticLight>();
		EntityID id = new EntityID(EntityType.PLAYER, playerCode);

		lightComponent.enabled = false;
		light.enabled = false;
		realLight.enabled = false;

		cl.voxelLightHandler.Remove(id);
		cl.sfx.RemoveEntitySFX(id);
	}
}