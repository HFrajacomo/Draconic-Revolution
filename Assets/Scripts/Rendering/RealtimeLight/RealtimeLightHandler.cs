using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/*
Client-side budget of realtime HDRP lights.

HDRP silently drops every light past ShaderConfig.FPTLMaxLightCount inside a 16x16 pixel screen tile,
so the total amount of realtime lights in the scene is kept below that value:
	- DYNAMIC_SLOTS are strictly reserved for lights created by players (animation/attachment VFX and items)
	- The rest is a pool of point lights shared by every static light source in the world (torches, lamps, crystals...)

Static light sources reference a Light Profile: the scene path of a template GameObject with a Light
(e.g. "----- PrefabLights -----/Torch"). The template defines color, intensity, range, fade distance and volumetrics.
An optional RealisticLight on the template (may be disabled) makes the light flicker, using its minLightPercentage and steps.

Static lights with the same profile are merged into cells of CELL_SIZE^3 blocks. Each cell is a single light
candidate placed at the centroid of its sources, with the summed intensity of all of them.
Only the closest candidates that can reach the camera frustum receive a real light.

The volumetric dimmer of every light (static profiles and the original dimmer of dynamic lights) is scaled by
AmbientHandler.GetLightVolumetricMultiplier, so lights don't flood the dense fog of rain.
*/
public class RealtimeLightHandler : MonoBehaviour {
	private ChunkLoader cl;

	// Budget
	public static readonly int MAX_REALTIME_LIGHTS = ShaderConfig.FPTLMaxLightCount - 1;
	public static readonly int DYNAMIC_SLOTS = 30;
	public static readonly int STATIC_SLOTS = MAX_REALTIME_LIGHTS - DYNAMIC_SLOTS;

	// Static light merging
	private static readonly int CELL_SIZE = 2;
	private static readonly int CELLS_PER_AXIS = Chunk.chunkWidth / CELL_SIZE;
	private static readonly int CELLS_PER_CHUNK = CELLS_PER_AXIS * CELLS_PER_AXIS * (Chunk.chunkDepth / CELL_SIZE);

	// Selection triggers
	private static readonly float RESELECT_DISTANCE_SQR = 1f;
	private static readonly float RESELECT_ANGLE_DOT = Mathf.Cos(10f * Mathf.Deg2Rad);
	private static readonly float DYNAMIC_RESELECT_INTERVAL = 0.25f;
	private static readonly float INCUMBENT_SCORE_BIAS = 0.7f;

	// Visuals
	private static readonly float FADE_SPEED = 4f;
	private static readonly float REALISTIC_LIGHT_REFERENCE_FPS = 60f;

	// Light Profiles
	private Dictionary<string, int> profileIDs = new Dictionary<string, int>();
	private List<LightProfile> profiles = new List<LightProfile>();

	// Static light data
	private Dictionary<ChunkPos, ChunkLights> chunks = new Dictionary<ChunkPos, ChunkLights>();
	private List<LightCell> staticCandidates = new List<LightCell>();
	private int selectedStaticCount = 0;
	private int selectionStamp = 0;
	private LightSlot[] staticSlots;
	private Stack<int> freeStaticSlots;
	private GameObject poolRoot;

	// Dynamic light data
	private List<DynamicLight> dynamicLights = new List<DynamicLight>();
	private List<DynamicCandidate> dynamicCandidates = new List<DynamicCandidate>();

	// Weather scaling of light volumetrics
	private float volumetricMultiplier = 1f;

	// Selection state
	private Plane[] frustumPlanes = new Plane[6];
	private Vector3 lastCameraPosition;
	private Vector3 lastCameraForward;
	private float dynamicTimer = 0f;
	private bool dirty = true;

	private static readonly LightCellComparer CELL_COMPARER = new LightCellComparer();
	private static readonly DynamicCandidateComparer DYNAMIC_COMPARER = new DynamicCandidateComparer();


	public void SetChunkLoader(ChunkLoader cl){this.cl = cl;}

	void Awake(){
		BuildStaticPool();
	}

	void OnDestroy(){
		if(this.poolRoot != null)
			Destroy(this.poolRoot);
	}

