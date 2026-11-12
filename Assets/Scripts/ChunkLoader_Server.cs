using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

public class ChunkLoader_Server : MonoBehaviour
{
	// Basic ChunkLoader Data
	public int unloadAssetsCount = 25;
	private Dictionary<ChunkPos, Chunk> chunks = new Dictionary<ChunkPos, Chunk>();
	public ChunkPos currentChunk;
	public ChunkPos newChunk;
    public BUDScheduler budscheduler;
    public VFXLoader vfx;
    public TimeOfDay time;
    public WorldGenerator worldGen;
    public Server server;

    // Generation Queue, fed by the ChunkStreamer
    private Queue<ChunkPos> loadQueue = new Queue<ChunkPos>();
    private HashSet<ChunkPos> loadQueueSet = new HashSet<ChunkPos>();

    // Chunks being read from disk by the ChunkReader thread
    private ChunkReader chunkReader;
    private HashSet<ChunkPos> readsInFlight = new HashSet<ChunkPos>();
    private System.Diagnostics.Stopwatch drainStopwatch = new System.Diagnostics.Stopwatch();

    // Chunks being generated in the current batch, in request order
    private List<GenerationSlot> pendingGeneration = new List<GenerationSlot>();
    private HashSet<ChunkPos> pendingGenerationSet = new HashSet<ChunkPos>();
    private System.Diagnostics.Stopwatch loadStopwatch = new System.Diagnostics.Stopwatch();
    public Dictionary<ChunkPos, HashSet<ulong>> loadedChunks = new Dictionary<ChunkPos, HashSet<ulong>>();

	// World Generation
	public int worldSeed = -1; // 6 number integer
    public BiomeHandler biomeHandler;

	// Persistence
    public RegionFileHandler regionHandler;
    public EntityFileHandler entityFileHandler;
    public PlayerServerInventory playerServerInventory;
    public CharacterFileHandler characterFileHandler;

	// Flags
    public int reloadMemoryCounter = 30;
    public bool RECEIVEDWORLDDATA = false;
    public bool INITIALIZEDWORLD = false;

    // Cache Data
    private ChunkPos cachePos = new ChunkPos(0,0,0);
    private Chunk cacheChunk;


    void OnApplicationQuit(){
        if(this.chunkReader != null){
            this.chunkReader.Stop();
            this.chunkReader = null;
        }

        if(regionHandler != null){
            FlushPendingStructures();
            regionHandler.CloseAll();
        }

        ForceUnloadAll();

        if(this.entityFileHandler != null)
            this.entityFileHandler.CloseAll();
        
        if(this.worldGen != null)
            this.worldGen.DestroyNativeMemory();

        if(this.playerServerInventory != null)
            this.playerServerInventory.Destroy();

        // Sheets changed in memory are written before the file is closed
        if(this.server != null && this.characterFileHandler != null)
            this.server.entityHandler.FlushSheets();

        if(this.characterFileHandler != null)
            this.characterFileHandler.Close();
    }

    void Start(){
        this.server = new Server(this);

        if(this.server.IsLocal())
            World.SetToClient();
        else
            World.SetToServer();

        EnvironmentVariablesCentral.StartServer();

        if(!this.server.applicationClose){
            this.time.SetServer(this.server);
            this.characterFileHandler = new CharacterFileHandler(World.worldName);
            this.playerServerInventory = new PlayerServerInventory();
        }
    }

    void Update(){
        if(this.RECEIVEDWORLDDATA && this.INITIALIZEDWORLD){
            // Decides what to do for current tick
            HandleServerCommunication();

            if(this.loadQueue.Count > 0 || this.readsInFlight.Count > 0)
                LoadChunk();

            DrainPendingStructures();
            this.regionHandler.ProcessSaves(Constants.SERVER_CHUNK_SAVE_BUDGET_MS);
            this.server.chunkStreamer.Tick();
        }
        else if(!this.INITIALIZEDWORLD && this.RECEIVEDWORLDDATA){
            InitWorld();
        }
        else{
            HandleServerCommunication();
        }
    }

