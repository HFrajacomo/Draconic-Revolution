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


	public ShaderLoader(bool client){
		isClient = client;

		if(client)
			InstantiateBuffers();
	}

	public override bool Load(){
		if(isClient)
			InitVoxelLightBuffer();
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
}