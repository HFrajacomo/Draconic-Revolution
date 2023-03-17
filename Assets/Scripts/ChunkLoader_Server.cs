using System.Collections;
using System.Collections.Generic;
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
	public Dictionary<ChunkPos, Chunk> chunks = new Dictionary<ChunkPos, Chunk>();
	public ChunkPos currentChunk;
	public ChunkPos newChunk;
	public BlockEncyclopedia blockBook;
    public BUDScheduler budscheduler;
    public VFXLoader vfx;
    public TimeOfDay time;
    public WorldGenerator worldGen;
    public StructureHandler structHandler;
    public Server server;

    // Queues
    public List<ChunkPos> toLoad = new List<ChunkPos>();
    public List<ChunkPos> toUnload = new List<ChunkPos>();
    public Dictionary<ChunkPos, HashSet<ulong>> loadedChunks = new Dictionary<ChunkPos, HashSet<ulong>>();

	// World Generation
	public int worldSeed = -1; // 6 number integer
    public BiomeHandler biomeHandler;

	// Persistence
    public RegionFileHandler regionHandler;
    public PlayerServerInventory playerServerInventory;

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
        
        if(this.worldGen != null)
            this.worldGen.DestroyNativeMemory();

        if(this.playerServerInventory != null)
            this.playerServerInventory.Destroy();
    }

    void Start(){
        this.server = new Server(this);
        this.time.SetServer(this.server);
    }

    void Update(){ 
        if(this.RECEIVEDWORLDDATA && this.INITIALIZEDWORLD){
            // Decides what to do for current tick
            HandleServerCommunication();

            if(toLoad.Count > 0)
                LoadChunk();
            else if(Structure.reloadChunks.Count > 0)
                SavePregenChunk();
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
        this.playerServerInventory = new PlayerServerInventory();
        worldSeed = regionHandler.GetRealSeed();
        biomeHandler = new BiomeHandler();
        this.worldGen = new WorldGenerator(worldSeed, biomeHandler, structHandler, this);
        biomeHandler.SetWorldGenerator(this.worldGen);


        print("Initializing World");
    
        // Sends the first player it's information
        PlayerData pdat = this.regionHandler.LoadPlayer(this.server.firstConnectedID);
        Vector3 playerPos = pdat.GetPosition();
        Vector3 playerDir = pdat.GetDirection();
        pdat.SetOnline(true);

        this.regionHandler.InitDataFiles(new ChunkPos((int)(playerPos.x/Chunk.chunkWidth), (int)(playerPos.z/Chunk.chunkWidth), (int)(playerPos.y/Chunk.chunkDepth)));

        HandleServerCommunication();

        // Send first player info
        NetMessage message = new NetMessage(NetCode.SENDSERVERINFO);
        message.SendServerInfo(playerPos.x, playerPos.y, playerPos.z, playerDir.x, playerDir.y, playerDir.z);
        this.server.Send(message.GetMessage(), message.size, this.server.firstConnectedID); 

        // Send first player inventory info
        NetMessage inventoryMessage = new NetMessage(NetCode.SENDINVENTORY);
        inventoryLength = this.playerServerInventory.LoadInventoryIntoBuffer(this.server.firstConnectedID, out isEmptyInventory);
        if(!isEmptyInventory)
            inventoryMessage.SendInventory(this.playerServerInventory.GetBuffer(), inventoryLength);
        else
            inventoryMessage.SendInventory(this.playerServerInventory.GetEmptyBuffer(), inventoryLength);

        this.server.Send(inventoryMessage.GetMessage(), inventoryMessage.size, this.server.firstConnectedID);

        this.INITIALIZEDWORLD = true;
        this.time.SetLock(false);
    }

    // Deals with the handling of Server received information
    private void HandleServerCommunication(){
        // If NetMessage queue is not empty
        int queueCount = this.server.queue.Count;

        if(queueCount > 0){
            for(ulong i=0; i<(ulong)queueCount; i++){
                this.server.HandleReceivedMessage(this.server.queue[0].GetData(), this.server.queue[0].GetID());
                this.server.queue.RemoveAt(0);
            }
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

    // Loads Chunk data, but doesn't draw them
    private void LoadChunk(){
        for(int i = 0; i < 3; i++){
        	if(toLoad.Count > 0){
        		// Prevention
        		if(toUnload.Contains(toLoad[0])){
        			toUnload.Remove(toLoad[0]);
        			toLoad.RemoveAt(0);
        			return;
        		}

                if(chunks.ContainsKey(toLoad[0])){
                    toLoad.RemoveAt(0);
                    return;
                }

                bool isPregen;

                // Gets correct region file
                regionHandler.GetCorrectRegion(toLoad[0]);

                // If current chunk toLoad was already generated
                if(regionHandler.IsIndexed(toLoad[0])){

                    isPregen = regionHandler.GetsNeedGeneration(toLoad[0]);

                    // If chunk is Pre-Generated
                    if(isPregen){
                        chunks.Add(toLoad[0], new Chunk(toLoad[0], server:true));
                        vfx.NewChunk(toLoad[0], isServer:true);
                        regionHandler.LoadChunk(chunks[toLoad[0]]);
                        this.worldGen.ClearCaches();
                        this.worldGen.SetVoxdata(chunks[toLoad[0]].data.GetData());
                        this.worldGen.SetCacheHP(chunks[toLoad[0]].metadata.GetHPData());
                        this.worldGen.SetCacheState(chunks[toLoad[0]].metadata.GetStateData());
                        this.worldGen.GenerateChunk(toLoad[0], isPregen:true);
                        chunks[toLoad[0]].biomeName = this.worldGen.GetCacheBiome();
                        chunks[toLoad[0]].BuildOnVoxelData(new VoxelData(this.worldGen.GetVoxdata(), toLoad[0]));
                        chunks[toLoad[0]].metadata = new VoxelMetadata(this.worldGen.GetCacheHP(), this.worldGen.GetCacheState());
                        chunks[toLoad[0]].needsGeneration = 0;
                        regionHandler.SaveChunk(chunks[toLoad[0]]);
                    }
                    // If it's just a normally generated chunk
                    else{
                        chunks.Add(toLoad[0], new Chunk(toLoad[0], server:true));
                        vfx.NewChunk(toLoad[0], isServer:true);
                        regionHandler.LoadChunk(chunks[toLoad[0]]);
                        chunks[toLoad[0]].needsGeneration = 0;
                    }
                }
                // If it's a new chunk to be generated
                else{
                    chunks.Add(toLoad[0], new Chunk(toLoad[0], server:true));
                    vfx.NewChunk(toLoad[0], isServer:true);

                    this.worldGen.ClearCaches();
                    this.worldGen.GenerateChunk(toLoad[0]);
                    chunks[toLoad[0]].BuildOnVoxelData(new VoxelData(this.worldGen.GetVoxdata(), toLoad[0]));
                    chunks[toLoad[0]].metadata = new VoxelMetadata(this.worldGen.GetCacheHP(), this.worldGen.GetCacheState());
                    chunks[toLoad[0]].needsGeneration = 0;
                    chunks[toLoad[0]].biomeName = this.worldGen.GetCacheBiome();
                    regionHandler.SaveChunk(chunks[toLoad[0]]);
                }


                SendChunkToRequestingClients(toLoad[0]);
        		toLoad.RemoveAt(0);
        	}
        }
    }

    // Post-processing of all deletes chunks from cl.chunks
    public void UnloadChunk(ChunkPos pos, ulong id){
        // If chunk is already gone
        if(!this.loadedChunks.ContainsKey(pos)){
            if(this.chunks.ContainsKey(pos)){
                this.chunks.Remove(pos);
            }
            if(this.vfx.Contains(pos, isServer:true)){
                this.vfx.RemoveChunk(pos, isServer:true);
            }
        }
        else{
            this.loadedChunks[pos].Remove(id);

            // If connected clients to this chunk are none
            if(this.loadedChunks[pos].Count == 0){
                this.loadedChunks.Remove(pos);
                this.chunks.Remove(pos);
                this.vfx.RemoveChunk(pos, isServer:true);
            }
        }

    }

    // Sends chunk information to clients that need it
    private void SendToClients(ChunkPos pos){
        NetMessage message = new NetMessage(NetCode.SENDCHUNK);
        message.SendChunk(this.chunks[pos]);
        this.server.SendToClients(pos, message);
    }

    // Sends chunk information to all requesting clients
    private void SendChunkToRequestingClients(ChunkPos pos){
        // If there was no request for this chunk yet
        if(!this.server.chunksRequested.ContainsKey(pos))
            return;

        HashSet<ulong> iterator = new HashSet<ulong>(this.server.chunksRequested[pos]);

        foreach(ulong id in iterator)
            this.server.RequestChunkLoad(pos, id);
    }

    // Returns block code of a castcoord
    public ushort GetBlock(CastCoord c){
        if(this.chunks.ContainsKey(c.GetChunkPos())){
            return this.chunks[c.GetChunkPos()].data.GetCell(c.blockX, c.blockY, c.blockZ);
        }
        else{
            return (ushort)(ushort.MaxValue/2); // Error Code
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
            this.toLoad.Insert(0, pos);
            this.LoadChunk();
            return GetBlockHeight(pos, blockX, blockZ) + (pos.y*Chunk.chunkDepth)+1;
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

    public void TestInventoryReceive(ulong id){
        PlayerServerInventorySlot[] slots = new PlayerServerInventorySlot[45];
        NetMessage message;
        int length;

        for(int i=0; i < 45; i++){
            if(i == 1){
                slots[i] = new ItemPlayerInventorySlot(ItemID.STONEBLOCK, 50);
            }
            else if(i == 2){
                slots[i] = new ItemPlayerInventorySlot(ItemID.TORCH, 50);
            }
            else if(i == 3){
                slots[i] = new ItemPlayerInventorySlot(ItemID.STONEBRICKBLOCK, 50);
            }
            else if(i == 4){
                slots[i] = new ItemPlayerInventorySlot(ItemID.WOODENPLANKSREGULARBLOCK, 50);
            }
            else if(i == 5){
                slots[i] = new ItemPlayerInventorySlot(ItemID.WOODENPLANKSPINEBLOCK, 50);
            }
            else if(i == 6){
                slots[i] = new ItemPlayerInventorySlot(ItemID.BONEBLOCK, 50);
            }
            else if(i == 7){
                slots[i] = new ItemPlayerInventorySlot(ItemID.WATERBLOCK, 50);
            }
            else if(i == 8){
                slots[i] = new ItemPlayerInventorySlot(ItemID.LAVABLOCK, 50);
            }
            else{
                slots[i] = new EmptyPlayerInventorySlot();
            }
        }

        this.playerServerInventory.AddInventory(id, slots);
        length = this.playerServerInventory.ConvertInventoryToBytes(id);
        message = new NetMessage(NetCode.SENDINVENTORY);
        message.SendInventory(this.playerServerInventory.GetBuffer(), length);
        this.server.Send(message.GetMessage(), message.size, id);
    }
}