    private void InitWorld(){
        int inventoryLength;
        bool isEmptyInventory;

        this.regionHandler = new RegionFileHandler(this);
        this.entityFileHandler = new EntityFileHandler(this);
        this.chunkReader = new ChunkReader();

        worldSeed = regionHandler.GetRealSeed();
        biomeHandler = new BiomeHandler(false);
        this.worldGen = new WorldGenerator(worldSeed, biomeHandler, this);
        biomeHandler.SetWorldGenerator(this.worldGen);


        print("Initializing World");
    
        // Sends the first player it's information
        PlayerData pdat = this.regionHandler.LoadPlayer(this.server.firstConnectedID);
        Vector3 playerPos = pdat.GetPosition();
        Vector3 playerDir = pdat.GetDirection();
        pdat.SetOnline(true);

        this.regionHandler.InitDataFiles(new ChunkPos((int)(playerPos.x/Chunk.chunkWidth), (int)(playerPos.z/Chunk.chunkWidth), (int)(playerPos.y/Chunk.chunkDepth)));
        this.entityFileHandler.InitDataFiles(new ChunkPos((int)(playerPos.x/Chunk.chunkWidth), (int)(playerPos.z/Chunk.chunkWidth), (int)(playerPos.y/Chunk.chunkDepth)));

        HandleServerCommunication();

        // Send first player info
        NetMessage message = new NetMessage(NetCode.SENDSERVERINFO);
        message.SendServerInfo(playerPos.x, playerPos.y, playerPos.z, playerDir.x, playerDir.y, playerDir.z, this.time.days, this.time.hours, this.time.minutes, this.server.GetPlayerNetID(this.server.firstConnectedID));
        this.server.Send(message.GetMessage(), message.size, this.server.firstConnectedID); 

        // Send first player inventory info
        NetMessage inventoryMessage = new NetMessage(NetCode.SENDINVENTORY);
        inventoryLength = this.playerServerInventory.LoadInventoryIntoBuffer(this.server.firstConnectedID, out isEmptyInventory);
        inventoryMessage.SendInventory(this.playerServerInventory.GetBuffer(), inventoryLength);

        this.server.Send(inventoryMessage.GetMessage(), inventoryMessage.size, this.server.firstConnectedID);

        // Send first player the weather information
        NetMessage weatherMessage = new NetMessage(NetCode.SENDNOISE);
        weatherMessage.SendNoise(GenerationSeed.weatherNoise, World.worldSeed);
        this.server.Send(weatherMessage.GetMessage(), weatherMessage.size, this.server.firstConnectedID);

        this.INITIALIZEDWORLD = true;
        this.time.SetLock(false);
    }

    // Deals with the handling of Server received information
    private void HandleServerCommunication(){
        this.server.ProcessIncoming();
    }

    // Sends everything gathered during the frame and writes the messages to the sockets
    void LateUpdate(){
        if(this.server != null && !this.server.applicationClose)
            this.server.LateTick();
    }


    // Saves and sends chunks changed by structures until the time budget is spent (at least one per call)
    // Pending chunks that are being read or generated are consumed by those instead
    private void DrainPendingStructures(){
        if(Structure.PendingCount() == 0)
            return;

        long budget = (long)(Constants.SERVER_STRUCTURE_DRAIN_BUDGET_MS * System.Diagnostics.Stopwatch.Frequency / 1000f);
        int attempts = Structure.PendingCount();
        Chunk pending;

        this.drainStopwatch.Restart();

        while(attempts > 0 && Structure.TryDequeuePending(out pending)){
            attempts--;

            if(this.readsInFlight.Contains(pending.pos) || this.pendingGenerationSet.Contains(pending.pos)){
                Structure.AddPending(pending);
                continue;
            }

            SavePendingStructure(pending, sendToClients:true);

            if(this.drainStopwatch.ElapsedTicks >= budget)
                break;
        }
    }

    // Saves every chunk changed by structures
    private void FlushPendingStructures(){
        Chunk pending;

        while(Structure.TryDequeuePending(out pending))
            SavePendingStructure(pending, sendToClients:false);
    }

    private void SavePendingStructure(Chunk pending, bool sendToClients){
        Chunk loaded;

        // Loaded chunks were changed directly
        if(this.chunks.TryGetValue(pending.pos, out loaded)){
            if(loaded != pending)
                Structure.Overlay(loaded, pending);

            loaded.needsGeneration = 0;
            this.regionHandler.SaveChunk(loaded);

            if(sendToClients)
                SendToClients(loaded.pos);
        }
        // Unloaded chunks are saved as they are. Ungenerated ones are kept as pre-generated
        else{
            this.regionHandler.SaveChunk(pending);
        }
    }

