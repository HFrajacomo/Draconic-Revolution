using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public abstract class ItemBehaviour{
	// Overloads
	public override string ToString(){return GetType().Name;}


    // Constructor and Deserialization
	public virtual void PostDeserializationSetup(bool isClient){return;}

	// Events
	public virtual void OnHoldPlayer(ChunkLoader cl, ItemStack its, ulong code){return;}
	public virtual void OnHoldClient(ChunkLoader cl, ItemStack its, ulong code){return;}
	public virtual void OnHoldServer(ChunkLoader_Server cl, ItemStack its, ulong code){return;}
	public virtual void OnUnholdPlayer(ChunkLoader cl, ItemStack its, ulong code){return;}
	public virtual void OnUnholdClient(ChunkLoader cl, ItemStack its, ulong code){return;}
	public virtual void OnUnholdServer(ChunkLoader_Server cl, ItemStack its, ulong code){return;}
	public virtual void OnUseClient(ChunkLoader cl, ItemStack its, Vector3 usagePos, CastCoord targetBlock, CastCoord referencePoint1, CastCoord referencePoint2, CastCoord referencePoint3){return;}
	public virtual void OnUseServer(ChunkLoader_Server cl, ItemStack its, Vector3 usagePos, CastCoord targetBlock, CastCoord referencePoint1, CastCoord referencePoint2, CastCoord referencePoint3){return;}
}