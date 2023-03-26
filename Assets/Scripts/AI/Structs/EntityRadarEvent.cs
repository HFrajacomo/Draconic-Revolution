using UnityEngine;


public struct EntityRadarEvent{
	public EntityID entityID;
	public Vector3 pointerToEntity;
	public Vector3 entityRotation;
	public AbstractAI entity;

	public EntityRadarEvent(EntityID id, Vector3 position, Vector3 rotation, AbstractAI ai){
		this.entityID = id;
		this.pointerToEntity = position;
		this.entityRotation = rotation;
		this.entity = ai;
	}
}