    // Asks for a chunk to be loaded or generated. Duplicates are ignored
    public void RequestLoad(ChunkPos pos){
        if(this.loadQueueSet.Add(pos))
            this.loadQueue.Enqueue(pos);
    }

    /*
    Loads or generates queued chunks until the frame budget is spent (at least one per frame)
        Chunks stored on disk are read by the ChunkReader thread and loaded when the read finishes
        Chunks that need generation are generated in parallel batches, finished in the order they were scheduled
    */
    private void LoadChunk(){
        long budget = (long)(Constants.SERVER_CHUNK_LOAD_BUDGET_MS * System.Diagnostics.Stopwatch.Frequency / 1000f);
        bool processedAny = false;
        ChunkPos pos;
        Chunk pending;

        this.loadStopwatch.Restart();

        ProcessReadCompletions(budget);

        while(this.loadQueue.Count > 0){
            if(processedAny && this.loadStopwatch.ElapsedTicks >= budget)
                break;

            pos = this.loadQueue.Peek();

            // Already being read. Clients waiting for it are informed when the read finishes
            if(this.readsInFlight.Contains(pos)){
                DequeueLoad();
                continue;
            }

            // Requested again while still being generated
            if(this.pendingGenerationSet.Contains(pos))
                FinishGenerationBatch();

            if(this.chunks.ContainsKey(pos)){
                DequeueLoad();
                this.server.chunkStreamer.OnChunkLoaded(pos, true);
                continue;
            }

            // Every client that asked for it has moved away
            if(!this.server.chunkStreamer.IsAwaited(pos)){
                DequeueLoad();
                this.server.chunkStreamer.OnChunkLoaded(pos, false);
                continue;
            }

            processedAny = true;

            // Chunks changed by structures are newer than their disk data
            pending = Structure.TakePending(pos);

            if(pending != null){
                DequeueLoad();

                if(pending.needsGeneration == 0)
                    InstallChunk(pending, null, save:true);
                else
                    QueueGeneration(pending, isPregen:true);

                continue;
            }

            regionHandler.GetCorrectRegion(pos);

            if(regionHandler.IsIndexed(pos)){
                // Waits for running reads to finish
                if(this.readsInFlight.Count >= Constants.SERVER_MAX_PENDING_READS)
                    break;

                DequeueLoad();
                RequestRead(pos);
                continue;
            }

            DequeueLoad();
            QueueGeneration(new Chunk(pos, server:true), isPregen:false);
        }

        FinishGenerationBatch();

        // Reads requested this tick usually finish within it, so their chunks are loaded right away
        WaitForReads(budget);

        // Reads of pre-generated chunks schedule their generation
        FinishGenerationBatch();
    }

    // Loads chunks whose read finishes until the time budget is spent or no reads are left
    private void WaitForReads(long budget){
        long remaining;

        ProcessReadCompletions(budget);

        while(this.readsInFlight.Count > 0){
            remaining = budget - this.loadStopwatch.ElapsedTicks;

            if(remaining <= 0)
                break;

            if(!this.chunkReader.WaitForCompleted(Math.Max(1, (int)(remaining * 1000 / System.Diagnostics.Stopwatch.Frequency))))
                break;

            ProcessReadCompletions(budget);
        }
    }

    private void DequeueLoad(){
        this.loadQueueSet.Remove(this.loadQueue.Dequeue());
    }

    // Loads a chunk right away, waiting for its read if it's being read, along with its entities
    private void LoadSingleChunk(ChunkPos pos){
        Chunk pending;
        Chunk chunk;

        FinishGenerationBatch();

        // Waits for a running read of this chunk
        while(this.readsInFlight.Contains(pos)){
            ProcessReadCompletions(long.MaxValue);
            System.Threading.Thread.Sleep(0);
        }

        FinishGenerationBatch();

        if(this.chunks.ContainsKey(pos))
            return;

        pending = Structure.TakePending(pos);

        if(pending != null){
            if(pending.needsGeneration == 0)
                InstallChunk(pending, null, save:true);
            else{
                QueueGeneration(pending, isPregen:true);
                FinishGenerationBatch();
            }

            return;
        }

        regionHandler.GetCorrectRegion(pos);

        if(regionHandler.IsIndexed(pos)){
            chunk = new Chunk(pos, server:true);
            regionHandler.LoadChunk(chunk);

            if(chunk.needsGeneration == 0){
                InstallChunk(chunk, null, save:false);
                return;
            }

            QueueGeneration(chunk, isPregen:true);
        }
        else{
            QueueGeneration(new Chunk(pos, server:true), isPregen:false);
        }

        FinishGenerationBatch();
    }

