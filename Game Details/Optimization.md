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
- The amount of build and lighting slots is limited by the job workers of the process (`ChunkLoader.CalculateJobSlots`). Running more jobs than workers only makes them compete, and the main thread waits longer whenever it needs one of them finished.
- Periodic client events (`Resources.UnloadUnusedAssets` and the forced unload of far chunks) run on real-time intervals, so faster machines don't run them more often.
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

# GPU Hangs (TDR)

A GPU hang looks like a crash with no error: builds die, and the Editor freezes and closes without a log or exception. This is Windows' TDR (Timeout Detection and Recovery). When the GPU takes more than about 2 seconds on a command, Windows resets the driver and the game loses its graphics device. C# and Burst code never throw in this case, so the cause has to be found with GPU tools.

**How to recognize it**
- `Player.log` or the crash copy in `%LOCALAPPDATA%\Temp\Miraboa Studios\Draconic Revolution\Crashes\` has `Device removed reason (887a0006)` (`DXGI_ERROR_DEVICE_HUNG`), and the stack trace ends inside the GPU driver (`nvwgf2umx.dll` on NVIDIA).
- The Windows Event Viewer has `nvlddmkm` errors (System log) and `LiveKernelEvent` 141 reports (Application log) at the time of the crash.
- The first D3D12 call that fails is usually unrelated to the cause, like a buffer creation. It's just the first call made after the device was lost.

**How to find the cause**
1. **Breadcrumbs:** install the optional Windows feature "Graphics Tools". From an admin terminal, register the game with `d3dconfig apps --add "Draconic Revolution.exe"` (D3DConfig settings only affect registered apps; add `Unity.exe` to debug the Editor), then run `d3dconfig dred --force-on-all`, which turns on auto-breadcrumbs, breadcrumb contexts, page faults and DRED data in Windows crash dumps (`d3dconfig dred --help` lists every option).
   DRED keeps its data in the heap of the process, which minidumps (Unity's `crash.dmp`, and the default Windows dump) don't have. So also make Windows write full dumps of the game: `reg add "HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\Draconic Revolution.exe" /v DumpType /t REG_DWORD /d 2 /f` (dumps go to `%LOCALAPPDATA%\CrashDumps`, a few GB each).
   Reproduce the crash and open the full dump in WinDbg. Load Microsoft's DRED script (`D3DDred-WinDbg/D3DDred.js` from the `microsoft/DirectX-Debugging-Tools` GitHub repository) with `.scriptload <path>\D3DDred.js`, then run `!d3ddred`. `dx -r2 @$d3ddred().AutoBreadcrumbNodes[0].OutstandingOps` shows the render operations the GPU hadn't finished. On NVIDIA GPUs, *Nsight Aftermath Monitor* gives the same information, including the shader. Afterwards, run `d3dconfig dred --force-sys-controlled-all` and delete that `LocalDumps` key to turn it all back off.
2. **A/B runs:** run the same build with `-force-gfx-direct` (no Graphics Jobs), with `-force-d3d11`, and with a lower `render_distance` in `config.cfg`. Write down which runs still crash.

The local Server runs headless (`-batchmode -nographics`, see *Networking.md*), so it never holds a graphics device of its own. Any GPU hang comes from the Client.

**Known cause: split graphics jobs.** On Unity 6000.0.25f1 (D3D12, NVIDIA RTX 3070), the Client hung the GPU within seconds to minutes of streaming new chunks, and the Editor froze the same way. A/B runs confirmed the cause:
- Default mode, Graphics Jobs on in Split mode (`kGfxThreadingModeSplitJobs`): crashed every few minutes.
- With `-force-gfx-direct`: no crashes and no driver errors.
- With DRED fully on (`--force-on-all`): no crashes either. Its extra synchronization hides the problem, so never test with DRED on to decide whether a hang is fixed.

The full dump showed every job worker recording draws in `GfxDeviceWorker::RunCommand` when the device was lost. The project was then upgraded to Unity 6000.6.11f1, which has many DX12 Graphics Jobs fixes, but Unity's open bug UUM-153790 (a D3D12 device hang with split or native Graphics Jobs, gone with `-force-gfx-direct`) still reproduces on 6000.6. Re-test after every Unity upgrade without `-force-gfx-direct` and with DRED off. If the hang is still there, disable Graphics Jobs for the Windows build (Player Settings, `WindowsStandaloneSupport: m_GraphicsJobs`) and for the Editor (Preferences > Job Settings). Rendering then still runs on its own render thread (`m_MTRendering`).

# Congratulations

Now you know a little bit more about Optimization, be happy that you are running the game at a solid FPS rate. Seriously, if you are trying to develop a game, optimize it as you go. **Do not leave the optimization to be done later in development!!**