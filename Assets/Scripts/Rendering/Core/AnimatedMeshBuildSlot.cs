using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/*
Persistent buffers and state of one Animated Objects mesh build (see ChunkAnimatedObjects):
	Schedule (main thread) -> BuildAnimatedMeshJob -> Apply (main thread)
Builds are independent from the chunk build pipeline (ChunkBuildSlot), so static chunk changes never rebuild Animated Objects
*/
public class AnimatedMeshBuildSlot{
	#if UNITY_EDITOR
		private static readonly MeshUpdateFlags MESH_FLAGS = MeshUpdateFlags.DontRecalculateBounds;
	#else
		private static readonly MeshUpdateFlags MESH_FLAGS = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices;
	#endif

	private NativeList<int2> objects;
	private Mesh.MeshDataArray meshDataArray;
	private bool hasMeshData = false;
	private JobHandle handle;
	private ChunkAnimatedObjects target;
	private Bounds bounds;
	private bool isDisposed = false;


	public AnimatedMeshBuildSlot(){
		this.objects = new NativeList<int2>(Allocator.Persistent);
	}

	public bool IsFree(){return this.target == null;}
	public bool IsDone(){return this.target != null && this.handle.IsCompleted;}

	public void Schedule(ChunkAnimatedObjects target){
		this.target = target;
		target.PrepareBuild(this.objects, out this.bounds);

		this.meshDataArray = Mesh.AllocateWritableMeshData(1);
		this.hasMeshData = true;

		BuildAnimatedMeshJob job = new BuildAnimatedMeshJob{
			meshes = this.meshDataArray,
			flags = MESH_FLAGS,
			layout = ChunkMeshLayout.animated,
			meshBounds = this.bounds,
			objects = this.objects,

			loadedVerts = ObjectMeshTables.verts,
			loadedUV = ObjectMeshTables.uvs,
			loadedNormals = ObjectMeshTables.normals,
			loadedTangents = ObjectMeshTables.tangents,
			loadedParts = ObjectMeshTables.partIndex,
			loadedTris = ObjectMeshTables.tris,
			vertsOffset = ObjectMeshTables.vertOffset,
			trisOffset = ObjectMeshTables.trisOffset
		};

		this.handle = job.Schedule();
		target.SetBuildSlot(this);
	}

	// Must only be called when IsDone()
	public void Apply(){
		this.handle.Complete();

		ChunkAnimatedObjects applied = this.target;
		Mesh mesh = applied.GetMesh();

		Mesh.ApplyAndDisposeWritableMeshData(this.meshDataArray, mesh, MESH_FLAGS);
		this.hasMeshData = false;
		mesh.bounds = this.bounds;

		Release();
		applied.OnMeshApplied();
	}

	// Cancels the current build, waiting for its job to finish
	public void Abort(){
		if(this.target == null)
			return;

		this.handle.Complete();

		if(this.hasMeshData){
			this.meshDataArray.Dispose();
			this.hasMeshData = false;
		}

		ChunkAnimatedObjects aborted = this.target;
		Release();
		aborted.OnBuildAborted();
	}

	public void Dispose(){
		if(this.isDisposed)
			return;

		Abort();
		this.objects.Dispose();
		this.isDisposed = true;
	}

	private void Release(){
		this.target.SetBuildSlot(null);
		this.target = null;
	}
}