    // Starts reading a chunk and its entities in the ChunkReader thread
    private void RequestRead(ChunkPos pos){
        ChunkReader.Request request = this.chunkReader.GetRequest();

        if(!this.regionHandler.TryLockForRead(pos, out request.chunkPath, out request.chunkOffset)){
            QueueGeneration(new Chunk(pos, server:true), isPregen:false);
            return;
        }

        request.pos = pos;
        request.hasEntities = this.entityFileHandler.TryGetEntityLocation(pos, out request.entityPath, out request.entityOffset);

        this.readsInFlight.Add(pos);
        this.chunkReader.Enqueue(request);
    }

    // Loads chunks whose read finished, until the time budget is spent
    private void ProcessReadCompletions(long budget){
        ChunkReader.Result result;

        while(this.chunkReader.TryGetCompleted(out result)){
            this.readsInFlight.Remove(result.pos);
            this.regionHandler.UnlockRead(result.pos);

            OnReadCompleted(result);
            this.chunkReader.Recycle(result);

            if(this.loadStopwatch.ElapsedTicks >= budget)
                break;
        }
    }

    private void OnReadCompleted(ChunkReader.Result result){
        ChunkPos pos = result.pos;
        Chunk chunk;
        int blockSize, hpSize, stateSize;

        if(this.chunks.ContainsKey(pos)){
            this.server.chunkStreamer.OnChunkLoaded(pos, true);
            return;
        }

        if(!this.server.chunkStreamer.IsAwaited(pos)){
            this.server.chunkStreamer.OnChunkLoaded(pos, false);
            return;
        }

        // A structure changed the chunk while it was being read
        chunk = Structure.TakePending(pos);

        if(chunk != null){
            if(chunk.needsGeneration == 0)
                InstallChunk(chunk, result, save:true);
            else
                QueueGeneration(chunk, isPregen:true);

            return;
        }

        // Falls back to reading it in the main thread
        if(result.failed){
            chunk = new Chunk(pos, server:true);
            regionHandler.LoadChunk(chunk);
            result = null;
        }
        else{
            chunk = new Chunk(pos, server:true);
            this.regionHandler.ApplyHeader(chunk, result.header, out blockSize, out hpSize, out stateSize);
            Compression.DecompressChunk(chunk, result.chunkData, 0, blockSize, blockSize+hpSize, true);

            if(chunk.needsGeneration == 0){
                InstallChunk(chunk, result, save:false);

                // The data read from disk is already compressed the way clients receive it
                this.server.SeedChunkPayload(chunk, result.chunkData, blockSize, hpSize, stateSize);
                return;
            }
        }

        if(chunk.needsGeneration != 0){
            QueueGeneration(chunk, isPregen:true);
            return;
        }

        InstallChunk(chunk, result, save:false);
    }

    // Adds a ready chunk to the loaded chunks, recreating its entities from a read result or from disk
    private void InstallChunk(Chunk chunk, ChunkReader.Result result, bool save){
        ChunkPos pos = chunk.pos;

        AddChunk(pos, chunk);
        vfx.NewChunk(pos, isServer:true);
        chunk.needsGeneration = 0;

        if(save)
            this.regionHandler.SaveChunk(chunk);

        if(result == null)
            this.entityFileHandler.LoadChunkEntities(pos);
        else if(result.hasEntities)
            this.entityFileHandler.LoadChunkEntities(pos, result.entityData, result.entitySize);

        this.server.chunkStreamer.OnChunkLoaded(pos, true);
    }

