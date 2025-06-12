using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public abstract class EntityRadar{
	// Unity Reference
	protected EntityHandler_Server entityHandler;

	// Identity Information
	protected EntityID ID;

	// Positional Information
	protected Vector3 position;
	protected Vector3 direction;
	protected CastCoord coords;

	// View Information
	protected int FOV; // In degrees
	protected float visionDistance;
	protected ChunkPos minViewCoord;
	protected ChunkPos maxViewCoord;

	// Filter Information
	protected HashSet<EntityType> entitySubscription = new HashSet<EntityType>();
	protected HashSet<ChunkPos> analyzableChunks = new HashSet<ChunkPos>();

	// Cached Information
	private AbstractAI cachedEntity;


	public virtual void Search(ref List<EntityEvent> ieq){
		GetAnalyzableChunks();

		foreach(ChunkPos pos in this.analyzableChunks){
			foreach(EntityType t in this.entitySubscription){
				switch(t){
					case EntityType.PLAYER:
						if(entityHandler.playerObject.ContainsKey(pos)){
							foreach(ulong code in entityHandler.playerObject[pos].Keys){
								this.cachedEntity = entityHandler.playerObject[pos][code];
								AnalyzeEntityDistance(this.cachedEntity, ref ieq);
							}
						}
						break;
					case EntityType.DROP:
						if(entityHandler.dropObject.ContainsKey(pos)){
							foreach(ulong code in entityHandler.dropObject[pos].Keys){
								this.cachedEntity = entityHandler.dropObject[pos][code];
								AnalyzeEntityDistance(this.cachedEntity, ref ieq);
							}
						}
						break;			
				}

			}
		}
	}

	public void SetTransform(ref Vector3 pos, ref Vector3 dir, ref CastCoord coords){
		this.position = pos;
		this.direction = dir;
		this.coords = coords;
	}

	public Vector3 GetPosition(){
		return this.position;
	}

	/**
	 * Checks if the entity found is in viewDistance ignoring walls
	 */
	protected void AnalyzeEntityDistance(AbstractAI ai, ref List<EntityEvent> ieq){
		Vector3 ePos = ai.GetPosition();

		if(IsThisEntity(ai.GetID()))
			return;

		if(!PreAnalysisAI(ai))
			return;
		
		Debug.Log($"Radar: {this.position} -- AI: {ePos} -- Distance: {Vector3.Distance(this.position, ePos)} -- Vision: {this.visionDistance}");

		// Optimization limits
		if(Mathf.Abs(this.position.y - ePos.y) > this.visionDistance)
			return;
		if(Mathf.Abs(this.position.x - ePos.x) > this.visionDistance)
			return;
		if(Mathf.Abs(this.position.z - ePos.z) > this.visionDistance)
			return;

		if(Vector3.Distance(this.position, ePos) <= this.visionDistance){

			if(!PostAnalysisAI(ai))
				return;

			CreateEntityRadarEvent(ai, ref ieq);
		}
	}

	protected virtual bool PreAnalysisAI(AbstractAI ai){
		if(ai.markedForDelete || ai.markedForChange)
			return false;
		return true;
	}

	protected virtual bool PostAnalysisAI(AbstractAI ai){
		return true;
	}

	protected virtual void CreateEntityRadarEvent(AbstractAI ai, ref List<EntityEvent> ieq){
		ieq.Add(new EntityEvent(EntityEventType.VISION, true, new EntityRadarEvent(ai.GetID(), ai.GetPosition(), ai.GetPosition(), ai)));
	}

	protected virtual bool IsThisEntity(EntityID otherID){
		return this.ID.Equals(otherID);
	}

	/**
	Does a rectangular gathering of the available chunks in the region given the position and visionDistance
	Currently only does spherical analysis. Cone analysis still has to be coded in
	*/
	protected virtual void GetAnalyzableChunks(){
		ChunkPos minLimit, maxLimit;
		Vector3 minVector = Vector3.one * -visionDistance;
		Vector3 maxVector = Vector3.one * visionDistance;

		minLimit = new CastCoord(position + minVector).GetChunkPos();
		maxLimit = new CastCoord(position + maxVector).GetChunkPos();

		if(minLimit == this.minViewCoord && maxLimit == this.maxViewCoord)
			return;

		this.analyzableChunks.Clear();

		for(int x = minLimit.x; x <= maxLimit.x; x++){
			for(int z = minLimit.z; z <= maxLimit.z; z++){
				for(int y = minLimit.y; y <= maxLimit.y; y++){
					this.analyzableChunks.Add(new ChunkPos(x, z, y));
				}
			}
		}

		this.minViewCoord = minLimit;
		this.maxViewCoord = maxLimit;
	}


}