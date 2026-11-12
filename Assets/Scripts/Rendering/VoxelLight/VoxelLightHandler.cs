using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/*
Holds every moving light (position + radius) and sends them to the shader buffer
	Priority lights are sent first (newest first), then normal lights in insertion order, up to the buffer size
	Lights that don't fit stay registered and are sent once there is room
*/
public class VoxelLightHandler: MonoBehaviour {
	private Dictionary<EntityID, Vector4> lights;
	private List<EntityID> priorityOrder;
	private List<EntityID> normalOrder;
	private Vector4[] cachedPositions;

	private bool dirty = false;
	private static readonly Vector4 STOP_ELEMENT = new Vector4(0,-1000, 0, 0);


	void Awake(){
		this.lights = new Dictionary<EntityID, Vector4>();
		this.priorityOrder = new List<EntityID>();
		this.normalOrder = new List<EntityID>();
		this.cachedPositions = new Vector4[ShaderLoader.GetVoxelLightBufferSize()];

		BuildAndSendBuffer();
	}

	void Update(){
		if(!this.dirty)
			return;

		this.dirty = false;
		BuildAndSendBuffer();
	}

	public void Add(EntityID id, Vector3 pos, float lightRadius, bool priority=false){
		Vector4 light = new Vector4(pos.x, pos.y, pos.z, lightRadius);
		Vector4 current;

		// If ID is already registered
		if(this.lights.TryGetValue(id, out current)){
			// Promotes a normal light that is re-added as priority
			if(priority && this.normalOrder.Remove(id)){
				this.priorityOrder.Insert(0, id);
				this.dirty = true;
			}

			if(current == light)
				return;

			this.lights[id] = light;
		}
		// If is a new ID
		else{
			this.lights.Add(id, light);

			if(priority)
				this.priorityOrder.Insert(0, id);
			else
				this.normalOrder.Add(id);
		}

		this.dirty = true;
	}

	public void UpdatePosition(EntityID id, Vector3 pos){
		Vector4 current;

		if(!this.lights.TryGetValue(id, out current))
			return;

		Vector4 light = new Vector4(pos.x, pos.y, pos.z, current.w);

		if(current == light)
			return;

		this.lights[id] = light;
		this.dirty = true;
	}

	public bool Contains(EntityID id){return this.lights.ContainsKey(id);}

	public void Remove(EntityID id){
		if(!this.lights.Remove(id))
			return;

		if(!this.priorityOrder.Remove(id))
			this.normalOrder.Remove(id);

		this.dirty = true;
	}

	private void BuildAndSendBuffer(){
		int bufferSize = ShaderLoader.GetVoxelLightBufferSize();
		int count = 0;

		for(int i=0; i < this.priorityOrder.Count && count < bufferSize; i++){
			this.cachedPositions[count] = this.lights[this.priorityOrder[i]];
			count++;
		}

		for(int i=0; i < this.normalOrder.Count && count < bufferSize; i++){
			this.cachedPositions[count] = this.lights[this.normalOrder[i]];
			count++;
		}

		for(int i=count; i < bufferSize; i++){
			if(i == count)
				this.cachedPositions[i] = STOP_ELEMENT;
			else
				this.cachedPositions[i] = new Vector4(0,0,0,0);
		}

		Shader.SetGlobalVectorArray("_VOXEL_LIGHT_BUFFER", this.cachedPositions);
	}
}