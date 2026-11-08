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
        if(regionHandler != null)
            regionHandler.CloseAll();

        ForceUnloadAll();

        if(this.entityFileHandler != null)
            this.entityFileHandler.CloseAll();
        
        if(this.worldGen != null)
            this.worldGen.DestroyNativeMemory();

        if(this.playerServerInventory != null)
            this.playerServerInventory.Destroy();

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

            if(this.loadQueue.Count > 0)
                LoadChunk();
            else if(Structure.reloadChunks.Count > 0)
                SavePregenChunk();

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
        message.SendServerInfo(playerPos.x, playerPos.y, playerPos.z, playerDir.x, playerDir.y, playerDir.z, this.time.days, this.time.hours, this.time.minutes);
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
        // Messages that arrive while handling these are left for the next frame
        int queueCount = this.server.queue.Count;
        NetMessage message;

        for(int i=0; i < queueCount; i++){
            if(!this.server.queue.TryDequeue(out message))
                break;

            this.server.HandleReceivedMessage(message.GetData(), message.GetID());
        }
    }


    // Builds Structure data in non-indexed Chunks
    private void SavePregenChunk(){
        cacheChunk = Structure.reloadChunks[0];
        regionHandler.GetCorrectRegion(cacheChunk.pos);

        // If it's loaded
        if(chunks.ContainsKey(cacheChunk.pos)){
            cacheChunk.needsGeneration = 0;

            // Rough Application of Structures
            Structure.RoughApply(chunks[cacheChunk.pos], cacheChunk);

            this.regionHandler.SaveChunk(chunks[cacheChunk.pos]);

            SendToClients(cacheChunk.pos);
        }

        // If is in an unloaded indexed chunk
        else if(this.regionHandler.IsIndexed(cacheChunk.pos)){
            Chunk c = new Chunk(cacheChunk.pos, server:true);
            this.regionHandler.LoadChunk(c);

            // Rough Application of Structures
            Structure.RoughApply(c, cacheChunk);

            this.regionHandler.SaveChunk(c);
        }

        // If is in an unloaded unknown chunk
        else{
            this.regionHandler.SaveChunk(cacheChunk);
        }


        Structure.reloadChunks.RemoveAt(0);
    }

    // Asks for a chunk to be loaded or generated. Duplicates are ignored
    public void RequestLoad(ChunkPos pos){
        if(this.loadQueueSet.Add(pos))
            this.loadQueue.Enqueue(pos);
    }

    // Loads or generates queued chunks until the frame budget is spent (at least one per frame)
    // Chunks that need generation are generated in parallel batches, but are finished in the order they were requested
    private void LoadChunk(){
        long budget = (long)(Constants.SERVER_CHUNK_LOAD_BUDGET_MS * System.Diagnostics.Stopwatch.Frequency / 1000f);
        ChunkPos pos;
        bool isPregen;

        this.loadStopwatch.Restart();

        do{
            pos = this.loadQueue.Dequeue();
            this.loadQueueSet.Remove(pos);

            // Requested again while still being generated
            if(this.pendingGenerationSet.Contains(pos))
                FinishGenerationBatch();

            if(this.chunks.ContainsKey(pos)){
                this.server.chunkStreamer.OnChunkLoaded(pos, true);
                continue;
            }

            // Every client that asked for it has moved away
            if(!this.server.chunkStreamer.IsAwaited(pos)){
                this.server.chunkStreamer.OnChunkLoaded(pos, false);
                continue;
            }

            if(!NeedsGeneration(pos, out isPregen)){
                // Chunks generated before this one must exist first, since their structures may reach it
                FinishGenerationBatch();
                LoadFromDisk(pos);
                this.server.chunkStreamer.OnChunkLoaded(pos, true);
                continue;
            }

            StartGeneration(pos, isPregen);

            if(this.pendingGeneration.Count == this.worldGen.GetSlotCount())
                FinishGenerationBatch();
        } while(this.loadQueue.Count > 0 && this.loadStopwatch.ElapsedTicks < budget);

        FinishGenerationBatch();
    }

    // Loads a chunk from disk or generates it right away, along with its entities
    private void LoadSingleChunk(ChunkPos pos){
        bool isPregen;

        FinishGenerationBatch();

        if(!NeedsGeneration(pos, out isPregen)){
            LoadFromDisk(pos);
            return;
        }

        StartGeneration(pos, isPregen);
        FinishGenerationBatch();
    }

    // Returns true if the chunk has never been generated or was only pre-generated by structures
    private bool NeedsGeneration(ChunkPos pos, out bool isPregen){
        regionHandler.GetCorrectRegion(pos);

        if(regionHandler.IsIndexed(pos)){
            isPregen = regionHandler.GetsNeedGeneration(pos);
            return isPregen;
        }

        isPregen = false;
        return true;
    }

    private void LoadFromDisk(ChunkPos pos){
        Chunk chunk = new Chunk(pos, server:true);

        AddChunk(pos, chunk);
        vfx.NewChunk(pos, isServer:true);
        regionHandler.LoadChunk(chunk);
        chunk.needsGeneration = 0;

        this.entityFileHandler.LoadChunkEntities(pos);
    }

    // Schedules the generation of a chunk into the next free generation slot
    private void StartGeneration(ChunkPos pos, bool isPregen){
        Chunk chunk = new Chunk(pos, server:true);

        // Pre-generated chunks only have their empty voxels generated
        if(isPregen)
            regionHandler.LoadChunk(chunk);

        GenerationSlot slot = this.worldGen.GetSlot(this.pendingGeneration.Count);
        this.worldGen.ScheduleGeneration(slot, chunk, isPregen);

        this.pendingGeneration.Add(slot);
        this.pendingGenerationSet.Add(pos);
    }

    // Finishes every scheduled generation in request order. Chunks only become loaded at their turn, as if generated one by one
    private void FinishGenerationBatch(){
        if(this.pendingGeneration.Count == 0)
            return;

        GenerationSlot slot;
        Chunk chunk;
        ChunkPos pos;

        JobHandle.ScheduleBatchedJobs();

        for(int i=0; i < this.pendingGeneration.Count; i++){
            slot = this.pendingGeneration[i];
            chunk = slot.chunk;
            pos = chunk.pos;

            AddChunk(pos, chunk);
            vfx.NewChunk(pos, isServer:true);

            this.worldGen.FinishGeneration(slot);
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
                this.chunks.Remove(pos);
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
    public bool Contains(ChunkPos pos){return this.chunks.ContainsKey(pos);}
    private void AddChunk(ChunkPos pos, Chunk c){
        if(this.chunks.ContainsKey(pos))
            this.chunks[pos] = c;
        else
            this.chunks.Add(pos, c);
    }
    private void RemoveChunk(ChunkPos pos){this.chunks.Remove(pos);}

    // Frees a loaded chunk that no client has, saving its entities
    public void ReleaseChunk(ChunkPos pos){
        if(!this.chunks.ContainsKey(pos) || this.loadedChunks.ContainsKey(pos))
            return;

        this.entityFileHandler.SaveChunkEntities(pos, this.server.entityHandler.GetEntitiesInChunk(pos));
        this.chunks.Remove(pos);

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

    // Sends chunk information to clients that need it
    private void SendToClients(ChunkPos pos){
        NetMessage message = new NetMessage(NetCode.SENDCHUNK);
        message.SendChunk(this.chunks[pos]);
        this.server.SendToClients(pos, message);
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
