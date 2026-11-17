using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;

using Object = UnityEngine.Object;

/*
Client only. Animated Objects (ShaderIndex.ANIMATED_OBJECTS) of a chunk, referenced by their in-chunk position.

Every Animated Object owns a slot (AnimatedSlot) holding its transform, animation and light in a GraphicsBuffer read by AnimatedObjects.hlsl,
so playing animations or changing their state, rotation or light is a buffer write, never a mesh rebuild.
Their geometry is in a mesh of its own, built by AnimatedMeshBuildSlot only when Animated Objects are added or removed.
Changes to the static chunk mesh never rebuild it: static builds only send the light of the visible Animated Objects (ApplyLight).

A removed slot is hidden (all zero transform) and only reused once a mesh built without it is applied, so the mesh and the slots always match
*/
public class ChunkAnimatedObjects{
	private static readonly int INITIAL_CAPACITY = 16;
	private static readonly int SLOTS_ID = Shader.PropertyToID("_AnimatedSlots");
	private static readonly float BOUNDS_MARGIN = 1f;
	// Start time of animations shown on their last frame. Far enough in the past that any animation is over
	public static readonly float FINISHED_START_TIME = -100000f;

	private static bool warnedMissingMaterial = false;

	private Chunk chunk;
	private AnimatedObjectHandler handler;

	// Slots
	private Dictionary<int, int> voxelToSlot = new Dictionary<int, int>();
	private AnimatedSlot[] slots;
	private int[] slotVoxel; // Voxel index of each slot, -1 if free
	private ushort[] slotCode;
	private ushort[] slotState;
	private int slotCount = 0; // Slots ever used. The ones after it were never used
	private Stack<int> freeSlots = new Stack<int>();
	private List<int> releasedSlots = new List<int>(); // Removed after the last build was scheduled
	private List<int> pendingSlots = new List<int>(); // Removed before the running build was scheduled, free once it's applied

	// GPU
	private GraphicsBuffer buffer;
	private Material material; // Instance of the Animated Objects material, so the slot buffer is bound in every pass
	private int dirtyMin = int.MaxValue;
	private int dirtyMax = -1;

	// Rendering
	private GameObject obj;
	private MeshRenderer meshRenderer;
	private Mesh mesh;
	private bool hasMesh = false;
	private bool meshDirty = false;

	// Handler queues
	public bool queuedBuild = false;
	public bool queuedUpload = false;

	private AnimatedMeshBuildSlot buildSlot;
	private bool isDestroyed = false;


	public ChunkAnimatedObjects(Chunk chunk, AnimatedObjectHandler handler){
		this.chunk = chunk;
		this.handler = handler;

		this.slots = new AnimatedSlot[INITIAL_CAPACITY];
		this.slotVoxel = new int[INITIAL_CAPACITY];
		this.slotCode = new ushort[INITIAL_CAPACITY];
		this.slotState = new ushort[INITIAL_CAPACITY];

		for(int i=0; i < INITIAL_CAPACITY; i++)
			this.slotVoxel[i] = -1;

		this.buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, INITIAL_CAPACITY, AnimatedSlot.SIZE);

		// A child of the chunk, so it follows the floating origin with it
		this.obj = new GameObject("AnimatedObjects " + chunk.pos.x + ", " + chunk.pos.z + ", " + ((ChunkDepthID)chunk.pos.y).ToString()[0]);
		this.obj.transform.SetParent(chunk.obj.transform, false);
		this.obj.layer = chunk.obj.layer;

		this.mesh = new Mesh();
		this.mesh.name = this.obj.name;

		MeshFilter filter = this.obj.AddComponent<MeshFilter>();
		filter.sharedMesh = this.mesh;

		Material sharedMaterial = chunk.renderer.GetAnimatedObjectsMaterial();

		// Buffers given through MaterialPropertyBlocks aren't bound in every pass, and DX12 skips draws with unbound buffers
		if(sharedMaterial != null){
			this.material = new Material(sharedMaterial);
			this.material.name = sharedMaterial.name + " " + this.obj.name;
		}

		this.meshRenderer = this.obj.AddComponent<MeshRenderer>();
		this.meshRenderer.sharedMaterial = this.material;
		this.meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
		this.meshRenderer.enabled = false;

		if(this.material == null && !warnedMissingMaterial){
			Debug.LogError($"[ChunkAnimatedObjects] ChunkRenderer has no Animated Objects material (material {(int)ShaderIndex.ANIMATED_OBJECTS} of its MeshRenderer). Animated Objects won't be drawn");
			warnedMissingMaterial = true;
		}

