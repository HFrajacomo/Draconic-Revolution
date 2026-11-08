using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

/*
Exposes a managed array as a NativeArray without copying it.
The view is only valid until Release() is called, so it can only be used by jobs that are completed before that
*/
public struct PinnedArray<T> where T : unmanaged{
	public NativeArray<T> array;
	private ulong gcHandle;
	private bool pinned;

	public static unsafe PinnedArray<T> Pin(T[] managed){
		PinnedArray<T> output = new PinnedArray<T>();
		void* ptr = UnsafeUtility.PinGCArrayAndGetDataAddress(managed, out output.gcHandle);

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

		UnsafeUtility.ReleaseGCObject(this.gcHandle);
		this.array = default(NativeArray<T>);
		this.pinned = false;
	}
}
