using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class IT_ChangeStyleBehaviour : ItemBehaviour {
	public string battleStyle;
	private int styleCode;

	public override void PostDeserializationSetup(bool isClient){this.styleCode = AnimationLoader.GetBattleStyle(this.battleStyle).GetCode();}

	public override void OnEquipServer(ChunkLoader_Server cl, Item it, ulong code){
		cl.server.SendBattleStyle(code, this.styleCode);
	}
}