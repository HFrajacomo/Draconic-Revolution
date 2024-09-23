using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VoxelLightHandler: MonoBehaviour {
	private List<Vector4> positions;
	private Dictionary<EntityID, int> entitiesMap;
	private HashSet<EntityID> entities;
	private Vector4[] cachedPositions;

	private bool dirty = false;
	private static Vector4 cachedVector;
	private static readonly Vector4 STOP_ELEMENT = new Vector4(0,-1000, 0, 0);


	void Awake(){
		this.positions = new List<Vector4>();
		this.cachedPositions = new Vector4[ShaderLoader.GetVoxelLightBufferSize()];
		this.entitiesMap = new Dictionary<EntityID, int>();
		this.entities = new HashSet<EntityID>();

		BuildAndSendBuffer();
	}

	void Update(){
		if(!this.dirty)
			return;

		this.dirty = false;
		BuildAndSendBuffer();
	}

	public void Add(EntityID id, Vector3 pos, float lightRadius, bool priority=false){
		// If ID is already registered in Buffer
		if(this.entitiesMap.ContainsKey(id)){
			cachedVector = new Vector4(pos.x, pos.z, pos.z, lightRadius);

			if(this.positions[this.entitiesMap[id]] == cachedVector)
				return;

			this.positions[this.entitiesMap[id]] = new Vector4(pos.x, pos.y, pos.z, lightRadius);
		}
		// If is a new ID
		else{
			// Non-Priority handling
			if(!priority){
				if(this.positions.Count >= ShaderLoader.GetVoxelLightBufferSize()){
					return;
				}
				else{
					this.positions.Add(new Vector4(pos.x, pos.y, pos.z, lightRadius));
					this.entitiesMap.Add(id, this.positions.Count-1);
					this.entities.Add(id);
				}
			}
			else{
				MoveEntityMap(true, 0);
				this.positions.Insert(0, new Vector4(pos.x, pos.y, pos.z, lightRadius));
				this.entitiesMap.Add(id, 0);
				this.entities.Add(id);
			}
		}

		this.dirty = true;
	}

	public void Remove(EntityID id){
		if(!this.entitiesMap.ContainsKey(id))
			return;

		int index = this.entitiesMap[id];
		this.positions.RemoveAt(index);
		this.entitiesMap.Remove(id);
		this.entities.Remove(id);
		MoveEntityMap(false, index);

		this.dirty = true;
	}

	private void MoveEntityMap(bool forward, int fromPos){
		if(forward){
			foreach(EntityID id in this.entities){
				if(this.entitiesMap[id] > fromPos){
					this.entitiesMap[id] += 1;
				}
			}
		}
		else{
			foreach(EntityID id in this.entities){
				if(this.entitiesMap[id] > fromPos){
					this.entitiesMap[id] -= 1;
				}
			}
		}
	}

	private void BuildAndSendBuffer(){
		for(int i=0; i < this.positions.Count; i++){
			this.cachedPositions[i] = this.positions[i];
		}

		for(int i=this.positions.Count; i < ShaderLoader.GetVoxelLightBufferSize(); i++){
			if(i == this.positions.Count)
				this.cachedPositions[i] = STOP_ELEMENT;
			else
				this.cachedPositions[i] = new Vector4(0,0,0,0);
		}

		Shader.SetGlobalVectorArray("_VOXEL_LIGHT_BUFFER", this.cachedPositions);
	}
}