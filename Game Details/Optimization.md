# Optimization

Making a game is easy. Making a good game is harder. Making a good and fast game, is very hard. Go get your popcorn and let's go on an Optimization adventure!

## Chunk Loading Optimization (CPU)

In a Procedurally Generated game, loading and unloading chunks is key. Chunks are **streamed** by the Server:

- Every client keeps a **StreamWindow**, a square of chunks around the player with a radius of Render Distance + 1, plus one vertical layer when the player is close to the top or bottom of a chunk. The client sends its center to the Server (`STREAMCENTER`) whenever it changes, and the Server pushes every chunk inside it, nearest first. Client and Server compute the window with the same code (`ChunkStreaming`), so both always agree on which chunks should be loaded.
- When the window moves, only the strips of chunks that entered or left it are computed (rectangle difference). Moving one chunk costs O(Render Distance) instead of O(Render Distance²), so large Render Distances stay cheap. Both sides unload the chunks that left the window on their own, without extra messages.
- The client acknowledges consumed chunks (`CHUNKACK`). The Server never has more unacknowledged chunks than an adaptive send window allows, so fast clients fill their window quickly and slow clients aren't flooded.
- The Server's generation queue, the client's load, unload and draw steps all run under a per-frame **time budget** (see `Constants`) instead of a fixed amount of chunks. Larger windows take more frames to fill, but frames never get longer.
- Chunks are only queued to be drawn once all their neighbors are loaded, instead of being retried every frame.
- If the stream goes idle while chunks of the window are still missing (lost packets), the client scans its window a few chunks per frame and asks the Server for the missing ones (`REQUESTCHUNKLOAD`).

## Interpolative Generation (CPU)

As mentioned in the *World_Generation.md*, generating the whole heightmap of a Chunk everytime is a costly operation. In order to save CPU, we sample pivot points of the Chunk only, and then use those pivot points to guess the in-between height values.

## Class Instantiations (CPU and RAM)

Creating objects is fairly slow. Classes are slow. You can't have a *Block* class that is instantiated every single time a block is inserted into a Chunk. That would be roughly 25600 instantiations per chunk. In order to keep block-type information, we generate those classes as static classes and store them separatedly.
This saves a huge amount of processing, but increases the loading time.

## Texture Atlas (CPU and GPU)

It's no secret that using a Texture Atlas is a big boost in CPU-to-GPU optimization. A texture atlas is a big texture image that can be broken down into smaller (and the actual game textures) that can be loaded separatedly. Attributing this atlas to an Unity Material let's you render all block textures using the same Material, thus, saving a whole bunch of data preparation in the CPU and processing on the GPU.

## Data Compression Algorithm (CPU and Disk)

As mentioned in the *World_Save.md*, our compression algorithm is a Pallete-based Compression. Initially, the World would fully save every single block data and metadata. That would constantly lead to Region Files having >200MB. Not only it pollutes your Hard Drive, but it also makes reading those files a pain to the CPU. The compression algorithm solves both problems. It makes files smaller and read times are faster!

## Garbage Collector (GC) Optimization (CPU and RAM)

Interestingly enough, the Garbage Collection optimization is meant to decrease the number of *GC.Collect()* calls. In order to do that, we have to keep garbage generation to a minimum. Thus, we use cached information. If we commonly use a certain list or array, save them as part of the class!

When working with Native Data Structures, disposing of them correctly and accessing their data as non-Native structures without burdening the GC is a good thing.

## Persistent Native Buffers and Zero-Copy Meshes (CPU and RAM)

The hot paths of the Chunk Renderer and Voxel Lighting don't allocate per call:

