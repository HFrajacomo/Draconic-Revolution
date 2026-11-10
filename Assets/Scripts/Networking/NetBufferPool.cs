using System.Collections.Concurrent;
using System.Threading;

/*
Thread-safe pool of byte arrays used for received network frames
	Arrays are grouped in power of two sizes, so a rented array is usually bigger than asked for
	Arrays that aren't given back are simply collected by the GC
*/
public static class NetBufferPool
{
	private static readonly int MIN_BUCKET = 6; // 64 bytes
	private static readonly int MAX_BUCKET = 20; // 1MB
	private static readonly int MAX_ARRAYS_PER_BUCKET = 64;

	private static ConcurrentQueue<byte[]>[] buckets = CreateBuckets();
	private static int[] counts = new int[MAX_BUCKET+1];


	public static byte[] Rent(int size){
		int bucket = GetBucket(size);
		byte[] array;

		// Too big to be pooled
		if(bucket > MAX_BUCKET)
			return new byte[size];

		if(buckets[bucket].TryDequeue(out array)){
			Interlocked.Decrement(ref counts[bucket]);
			return array;
		}

		return new byte[1 << bucket];
	}

	public static void Return(byte[] array){
		if(array == null)
			return;

		int bucket = GetBucket(array.Length);

		if(bucket > MAX_BUCKET || (1 << bucket) != array.Length)
			return;

		if(Interlocked.Increment(ref counts[bucket]) > MAX_ARRAYS_PER_BUCKET){
			Interlocked.Decrement(ref counts[bucket]);
			return;
		}

		buckets[bucket].Enqueue(array);
	}

	private static int GetBucket(int size){
		int bucket = MIN_BUCKET;

		while((1 << bucket) < size && bucket <= MAX_BUCKET)
			bucket++;

		return bucket;
	}

	private static ConcurrentQueue<byte[]>[] CreateBuckets(){
		ConcurrentQueue<byte[]>[] output = new ConcurrentQueue<byte[]>[MAX_BUCKET+1];

		for(int i=0; i < output.Length; i++)
			output[i] = new ConcurrentQueue<byte[]>();

		return output;
	}
}
