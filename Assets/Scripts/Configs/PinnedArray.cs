using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

/*
Exposes a managed array as a NativeArray without copying it.
The array is pinned by a GCHandle, so the GC can't move it while jobs read or write it.
The view is only valid until Release() is called, so it can only be used by jobs that are completed before that
*/
public struct PinnedArray<T> where T : unmanaged{
	public NativeArray<T> array;
	private GCHandle gcHandle;
	private bool pinned;

	public static unsafe PinnedArray<T> Pin(T[] managed){
		PinnedArray<T> output = new PinnedArray<T>();
		output.gcHandle = GCHandle.Alloc(managed, GCHandleType.Pinned);
		void* ptr = (void*)output.gcHandle.AddrOfPinnedObject();

		output.array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, managed.Length, Allocator.None);

		#if ENABLE_UNITY_COLLECTIONS_CHECKS
			NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output.array, AtomicSafetyHandle.Create());
		#endif

		output.pinned = true;
		return output;
	}

	public void Release(){
		if(!this.pinned)
			return;

		#if ENABLE_UNITY_COLLECTIONS_CHECKS
			AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(this.array));
		#endif

		this.gcHandle.Free();
		this.array = default(NativeArray<T>);
		this.pinned = false;
	}
}
