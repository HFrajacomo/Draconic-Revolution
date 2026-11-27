using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

using Object = UnityEngine.Object;

public class ShaderLoader : BaseLoader {
	private static bool isClient;

	// Voxel Lights
	private static Vector4[] VOXEL_LIGHT_INITIAL_BUFFER;
	private static readonly int VOXEL_LIGHT_BUFFER_SIZE = 32; 

	// Voxel Light Curve (see VoxelLightIntensity.hlsl). Falloff is the brightness multiplier per light level lost
	private static readonly float VOXEL_LIGHT_FALLOFF = 0.8f;
	private static readonly float VOXEL_LIGHT_FLOOR = 0.01f;
	// Brightness of Extra Lights (light blocks and moving lights), in nits. Their color comes from their realtime Point Lights
	private static readonly float EXTRA_LIGHT_INTENSITY = 0.6f;

	// Damage decals of Objects (see DamageDecal.hlsl). Same texture and opacity as the block decals (DecalMaterial)
	private static readonly string DAMAGE_DECAL_TEXTURE_RESPATH = "decalTest";
	private static readonly float DAMAGE_DECAL_OPACITY = 0.4f;
	// Global shader textures aren't references, so scene loads would unload a texture only bound as one. This field keeps it loaded
	private static Texture2D damageDecalTexture;


	public ShaderLoader(bool client){
		isClient = client;

		if(client)
			InstantiateBuffers();
	}

	public override bool Load(){
		if(isClient){
			InitVoxelLightBuffer();
			InitVoxelLightCurve();
			InitDamageDecals();
		}
		return true;
	}

	public static int GetVoxelLightBufferSize(){return VOXEL_LIGHT_BUFFER_SIZE;}

	private void InstantiateBuffers(){
 		VOXEL_LIGHT_INITIAL_BUFFER = new Vector4[VOXEL_LIGHT_BUFFER_SIZE];
	}

	private void InitVoxelLightBuffer(){
		VOXEL_LIGHT_INITIAL_BUFFER[0] = new Vector4(0,-1000,0,0);

		for(int i=1; i < VOXEL_LIGHT_BUFFER_SIZE; i++){
			VOXEL_LIGHT_INITIAL_BUFFER[i] = new Vector4(0,0,0,0);
		}

		Shader.SetGlobalVectorArray("_VOXEL_LIGHT_BUFFER", VOXEL_LIGHT_INITIAL_BUFFER);
	}

	private void InitVoxelLightCurve(){
		Shader.SetGlobalFloat("_VoxelLightFalloff", VOXEL_LIGHT_FALLOFF);
		Shader.SetGlobalFloat("_VoxelLightFloor", VOXEL_LIGHT_FLOOR);
		// Driven by AmbientHandler inside caves
		Shader.SetGlobalFloat("_DarkAdaptation", 0f);
		Shader.SetGlobalVector("_DarkAdaptationCenter", Vector4.zero);
		// Driven by AmbientHandler from the Ambience Preset
		Shader.SetGlobalFloat("_VoxelLightAmbient", 0f);
		Shader.SetGlobalVector("_VoxelShaftLight", Vector4.zero);
		Shader.SetGlobalVector("_VoxelShaftDirection", Vector3.up);
		Shader.SetGlobalVector("_VoxelShaftAmbient", Vector4.zero);
		Shader.SetGlobalFloat("_ExtraLightIntensity", EXTRA_LIGHT_INTENSITY);
	}

	private void InitDamageDecals(){
		damageDecalTexture = Resources.Load<Texture2D>(DAMAGE_DECAL_TEXTURE_RESPATH);

		if(damageDecalTexture == null)
			throw new DeserializationErrorException($"[ShaderLoader] Couldn't find the damage decal texture at Resources/{DAMAGE_DECAL_TEXTURE_RESPATH}");

		damageDecalTexture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
		BindDamageDecals();
	}

	// Sets the damage decal globals. Also called when the Game scene starts, so they're always bound to the loaded texture
	public static void BindDamageDecals(){
		if(damageDecalTexture == null)
			return;

		Shader.SetGlobalTexture("_DamageDecalTexture", damageDecalTexture);
		Shader.SetGlobalFloat("_DamageDecalOpacity", DAMAGE_DECAL_OPACITY);
		Shader.SetGlobalFloat("_DamageDecalStages", Constants.DECAL_STAGE_SIZE);
	}
}