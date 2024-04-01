using System;
using UnityEngine;

[Serializable]
public struct PositionState{
	public ushort state;
	public Vector3 position;

	public override string ToString(){return $"State: {state} -> ({position.x}, {position.y}, {position.z})";}
}