	void Update(){
		if(this.cl == null)
			return;

		Camera cam = GetCamera();

		if(cam == null)
			return;

		Transform camTransform = cam.transform;
		this.dynamicTimer += Time.deltaTime;

		if(this.cl.ambientHandler != null)
			this.volumetricMultiplier = this.cl.ambientHandler.GetLightVolumetricMultiplier();

		UpdateDynamicVolumetrics();

		if(this.dirty ||
			(camTransform.position - this.lastCameraPosition).sqrMagnitude > RESELECT_DISTANCE_SQR ||
			Vector3.Dot(camTransform.forward, this.lastCameraForward) < RESELECT_ANGLE_DOT ||
			(this.dynamicLights.Count > 0 && this.dynamicTimer >= DYNAMIC_RESELECT_INTERVAL)){

			this.dirty = false;
			this.dynamicTimer = 0f;
			this.lastCameraPosition = camTransform.position;
			this.lastCameraForward = camTransform.forward;

			GeometryUtility.CalculateFrustumPlanes(cam, this.frustumPlanes);
			SelectDynamicLights(camTransform.position);
			SelectStaticCells(camTransform.position);
		}

		UpdateStaticSlots();
	}

	// Registers a static light source in a block. A block holds a single light source
	public void AddStaticLight(ChunkPos pos, int x, int y, int z, Vector3 position, string profilePath){
		int profile = GetProfileID(profilePath);

		if(profile < 0)
			return;

		int blockKey = GetBlockKey(x, y, z);
		ChunkLights chunk;
		LightCell cell;

		if(this.chunks.TryGetValue(pos, out chunk) && chunk.blocks.TryGetValue(blockKey, out cell)){
			if(cell.profile == profile){
				cell.positions[cell.blocks.IndexOf(blockKey)] = position;
				cell.Recalculate();
				this.dirty = true;
				return;
			}

			RemoveBlock(pos, chunk, blockKey);
		}

		if(!this.chunks.TryGetValue(pos, out chunk)){
			chunk = new ChunkLights();
			this.chunks.Add(pos, chunk);
		}

		int cellKey = GetCellKey(x, y, z) + profile * CELLS_PER_CHUNK;

		if(!chunk.cells.TryGetValue(cellKey, out cell)){
			cell = new LightCell(cellKey, profile);
			chunk.cells.Add(cellKey, cell);
		}

		cell.blocks.Add(blockKey);
		cell.positions.Add(position);
		cell.Recalculate();
		chunk.blocks.Add(blockKey, cell);
		this.dirty = true;
	}

	// Unregisters the static light source of a block that was broken or turned off
	public void RemoveStaticLight(ChunkPos pos, int x, int y, int z){
		ChunkLights chunk;

		if(this.chunks.TryGetValue(pos, out chunk))
			RemoveBlock(pos, chunk, GetBlockKey(x, y, z));
	}

	// Unregisters every static light source in an unloaded chunk
	public void RemoveChunk(ChunkPos pos){
		ChunkLights chunk;

		if(!this.chunks.TryGetValue(pos, out chunk))
			return;

		foreach(LightCell cell in chunk.cells.Values)
			RemoveCell(cell);

		this.chunks.Remove(pos);
		this.dirty = true;
	}

	// Hands control over a player-created light's enabled state to the handler
	public void RegisterDynamic(Light light){
		if(light == null || IndexOfDynamic(light) >= 0)
			return;

		this.dynamicLights.Add(new DynamicLight(light, light.GetComponent<HDAdditionalLightData>()));
		this.dirty = true;
	}

	// Returns control over a light's enabled state to its owner
	public void UnregisterDynamic(Light light){
		int index = IndexOfDynamic(light);

		if(index < 0)
			return;

		this.dynamicLights[index].RestoreVolumetrics();
		this.dynamicLights.RemoveAt(index);
		this.dirty = true;
	}

	private int IndexOfDynamic(Light light){
		for(int i=0; i < this.dynamicLights.Count; i++){
			if(this.dynamicLights[i].light == light)
				return i;
		}
		return -1;
	}

	private void UpdateDynamicVolumetrics(){
		for(int i=0; i < this.dynamicLights.Count; i++)
			this.dynamicLights[i].ApplyVolumetrics(this.volumetricMultiplier);
	}

	private Camera GetCamera(){
		if(this.cl.playerRaycast != null && this.cl.playerRaycast.playerCamera != null)
			return this.cl.playerRaycast.playerCamera;
		return Camera.main;
	}

	// Profiles are read from their template once and cached. Invalid paths are cached as -1
	private int GetProfileID(string path){
		int id;

		if(path == null)
			return -1;
		if(this.profileIDs.TryGetValue(path, out id))
			return id;

		GameObject template = GameObject.Find(path);
		Light light = template != null ? template.GetComponent<Light>() : null;

		if(light == null){
			Debug.LogError($"[RealtimeLightHandler] Could not find a Light in light profile template: {path}");
			this.profileIDs.Add(path, -1);
			return -1;
		}

		id = this.profiles.Count;
		this.profiles.Add(new LightProfile(light, template.GetComponent<HDAdditionalLightData>(), template.GetComponent<RealisticLight>()));
		this.profileIDs.Add(path, id);
		return id;
	}

