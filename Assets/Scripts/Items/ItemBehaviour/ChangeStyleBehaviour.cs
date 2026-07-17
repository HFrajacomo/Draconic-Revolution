using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class ChangeStyleBehaviour : ItemBehaviour{
	public string battleStyle;
	private int styleCode;

	public override void PostDeserializationSetup(bool isClient){this.styleCode = AnimationLoader.GetBattleStyle(this.battleStyle).GetCode();}

	public override void OnHoldServer(ChunkLoader_Server cl, ItemStack its, ulong code){
		Debug.Log("Inside ChangeStyleBehaviour.OnHoldServer");
		cl.server.SendBattleStyle(code, this.styleCode);
	}
}