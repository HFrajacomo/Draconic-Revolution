using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class TestVoxelBehaviour : VoxelBehaviour{
	public string testString;

	public override int OnBreak(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){
		Debug.Log("TEST");
		return 0;
	}

	public override void OnPlayerStepEnter(PlayerVoxelLocation location, CharacterSheet sheet, ChunkLoader cl){
		Debug.Log("TEST");
	}
}