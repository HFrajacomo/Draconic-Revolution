using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using Unity.Mathematics;

using Random = System.Random;

public class SpawnCrystal_Object : BlocklikeObject
{
	private static readonly VisCrystalBehaviour vcb = new VisCrystalBehaviour((ushort)BlockID.SPAWN_CRYSTAL);

	public SpawnCrystal_Object(bool isClient){
		this.shaderIndex = ShaderIndex.ASSETS_SOLID;
		this.name = "Spawn Crystal";
		this.solid = true;
		this.transparent = 1;
		this.invisible = false;
		this.liquid = false;
		this.washable = false;
		this.hasLoadEvent = true;
		this.customPlace = true;
		this.affectLight = true;
		this.maxHP = 100;
		this.atlasPosition = new int2(7,0);

		if(isClient){
			this.go = GameObject.Find("----- PrefabObjects -----/SpawnCrystal_Object");
			this.hitboxObject = GameObject.Find("----- PrefabObjects -----/SpawnCrystal_Object/Hitbox");
			this.mesh = this.go.GetComponent<MeshFilter>().mesh;
			this.scaling = new Vector3(12, 12, 37);
			this.hitboxScaling = new Vector3(.75f, .75f, 1.8f);
			this.hitboxMesh = hitboxObject.GetComponent<MeshFilter>().sharedMesh;
			RemapMeshUV();
		}
	}

	// Randomly converts to a elemental Vis Crystal and runs their OnPlace() event
	public override int OnLoad(CastCoord coord, ChunkLoader_Server cl){
		ApplyLoadTransformation(coord, cl);

		return 0;
	}

	public override int OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ChunkLoader_Server cl){
		ConvertToElementalVis(new CastCoord(pos, blockX, blockY, blockZ), facing, cl);
		return 0;
	}

	private void ApplyLoadTransformation(CastCoord pos, ChunkLoader_Server cl){
		if(!vcb.FindAndPlaceCrystal(pos, cl)){
			vcb.DeleteCrystal(pos, cl);
		}
	}

	private void ConvertToElementalVis(CastCoord coord, int facing, ChunkLoader_Server cl){
		int codeAddition = vcb.GetRandomDirection();
		ushort newCode = (ushort)(cl.chunks[coord.GetChunkPos()].data.GetCell(coord.blockX, coord.blockY, coord.blockZ) + codeAddition);

		cl.chunks[coord.GetChunkPos()].data.SetCell(coord.blockX, coord.blockY, coord.blockZ, newCode);
		cl.blockBook.objects[ushort.MaxValue - newCode].OnPlace(coord.GetChunkPos(), coord.blockX, coord.blockY, coord.blockZ, facing, cl);
	}
}