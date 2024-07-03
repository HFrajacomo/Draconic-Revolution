using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public struct RotationState{
	public ushort state;
	public int2 rotation;
}