using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class IT_GetItemActionBehaviour : ItemBehaviour {
	public string name;
	public Wrapper<ActionArgumentInjector> actionArguments;
	public EntityAction action; // MOVE TO PRIVATE

	public override void PostDeserializationSetup(bool isClient){
		this.action = ActionLoader.GetCopy(this.name);

		if(actionArguments.data != null){
			EntityActionDeserializer.DeserializeKwargs(this.action, this.actionArguments);
		}
	}

	public override EntityAction OnCreateAction(ChunkLoader cl, ItemStack its){
		this.action.SetItemStack(its);
		return this.action;
	}
}