using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public abstract class ItemBehaviour {
	// Overloads
	public override string ToString(){return GetType().Name;}


    // Constructor and Deserialization
	public virtual void PostDeserializationSetup(bool isClient){return;}

	// Events
	public virtual void OnHold(ChunkLoader cl, ItemStack its){return;}
	public virtual void OnUseClient(ChunkLoader cl, ItemStack its, Vector3 usagePos, CastCoord targetBlock, CastCoord referencePoint1, CastCoord referencePoint2, CastCoord referencePoint3){return;}
	public virtual void OnUseServer(ChunkLoader_Server cl, ItemStack its, Vector3 usagePos, CastCoord targetBlock, CastCoord referencePoint1, CastCoord referencePoint2, CastCoord referencePoint3){return;}
}