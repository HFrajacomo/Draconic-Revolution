using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class ChunkLoader : MonoBehaviour
{
	// Basic ChunkLoader Data
    private Dictionary<ChunkPos, Chunk> chunks = new Dictionary<ChunkPos, Chunk>(); 
	public int renderDistance = 0;
	public Transform player;
    public Transform cam;
	public ChunkPos currentChunk;
    public ChunkPriorityQueue updatePriorityQueue = new ChunkPriorityQueue(DistanceMetric.EDGE_LIMITING, edgeLimit:World.renderDistance);
    public ChunkPriorityQueue drawPriorityQueue = new ChunkPriorityQueue(DistanceMetric.EDGE_LIMITING, edgeLimit:World.renderDistance);
    public ChunkPriorityQueue updateNoLightPriorityQueue = new ChunkPriorityQueue(DistanceMetric.EDGE_LIMITING, edgeLimit:World.renderDistance);

    // Chunk Streaming
    private StreamWindow streamWindow;
    private bool hasStreamWindow = false;
    private Dictionary<ChunkPos, PendingChunkData> toLoad = new Dictionary<ChunkPos, PendingChunkData>();
    private Queue<ChunkPos> toLoadOrder = new Queue<ChunkPos>();
    private HashSet<ChunkPos> toUnload = new HashSet<ChunkPos>();
    private Queue<ChunkPos> toUnloadOrder = new Queue<ChunkPos>();
    private int pendingChunkAcks = 0;
    private float lastStreamReceiveTime = 0f;
    private float nextResyncTime = 0f;
    private int resyncCursor = 0;
    private int resyncScanned = 0;

    // Chunks that need to be built but are missing neighbors, and which kind of build they need
    private Dictionary<ChunkPos, byte> awaitingNeighbors = new Dictionary<ChunkPos, byte>();
    private static readonly byte BUILD_DRAW = 1;
    private static readonly byte BUILD_UPDATE = 2;
    private static readonly byte BUILD_UPDATE_NOLIGHT = 4;

    // Received from Server
    public float playerX;
    public float playerZ;
    public float playerY;
    public float playerDirX, playerDirY, playerDirZ;
    
    // Unity Reference
    public VFXLoader vfx;
    public TimeOfDay time;
    public GameObject gameUI;
    public Client client;
    public BiomeHandler biomeHandler = new BiomeHandler(true);
    public PlayerMovement playerMovement;
    public PlayerSheetController playerSheetController;
    public PlayerRaycast playerRaycast;
    public PlayerInventoryManager playerInventoryManager;
    public VolumeProfile volume;
    public GameObject mainControllerManager;
    public AudioManager audioManager;
    public SFXLoader sfx;
    public PlayerPositionHandler playerPositionHandler;
    public VoxelLightHandler voxelLightHandler;
    public RealtimeLightHandler realtimeLightHandler;
    public PlayerActionController playerActionController;
    public AmbientHandler ambientHandler;
    public VignetteController vignetteController;
    public GameObject audioListenerObject;
    public PlacementPredictor placementPredictor;

    // Initialization
    public GameObject playerCharacter;
    public PlayerHotbarHandler hotbarHandler;
    public PlayerModelHandler playerModelHandler;

	// Chunk Rendering
	public ChunkRenderer rend;

    // Chunk Building
    private ChunkBuildSlot[] drawSlots;
    private ChunkBuildSlot[] updateSlots;

    // Chunks that were requested to build while already building, and which kind of build they need
    private Dictionary<ChunkPos, byte> deferredBuilds = new Dictionary<ChunkPos, byte>();

    // Voxel Lighting
    private LightingSystem lightingSystem;
    private Dictionary<ChunkPos, byte> awaitingLight = new Dictionary<ChunkPos, byte>(); // Chunks waiting for the light of their neighborhood before building
    private HashSet<ChunkPos> verifyOnUpdate = new HashSet<ChunkPos>(); // Chunks whose next light update also sends light both ways through their borders
    private List<ChunkLightPropagInfo> toCallLightCascade = new List<ChunkLightPropagInfo>();

	// Flags
	public bool WORLD_GENERATED = false; 
    public bool PLAYERSPAWNED = false;
    public bool REQUESTEDCHUNKS = false;
    public bool CONNECTEDTOSERVER = false;
    public bool SENTINFOTOSERVER = false;

    // Timer
    private ushort timer = 0;

    // Cache Data
    private ChunkPos cachePos = new ChunkPos(0,0,0);
    private Chunk cacheChunk;
    private NetMessage message;
    private List<ChunkPos> cacheAddedChunks = new List<ChunkPos>();
    private List<ChunkPos> cacheRemovedChunks = new List<ChunkPos>();
    private System.Diagnostics.Stopwatch budgetStopwatch = new System.Diagnostics.Stopwatch();

    // Player information
    public ulong playerAccountID = 1;


    void Awake(){
        BlockEncyclopediaECS.InitializeNativeStructures();
        VoxelLoader.InitBlockEncyclopediaECS();
        ObjectMeshTables.Build();
        ChunkMeshLayout.Init();
        CreateBuildSlots();
        this.lightingSystem = new LightingSystem(this);
        this.placementPredictor = new PlacementPredictor(this);
        PlayerHotbarHandler.ResetSessionState();

        this.playerCharacter.SetActive(false);
        this.playerCharacter.transform.position = new Vector3(0,-999,0);
        this.mainControllerManager.SetActive(false);
        this.gameUI.SetActive(false);
        this.client = new Client(this);
        HandleClientCommunication();
        this.player.position = new Vector3(0,-999,0);
        this.playerAccountID = Configurations.accountID;
        this.time.SetClient(this.client);
        this.realtimeLightHandler = this.gameObject.AddComponent<RealtimeLightHandler>();
        this.realtimeLightHandler.SetChunkLoader(this);
        this.vfx.SetRealtimeLightHandler(this.realtimeLightHandler);
        SetAudioManager();
        this.sfx.SetAudioManager(this.audioManager);
        this.playerPositionHandler.SetAudioManager(this.audioManager);
        World.SetGameSceneFlag(true);
        VoxelData.SetChunkLoader(this);
        this.playerModelHandler.BuildModel(PlayerAppearanceData.GetAppearance(), PlayerAppearanceData.GetGender(), true);
        this.playerModelHandler.parent.AddComponent<EntityVoxelLight>().SetChunkLoader(this);
        this.ambientHandler.SetChunkLoader(this);
    }

    public Chunk Get(ChunkPos pos){return this.chunks[pos];}
    public bool Contains(ChunkPos pos){return this.chunks.ContainsKey(pos);}

    // Columns where direct sunlight enters the top of a chunk (see LightingSystem)
    public SunExitMask GetSkyFromAbove(ChunkPos pos){
        if(this.lightingSystem == null)
            return SunExitMask.ALL;

        return this.lightingSystem.GetSkyFromAbove(pos);
    }

    public void Cleanup(bool comesFromClient=false){
        // The Server is told before the connection is closed
        if(!comesFromClient)
            this.client.Shutdown(notifyServer:true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Redirector.SetServerClosure();
        this.biomeHandler.Clear();
        this.biomeHandler = null;
        this.mainControllerManager = null;
        this.volume = null;
        this.rend = null;
        this.hotbarHandler = null;
        this.playerModelHandler = null;
        this.time = null;
        this.client = null;
        this.audioManager.Stop(AudioUsecase.MUSIC_CLIP);
        this.audioManager.Destroy();
        this.placementPredictor.Clear();
        ClearAllChunks();
        DisposeBuildSlots();
        DisposeLighting();

        Destroy(this);
    }

    void OnDestroy(){
        DisposeBuildSlots();
        DisposeLighting();
    }

    // Light calculations are also finished and started at the end of the frame, so more of them run per frame
    // Every message sent during the frame is written to the socket at once
    void LateUpdate(){
        if(this.lightingSystem != null && this.REQUESTEDCHUNKS)
            this.lightingSystem.Update();

        if(this.client != null)
            this.client.Flush();
    }

    private void DisposeLighting(){
        if(this.lightingSystem == null)
            return;

        this.lightingSystem.Dispose();
        this.lightingSystem = null;
    }

    void Update(){
        // If hasn't connected to the server yet
        if(this.CONNECTEDTOSERVER && !this.SENTINFOTOSERVER){
            this.message = new NetMessage(NetCode.SENDCLIENTINFO);
            if(World.isClient)
                this.message.SendClientInfo(this.playerAccountID, World.renderDistance, World.worldSeed, World.worldName);
            else
                this.message.SendClientInfo(this.playerAccountID, World.renderDistance, 0, "a");

            this.client.SetPlayerModelHandler(this.playerModelHandler);
            this.playerInventoryManager.Init();
            this.renderDistance = ChunkStreaming.GetRadius(World.renderDistance);
            this.client.Send(this.message);
            this.SENTINFOTOSERVER = true;
        }
        else if(!this.CONNECTEDTOSERVER){
            HandleClientCommunication();
            return;
        }

        // If client hasn't received player data from server yet 
        if(!this.PLAYERSPAWNED){
            HandleClientCommunication();
        }
        // If has received chunks and needs to load them
        else if(this.PLAYERSPAWNED && !this.REQUESTEDCHUNKS){
            InitConfigurationFunctions();

            this.player.position = new Vector3(playerX, playerY+0.8f, playerZ);
            this.playerCharacter.transform.position = new Vector3(playerX, playerY+0.8f, playerZ);

            this.player.eulerAngles = new Vector3(0, playerDirY, 0);
            MouseLook.SetLookDirection(playerDirX);
            
            this.currentChunk = new CastCoord(playerX, playerY, playerZ).GetChunkPos();
            this.playerPositionHandler.Activate();

            this.audioManager.SetPlayerPositionInVoice3DTrack(this.player);

            GetChunks(true);  
            this.REQUESTEDCHUNKS = true;
            HandleClientCommunication();
        }
        else{
            // If current chunk is drawn and world is generated
        	if(!WORLD_GENERATED){
                if(this.chunks.ContainsKey(this.currentChunk) && this.chunks[this.currentChunk].drawMain){
                    HandleClientCommunication();
            		WORLD_GENERATED = true;

                    this.gameUI.SetActive(true);
                    this.playerCharacter.SetActive(true);
                    LoadAudio();
                    this.mainControllerManager.SetActive(true);
                    this.time.SetPlayer(this.playerCharacter);
                    this.client.SetRaycast(this.playerCharacter.GetComponent<PlayerRaycast>());
                    this.client.SetPlayerHotbarHandler(this.hotbarHandler);
                    this.playerPositionHandler.SetChunkLoaderChunkPos();
                    this.playerMovement.Init();
                }
        	}

            MoveEntities();
            HandleClientCommunication();

            // Disconnected while handling messages
            if(this.client == null)
                return;

            // Disconnected by timeout
            if(!RunTimerFunctions())
                return;

            GetChunks(false);

        	UnloadChunk();
            LoadChunk();
            this.lightingSystem.Update();
            FinishBuilds();
            this.placementPredictor.Update();
            DrawChunk();
            UpdateChunk();
            JobHandle.ScheduleBatchedJobs();
            ResyncStream();
            SendChunkAck();
        }
    }

    /*
    Registered Events:

    Connection checks:  every frame (time-based heartbeat and timeout)
    UnloadUnityObjects: 1200 ticks
    ForceUnload:        1500 ticks

    Returns false if the Client was disconnected
    */
    private bool RunTimerFunctions(){
        if(!this.client.CheckConnection())
            return false;

        if(this.timer < 6000)
            this.timer++;
        else
            this.timer = 1;

        // Garbage Collect Unity Assets
        if(this.timer % 1200 == 0){
            Resources.UnloadUnusedAssets();
        }

        // Remove Loaded Chunks outside render distance
        if(this.timer % 1500 == 0){
            ForceUnload();
        }

        return true;
    }

    public void InitConfigurationFunctions(){
        this.rend.rend.sharedMaterials[0].SetFloat("_Fullbright", Configurations.GetFullbright());
        this.rend.rend.sharedMaterials[7].SetFloat("_Fullbright", Configurations.GetFullbright());
    }

    // Moves all entities with SmoothMovement
    private void MoveEntities(){
        this.client.MoveEntities();
    }

    
    // Handles communication received from Server
    private void HandleClientCommunication(){
        if(this.client == null)
            return;

        if(this.client.HasConnectionFailed()){
            this.client.Panic();
            return;
        }

        // Messages that arrive while handling these are left for the next frame
        int queueCount = this.client.queue.Count;
        NetMessage received;

        for(int i=0; i < queueCount; i++){
            // A DISCONNECT message cleans up the client
            if(this.client == null)
                return;
            if(!this.client.queue.TryDequeue(out received))
                return;

            this.client.HandleReceivedMessage(received);
        }
    }

    // Erases loaded chunks dictionary
    private void ClearAllChunks(){
        // Running lighting reads chunk data, so it must stop first
        if(this.lightingSystem != null)
            this.lightingSystem.Clear();

        this.awaitingLight.Clear();
        this.verifyOnUpdate.Clear();

    	foreach(ChunkPos item in chunks.Keys){
    		chunks[item].Destroy();
            vfx.RemoveChunk(item);
            sfx.RemoveChunkSFX(item);
    	}

        chunks.Clear();
        this.deferredBuilds.Clear();
    }

    // Check if the chunkpos in a given (x,z) position is loaded and drawn
    private bool CheckChunkDrawn(float x, float z, float y){
        this.cachePos = new ChunkPos(Mathf.FloorToInt((x+0.5f)/Chunk.chunkWidth), Mathf.FloorToInt((z+0.5f)/Chunk.chunkWidth), Mathf.FloorToInt(y/Chunk.chunkDepth));
    
        if(this.chunks.ContainsKey(this.cachePos)){
            return this.chunks[this.cachePos].drawMain;
        }
        return false;
    }

    // Checks if all 8 chunks around this one exists
    public bool CanBeDrawn(ChunkPos pos){
        ChunkPos aux;

        aux = new ChunkPos(pos.x-1, pos.z, pos.y);
        if(!this.chunks.ContainsKey(aux)){
            return false;
        }
        aux = new ChunkPos(pos.x+1, pos.z, pos.y);
        if(!this.chunks.ContainsKey(aux)){
            return false;
        }
        aux = new ChunkPos(pos.x, pos.z-1, pos.y);
        if(!this.chunks.ContainsKey(aux)){
            return false;
        }
        aux = new ChunkPos(pos.x, pos.z+1, pos.y);
        if(!this.chunks.ContainsKey(aux)){
            return false;
        }
        aux = new ChunkPos(pos.x-1, pos.z-1, pos.y);
        if(!this.chunks.ContainsKey(aux)){
            return false;
        }
        aux = new ChunkPos(pos.x-1, pos.z+1, pos.y);
        if(!this.chunks.ContainsKey(aux)){
            return false;
        }
        aux = new ChunkPos(pos.x+1, pos.z-1, pos.y);
        if(!this.chunks.ContainsKey(aux)){
            return false;
        }
        aux = new ChunkPos(pos.x+1, pos.z+1, pos.y);
        if(!this.chunks.ContainsKey(aux)){
            return false;
        }

        return true;
    }

    // Adds chunk to Update queue
    // verify makes its light update also send light both ways through its borders. It's used for changes made after the neighbors were drawn,
    // and not by the light propagation itself, which would make chunks update each other endlessly
    public void AddToUpdate(ChunkPos pos, bool noLight=false, bool verify=true){
        if(!noLight){
            updatePriorityQueue.Add(pos);

            if(verify)
                this.verifyOnUpdate.Add(pos);
        }
        else
            updateNoLightPriorityQueue.Add(pos);
    }

    // Receives chunk data from the Server. Data of a chunk that wasn't loaded yet is replaced by the newest
    // The data is a pooled network buffer, given back once it's loaded or discarded
    public void EnqueueChunkData(ChunkPos pos, byte[] data, bool streamed){
        PendingChunkData pending;

        if(streamed)
            this.lastStreamReceiveTime = Time.unscaledTime;

        if(this.toLoad.TryGetValue(pos, out pending)){
            NetBufferPool.Return(pending.data);
            pending.data = data;

            if(streamed)
                pending.streamCount++;

            this.toLoad[pos] = pending;
            return;
        }

        this.toLoad.Add(pos, new PendingChunkData(data, streamed ? 1 : 0));
        this.toLoadOrder.Enqueue(pos);
    }

    // Loads received chunks into the ChunkLoader until the frame budget is spent (at least one per frame)
    private void LoadChunk(){
        if(!this.hasStreamWindow)
            return;

        long budget = GetBudgetTicks(Constants.CLIENT_CHUNK_LOAD_BUDGET_MS);
        bool loadedAny = false;
        ChunkPos pos;
        PendingChunkData pending;

        this.budgetStopwatch.Restart();

        while(this.toLoadOrder.Count > 0){
            if(loadedAny && this.budgetStopwatch.ElapsedTicks >= budget)
                break;

            pos = this.toLoadOrder.Dequeue();

            // Already discarded
            if(!this.toLoad.TryGetValue(pos, out pending))
                continue;

            this.toLoad.Remove(pos);
            this.pendingChunkAcks += pending.streamCount;

            // Left the StreamWindow while waiting to be loaded
            if(!this.streamWindow.Contains(pos)){
                NetBufferPool.Return(pending.data);
                continue;
            }

            LoadChunkData(pos, pending.data);
            NetBufferPool.Return(pending.data);
            loadedAny = true;
        }
    }

    // Decompresses a chunk received from the Server and schedules its drawing
    private void LoadChunkData(ChunkPos cp, byte[] data){
        bool chunkOverwrite = true;
        int headerSize = NetMessage.CHUNK_HEADER_SIZE;
        int blockDataSize = NetDecoder.ReadInt(data, 11);
        int hpDataSize = NetDecoder.ReadInt(data, 15);
        Chunk chunk;

        if(!this.chunks.TryGetValue(cp, out chunk)){
            chunk = new Chunk(cp, this.rend, this);
            chunk.biome = data[10];
            this.chunks.Add(cp, chunk);
            chunkOverwrite = false;
        }

        Compression.DecompressChunk(chunk, data, headerSize, headerSize+blockDataSize, headerSize+blockDataSize+hpDataSize, false);

        if(this.vfx.data.ContainsKey(cp))
            this.vfx.RemoveChunk(cp);
            
        this.vfx.NewChunk(cp);

        // VFX, SFX and realtime lights of this chunk are only (re)created by its load events
        chunk.RequestLoadEvents();

        // If is an overwrite of an existing chunk, calculate Heightmap and rendermap again
        if(chunkOverwrite){
            chunk.data.CalculateHeightMap();
        }

        // Light is calculated in worker threads. The chunk below is calculated again if the sunlight leaving this one changed
        this.lightingSystem.Enqueue(cp);

        CancelUnload(cp);

        RequestDraw(cp);

        // Neighbors that were missing this chunk may be drawn now
        if(!chunkOverwrite)
            WakeNeighbors(cp);
    }

    // Unloads chunks that left the StreamWindow until the frame budget is spent (at least one per frame)
    // The Server unloads them on its side when it receives the new StreamWindow
    private void UnloadChunk(){
        long budget = GetBudgetTicks(Constants.CLIENT_CHUNK_UNLOAD_BUDGET_MS);
        bool unloadedAny = false;
        ChunkPos pos;

        this.budgetStopwatch.Restart();

        while(this.toUnloadOrder.Count > 0){
            if(unloadedAny && this.budgetStopwatch.ElapsedTicks >= budget)
                break;

            pos = this.toUnloadOrder.Dequeue();

            // Cancelled
            if(!this.toUnload.Remove(pos))
                continue;
            if(!this.chunks.ContainsKey(pos))
                continue;
            if(this.hasStreamWindow && this.streamWindow.Contains(pos))
                continue;

            this.chunks[pos].Destroy();
            this.vfx.RemoveChunk(pos);
            this.sfx.RemoveChunkSFX(pos);

            this.chunks.Remove(pos);
            this.awaitingNeighbors.Remove(pos);
            this.deferredBuilds.Remove(pos);
            this.awaitingLight.Remove(pos);
            this.verifyOnUpdate.Remove(pos);
            this.lightingSystem.Cancel(pos);
            this.drawPriorityQueue.Remove(pos);
            this.updatePriorityQueue.Remove(pos);
            this.updateNoLightPriorityQueue.Remove(pos);

            unloadedAny = true;
        }
    }

    private void ScheduleUnload(ChunkPos pos){
        if(this.toUnload.Add(pos))
            this.toUnloadOrder.Enqueue(pos);
    }

    private void CancelUnload(ChunkPos pos){
        this.toUnload.Remove(pos);
    }

    // Queues a chunk to be drawn if all its neighbors are loaded, otherwise waits for them
    private void RequestDraw(ChunkPos pos){
        if(!this.chunks.ContainsKey(pos))
            return;

        if(CanBeDrawn(pos))
            this.drawPriorityQueue.Add(pos);
        else
            MarkAwaiting(pos, BUILD_DRAW);
    }

    private void MarkAwaiting(ChunkPos pos, byte flag){
        byte flags;

        this.awaitingNeighbors.TryGetValue(pos, out flags);
        this.awaitingNeighbors[pos] = (byte)(flags | flag);
    }

    // Re-queues the builds of the neighbors of a freshly loaded chunk that were waiting for it
    private void WakeNeighbors(ChunkPos pos){
        for(int x=-1; x <= 1; x++){
            for(int z=-1; z <= 1; z++){
                if(x == 0 && z == 0)
                    continue;

                WakeAwaiting(new ChunkPos(pos.x+x, pos.z+z, pos.y));
            }
        }
    }

    private void WakeAwaiting(ChunkPos pos){
        byte flags;

        if(!this.awaitingNeighbors.TryGetValue(pos, out flags))
            return;

        if(!this.chunks.ContainsKey(pos)){
            this.awaitingNeighbors.Remove(pos);
            return;
        }

        if(!CanBeDrawn(pos))
            return;

        this.awaitingNeighbors.Remove(pos);

        if((flags & BUILD_DRAW) != 0)
            this.drawPriorityQueue.Add(pos);
        if((flags & BUILD_UPDATE) != 0)
            this.updatePriorityQueue.Add(pos);
        if((flags & BUILD_UPDATE_NOLIGHT) != 0)
            this.updateNoLightPriorityQueue.Add(pos);
    }

    // Pops chunks until one that can be built is found. Chunks missing neighbors wait for them instead of going back to the queue
    private bool PopBuildable(ChunkPriorityQueue queue, byte flag, out ChunkPos pos){
        while(queue.GetSize() > 0){
            pos = queue.Pop();

            if(!this.chunks.ContainsKey(pos))
                continue;

            if(!CanBeDrawn(pos)){
                MarkAwaiting(pos, flag);
                continue;
            }

            if(this.chunks[pos].IsBuilding()){
                MarkDeferred(pos, flag);
                continue;
            }

            return true;
        }

        pos = default(ChunkPos);
        return false;
    }

    private void MarkDeferred(ChunkPos pos, byte flag){
        byte flags;

        this.deferredBuilds.TryGetValue(pos, out flags);
        this.deferredBuilds[pos] = (byte)(flags | flag);
    }

    // Re-queues the builds requested for a chunk while it was building
    private void WakeDeferred(ChunkPos pos){
        byte flags;

        if(!this.deferredBuilds.TryGetValue(pos, out flags))
            return;

        this.deferredBuilds.Remove(pos);

        if(!this.chunks.ContainsKey(pos))
            return;

        if((flags & BUILD_DRAW) != 0)
            this.drawPriorityQueue.Add(pos);
        if((flags & BUILD_UPDATE) != 0)
            this.updatePriorityQueue.Add(pos);
        if((flags & BUILD_UPDATE_NOLIGHT) != 0)
            this.updateNoLightPriorityQueue.Add(pos);
    }

    private void CreateBuildSlots(){
        this.drawSlots = new ChunkBuildSlot[Constants.CLIENT_DRAW_BUILD_SLOTS];
        this.updateSlots = new ChunkBuildSlot[Constants.CLIENT_UPDATE_BUILD_SLOTS];

        for(int i=0; i < this.drawSlots.Length; i++)
            this.drawSlots[i] = new ChunkBuildSlot();
        for(int i=0; i < this.updateSlots.Length; i++)
            this.updateSlots[i] = new ChunkBuildSlot();
    }

    // Waits for running builds and frees the native memory of the build slots
    private void DisposeBuildSlots(){
        DisposeBuildSlots(this.drawSlots);
        DisposeBuildSlots(this.updateSlots);

        this.drawSlots = null;
        this.updateSlots = null;
    }

    private void DisposeBuildSlots(ChunkBuildSlot[] slots){
        if(slots == null)
            return;

        for(int i=0; i < slots.Length; i++)
            slots[i].Dispose();
    }

    /*
    Schedules the mesh building of new chunks
        Chunks wait until the light of their neighborhood is calculated (in worker threads)
        Then their light propagation with their neighbors is verified and applied before building
    */
    private void DrawChunk(){
        ChunkPos pos;

        for(int i=0; i < this.drawSlots.Length; i++){
            if(this.drawPriorityQueue.GetSize() == 0)
                break;
            if(!this.drawSlots[i].IsFree())
                continue;

            while(PopBuildable(this.drawPriorityQueue, BUILD_DRAW, out pos)){
                if(!IsNeighborhoodLit(pos)){
                    MarkAwaitingLight(pos, BUILD_DRAW);
                    continue;
                }

                this.chunks[pos].data.VerifyPropagation(this);
                CheckLightPropagation(pos);

                ScheduleBuild(this.drawSlots[i], this.chunks[pos], isDraw:true);
                break;
            }
        }
    }

    // True if the light of a chunk, its horizontal neighbors and its loaded vertical neighbors is calculated
    private bool IsNeighborhoodLit(ChunkPos pos){
        ChunkPos vertical;

        for(int x=-1; x <= 1; x++){
            for(int z=-1; z <= 1; z++){
                if(!this.lightingSystem.IsLit(new ChunkPos(pos.x+x, pos.z+z, pos.y)))
                    return false;
            }
        }

        vertical = new ChunkPos(pos.x, pos.z, pos.y+1);
        if(this.chunks.ContainsKey(vertical) && !this.lightingSystem.IsLit(vertical))
            return false;

        vertical = new ChunkPos(pos.x, pos.z, pos.y-1);
        if(this.chunks.ContainsKey(vertical) && !this.lightingSystem.IsLit(vertical))
            return false;

        return true;
    }

    private void MarkAwaitingLight(ChunkPos pos, byte flag){
        byte flags;

        this.awaitingLight.TryGetValue(pos, out flags);
        this.awaitingLight[pos] = (byte)(flags | flag);
    }

    // Called by the LightingSystem when the light of a chunk is calculated. Chunks waiting for it to build may build now
    public void OnLightReady(ChunkPos pos){
        if(this.awaitingLight.Count == 0)
            return;

        for(int x=-1; x <= 1; x++){
            for(int z=-1; z <= 1; z++){
                WakeAwaitingLight(new ChunkPos(pos.x+x, pos.z+z, pos.y));
            }
        }

        WakeAwaitingLight(new ChunkPos(pos.x, pos.z, pos.y+1));
        WakeAwaitingLight(new ChunkPos(pos.x, pos.z, pos.y-1));
    }

    private void WakeAwaitingLight(ChunkPos pos){
        byte flags;

        if(!this.awaitingLight.TryGetValue(pos, out flags))
            return;

        if(!this.chunks.ContainsKey(pos)){
            this.awaitingLight.Remove(pos);
            return;
        }

        if(!IsNeighborhoodLit(pos))
            return;

        this.awaitingLight.Remove(pos);

        if((flags & BUILD_DRAW) != 0)
            this.drawPriorityQueue.Add(pos);
        if((flags & BUILD_UPDATE) != 0)
            this.updatePriorityQueue.Add(pos);
        if((flags & BUILD_UPDATE_NOLIGHT) != 0)
            this.updateNoLightPriorityQueue.Add(pos);
    }

    // Schedules the mesh rebuilding of changed chunks
    private void UpdateChunk(){
        bool lightUpdate = false;
        bool withLight;
        ChunkPos pos;

        for(int i=0; i < this.updateSlots.Length; i++){
            if(this.updatePriorityQueue.GetSize() + this.updateNoLightPriorityQueue.GetSize() == 0)
                break;
            if(!this.updateSlots[i].IsFree())
                continue;

            // Only one update with light is preferred per frame, the others go to updates without light
            if(!PopUpdate(!lightUpdate, out pos, out withLight))
                break;

            // Updates come from changes the player sees, so their light is calculated right away
            if(withLight){
                this.lightingSystem.RecalcNow(pos);

                if(this.verifyOnUpdate.Remove(pos))
                    VerifyLightBothWays(pos);

                CheckLightPropagation(pos);
                lightUpdate = true;
            }

            ScheduleBuild(this.updateSlots[i], this.chunks[pos], isDraw:false);
        }
    }

    private bool PopUpdate(bool preferLight, out ChunkPos pos, out bool withLight){
        if(preferLight){
            withLight = true;

            if(PopBuildable(this.updatePriorityQueue, BUILD_UPDATE, out pos))
                return true;

            withLight = false;
            return PopBuildable(this.updateNoLightPriorityQueue, BUILD_UPDATE_NOLIGHT, out pos);
        }

        withLight = false;

        if(PopBuildable(this.updateNoLightPriorityQueue, BUILD_UPDATE_NOLIGHT, out pos))
            return true;

        withLight = true;
        return PopBuildable(this.updatePriorityQueue, BUILD_UPDATE, out pos);
    }

    // Draw builds always send load events. Update builds also send them while a load request is pending
    private void ScheduleBuild(ChunkBuildSlot slot, Chunk chunk, bool isDraw){
        int requestedLoad = chunk.GetRequestedLoadVersion();
        bool load = isDraw || requestedLoad != chunk.GetSentLoadVersion();
        ChunkPos pos = chunk.pos;

        // The build snapshot reads the light of the chunk and its neighbors
        for(int y=-1; y <= 1; y++){
            for(int x=-1; x <= 1; x++){
                for(int z=-1; z <= 1; z++){
                    this.lightingSystem.EnsureLit(new ChunkPos(pos.x+x, pos.z+z, pos.y+y));
                }
            }
        }

        slot.Schedule(chunk, this, load, requestedLoad);
    }

    // Applies finished builds until the frame budget is spent (at least one per frame) and swaps cooked colliders
    private void FinishBuilds(){
        long budget = GetBudgetTicks(Constants.CLIENT_DRAW_FINISH_BUDGET_MS);
        bool appliedAny = false;

        this.budgetStopwatch.Restart();

        // Updates first, since they come from changes the player can see
        FinishBuilds(this.updateSlots, budget, ref appliedAny);
        FinishBuilds(this.drawSlots, budget, ref appliedAny);
    }

    private void FinishBuilds(ChunkBuildSlot[] slots, long budget, ref bool appliedAny){
        ChunkBuildSlot slot;
        Chunk chunk;

        for(int i=0; i < slots.Length; i++){
            slot = slots[i];

            if(slot.IsBakeDone()){
                chunk = slot.GetChunk();
                slot.FinishBake();
                chunk.drawMain = true;
                WakeDeferred(chunk.pos);
                continue;
            }

            if(!slot.IsBuildDone())
                continue;
            if(appliedAny && this.budgetStopwatch.ElapsedTicks >= budget)
                continue;

            chunk = slot.GetChunk();
            slot.ApplyMeshes();

            if(slot.IsLoadBuild())
                chunk.SendLoadEvents(slot.GetLoadCoords(), slot.GetLoadVersion());

            if(this.WORLD_GENERATED)
                this.vfx.UpdateLights(chunk.pos);

            appliedAny = true;
        }
    }

    private long GetBudgetTicks(float milliseconds){
        return (long)(milliseconds * System.Diagnostics.Stopwatch.Frequency / 1000f);
    }

    // Checks if neighbor chunks should have light propagated
    // MUST BE USED AFTER THE CalculateLightMap FUNCTION
    // Returns true if should update current chunk and false if not
    public bool CheckLightPropagation(ChunkPos pos, byte flag=255, int recursionDepth=0){
        byte propagationFlag;

        if(recursionDepth >= 5)
            return false;

        if(flag == 255)
            propagationFlag = this.chunks[pos].data.GetPropagationFlag();
        else{
            propagationFlag = flag;
        }

        // None
        if(propagationFlag == 0)
            return false;

        // Light of chunks still being calculated in worker threads is finished first
        this.lightingSystem.EnsureLit(pos);

        // xm
        if((propagationFlag & 1) != 0)
            PropagateBorder(pos, new ChunkPos(pos.x-1, pos.z, pos.y), 0, recursionDepth);
        // xp
        if((propagationFlag & 2) != 0)
            PropagateBorder(pos, new ChunkPos(pos.x+1, pos.z, pos.y), 1, recursionDepth);
        // zm
        if((propagationFlag & 4) != 0)
            PropagateBorder(pos, new ChunkPos(pos.x, pos.z-1, pos.y), 2, recursionDepth);
        // zp
        if((propagationFlag & 8) != 0)
            PropagateBorder(pos, new ChunkPos(pos.x, pos.z+1, pos.y), 3, recursionDepth);
        // ym
        if((propagationFlag & 16) != 0)
            PropagateBorder(pos, new ChunkPos(pos.x, pos.z, pos.y-1), 4, recursionDepth);
        // yp
        if((propagationFlag & 32) != 0)
            PropagateBorder(pos, new ChunkPos(pos.x, pos.z, pos.y+1), 5, recursionDepth);

        // Only the outermost call runs the cascade. Nested calls only add to it
        if(recursionDepth == 0){
            for(int i=0; i < this.toCallLightCascade.Count; i++){
                CheckLightPropagation(this.toCallLightCascade[i]);
            }

            this.toCallLightCascade.Clear();
        }

        return false;
    }

    /*
    Propagates light through the border between a chunk and its neighbor, and updates the chunks whose light changed.
    Vertical borders are always propagated from the upper chunk (border ym of the upper chunk), so the sunlight rules of
    the upper chunk apply no matter which of the two chunks asked for it
    */
    private void PropagateBorder(ChunkPos pos, ChunkPos neighbor, byte borderCode, int recursionDepth){
        ChunkPos first = pos;
        ChunkPos second = neighbor;
        ushort updateCode;

        if(!this.chunks.ContainsKey(neighbor))
            return;

        if(borderCode == 5){
            first = neighbor;
            second = pos;
            borderCode = 4;
        }

        this.lightingSystem.EnsureLit(neighbor);
        updateCode = VoxelData.PropagateLight(this.chunks[first].data, this.chunks[first].metadata, this.chunks[second].data, this.chunks[second].metadata, borderCode);

        if((updateCode & 4) == 4)
            AddToUpdate(second, noLight:false, verify:false);
        if(((updateCode & 7) == 2 || (updateCode & 7) == 3) && (updateCode & 4) != 4)
            AddToUpdate(second, noLight:true);
        if((updateCode & 7) == 1 || (updateCode & 7) == 3)
            AddToUpdate(first, noLight:true);
        if(updateCode >= 8)
            toCallLightCascade.Add(new ChunkLightPropagInfo(neighbor, (byte)(updateCode >> 3), recursionDepth+1));
    }

    // Sends light both ways through every border of a chunk that changed after its neighbors were drawn
    private void VerifyLightBothWays(ChunkPos pos){
        this.lightingSystem.EnsureLit(new ChunkPos(pos.x-1, pos.z, pos.y));
        this.lightingSystem.EnsureLit(new ChunkPos(pos.x+1, pos.z, pos.y));
        this.lightingSystem.EnsureLit(new ChunkPos(pos.x, pos.z-1, pos.y));
        this.lightingSystem.EnsureLit(new ChunkPos(pos.x, pos.z+1, pos.y));
        this.lightingSystem.EnsureLit(new ChunkPos(pos.x, pos.z, pos.y+1));
        this.lightingSystem.EnsureLit(new ChunkPos(pos.x, pos.z, pos.y-1));

        this.chunks[pos].data.VerifyPropagation(this, bidirectional:true);
    }
    private bool CheckLightPropagation(ChunkLightPropagInfo info){
        return CheckLightPropagation(info.pos, info.propagationFlag, info.recursionDepth);
    }


    // Keeps the StreamWindow around the player. Chunks that leave it are unloaded locally and the Server is informed of every change
    // reload=true discards every chunk and starts streaming from scratch
    public void GetChunks(bool reload){
        StreamWindow newWindow = new StreamWindow(this.playerPositionHandler.GetCurrentChunk(), this.playerPositionHandler.GetPlayerVerticalChunk(), this.renderDistance);

    	if(reload){
    		ClearAllChunks();
            DiscardAllPendingChunks();
    		this.toUnload.Clear();
            this.toUnloadOrder.Clear();
            this.awaitingNeighbors.Clear();
            this.drawPriorityQueue.Clear();
            this.updatePriorityQueue.Clear();
            this.updateNoLightPriorityQueue.Clear();

            SetQueuesPosition(newWindow.center);
            SetStreamWindow(newWindow);
	        return;
	    }

        if(newWindow == this.streamWindow)
            return;

        ChunkStreaming.Diff(this.streamWindow, newWindow, this.cacheAddedChunks, this.cacheRemovedChunks);

        for(int i=0; i < this.cacheRemovedChunks.Count; i++){
            if(this.chunks.ContainsKey(this.cacheRemovedChunks[i]))
                ScheduleUnload(this.cacheRemovedChunks[i]);

            DiscardPendingChunk(this.cacheRemovedChunks[i]);
        }

        for(int i=0; i < this.cacheAddedChunks.Count; i++)
            CancelUnload(this.cacheAddedChunks[i]);

        if(newWindow.center != this.streamWindow.center)
            SetQueuesPosition(newWindow.center);

        SetStreamWindow(newWindow);
    }

    private void SetStreamWindow(StreamWindow window){
        this.streamWindow = window;
        this.hasStreamWindow = true;
        this.currentChunk = window.center;

        // Window changed, so the Server is about to stream again
        this.lastStreamReceiveTime = Time.unscaledTime;
        this.nextResyncTime = 0f;
        this.resyncCursor = 0;
        this.resyncScanned = 0;

        NetMessage message = new NetMessage(NetCode.STREAMCENTER);
        message.StreamCenter(window.center, window.verticalLayer);
        this.client.Send(message);
    }

    private void SetQueuesPosition(ChunkPos pos){
        this.lightingSystem.SetPlayerPosition(pos);
        this.drawPriorityQueue.SetPlayerPosition(pos);
        this.updatePriorityQueue.SetPlayerPosition(pos);
        this.updateNoLightPriorityQueue.SetPlayerPosition(pos);
    }

    // Drops received data of a chunk that is no longer wanted. Streamed data still has to be acknowledged
    private void DiscardPendingChunk(ChunkPos pos){
        PendingChunkData pending;

        if(!this.toLoad.TryGetValue(pos, out pending))
            return;

        this.pendingChunkAcks += pending.streamCount;
        this.toLoad.Remove(pos);
        NetBufferPool.Return(pending.data);
    }

    private void DiscardAllPendingChunks(){
        foreach(PendingChunkData pending in this.toLoad.Values){
            this.pendingChunkAcks += pending.streamCount;
            NetBufferPool.Return(pending.data);
        }

        this.toLoad.Clear();
        this.toLoadOrder.Clear();
    }

    // Informs the Server how many streamed chunks were consumed this frame, so it can send more
    private void SendChunkAck(){
        if(this.pendingChunkAcks == 0)
            return;

        NetMessage message = new NetMessage(NetCode.CHUNKACK);
        message.ChunkAck(this.pendingChunkAcks, this.toLoad.Count);
        this.client.Send(message);

        this.pendingChunkAcks = 0;
    }

    // Returns block code of a castcoord
    public ushort GetBlock(CastCoord c, bool errorZero = false){
        if(this.chunks.ContainsKey(c.GetChunkPos())){
            return this.chunks[c.GetChunkPos()].data.GetCell(c.blockX, c.blockY, c.blockZ);
        }
        else{
            if(errorZero)
                return (ushort)0;
            else
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

    // Returns the voxel light level of a castcoord, or -1 if its chunk is not loaded or its light is still being calculated.
    // Never forces a light calculation, so it is safe to call every frame
    public int GetLight(CastCoord c, bool isNatural=true){
        ChunkPos pos = c.GetChunkPos();

        if(this.lightingSystem == null || !this.chunks.ContainsKey(pos) || !this.lightingSystem.IsLit(pos))
            return -1;

        VoxelData data = this.chunks[pos].data;

        if(data.GetLightMap() == null)
            return -1;

        return data.GetLight(c.blockX, c.blockY, c.blockZ, isNatural);
    }

    // Attaches the AudioManager that has the "DontDestroyOnLoad" flag on
    private void SetAudioManager(){
        this.audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
    }

    /*
    Looks for chunks of the StreamWindow that never arrived (lost or dropped packets) and asks the Server for them
        Only runs once the Server stopped streaming for a while
        Scans a slice of the window per frame, so large Render Distances never cause a single-frame spike
    */
    private void ResyncStream(){
        if(!this.hasStreamWindow)
            return;

        float now = Time.unscaledTime;

        if(now < this.nextResyncTime)
            return;
        if(now - this.lastStreamReceiveTime < Constants.CHUNK_RESYNC_IDLE_SECONDS)
            return;
        if(this.toLoadOrder.Count > 0)
            return;

        int total = this.streamWindow.Count();
        int requests = 0;
        ChunkPos pos;
        NetMessage message;

        for(int i=0; i < Constants.CHUNK_RESYNC_CHECKS_PER_FRAME; i++){
            // Full scan of the window finished
            if(this.resyncScanned >= total){
                this.resyncScanned = 0;
                this.nextResyncTime = now + Constants.CHUNK_RESYNC_INTERVAL_SECONDS;
                break;
            }

            if(this.resyncCursor >= total)
                this.resyncCursor = 0;

            pos = this.streamWindow.GetByIndex(this.resyncCursor);
            this.resyncCursor++;
            this.resyncScanned++;

            if(this.chunks.ContainsKey(pos) || this.toLoad.ContainsKey(pos))
                continue;

            message = new NetMessage(NetCode.REQUESTCHUNKLOAD);
            message.RequestChunkLoad(pos);
            this.client.Send(message);

            requests++;

            if(requests >= Constants.CHUNK_RESYNC_MAX_REQUESTS)
                break;
        }

        // Gives the Server time to answer before asking again
        if(requests > 0)
            this.lastStreamReceiveTime = now;
    }

    // Safety net: schedules the unload of every loaded chunk outside the StreamWindow
    private void ForceUnload(){
        if(!this.hasStreamWindow)
            return;

        foreach(ChunkPos pos in this.chunks.Keys){
            if(!this.streamWindow.Contains(pos))
                ScheduleUnload(pos);
        }
    }

    // Passes the AudioListener from the scene to the Character's Camera
    private void LoadAudio(){
        this.audioListenerObject.transform.parent = this.cam.transform;
        this.audioListenerObject.transform.localPosition = Vector3.zero;
        this.audioListenerObject.transform.localRotation = Quaternion.identity;
        this.audioListenerObject.transform.localScale = Vector3.one;
    }
}


public struct Coord3D{
	public int x;
	public int y;
	public int z;

	public Coord3D(int a, int b, int c){
		this.x = a;
		this.y = b;
		this.z = c;
	}

	public Coord3D(Coord3D coord, int x=0, int y=0, int z=0){
		this.x = coord.x + x;
		this.y = coord.y + y;
		this.z = coord.z + z;
	}

	public float Sum(){
		return this.x + this.y + this.z;
	}

	public override string ToString(){
		return "(" + this.x + ", " + this.y + ", " + this.z + ")";
	}

}


public struct ChunkLightPropagInfo{
    public ChunkPos pos;
    public byte propagationFlag;
    public int recursionDepth;

    public ChunkLightPropagInfo(ChunkPos a, byte flag, int recursionDepth){
        this.pos = a;
        this.propagationFlag = flag;
        this.recursionDepth = recursionDepth;
    }
}


// Chunk data received from the Server that wasn't loaded yet
public struct PendingChunkData{
    public byte[] data;
    public int streamCount; // Amount of streamed copies received, that need to be acknowledged

    public PendingChunkData(byte[] data, int streamCount){
        this.data = data;
        this.streamCount = streamCount;
    }
}
