using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class ConfigurablePositionBehaviour : VoxelBehaviour{
	public PositionState[] states;
	private Dictionary<ushort, Vector3> positionStates;

	public override void PostDeserializationSetup(bool isClient){
		if(isClient){
			this.positionStates = new Dictionary<ushort, Vector3>();

			foreach(PositionState r in this.states){
				this.positionStates[r.state] = r.position;
			}
		}
	}

	public override Vector3 GetOffsetVector(ushort state){
		if(this.positionStates.ContainsKey(state)){
			return this.positionStates[state];
		}
		return new Vector3(0,0,0);
	}
}