using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leaf_Object : BlocklikeObject
{
	public Leaf_Object(){
		this.name = "Leaf";
		this.solid = true;
		this.transparent = true;
		this.invisible = false;
		this.liquid = false;
		this.hasLoadEvent = false;
		this.go = GameObject.Find("----- PrefabObjects -----/Leaf_Object");
		this.mesh = this.go.GetComponent<MeshFilter>().sharedMesh;
		this.scaling = new Vector3(50, 50, 50);
	}

}