	private void BuildStaticPool(){
		this.poolRoot = new GameObject("----- RealtimeLightPool -----");
		this.staticSlots = new LightSlot[STATIC_SLOTS];
		this.freeStaticSlots = new Stack<int>(STATIC_SLOTS);

		for(int i=STATIC_SLOTS-1; i >= 0; i--){
			GameObject go = new GameObject("StaticLight " + i);
			go.transform.SetParent(this.poolRoot.transform, false);

			Light light = go.AddComponent<Light>();
			light.type = LightType.Point;
			light.shadows = LightShadows.None;
			light.intensity = 0f;
			HDAdditionalLightData data = go.AddComponent<HDAdditionalLightData>();
			light.enabled = false;

			this.staticSlots[i] = new LightSlot(light, data, Random.value * 2f);
			this.freeStaticSlots.Push(i);
		}
	}

	private void SelectDynamicLights(Vector3 camPos){
		this.dynamicCandidates.Clear();

		for(int i=this.dynamicLights.Count-1; i >= 0; i--){
			Light light = this.dynamicLights[i].light;

			// Lights destroyed together with their entity
			if(light == null){
				this.dynamicLights.RemoveAt(i);
				continue;
			}

			Vector3 position = light.transform.position;

			if(!IsInFrustum(position, light.range)){
				light.enabled = false;
				continue;
			}

			this.dynamicCandidates.Add(new DynamicCandidate(light, (position - camPos).sqrMagnitude));
		}

		if(this.dynamicCandidates.Count > DYNAMIC_SLOTS)
			this.dynamicCandidates.Sort(DYNAMIC_COMPARER);

		for(int i=0; i < this.dynamicCandidates.Count; i++)
			this.dynamicCandidates[i].light.enabled = i < DYNAMIC_SLOTS;
	}

	private void SelectStaticCells(Vector3 camPos){
		this.staticCandidates.Clear();
		this.selectionStamp++;

		foreach(ChunkLights chunk in this.chunks.Values){
			foreach(LightCell cell in chunk.cells.Values){
				LightProfile profile = this.profiles[cell.profile];
				float range = GetCellRange(cell);
				float maxDistance = profile.fadeDistance + range;
				float distanceSqr = (cell.centroid - camPos).sqrMagnitude;

				if(distanceSqr > maxDistance * maxDistance)
					continue;
				if(!IsInFrustum(cell.centroid, range))
					continue;

				cell.score = cell.slot >= 0 ? distanceSqr * INCUMBENT_SCORE_BIAS : distanceSqr;
				this.staticCandidates.Add(cell);
			}
		}

		if(this.staticCandidates.Count > STATIC_SLOTS)
			this.staticCandidates.Sort(CELL_COMPARER);

		this.selectedStaticCount = Mathf.Min(this.staticCandidates.Count, STATIC_SLOTS);

		for(int i=0; i < this.selectedStaticCount; i++)
			this.staticCandidates[i].selectionStamp = this.selectionStamp;
	}

	private void UpdateStaticSlots(){
		float step = FADE_SPEED * Time.deltaTime;
		LightSlot slot;
		LightCell cell;
		LightProfile profile;

		// Fades lights in and releases the ones that lost their selection
		for(int i=0; i < this.staticSlots.Length; i++){
			slot = this.staticSlots[i];

			if(slot.owner == null)
				continue;

			if(slot.owner.selectionStamp == this.selectionStamp){
				slot.fade = Mathf.Min(1f, slot.fade + step);
			}
			else{
				slot.fade -= step;

				if(slot.fade <= 0f)
					ReleaseSlot(i);
			}
		}

		// Assigns free slots to selected cells that don't have a light yet
		for(int i=0; i < this.selectedStaticCount && this.freeStaticSlots.Count > 0; i++){
			cell = this.staticCandidates[i];

			if(cell.slot >= 0 || cell.removed)
				continue;

			int index = this.freeStaticSlots.Pop();
			slot = this.staticSlots[index];
			slot.owner = cell;
			slot.fade = 0f;
			slot.appliedVersion = -1;

			if(slot.appliedProfile != cell.profile){
				slot.appliedProfile = cell.profile;
				this.profiles[cell.profile].ApplyTo(slot.light, slot.data);
				slot.appliedVolumetric = -1f;
			}

			slot.light.enabled = true;
			cell.slot = index;
		}

		// Applies position, range and flicker
		float time = Time.time;

		for(int i=0; i < this.staticSlots.Length; i++){
			slot = this.staticSlots[i];

			if(slot.owner == null)
				continue;

			if(slot.appliedVersion != slot.owner.version){
				slot.appliedVersion = slot.owner.version;
				slot.transform.position = slot.owner.centroid;
				slot.light.range = GetCellRange(slot.owner);
			}

			profile = this.profiles[slot.owner.profile];
			slot.light.intensity = profile.intensity * slot.owner.blocks.Count * profile.GetFlicker(time, slot.flickerPhase) * slot.fade;

			float volumetric = profile.volumetricDimmer * this.volumetricMultiplier;

			if(slot.appliedVolumetric != volumetric){
				slot.appliedVolumetric = volumetric;
				slot.data.volumetricDimmer = volumetric;
			}
		}
	}

