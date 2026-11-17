using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

/*
Cooks MeshCollider data in worker threads, so assigning the meshes to their MeshColliders doesn't cook them in the main thread.
The cooking options must match the ones of the MeshColliders, otherwise the meshes are cooked again on assignment
*/
public struct BakeColliderJob : IJobParallelFor{
	[ReadOnly]
	public NativeArray<int> meshIDs;
	[ReadOnly]
	public MeshColliderCookingOptions cookingOptions;

	public void Execute(int index){
		if(meshIDs[index] != 0)
			Physics.BakeMesh(meshIDs[index], false, cookingOptions);
	}
}
