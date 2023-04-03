using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using Unity.Mathematics;

/*
STATES:
	0 = To be set on the OnLoad() event
	1 = Fire X+
	2 = Fire X-
	3 = Fire Z+
	4 = Fire Z-
	5 = Fire Y+
	6 = Fire Y-
	7 = Water X+
	8 = Water X-
	9 = Water Z+
	10 = Water Z-
	11 = Water Y+
	12 = Water Y-
	13 = Air X+
	14 = Air X-
	15 = Air Z+
	16 = Air Z-
	17 = Air Y+
	18 = Air Y-
	19 = Terra X+
	20 = Terra X-
	21 = Terra Z+
	22 = Terra Z-
	23 = Terra Y+
	24 = Terra Y-
	25 = Ordo X+
	26 = Ordo X-
	27 = Ordo Z+
	28 = Ordo Z-
	29 = Ordo Y+
	30 = Ordo Y-
	31 = Perditio X+
	32 = Perditio X-
	33 = Perditio Z+
	34 = Perditio Z-
	35 = Perditio Y+
	36 = Perditio Y-
	37 = Precantio X+
	38 = Precantio X-
	39 = Precantio Z+
	40 = Precantio Z-
	41 = Precantio Y+
	42 = Precantio Y-
*/
public class VisCrystal_Object : BlocklikeObject
{
	public VisCrystal_Object(bool isClient){
		this.shaderIndex = ShaderIndex.ASSETS_SOLID;
		this.name = "Vis Crystal";
		this.solid = true;
		this.transparent = 1;
		this.invisible = false;
		this.liquid = false;
		this.washable = false;
		this.hasLoadEvent = true;
		this.affectLight = true;
		this.maxHP = 100;

		if(isClient){
			this.go = GameObject.Find("----- PrefabObjects -----/VisCrystal_Object");
			this.hitboxObject = GameObject.Find("----- PrefabObjects -----/VisCrystal_Object/Hitbox");
			this.mesh = this.go.GetComponent<MeshFilter>().sharedMesh;
			this.scaling = new Vector3(12, 12, 37);
			this.hitboxScaling = new Vector3(1,1,1);
			this.hitboxMesh = hitboxObject.GetComponent<MeshFilter>().sharedMesh;
		}

		this.needsRotation = true;
		this.stateNumber = 43;
	}

	// Get rotation in degrees
	public override int2 GetRotationValue(ushort state){
		int modState = (int)state % 6;

		if(modState == 1)
			return new int2(270,0);
		else if(modState == 3)
			return new int2(0,0);
		else if(modState == 2)
			return new int2(90,0);
		else if(modState == 4)
			return new int2(180,0);
		else if(modState == 5)
			return new int2(0,-90);
		else if(modState == 0 && state != 0)
			return new int2(0,90);
		else
			return new int2(0,0);
	}

	// Functions for the new Bursting Core Rendering
	public override Vector3 GetOffsetVector(ushort state){
		int modState = (int)state % 6;

		if(modState == 1)
			return new Vector3(-0.5f, 0, 0f);
		else if(modState == 3)
			return new Vector3(0f, 0f, -0.5f);
		else if(modState == 2)
			return new Vector3(0.5f, 0, 0f);
		else if(modState == 4)
			return new Vector3(0f, 0, 0.5f);
		else if(modState == 5)
			return new Vector3(0, -.5f, 0);
		else if(modState == 0 && state != 0)
			return new Vector3(0, .5f, 0);
		else
			return new Vector3(0,0,0);
	}
}