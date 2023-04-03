using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using Unity.Mathematics;

/*
STATES:
	0 = Ordo X+
	1 = Ordo X-
	2 = Ordo Z+
	3 = Ordo Z-
	4 = Ordo Y+
	5 = Ordo Y-
*/
public class OrdoCrystal_Object : BlocklikeObject
{
	public OrdoCrystal_Object(bool isClient){
		this.shaderIndex = ShaderIndex.ASSETS_SOLID;
		this.name = "Ordo Crystal";
		this.solid = true;
		this.transparent = 1;
		this.invisible = false;
		this.liquid = false;
		this.washable = false;
		this.hasLoadEvent = false;
		this.affectLight = true;
		this.maxHP = 100;
		this.atlasPosition = new int2(4,0);

		if(isClient){
			this.go = GameObject.Find("----- PrefabObjects -----/VisCrystal_Object");
			this.hitboxObject = GameObject.Find("----- PrefabObjects -----/VisCrystal_Object/Hitbox");
			this.mesh = this.go.GetComponent<MeshFilter>().sharedMesh;
			this.scaling = new Vector3(12, 12, 37);
			this.hitboxScaling = new Vector3(.75f, .75f, 1.8f);
			this.hitboxMesh = hitboxObject.GetComponent<MeshFilter>().sharedMesh;
		}

		this.needsRotation = true;
		this.stateNumber = 6;

		RemapMeshUV();
	}

	// Get rotation in degrees
	public override int2 GetRotationValue(ushort state){
		if(state == 0)
			return new int2(270,0);
		else if(state == 2)
			return new int2(0,0);
		else if(state == 1)
			return new int2(90,0);
		else if(state == 3)
			return new int2(180,0);
		else if(state == 4)
			return new int2(0,-90);
		else if(state == 5)
			return new int2(0,90);
		else
			return new int2(0,0);
	}

	// Functions for the new Bursting Core Rendering
	public override Vector3 GetOffsetVector(ushort state){
		if(state == 0)
			return new Vector3(-0.5f, 0, 0f);
		else if(state == 2)
			return new Vector3(0f, 0f, -0.5f);
		else if(state == 1)
			return new Vector3(0.5f, 0, 0f);
		else if(state == 3)
			return new Vector3(0f, 0, 0.5f);
		else if(state == 4)
			return new Vector3(0, -.5f, 0);
		else if(state == 5)
			return new Vector3(0, .5f, 0);
		else
			return new Vector3(0,0,0);
	}
}