- Every chunk build reuses the persistent buffers of a `ChunkBuildSlot`, and only copies a padded snapshot of the chunk plus a 1-voxel border of its neighbors, instead of the full data of every neighbor.
- Meshes are written by Burst jobs directly into `Mesh.MeshDataArray` and applied with `Mesh.ApplyAndDisposeWritableMeshData`. No managed vertex arrays are created and every chunk reuses the same Mesh objects.
- MeshColliders are cooked in worker threads with `Physics.BakeMesh` before they are assigned, so the main thread doesn't cook them.
- Object meshes are concatenated once into `ObjectMeshTables` and shared, read-only, by every build.
- The light of loaded chunks is calculated in worker threads, nearest to the player first, and each lighting slot keeps its own persistent buffers (see *Voxel_Lighting.md*). Light propagation and chunk updates run in the main thread, using the persistent buffers of `LightingScratch`. BFS queues are index-based FIFOs and visited voxels are tracked in bit arrays, keeping the BFS linear.
- Visible faces of a chunk are found in parallel slices, so building a single chunk (like after a block change) takes a fraction of the time.
- The render mesh stores normals and tangents as signed bytes and UVs as half floats, using 36 bytes per vertex instead of 64. Colliders are greedy meshed, merging flat surfaces into few large triangles.

## Uniform Metadata (RAM)

Most chunks have the same HP in every voxel, and many have the same State in every voxel too. `VoxelMetadata` keeps each of them as a single value while that's true, and only allocates the full array (128 KB) when a voxel gets a different value. Full arrays come from the `MetadataPool` and go back to it when the chunk is unloaded. Decompression detects uniform data, and the Chunk Builder, Voxel Lighting and Compression read uniform metadata without allocating its array.

## Parallel World Generation (CPU)

The Server generates chunks in batches of `SERVER_GENERATION_BATCH`, each in its own `GenerationSlot` of persistent native buffers. Biomes are assigned (and cached) before the jobs are scheduled, so each chunk runs its Generate, Populate and Cave jobs as a single chain, and the chains of a batch run in parallel. Structures can reach neighboring chunks, so chunks are finished one at a time, in the order they were requested, which gives the same result as generating them one by one.

Chunk compression and decompression run their blocks, HP and State in parallel, straight from and into the chunk arrays. Chunk saves are written to disk in the background and chunk reads happen in a background thread (see *World_Save.md*).

Biomes are stored as byte codes, so saving, sending and building chunks never compares biome names.

## Incremental Garbage Collector (Incremental GC) (CPU)

It's very hard to avoid lag spikes using the standard GC. These lag spikes were constant when playing in high render distances. The Incremental GC makes everything simpler. It distributes the memory flagging stage to multiple frames, making the GC load less noticeable, and lag spikes rare.+

# Burst Compiler

Okay, now this is the actual big boy. 
The Burst Compiler is a LLVM based compiler that pre-compiles Unity Job functions into extremely fast assembly code.
 **Note:** if you are a new programmer eager to use Burst, let me tell you something really important: **IT IS VERY HARD TO WRITE CODE FOR BURST**. Although every section of code compiled by Burst gets roughly a 20000% speed increase, writting this code can be very stressful, since you are very limited to simple data structures and the management of Native Structures.

Right now, Burst is in every critical operation of the game. Every Chunk Generation is made with Burst, every Chunk rendering is made with Burst. Even the compression algorithm is sped up by Burst. Without Burst, the game wouldn't be able to run smoothly with a render distance bigger than 5. Burst is a **necessity** to this game, and by far the **most important addition to optimization in the history of Unity**.

# NativeTools using unsafe C

C# lets you use C language inside a unsafe scope. C language's memcpy was used to convert NativeArray and NativeLists to their managed counterpart using a single memwrite operation.

# IL2CPP Scripting Backend

This is a big one. Why having C# VM mono code running if you can have an intermediate C++ build?
Although converting to IL2CPP makes everything way faster, build time and poor Unity support for native libs is daunting.
Be aware that Unity won't be capable of using every single lib or dll the underlying C language uses. Normally, Unity Documentation says that PInvoking is the way to go, but PInvoking didn't work for me in ANY case. PInvoking is difficult to set up and requires some advanced C# knowledge. If you are interested in switching to IL2CPP, be aware that you must have to find a workaround to half of System libraries.

\*PInvoking = Building manual support for underlying dll/syscall imports inside C# code.

# Congratulations

Now you know a little bit more about Optimization, be happy that you are running the game at a solid FPS rate. Seriously, if you are trying to develop a game, optimize it as you go. **Do not leave the optimization to be done later in development!!**