		BindBuffer();
	}

	public static int VoxelIndex(int x, int y, int z){return x*Chunk.chunkWidth*Chunk.chunkDepth + y*Chunk.chunkWidth + z;}

	public bool IsDestroyed(){return this.isDestroyed;}
	public bool IsBuilding(){return this.buildSlot != null;}
	public bool IsMeshDirty(){return this.meshDirty;}
	public int Count(){return this.voxelToSlot.Count;}
	public Mesh GetMesh(){return this.mesh;}
	public void SetBuildSlot(AnimatedMeshBuildSlot slot){this.buildSlot = slot;}
	public bool Contains(int x, int y, int z){return this.voxelToSlot.ContainsKey(VoxelIndex(x, y, z));}

	/*
	Registers the Animated Object of a voxel, or updates it if it's already registered.
	A new state shows the pose of that state (BlocklikeObject.GetAnimationPose), stopping any animation
	*/
	public void Set(int x, int y, int z, ushort code, ushort state, int packedLight){
		int index = VoxelIndex(x, y, z);
		int slot;

		if(this.voxelToSlot.TryGetValue(index, out slot)){
			if(this.slotCode[slot] == code){
				if(this.slotState[slot] != state){
					this.slotState[slot] = state;
					WriteTransform(slot);
					WritePose(slot);
				}

				return;
			}

			Remove(x, y, z);
		}

		slot = AllocateSlot();

		this.voxelToSlot.Add(index, slot);
		this.slotVoxel[slot] = index;
		this.slotCode[slot] = code;
		this.slotState[slot] = state;
		this.slots[slot].animation.w = packedLight;

		WriteTransform(slot);
		WritePose(slot);
		MarkMeshDirty();
	}

	public void Remove(int x, int y, int z){
		int index = VoxelIndex(x, y, z);
		int slot;

		if(!this.voxelToSlot.TryGetValue(index, out slot))
			return;

		this.voxelToSlot.Remove(index);
		this.slotVoxel[slot] = -1;
		this.slots[slot] = new AnimatedSlot();
		this.slots[slot].animation.x = -1;
		this.releasedSlots.Add(slot);

		MarkSlotDirty(slot);
		MarkMeshDirty();
		RefreshVisibility();
	}

	// Removes every Animated Object whose voxel index isn't in voxels
	public void RemoveMissing(NativeList<int> voxels){
		HashSet<int> keep = new HashSet<int>();
		List<int> remove = new List<int>();
		int x, y, z;

		for(int i=0; i < voxels.Length; i++)
			keep.Add(voxels[i]);

		foreach(int index in this.voxelToSlot.Keys){
			if(!keep.Contains(index))
				remove.Add(index);
		}

		foreach(int index in remove){
			GetCoords(index, out x, out y, out z);
			Remove(x, y, z);
		}
	}

	/*
	Plays an animation (index in the model identity of the Object) after the voxel changed to state.
	With continueNormalized, an animation that is still playing hands the normalized time it reached to the new one, which starts from there
	*/
	public void Play(int x, int y, int z, ushort state, int animation, bool continueNormalized){
		int slot;

		if(!this.voxelToSlot.TryGetValue(VoxelIndex(x, y, z), out slot))
			return;

		int globalClip = AnimatedObjectTables.GetGlobalClip(ushort.MaxValue - this.slotCode[slot], animation);
		float start = continueNormalized ? GetPlayingNormalizedTime(slot) : 0f;

		if(this.slotState[slot] != state){
			this.slotState[slot] = state;
			WriteTransform(slot);
		}

		if(globalClip < 0){
			WritePose(slot);
			return;
		}

		this.slots[slot].animation.x = globalClip;
		this.slots[slot].animation.y = Time.time;
		this.slots[slot].animation.z = start;
		MarkSlotDirty(slot);
	}

	// Light of the Animated Objects seen by a static build. x = voxel index, y = packed light
	public void ApplyLight(NativeList<int2> lights){
		int slot;

		for(int i=0; i < lights.Length; i++){
			if(!this.voxelToSlot.TryGetValue(lights[i].x, out slot))
				continue;
			if(this.slots[slot].animation.w == lights[i].y)
				continue;

			this.slots[slot].animation.w = lights[i].y;
			MarkSlotDirty(slot);
		}

		RefreshVisibility();
	}

	// Fills the input of a mesh build with every registered Object and its bounds. Slots removed until now are only reused once the build is applied
	public void PrepareBuild(NativeList<int2> objects, out Bounds bounds){
		Vector3 min = Vector3.zero;
		Vector3 max = Vector3.zero;
		Vector3 center, extent;
		BlocklikeObject blo;
		float size;
		int objIndex;

		objects.Clear();

		for(int slot=0; slot < this.slotCount; slot++){
			if(this.slotVoxel[slot] < 0)
				continue;

			objIndex = ushort.MaxValue - this.slotCode[slot];
			blo = VoxelLoader.GetObject(this.slotCode[slot]);
			objects.Add(new int2(slot, objIndex));

			// State changes may move the Object by its offset vectors after the mesh is built, which the margin covers
			center = new Vector3(this.slots[slot].row0.w, this.slots[slot].row1.w, this.slots[slot].row2.w);
			size = AnimatedObjectTables.GetRadius(objIndex) * Mathf.Max(Mathf.Abs(blo.scaling.x), Mathf.Max(Mathf.Abs(blo.scaling.y), Mathf.Abs(blo.scaling.z))) + BOUNDS_MARGIN;
			extent = new Vector3(size, size, size);

			if(objects.Length == 1){
				min = center - extent;
				max = center + extent;
			}
			else{
				min = Vector3.Min(min, center - extent);
				max = Vector3.Max(max, center + extent);
			}
		}

		bounds = new Bounds((min + max) * 0.5f, max - min);

		this.pendingSlots.AddRange(this.releasedSlots);
		this.releasedSlots.Clear();
		this.meshDirty = false;
	}

	// Called by the build slot once the mesh was applied
	public void OnMeshApplied(){
		foreach(int slot in this.pendingSlots)
			this.freeSlots.Push(slot);

		this.pendingSlots.Clear();
		this.hasMesh = true;

		if(this.meshDirty)
			this.handler.RequestBuild(this);

		RefreshVisibility();
	}

	// Called by the build slot when its build was cancelled. Its removed slots wait for the next build
	public void OnBuildAborted(){
		this.releasedSlots.AddRange(this.pendingSlots);
		this.pendingSlots.Clear();

		if(this.isDestroyed)
			return;

		MarkMeshDirty();
	}

	// Sends the changed slots to the GPU
	public void Upload(){
		if(this.dirtyMax < 0)
			return;

		this.buffer.SetData(this.slots, this.dirtyMin, this.dirtyMin, this.dirtyMax - this.dirtyMin + 1);
		this.dirtyMin = int.MaxValue;
		this.dirtyMax = -1;
	}

	// Only drawn after the chunk itself, so Objects never show up before the terrain around them
	public void RefreshVisibility(){
		if(this.isDestroyed)
			return;

		bool visible = this.hasMesh && this.voxelToSlot.Count > 0 && this.chunk.HasRenderMesh() && this.material != null;

		if(this.meshRenderer.enabled != visible)
			this.meshRenderer.enabled = visible;
	}

	public void Destroy(){
		if(this.isDestroyed)
			return;

		this.isDestroyed = true;

		if(this.buildSlot != null)
			this.buildSlot.Abort();

		this.buffer.Release();
		this.buffer = null;

		Object.Destroy(this.mesh);
		Object.Destroy(this.obj);

		if(this.material != null)
			Object.Destroy(this.material);

		this.material = null;
		this.mesh = null;
		this.obj = null;
		this.meshRenderer = null;
		this.chunk = null;
		this.handler = null;
	}

	private int AllocateSlot(){
		if(this.freeSlots.Count > 0)
			return this.freeSlots.Pop();

		if(this.slotCount == this.slots.Length)
			Grow();

		return this.slotCount++;
	}

	// Doubles the slot capacity. The new buffer receives every used slot
	private void Grow(){
		int oldCapacity = this.slots.Length;
		int capacity = oldCapacity * 2;

		Array.Resize(ref this.slots, capacity);
		Array.Resize(ref this.slotVoxel, capacity);
		Array.Resize(ref this.slotCode, capacity);
		Array.Resize(ref this.slotState, capacity);

		for(int i=oldCapacity; i < capacity; i++)
			this.slotVoxel[i] = -1;

		this.buffer.Release();
		this.buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity, AnimatedSlot.SIZE);
		BindBuffer();

		if(this.slotCount > 0){
			this.dirtyMin = 0;
			this.dirtyMax = Mathf.Max(this.dirtyMax, this.slotCount-1);
			this.handler.RequestUpload(this);
		}
	}

	private void BindBuffer(){
		if(this.material == null)
			return;

		this.material.SetBuffer(SLOTS_ID, this.buffer);
		AnimatedObjectTables.BindTo(this.material);
	}

	// Same placement as Objects of the chunk mesh (PrepareAssetsJob): scaling, then the rotation and offset of the state, then the voxel position
	private void WriteTransform(int slot){
		BlocklikeObject blo = VoxelLoader.GetObject(this.slotCode[slot]);
		ushort state = this.slotState[slot];
		Vector3 offset = Vector3.zero;
		int2 rotation = new int2(0, 0);
		int x, y, z;

		GetCoords(this.slotVoxel[slot], out x, out y, out z);

		if(blo.needsRotation && state < blo.maximumRotationScaleState){
			offset = blo.GetOffsetVector(state);
			rotation = blo.GetRotationValue(state);
		}

		Vector3 position = new Vector3(x, y + this.chunk.pos.y*Chunk.chunkDepth, z) + offset;
		Vector3 c0 = ObjectTransformMath.Rotate(new Vector3(blo.scaling.x, 0f, 0f), rotation);
		Vector3 c1 = ObjectTransformMath.Rotate(new Vector3(0f, blo.scaling.y, 0f), rotation);
		Vector3 c2 = ObjectTransformMath.Rotate(new Vector3(0f, 0f, blo.scaling.z), rotation);

		this.slots[slot].row0 = new Vector4(c0.x, c1.x, c2.x, position.x);
		this.slots[slot].row1 = new Vector4(c0.y, c1.y, c2.y, position.y);
		this.slots[slot].row2 = new Vector4(c0.z, c1.z, c2.z, position.z);
		MarkSlotDirty(slot);
	}

	// Shows the pose of the slot's state: the last frame of its animation, or the rest pose. Looping animations keep playing, each voxel at its own phase
	private void WritePose(int slot){
		BlocklikeObject blo = VoxelLoader.GetObject(this.slotCode[slot]);
		int globalClip = AnimatedObjectTables.GetGlobalClip(ushort.MaxValue - this.slotCode[slot], blo.GetAnimationPose(this.slotState[slot]));

		this.slots[slot].animation.x = globalClip;
		this.slots[slot].animation.z = 0f;

		if(globalClip >= 0 && AnimatedObjectTables.IsClipLooping(globalClip))
			this.slots[slot].animation.y = Time.time - GetPhase(slot) * AnimatedObjectTables.GetClipLength(globalClip);
		else
			this.slots[slot].animation.y = FINISHED_START_TIME;

		MarkSlotDirty(slot);
	}

	// Normalized time reached by the animation of a slot, or 0 if it has none or it's over
	private float GetPlayingNormalizedTime(int slot){
		int globalClip = (int)this.slots[slot].animation.x;

		if(globalClip < 0)
			return 0f;

		float normalized = (Time.time - this.slots[slot].animation.y) / AnimatedObjectTables.GetClipLength(globalClip) + this.slots[slot].animation.z;

		if(AnimatedObjectTables.IsClipLooping(globalClip))
			return normalized - Mathf.Floor(normalized);
		if(normalized >= 1f)
			return 0f;

		return Mathf.Max(0f, normalized);
	}

	// Stable phase in [0, 1) from the world position of the voxel
	private float GetPhase(int slot){
		int x, y, z;

		GetCoords(this.slotVoxel[slot], out x, out y, out z);

		uint hash = math.hash(new int3(this.chunk.pos.x*Chunk.chunkWidth + x, this.chunk.pos.y*Chunk.chunkDepth + y, this.chunk.pos.z*Chunk.chunkWidth + z));

		return (hash & 0xFFFF) / 65536f;
	}

	private void MarkSlotDirty(int slot){
		this.dirtyMin = Mathf.Min(this.dirtyMin, slot);
		this.dirtyMax = Mathf.Max(this.dirtyMax, slot);
		this.handler.RequestUpload(this);
	}

	private void MarkMeshDirty(){
		this.meshDirty = true;
		this.handler.RequestBuild(this);
	}

	private static void GetCoords(int index, out int x, out int y, out int z){
		x = index / (Chunk.chunkWidth*Chunk.chunkDepth);
		y = (index / Chunk.chunkWidth) % Chunk.chunkDepth;
		z = index % Chunk.chunkWidth;
	}
}