    // Schedules the generation of a chunk into the next free generation slot. Pre-generated chunks must have their data loaded
    private void QueueGeneration(Chunk chunk, bool isPregen){
        if(this.pendingGeneration.Count == this.worldGen.GetSlotCount())
            FinishGenerationBatch();

        GenerationSlot slot = this.worldGen.GetSlot(this.pendingGeneration.Count);
        this.worldGen.ScheduleGeneration(slot, chunk, isPregen);

        this.pendingGeneration.Add(slot);
        this.pendingGenerationSet.Add(chunk.pos);
    }

    // Finishes every scheduled generation in the order it was scheduled
    private void FinishGenerationBatch(){
        if(this.pendingGeneration.Count == 0)
            return;

        GenerationSlot slot;
        Chunk chunk;
        Chunk late;
        ChunkPos pos;

        JobHandle.ScheduleBatchedJobs();

        for(int i=0; i < this.pendingGeneration.Count; i++){
            slot = this.pendingGeneration[i];
            chunk = slot.chunk;
            pos = chunk.pos;

            AddChunk(pos, chunk);
            vfx.NewChunk(pos, isServer:true);

            this.worldGen.FinishGeneration(slot);

            // Structures applied to this chunk while it was being generated
            late = Structure.TakePending(pos);

            if(late != null && late != chunk)
                Structure.Overlay(chunk, late);

            chunk.needsGeneration = 0;
            regionHandler.SaveChunk(chunk);

            this.entityFileHandler.LoadChunkEntities(pos);
            this.server.chunkStreamer.OnChunkLoaded(pos, true);
        }

        this.pendingGeneration.Clear();
        this.pendingGenerationSet.Clear();
    }

    // Post-processing of all deleted chunks from cl.chunks
    // Return true if chunk has been deleted and false if it's still in cl.chunks
    public bool UnloadChunk(ChunkPos pos, ulong id){
        // If chunk is already gone
        if(!this.loadedChunks.ContainsKey(pos)){
            if(this.Contains(pos)){
                RemoveChunk(pos);
            }
            if(this.vfx.Contains(pos, isServer:true)){
                this.vfx.RemoveChunk(pos, isServer:true);
            }
            return true;
        }
        else{
            this.loadedChunks[pos].Remove(id);

            // If connected clients to this chunk are none
            if(this.loadedChunks[pos].Count == 0){
                this.loadedChunks.Remove(pos);
                RemoveChunk(pos);
                this.vfx.RemoveChunk(pos, isServer:true);
                this.entityFileHandler.SaveChunkEntities(pos, this.server.entityHandler.GetEntitiesInChunk(pos));
                return true;
            }

            this.entityFileHandler.SaveChunkEntities(pos, this.server.entityHandler.GetEntitiesInChunk(pos));

            return false;
        }
    }

    // Getter for Chunks
    public Chunk GetChunk(ChunkPos pos){return this.chunks[pos];}
    public bool TryGetChunk(ChunkPos pos, out Chunk chunk){return this.chunks.TryGetValue(pos, out chunk);}
    public bool Contains(ChunkPos pos){return this.chunks.ContainsKey(pos);}
    private void AddChunk(ChunkPos pos, Chunk c){
        // Changes are tracked so only changed voxels are sent to clients
        c.EnableChangeTracking();

        if(this.chunks.ContainsKey(pos))
            this.chunks[pos] = c;
        else
            this.chunks.Add(pos, c);
    }
    private void RemoveChunk(ChunkPos pos){
        this.chunks.Remove(pos);
        this.server.chunkPayloadCache.Remove(pos);
    }

    // Frees a loaded chunk that no client has, saving its entities
    public void ReleaseChunk(ChunkPos pos){
        if(!this.chunks.ContainsKey(pos) || this.loadedChunks.ContainsKey(pos))
            return;

        this.entityFileHandler.SaveChunkEntities(pos, this.server.entityHandler.GetEntitiesInChunk(pos));
        RemoveChunk(pos);

        if(this.vfx.Contains(pos, isServer:true))
            this.vfx.RemoveChunk(pos, isServer:true);

        this.server.entityHandler.UnloadChunk(pos);
    }


    // Saves all entities
    // Used whenever Server is shutdown
    private void ForceUnloadAll(){
        foreach(ChunkPos pos in this.chunks.Keys){
            this.entityFileHandler.SaveChunkEntities(pos, this.server.entityHandler.GetEntitiesInChunk(pos));
        }
    }

