using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class IT_GetItemActionBehaviour : ItemBehaviour {
	public string name;
	private EntityAction action;

	public override void PostDeserializationSetup(bool isClient){
		this.action = ActionLoader.GetCopy(this.name);
	}

	public override EntityAction OnCreateAction(ChunkLoader cl, ItemStack its){
		this.action.SetItemStack(its);
		return this.action;
	}
}