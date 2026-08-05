using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class GetItemAction : ItemBehaviour {
	public string codename;
	private EntityAction action;

	public override void PostDeserializationSetup(bool isClient){
		this.action = ActionLoader.GetAction(this.codename);
	}

	public override EntityAction OnCreateAction(ChunkLoader cl, ItemStack its){return this.action;}
}