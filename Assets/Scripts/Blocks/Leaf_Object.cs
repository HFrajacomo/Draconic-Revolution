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
		this.go = GameObject.Find("----- PrefabObjects -----/Leaf_Object");
	}

}