	private void RemoveBlock(ChunkPos pos, ChunkLights chunk, int blockKey){
		LightCell cell;

		if(!chunk.blocks.TryGetValue(blockKey, out cell))
			return;

		int index = cell.blocks.IndexOf(blockKey);
		cell.blocks.RemoveAt(index);
		cell.positions.RemoveAt(index);
		chunk.blocks.Remove(blockKey);

		if(cell.blocks.Count == 0){
			RemoveCell(cell);
			chunk.cells.Remove(cell.key);

			if(chunk.cells.Count == 0)
				this.chunks.Remove(pos);
		}
		else{
			cell.Recalculate();
		}

		this.dirty = true;
	}

	// Removed cells lose their light instantly instead of fading
	private void RemoveCell(LightCell cell){
		cell.removed = true;

		if(cell.slot >= 0)
			ReleaseSlot(cell.slot);
	}

	private void ReleaseSlot(int index){
		LightSlot slot = this.staticSlots[index];

		if(slot.owner != null)
			slot.owner.slot = -1;

		slot.owner = null;
		slot.fade = 0f;
		slot.light.intensity = 0f;
		slot.light.enabled = false;
		this.freeStaticSlots.Push(index);
	}

	private bool IsInFrustum(Vector3 center, float radius){
		for(int i=0; i < this.frustumPlanes.Length; i++){
			if(this.frustumPlanes[i].GetDistanceToPoint(center) < -radius)
				return false;
		}
		return true;
	}

	// Merged lights still reach everything the outermost source of the cell reached
	private float GetCellRange(LightCell cell){return this.profiles[cell.profile].range + cell.spread;}

	private static int GetCellKey(int x, int y, int z){
		return (x / CELL_SIZE) + (z / CELL_SIZE) * CELLS_PER_AXIS + (y / CELL_SIZE) * CELLS_PER_AXIS * CELLS_PER_AXIS;
	}

	private static int GetBlockKey(int x, int y, int z){
		return x + z * Chunk.chunkWidth + y * Chunk.chunkWidth * Chunk.chunkWidth;
	}


	private class LightProfile {
		public Color color;
		public float colorTemperature;
		public bool useColorTemperature;
		public UnityEngine.Rendering.LightUnit lightUnit;
		public float intensity;
		public float range;
		public int cullingMask;
		public int renderingLayerMask;

		public float fadeDistance = 10000f;
		public float volumetricFadeDistance = 10000f;
		public float volumetricDimmer = 1f;
		public float lightDimmer = 1f;
		public float shapeRadius = 0.025f;
		public bool affectDiffuse = true;
		public bool affectSpecular = true;

		public bool flicker = false;
		public float flickerMin = 1f;
		public float flickerHalfPeriod = 1f;

		public LightProfile(Light light, HDAdditionalLightData data, RealisticLight realisticLight){
			this.color = light.color;
			this.colorTemperature = light.colorTemperature;
			this.useColorTemperature = light.useColorTemperature;
			this.lightUnit = light.lightUnit;
			this.intensity = light.intensity;
			this.range = light.range;
			this.cullingMask = light.cullingMask;
			this.renderingLayerMask = light.renderingLayerMask;

			if(data != null){
				this.fadeDistance = data.fadeDistance;
				this.volumetricFadeDistance = data.volumetricFadeDistance;
				this.volumetricDimmer = data.volumetricDimmer;
				this.lightDimmer = data.lightDimmer;
				this.shapeRadius = data.shapeRadius;
				this.affectDiffuse = data.affectDiffuse;
				this.affectSpecular = data.affectSpecular;
			}

			// RealisticLight moves through its range in "steps" frames, so it is converted to seconds
			if(realisticLight != null){
				this.flicker = true;
				this.flickerMin = realisticLight.minLightPercentage;
				this.flickerHalfPeriod = Mathf.Max(0.01f, realisticLight.steps / REALISTIC_LIGHT_REFERENCE_FPS);
			}
		}

