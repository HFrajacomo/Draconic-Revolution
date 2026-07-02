using System;
using UnityEngine;

[Serializable]
public class TestPrintBehaviour : AnimationBehaviour {
	public override void Run(ChunkLoader cl, GameObject animatorParent, AnimationHandler animationHandler, ulong entityID, bool isPlayer){
		Debug.Log("Hello World");
	}
}