    // Sends the whole chunk to clients that have it, since it was changed in ways that can't be tracked per voxel
    private void SendToClients(ChunkPos pos){
        this.server.SendWholeChunk(pos);
    }

    // Returns block code of a castcoord
    public ushort GetBlock(CastCoord c){
        if(this.chunks.ContainsKey(c.GetChunkPos())){
            return this.chunks[c.GetChunkPos()].data.GetCell(c.blockX, c.blockY, c.blockZ);
        }
        else{
            return 0;
        }
    }

    // Returns block code of a castcoord
    public ushort GetState(CastCoord c){
        if(this.chunks.ContainsKey(c.GetChunkPos())){
            return this.chunks[c.GetChunkPos()].metadata.GetState(c.blockX, c.blockY, c.blockZ);
        }
        else{
            return (ushort)(ushort.MaxValue/2); // Error Code
        }
    }

    // Returns block code of a castcoord
    public ushort GetHP(CastCoord c){
        if(this.chunks.ContainsKey(c.GetChunkPos())){
            return this.chunks[c.GetChunkPos()].metadata.GetHP(c.blockX, c.blockY, c.blockZ);
        }
        else{
            return (ushort)(ushort.MaxValue/2); // Error Code
        }
    }

    // Returns the heightmap value of a generated chunk in block position
    public int GetBlockHeight(ChunkPos pos, int blockX, int blockZ){
        // Checks if chunk doesn't exist
        if(!chunks.ContainsKey(pos)){
            LoadSingleChunk(pos);
            return GetBlockHeight(pos, blockX, blockZ);
        }
        for(int i=Chunk.chunkDepth-1; i >= 0 ; i--){
            if(chunks[pos].data.GetCell(Mathf.Abs(blockX), i, Mathf.Abs(blockZ)) != 0){
                return i+2;
            }
        }

        if(blockX < 15)
            return GetBlockHeight(pos, blockX+1, blockZ);
        if(blockZ < 15)
            return GetBlockHeight(pos, blockX, blockZ+1);

        return GetBlockHeight(pos, 0, 0);
    }

    // Returns two Voxeldata-like array of blocks and states respectively
    public bool GetField(CastCoord c, int2 radius, ref ushort[] blocks, ref ushort[] states){
        if(!this.chunks.ContainsKey(c.GetChunkPos()))
            return false;

        int minX, minZ;
        int cX, cY, cZ;
        int minBoundsX, minBoundsY, minBoundsZ;
        ChunkPos pos;
        ChunkPos middleChunk = c.GetChunkPos();
    
        // Set affected chunks
        minX = ChunkOperation(c.blockX, radius.x, Chunk.chunkWidth);
        minZ = ChunkOperation(c.blockZ, radius.x, Chunk.chunkWidth);

        // Set border chunk bounds
        minBoundsX = NegativeFlip(c.blockX - radius.x, Chunk.chunkWidth);
        minBoundsZ = NegativeFlip(c.blockZ - radius.x, Chunk.chunkWidth);
        minBoundsY = c.blockY - radius.y;

        // Initial Pos
        pos = new ChunkPos(middleChunk.x + minX, middleChunk.z + minZ, 3);
        cX = minBoundsX;
        
        for(int x=0; x < (radius.x*2+1); x++){
            // If X goes to another chunk
            if(cX >= Chunk.chunkWidth){
                pos = new ChunkPos(pos.x+1, middleChunk.z, 3);
                cX = 0;
            }
            // If chunk doesn't exist
            if(!this.chunks.ContainsKey(pos)){
                cX++;
                continue;
            }
            cY = minBoundsY;
            for(int y=0; y < (radius.y*2+1); y++){
                cZ = minBoundsZ;
                // Set Y Limits
                if(cY < 0 || cY >= Chunk.chunkDepth){
                    cY++;
                    pos = new ChunkPos(pos.x, middleChunk.z, 3);
                    continue;
                }

                for(int z=0; z < (radius.x*2+1); z++){
                    // If Z goes to another chunk
                    if(cZ >= Chunk.chunkWidth){
                        pos = new ChunkPos(pos.x, pos.z+1, 3);
                        cZ = 0;
                    }
                    // If chunk doesn't exist
                    if(!this.chunks.ContainsKey(pos)){
                        cZ++;
                        continue;
                    }

                    blocks[x*(radius.x*2+1)*(radius.y*2+1)+y*(radius.x*2+1)+z] = this.chunks[pos].data.GetCell(cX, cY, cZ);
                    states[x*(radius.x*2+1)*(radius.y*2+1)+y*(radius.x*2+1)+z] = this.chunks[pos].metadata.GetState(cX, cY, cZ);
                    cZ++;
                }
                pos = new ChunkPos(pos.x, middleChunk.z, 3);
                cY++;
            }
            cX++;
        }

        return true;
    }

