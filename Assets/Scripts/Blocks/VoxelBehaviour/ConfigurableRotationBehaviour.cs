using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class ConfigurableRotationBehaviour : VoxelBehaviour{
	public RotationState[] states;
	private Dictionary<ushort, int2> rotationStates;

	public override void PostDeserializationSetup(bool isClient){
		if(isClient){
			this.rotationStates = new Dictionary<ushort, int2>();

			foreach(RotationState r in this.states){
				this.rotationStates[r.state] = r.rotation;
			}
		}
	}

	public override int2 GetRotationValue(ushort state){
		if(this.rotationStates.ContainsKey(state)){
			return this.rotationStates[state];
		}
		return new int2(0,0);
	}
}