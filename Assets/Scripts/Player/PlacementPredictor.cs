using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

/*
Client-side prediction of the collision of placed blocks.
A placed block only gets its chunk collision after the Server confirms it and the chunk is rebuilt and its collider cooked.
Until then, a box collider takes its place, so a player that places a block beneath themselves while jumping lands on it.
If the player's feet are still inside the placed block when its chunk collider is swapped in, they're nudged on top of it,
since a CharacterController inside a mesh collider has no faces to stand on and falls through the terrain.
*/
public class PlacementPredictor{
	private ChunkLoader cl;
	private GameObject holder;
	private Dictionary<int3, Prediction> predictions = new Dictionary<int3, Prediction>();
	private List<int3> order = new List<int3>();
	private Stack<GameObject> pool = new Stack<GameObject>();

	// Unconfirmed predictions are dropped after this time, in case the Server never answers
	private static readonly float TIMEOUT = 3f;
	private static readonly int GROUND_LAYER = 8;
	private static readonly int UNCONFIRMED = -1;
	// Horizontal overlap needed for the feet to count as inside the placed block, so merely touching its side doesn't nudge the player
	private static readonly float FOOTPRINT_MARGIN = 0.05f;
	// Added to the skin width, so a player already resting on the placed block is never nudged
	private static readonly float RESTING_TOLERANCE = 0.02f;

	private class Prediction{
		public GameObject obj;
		public ChunkPos chunk;
		public CastCoord coord;
		public Bounds cell;
		public float creationTime;
		// Snapshot id from which a chunk build already contains the placed voxel. UNCONFIRMED while the Server hasn't placed it
		public int confirmedSnapshot;
	}

	public PlacementPredictor(ChunkLoader cl){
		this.cl = cl;
	}

	// Called after a PLACEBLOCK is sent
	public void Predict(CastCoord coord, ushort blockCode){
		int3 key = GetKey(coord);

		if(!VoxelLoader.CheckCollidable(blockCode))
			return;
		if(this.predictions.ContainsKey(key))
			return;

		Prediction prediction = new Prediction();
		prediction.obj = GetObject();
		prediction.chunk = coord.GetChunkPos();
		prediction.coord = coord;
		prediction.cell = new Bounds(new Vector3(key.x, key.y, key.z), Vector3.one);
		prediction.creationTime = Time.unscaledTime;
		prediction.confirmedSnapshot = UNCONFIRMED;

		prediction.obj.transform.position = prediction.cell.center;

		// A collider spawned around the player would trap them, so it waits for the player to leave the voxel
		if(!prediction.cell.Intersects(GetPlayerBounds()))
			prediction.obj.SetActive(true);

		this.predictions.Add(key, prediction);
		this.order.Add(key);
	}

	public bool Has(CastCoord coord){return this.predictions.ContainsKey(GetKey(coord));}

	// The Server denied the placement
	public void Deny(ChunkPos pos, int x, int y, int z){
		Remove(GetKey(new CastCoord(pos, x, y, z)));
	}

	// Must run after the finished builds swapped their colliders
	public void Update(){
		Prediction prediction;
		Chunk chunk;
		ushort block;
		int3 key;

		for(int i=this.order.Count-1; i >= 0; i--){
			key = this.order[i];
			prediction = this.predictions[key];

			if(!this.cl.Contains(prediction.chunk)){
				Remove(key);
				continue;
			}

			chunk = this.cl.Get(prediction.chunk);
			block = chunk.data.GetCell(prediction.coord.blockX, prediction.coord.blockY, prediction.coord.blockZ);

			if(!prediction.obj.activeSelf && !prediction.cell.Intersects(GetPlayerBounds()))
				prediction.obj.SetActive(true);

			if(prediction.confirmedSnapshot == UNCONFIRMED){
				if(block != 0)
					prediction.confirmedSnapshot = ChunkBuildSlot.GetNextSnapshotID();
				else if(Time.unscaledTime - prediction.creationTime >= TIMEOUT)
					Remove(key);

				continue;
			}

			// The placed voxel was removed before its collider existed
			if(!VoxelLoader.CheckCollidable(block)){
				Remove(key);
				continue;
			}

			// The chunk collider now contains the placed voxel
			if(chunk.GetColliderSnapshot() >= prediction.confirmedSnapshot){
				if(AreFeetInside(prediction.cell))
					this.cl.playerMovement.PlaceFeetOn(prediction.cell.max.y);

				Remove(key);
			}
		}
	}

	public void Clear(){
		this.predictions.Clear();
		this.order.Clear();
		this.pool.Clear();

		if(this.holder != null)
			GameObject.Destroy(this.holder);

		this.holder = null;
	}

	// Whether the bottom of the player's hitbox is inside the voxel. Only the feet count, since lifting the player above a block
	// at head height would throw them far up
	private bool AreFeetInside(Bounds cell){
		Bounds player = GetPlayerBounds();
		float skin = this.cl.playerMovement.controller.skinWidth;

		if(Mathf.Min(player.max.x, cell.max.x) - Mathf.Max(player.min.x, cell.min.x) <= FOOTPRINT_MARGIN)
			return false;
		if(Mathf.Min(player.max.z, cell.max.z) - Mathf.Max(player.min.z, cell.min.z) <= FOOTPRINT_MARGIN)
			return false;

		return player.min.y >= cell.min.y - Constants.WORLD_BLOCK_GRID_DISPLACEMENT && player.min.y < cell.max.y - (skin + RESTING_TOLERANCE);
	}

	private Bounds GetPlayerBounds(){return this.cl.playerMovement.controller.bounds;}

	private void Remove(int3 key){
		Prediction prediction;

		if(!this.predictions.TryGetValue(key, out prediction))
			return;

		prediction.obj.SetActive(false);
		this.pool.Push(prediction.obj);
		this.predictions.Remove(key);
		this.order.Remove(key);
	}

	private GameObject GetObject(){
		GameObject obj;

		if(this.pool.Count > 0)
			return this.pool.Pop();

		if(this.holder == null)
			this.holder = new GameObject("PlacementPredictions");

		obj = new GameObject("PredictedBlock");
		obj.layer = GROUND_LAYER;
		obj.SetActive(false);
		obj.transform.SetParent(this.holder.transform);
		obj.AddComponent<BoxCollider>();

		return obj;
	}

	// Voxels are centered on integer world coordinates
	private int3 GetKey(CastCoord coord){
		return new int3(coord.GetWorldX(), coord.GetWorldY(), coord.GetWorldZ());
	}
}