    private int ChunkOperation(int x, int y, int div){
        if((float)x-y > 0)
            return 0;
        else
            return Mathf.FloorToInt(((float)x-y)/div);
    }

    private int NegativeFlip(int sub, int mod){
        if(sub > 0)
            return sub%mod;
        else
            return (sub%mod)+mod;
    }

    // DEBUG FUNCTION
    public void TestInventoryReceive(ulong id){
        int size = InventoryLoader.GetInventorySize("ACTION_HOTBAR") + InventoryLoader.GetInventorySize("HOTBAR") + InventoryLoader.GetInventorySize("PLAYER") + InventoryLoader.GetInventorySize("EQUIPMENT");

        PlayerServerInventorySlot[] slots = new PlayerServerInventorySlot[size];
        NetMessage message;
        int length;

        for(byte i=0; i < size; i++){
            if(i == 0){
                slots[i] = new ActionInventorySlot(ActionLoader.GetID("BASE_EquipmentAttack"), false, 0, 0, 0, 0, InventoryLoader.GetActionInventoryID("ACTION_HOTBAR"), 0);
            }
            else if(i == 10){
                slots[i] = new WeaponPlayerInventorySlot(ItemLoader.GetID("BASE_Bastard_Sword"), 1000, 0, EnchantmentType.NONE, InventoryLoader.GetInventoryID("HOTBAR"), i);
            }
            else if(i == 11){
                slots[i] = new WeaponPlayerInventorySlot(ItemLoader.GetID("BASE_Pickaxe"), 1000, 0, EnchantmentType.NONE, InventoryLoader.GetInventoryID("HOTBAR"), i);
            }
            else if(i == 12){
                slots[i] = new SubWeaponPlayerInventorySlot(ItemLoader.GetID("BASE_Torch"), 0, 50, InventoryLoader.GetInventoryID("HOTBAR"), i);
            }
            else if(i == 13){
                slots[i] = new ItemPlayerInventorySlot(ItemLoader.GetID("BASE_Stone"), 50, InventoryLoader.GetInventoryID("HOTBAR"), i);
            }
            else if(i < InventoryLoader.GetInventorySize("ACTION_HOTBAR")){
                slots[i] = new EmptyPlayerInventorySlot(InventoryLoader.GetActionInventoryID("ACTION_HOTBAR"), i);
            }
            else if(i < InventoryLoader.GetInventorySize("ACTION_HOTBAR") + InventoryLoader.GetInventorySize(InventoryLoader.GetInventoryID("HOTBAR"))){
                slots[i] = new EmptyPlayerInventorySlot(InventoryLoader.GetInventoryID("HOTBAR"), i);
            }
            else if(i < InventoryLoader.GetInventorySize("ACTION_HOTBAR") + InventoryLoader.GetInventorySize(InventoryLoader.GetInventoryID("HOTBAR")) + InventoryLoader.GetInventorySize(InventoryLoader.GetInventoryID("PLAYER"))){
                slots[i] = new EmptyPlayerInventorySlot(InventoryLoader.GetInventoryID("PLAYER"), i);
            }
            else{
                slots[i] = new EmptyPlayerInventorySlot(InventoryLoader.GetInventoryID("EQUIPMENT"), i);                
            }
        }

        this.playerServerInventory.AddInventory(id, slots);
        length = this.playerServerInventory.ConvertInventoryToBytes(id);
        message = new NetMessage(NetCode.SENDINVENTORY);
        message.SendInventory(this.playerServerInventory.GetBuffer(), length);
        this.server.Send(message.GetMessage(), message.size, id);
    }
}