		public void ApplyTo(Light light, HDAdditionalLightData data){
			light.color = this.color;
			light.colorTemperature = this.colorTemperature;
			light.useColorTemperature = this.useColorTemperature;
			light.lightUnit = this.lightUnit;
			light.cullingMask = this.cullingMask;
			light.renderingLayerMask = this.renderingLayerMask;

			data.fadeDistance = this.fadeDistance;
			data.volumetricFadeDistance = this.volumetricFadeDistance;
			data.volumetricDimmer = this.volumetricDimmer;
			data.lightDimmer = this.lightDimmer;
			data.shapeRadius = this.shapeRadius;
			data.affectDiffuse = this.affectDiffuse;
			data.affectSpecular = this.affectSpecular;
		}

		public float GetFlicker(float time, float phase){
			if(!this.flicker)
				return 1f;
			return Mathf.Lerp(this.flickerMin, 1f, Mathf.PingPong(time / this.flickerHalfPeriod + phase, 1f));
		}
	}

	private class ChunkLights {
		public Dictionary<int, LightCell> cells = new Dictionary<int, LightCell>();
		public Dictionary<int, LightCell> blocks = new Dictionary<int, LightCell>();
	}

	private class LightCell {
		public int key;
		public int profile;
		public List<int> blocks = new List<int>(CELL_SIZE * CELL_SIZE * CELL_SIZE);
		public List<Vector3> positions = new List<Vector3>(CELL_SIZE * CELL_SIZE * CELL_SIZE);
		public Vector3 centroid;
		public float spread;
		public float score;
		public int slot = -1;
		public int selectionStamp = -1;
		public int version = 0;
		public bool removed = false;

		public LightCell(int key, int profile){
			this.key = key;
			this.profile = profile;
		}

		public void Recalculate(){
			Vector3 sum = Vector3.zero;

			for(int i=0; i < this.positions.Count; i++)
				sum += this.positions[i];

			this.centroid = sum / this.positions.Count;
			this.spread = 0f;

			for(int i=0; i < this.positions.Count; i++)
				this.spread = Mathf.Max(this.spread, Vector3.Distance(this.centroid, this.positions[i]));

			this.version++;
		}
	}

	private class LightSlot {
		public Light light;
		public HDAdditionalLightData data;
		public Transform transform;
		public LightCell owner;
		public float fade;
		public float flickerPhase;
		public int appliedVersion = -1;
		public int appliedProfile = -1;
		public float appliedVolumetric = -1f;

		public LightSlot(Light light, HDAdditionalLightData data, float flickerPhase){
			this.light = light;
			this.data = data;
			this.transform = light.transform;
			this.flickerPhase = flickerPhase;
		}
	}

	// A player-created light and the volumetric dimmer it had before weather scaling
	private class DynamicLight {
		public Light light;
		public HDAdditionalLightData data;
		public float baseVolumetricDimmer;
		public float appliedMultiplier = 1f;

		public DynamicLight(Light light, HDAdditionalLightData data){
			this.light = light;
			this.data = data;
			this.baseVolumetricDimmer = data != null ? data.volumetricDimmer : 1f;
		}

		public void ApplyVolumetrics(float multiplier){
			if(this.data == null || this.appliedMultiplier == multiplier)
				return;

			this.appliedMultiplier = multiplier;
			this.data.volumetricDimmer = this.baseVolumetricDimmer * multiplier;
		}

		public void RestoreVolumetrics(){
			if(this.data != null)
				this.data.volumetricDimmer = this.baseVolumetricDimmer;
		}
	}

	private struct DynamicCandidate {
		public Light light;
		public float score;

		public DynamicCandidate(Light light, float score){
			this.light = light;
			this.score = score;
		}
	}

	private class LightCellComparer : IComparer<LightCell> {
		public int Compare(LightCell a, LightCell b){return a.score.CompareTo(b.score);}
	}

	private class DynamicCandidateComparer : IComparer<DynamicCandidate> {
		public int Compare(DynamicCandidate a, DynamicCandidate b){return a.score.CompareTo(b.score);}
	}
}
