# Optimization

Making a game is easy. Making a good game is harder. Making a good and fast game, is very hard. Go get your popcorn and let's go on an Optimization adventure!

## Chunk Loading Optimization (CPU)

In a Procedurally Generated game, loading and unloading chunks is key. In order to conserve CPU, the ChunkLoader either generates one chunk or draws two into the screen. Normally, it's enough to render chunks given the speed the player will move in the future. Limiting the task of the ChunkLoader every frame makes the game run smoother.

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