using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
Keeps the player near the Unity origin by shifting the whole client world whenever the player gets REBASE_DISTANCE blocks away from it (see WorldOrigin)
	Every root GameObject of the scene is moved, so chunks, entities, effects and lights keep their relative positions
	World space particles and the Unity positions cached outside of transforms are shifted by hand
*/
public class FloatingOrigin {
	private ChunkLoader cl;
	private List<GameObject> cachedRoots = new List<GameObject>();
	private ParticleSystem.Particle[] cachedParticles = new ParticleSystem.Particle[256];


	public FloatingOrigin(ChunkLoader cl){
		this.cl = cl;
	}

	// Called once the spawn position is received, before any chunk or entity exists, so the player starts near the Unity origin
	public static void SetInitialOrigin(float absoluteX, float absoluteZ){
		WorldOrigin.Set(WorldOrigin.GetOriginFor(absoluteX), WorldOrigin.GetOriginFor(absoluteZ));
	}

	public void Update(){
		Vector3 pos = this.cl.player.position;

		if(Mathf.Abs(pos.x) < WorldOrigin.REBASE_DISTANCE && Mathf.Abs(pos.z) < WorldOrigin.REBASE_DISTANCE)
			return;

		int newX = WorldOrigin.GetOriginFor((double)pos.x + WorldOrigin.GetOffsetX());
		int newZ = WorldOrigin.GetOriginFor((double)pos.z + WorldOrigin.GetOffsetZ());

		Rebase(newX, newZ);
	}

	private void Rebase(int newX, int newZ){
		Vector3 delta = new Vector3(newX - WorldOrigin.GetOffsetX(), 0f, newZ - WorldOrigin.GetOffsetZ());

		if(delta == Vector3.zero)
			return;

		WorldOrigin.Set(newX, newZ);

		ShiftRoots(delta);
		ShiftWorldSpaceParticles(delta);

		this.cl.voxelLightHandler.ShiftOrigin(delta);
		this.cl.realtimeLightHandler.ShiftOrigin(delta);
		this.cl.placementPredictor.ShiftOrigin(delta);

		if(this.cl.client != null)
			this.cl.client.ShiftOrigin(delta);

		// CharacterController and chunk MeshColliders must see the new positions before the next physics query
		Physics.SyncTransforms();
	}

	// UI roots are skipped, since Screen Space canvases are placed by Unity
	private void ShiftRoots(Vector3 delta){
		SceneManager.GetActiveScene().GetRootGameObjects(this.cachedRoots);

		for(int i=0; i < this.cachedRoots.Count; i++){
			if(this.cachedRoots[i].transform is RectTransform)
				continue;

			this.cachedRoots[i].transform.position -= delta;
		}

		this.cachedRoots.Clear();
	}

	// Local and Custom space particles move with their transforms, World space ones keep absolute Unity positions
	private void ShiftWorldSpaceParticles(Vector3 delta){
		ParticleSystem[] systems = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
		int count;

		for(int i=0; i < systems.Length; i++){
			if(systems[i].main.simulationSpace != ParticleSystemSimulationSpace.World)
				continue;

			count = systems[i].particleCount;

			if(count == 0)
				continue;

			if(this.cachedParticles.Length < count)
				this.cachedParticles = new ParticleSystem.Particle[count];

			count = systems[i].GetParticles(this.cachedParticles);

			for(int j=0; j < count; j++)
				this.cachedParticles[j].position -= delta;

			systems[i].SetParticles(this.cachedParticles, count);
		}
	}
}
