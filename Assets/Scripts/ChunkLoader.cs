using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
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
	public int renderDistance = 0;
	public Dictionary<ChunkPos, Chunk> chunks = new Dictionary<ChunkPos, Chunk>();
	public Transform player;
	public ChunkPos currentChunk;
	public ChunkPos newChunk;
	public ChunkPriorityQueue requestPriorityQueue = new ChunkPriorityQueue();
    public List<byte[]> toLoad = new List<byte[]>();
    public List<ChunkPos> toLoadChunk = new List<ChunkPos>();
    public ChunkPriorityQueue updatePriorityQueue = new ChunkPriorityQueue();
	public List<ChunkPos> toUnload = new List<ChunkPos>();
    public ChunkPriorityQueue drawPriorityQueue = new ChunkPriorityQueue(metric:DistanceMetric.EDGE_LIMITING);
    public ChunkPriorityQueue updateNoLightPriorityQueue = new ChunkPriorityQueue();
    public List<ChunkLightPropagInfo> toCallLightCascade = new List<ChunkLightPropagInfo>();

    // Received from Server
    public float playerX;
    public float playerZ;
    public float playerY;
    public float playerDirX, playerDirY, playerDirZ;
    public ChunkPos playerCurrentChunk;
    
    // Unity Reference
    public VFXLoader vfx;
    public TimeOfDay time;
    public GameObject gameUI;
    public StructureHandler structHandler;
    public Client client;
    public BiomeHandler biomeHandler = new BiomeHandler();
    public PlayerMovement playerMovement;
    public PlayerSheetController playerSheetController;
    public PlayerRaycast playerRaycast;
    public VolumeProfile volume;
    public GameObject mainControllerManager;
    public AudioManager audioManager;
    public SFXLoader sfx;
    public AudioListener playerAudioListener;
    public PlayerPositionHandler playerPositionHandler;
    public VoxelLightHandler voxelLightHandler;

    // Initialization
    public GameObject playerCharacter;
    public PlayerEvents playerEvents;
    public PlayerModelHandler playerModelHandler;

	// Chunk Rendering
	public ChunkRenderer rend;

    // Multithreading Tasks
    private Task unloadTask;
    private Task[] drawTaskPool = new Task[3];
    private Task updateTask;

    // Multithreading Control Lists
    private ConcurrentQueue<ChunkPos> toUnloadFinish = new ConcurrentQueue<ChunkPos>();
    private ConcurrentQueue<ChunkPos> toDrawFinish = new ConcurrentQueue<ChunkPos>();

    // Multithreading Locks
    private object unloadLock;

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

    // Player information
    public ulong playerAccountID = 1;
    private int lastChunkYLayer = 0;


    void Awake(){
        BlockEncyclopediaECS.InitializeNativeStructures();
        VoxelLoader.InitBlockEncyclopediaECS();

        this.playerCharacter.SetActive(false);
        this.playerCharacter.transform.position = new Vector3(0,-999,0);
        this.mainControllerManager.SetActive(false);
        this.gameUI.SetActive(false);
        this.client = new Client(this);
        HandleClientCommunication();
        this.player.position = new Vector3(0,-999,0);
        this.playerAccountID = Configurations.accountID;
        this.time.SetClient(this.client);
        SetAudioManager();
        this.sfx.SetAudioManager(this.audioManager);
        this.playerPositionHandler.SetAudioManager(this.audioManager);
        World.SetGameSceneFlag(true);
        VoxelData.SetChunkLoader(this);
        this.playerModelHandler.BuildModel(PlayerAppearanceData.GetAppearance(), PlayerAppearanceData.GetGender(), true);
    }

    public void Cleanup(bool comesFromClient=false){
        if(!comesFromClient){
            this.message = new NetMessage(NetCode.DISCONNECT);
            this.client.Send(this.message);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Redirector.SetServerClosure();
        this.playerAudioListener.enabled = false;
        this.biomeHandler.Clear();
        this.biomeHandler = null;
        this.structHandler = null;
        this.mainControllerManager = null;
        this.volume = null;
        this.rend = null;
        this.playerEvents = null;
        this.playerModelHandler = null;
        this.time = null;
        this.client = null;
        this.audioManager.Stop(AudioUsecase.MUSIC_CLIP);
        this.audioManager.Destroy();
        ClearAllChunks();

        // Multithreading
        if(this.unloadTask != null)
            this.unloadTask.Wait();

        WaitTaskPool(this.drawTaskPool);

        Destroy(this);
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
            this.renderDistance = World.renderDistance + 1;
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

            this.player.eulerAngles = new Vector3(playerDirX, playerDirY, playerDirZ);

            this.currentChunk = new CastCoord(playerX, playerY, playerZ).GetChunkPos();
            this.playerPositionHandler.Activate();

            this.audioManager.SetPlayerPositionInVoice3DTrack(this.player);
            this.playerAudioListener.enabled = true;

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
                    playerCharacter.SetActive(true);
                    this.mainControllerManager.SetActive(true);
                    this.time.SetPlayer(playerCharacter);
                    this.playerEvents.SetPlayerObject(playerCharacter);
                    this.client.SetRaycast(playerCharacter.GetComponent<PlayerRaycast>());
                    this.client.SetPlayerEvents(this.playerEvents);
                    this.playerPositionHandler.SetChunkLoaderChunkPos();
                }
        	}

            MoveEntities();
            HandleClientCommunication();
            RunTimerFunctions();
            GetChunks(false);

        	UnloadChunk();
            UnloadChunkFinish();
            LoadChunk();
            RequestChunk();
            DrawChunk();
            DrawChunkFinish();
            //UpdateChunk();
        }
    }

    /*
    Registered Events:

    Heartbeart:         30 ticks
    UnloadUnityObjects: 600 ticks
    FixUnloadedChunks:  1200 ticks
    ForceUnload:        1500 ticks
    */
    private void RunTimerFunctions(){
        if(this.timer < 6000)
            this.timer++;
        else
            this.timer = 1;

        // Heartbeat
        if(this.timer % 30 == 0){
            this.client.CheckTimeout();
            this.message = new NetMessage(NetCode.HEARTBEAT);
            this.client.Send(this.message);
        }

        // Garbage Collect Unity Assets
        if(this.timer % 600 == 0){
            Resources.UnloadUnusedAssets();
        }

        // Fix Unloaded Chunks
        if(this.timer % 1200 == 0){
            FixUnloaded();
        }

        // Remove Loaded Chunks outside render distance
        if(this.timer % 1500 == 0){
            ForceUnload();
        }
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
        int queueCount = this.client.queue.Count;

        if(queueCount > 0){
            for(int i=0; i<queueCount; i++){
                this.client.HandleReceivedMessage(this.client.queue[0].GetData());
                this.client.queue.RemoveAt(0);
            }
        }
    }

    // Erases loaded chunks dictionary
    private void ClearAllChunks(){
    	foreach(ChunkPos item in chunks.Keys){
    		chunks[item].Destroy();
            vfx.RemoveChunk(item);
            sfx.RemoveChunkSFX(item);
    	}

        chunks.Clear();
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
    public void AddToUpdate(ChunkPos pos, bool noLight=false){
        if(!noLight){
            updatePriorityQueue.Add(pos);
        }
        else
            updateNoLightPriorityQueue.Add(pos);
    }

    // Asks the Server to send chunk information
    private void RequestChunk(){
    	if(requestPriorityQueue.GetSize() > 0){
            while(requestPriorityQueue.GetSize() > 0 && !SkipNotImplemented(requestPriorityQueue.Peek()))
                requestPriorityQueue.Pop();
            if(requestPriorityQueue.GetSize() == 0)
                return;

            if(chunks.ContainsKey(requestPriorityQueue.Peek())){
                requestPriorityQueue.Pop();
                return;
            }

            // Asks server to hand over chunk info
            this.message = new NetMessage(NetCode.REQUESTCHUNKLOAD);
            this.message.RequestChunkLoad(requestPriorityQueue.Pop());
            this.client.Send(this.message);
        }
    }

    // Loads the chunk into the Chunkloader
    private void LoadChunk(){
        if(toLoad.Count > 0){
            byte[] data = toLoad[0];

            int headerSize = RegionFileHandler.chunkHeaderSize;

            ChunkPos cp = NetDecoder.ReadChunkPos(data, 1);

            // Prevention
            if(this.chunks.ContainsKey(cp)){
                this.chunks[cp].Destroy();
                this.chunks.Remove(cp);
            }

            int blockDataSize = NetDecoder.ReadInt(data, 10);
            int hpDataSize = NetDecoder.ReadInt(data, 14);
            int stateDataSize = NetDecoder.ReadInt(data, 18);

            this.chunks[cp] = new Chunk(cp, this.rend, this);
            this.chunks[cp].biomeName = BiomeHandler.ByteToBiome(data[22]);

            Compression.DecompressBlocksClient(this.chunks[cp], data, initialPos:22+headerSize);
            Compression.DecompressMetadataHPClient(this.chunks[cp], data, initialPos:22+headerSize+blockDataSize);
            Compression.DecompressMetadataStateClient(this.chunks[cp], data, initialPos:22+headerSize+blockDataSize+hpDataSize);

            if(this.vfx.data.ContainsKey(cp))
                this.vfx.RemoveChunk(cp);
                
            this.vfx.NewChunk(cp);

            this.chunks[cp].data.CalculateLightMap(chunks[cp].metadata);

            if(toUnload.Contains(cp))
                toUnload.Remove(cp);

            this.drawPriorityQueue.Add(cp);

            // Vertical chunk update
            this.cachePos = new ChunkPos(cp.x, cp.z, cp.y-1);
            if(this.chunks.ContainsKey(this.cachePos))
                AddToUpdate(this.cachePos);

            toLoad.RemoveAt(0);
            toLoadChunk.RemoveAt(0);
        }
    }

    // Loads the chunk into the Chunkloader

    // Multithreaded call for Unload operation
    private void UnloadChunk(){
        if(this.unloadTask == null || this.unloadTask.IsCompleted){
            #if UNITY_EDITOR
                if(this.unloadTask != null){
                    if(this.unloadTask.Status == TaskStatus.Faulted){
                        Debug.LogException(this.unloadTask.Exception);
                    }
                }
            #endif

            if(this.toUnload.Count > 0){
                ChunkPos pos = this.toUnload[0];
                this.toUnload.RemoveAt(0);
                this.unloadTask = Task.Run(() => UnloadChunkTask(pos));
            }
        }
    }


    // Unloads a chunk per frame from the Unloading Buffer
    private void UnloadChunkTask(ChunkPos pos){
        if(!this.chunks.ContainsKey(pos)){
            return;
        }

        if(!SkipNotImplemented(pos)){
            return;
        }

        if(this.ShouldBeDrawn(pos)){
            return;
        }
        
        this.toUnloadFinish.Enqueue(pos);
    }

    // Main Thread's Unload operation Finisher
    private void UnloadChunkFinish(){
        if(this.toUnloadFinish.Count > 0){

            ChunkPos pos;
            bool successfulDequeueing = false;

            for(int i=0; i < this.toUnloadFinish.Count; i++){
                successfulDequeueing = this.toUnloadFinish.TryDequeue(out pos);

                if(!successfulDequeueing){
                    UnloadUnwanted();
                    continue;
                }

                if(!this.chunks.ContainsKey(pos)){
                    continue;
                }

                this.chunks[pos].Destroy();
                this.vfx.RemoveChunk(pos);
                this.sfx.RemoveChunkSFX(pos);

                NetMessage message = new NetMessage(NetCode.REQUESTCHUNKUNLOAD);
                message.RequestChunkUnload(pos);
                this.client.Send(message);

                this.chunks.Remove(pos);
            }
        }
    }

    // Multithreaded call for Draw operation
    private void DrawChunk(){
        for(int i=0; i < this.drawTaskPool.Length; i++){
            if(this.drawPriorityQueue.GetSize() == 0)
                break;

            if(this.drawTaskPool[i] == null || this.drawTaskPool[i].IsCompleted){
                #if UNITY_EDITOR
                    if(this.drawTaskPool[i] != null){
                        if(this.drawTaskPool[i].Status == TaskStatus.Faulted){
                            Debug.LogException(this.drawTaskPool[i].Exception);
                        }
                    }
                #endif

                if(this.drawPriorityQueue.GetSize() > 0){
                    ChunkPos pos = this.drawPriorityQueue.Pop();

                    if(this.chunks.ContainsKey(pos)){
                        if(!CanBeDrawn(pos)){
                            this.drawPriorityQueue.Add(pos);
                            continue;
                        }
                    }

                    this.drawTaskPool[i] = Task.Run(() => DrawChunkTask(pos));
                }
            }
        }
    }

    // Actually builds the mesh for loaded chunks
    private void DrawChunkTask(ChunkPos pos){
            Mutex mutex = new Mutex();

            // If chunk is still loaded
            if(this.chunks.ContainsKey(pos)){
                CheckLightPropagation(pos);

                mutex.WaitOne();
                this.chunks[pos].BuildChunk(mutex:mutex, load:true);

                if(WORLD_GENERATED)
                    this.vfx.UpdateLights(pos);

                mutex.WaitOne();
                this.toDrawFinish.Enqueue(pos);
                mutex.ReleaseMutex();
            }
    }

    // Main Thread's Draw Chunk finisher
    private void DrawChunkFinish(){
        if(this.toDrawFinish.Count > 0){
            ChunkPos pos;
            bool successfulDequeueing = false;

            for(int i=0; i < this.toDrawFinish.Count; i++){
                successfulDequeueing = this.toDrawFinish.TryDequeue(out pos);

                if(!successfulDequeueing){
                    Debug.Log("Problem when Dequeuing from DrawChunkFinish");
                    break;
                }

                if(!this.chunks.ContainsKey(pos)){
                    break;
                }

                this.chunks[pos].Draw();
            }
        }
    }

    // Reload a chunk in toUpdate
    
    private void UpdateChunk(){
        return; // DEBUG
        // Gets the minimum operational value
        if(updatePriorityQueue.GetSize() > 0){
            ChunkPos cachedPos;

            if(this.chunks.ContainsKey(updatePriorityQueue.Peek())){
                if(CanBeDrawn(updatePriorityQueue.Peek())){
                    cachedPos = updatePriorityQueue.Pop();

                    chunks[cachedPos].data.CalculateLightMap(chunks[cachedPos].metadata);

                    CheckLightPropagation(cachedPos);

                    chunks[cachedPos].BuildChunk();
                    chunks[cachedPos].Draw();

                    if(this.WORLD_GENERATED)
                        this.vfx.UpdateLights(cachedPos);
                }
                else{    
                    updatePriorityQueue.Add(updatePriorityQueue.Pop());
                }
            }
            else{
                updatePriorityQueue.Pop();
            }
        }

        if(updateNoLightPriorityQueue.GetSize() > 0){
            ChunkPos cachedPos;

            if(this.chunks.ContainsKey(updateNoLightPriorityQueue.Peek())){
                if(CanBeDrawn(updateNoLightPriorityQueue.Peek())){
                    cachedPos = updateNoLightPriorityQueue.Pop();

                    chunks[cachedPos].BuildChunk();
                    chunks[cachedPos].Draw();

                    if(this.WORLD_GENERATED)
                        this.vfx.UpdateLights(cachedPos);
                }
                else{
                    updateNoLightPriorityQueue.Add(updateNoLightPriorityQueue.Pop());
                }
            }
            else{
                updateNoLightPriorityQueue.Pop();
            }
        }
    }

    // Checks if neighbor chunks should have light propagated
    // MUST BE USED AFTER THE CalculateLightMap FUNCTION
    // Returns true if should update current chunk and false if not
    public bool CheckLightPropagation(ChunkPos pos, byte flag=255, int recursionDepth=0){
        byte propagationFlag;
        ChunkPos neighbor;
        bool updateCurrent = false;
        ushort updateCode = 0;

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

        // xm
        if((propagationFlag & 1) != 0){
            neighbor = new ChunkPos(pos.x-1, pos.z, pos.y);

            if(this.chunks.ContainsKey(neighbor)){
                updateCode = VoxelData.PropagateLight(this.chunks[pos].data, this.chunks[pos].metadata, this.chunks[neighbor].data, this.chunks[neighbor].metadata, 0);

                if((updateCode & 4) == 4)
                    AddToUpdate(neighbor, noLight:false);
                if(((updateCode & 7) == 2 || (updateCode & 7) == 3) && (updateCode & 4) != 4)
                    AddToUpdate(neighbor, noLight:true);
                if((updateCode & 7) == 1 || (updateCode & 7) == 3)
                    AddToUpdate(pos, noLight:true);
                if(updateCode >= 8)
                    toCallLightCascade.Add(new ChunkLightPropagInfo(neighbor, (byte)(updateCode >> 3), recursionDepth+1));
            }
        }
        // xp
        if((propagationFlag & 2) != 0){
            neighbor = new ChunkPos(pos.x+1, pos.z, pos.y);

            if(this.chunks.ContainsKey(neighbor)){
                updateCode = VoxelData.PropagateLight(this.chunks[pos].data, this.chunks[pos].metadata, this.chunks[neighbor].data, this.chunks[neighbor].metadata, 1);

                if((updateCode & 4) == 4)
                    AddToUpdate(neighbor, noLight:false);
                if(((updateCode & 7) == 2 || (updateCode & 7) == 3) && (updateCode & 4) != 4)
                    AddToUpdate(neighbor, noLight:true);
                if((updateCode & 7) == 1 || (updateCode & 7) == 3)
                    AddToUpdate(pos, noLight:true);
                if(updateCode >= 8)
                    toCallLightCascade.Add(new ChunkLightPropagInfo(neighbor, (byte)(updateCode >> 3), recursionDepth+1));
            }
        }
        // zm
        if((propagationFlag & 4) != 0){
            neighbor = new ChunkPos(pos.x, pos.z-1, pos.y);

            if(this.chunks.ContainsKey(neighbor)){
                updateCode = VoxelData.PropagateLight(this.chunks[pos].data, this.chunks[pos].metadata, this.chunks[neighbor].data, this.chunks[neighbor].metadata, 2);

                if((updateCode & 4) == 4)
                    AddToUpdate(neighbor, noLight:false);
                if(((updateCode & 7) == 2 || (updateCode & 7) == 3) && (updateCode & 4) != 4)
                    AddToUpdate(neighbor, noLight:true);
                if((updateCode & 7) == 1 || (updateCode & 7) == 3)
                    AddToUpdate(pos, noLight:true);
                if(updateCode >= 8)
                    toCallLightCascade.Add(new ChunkLightPropagInfo(neighbor, (byte)(updateCode >> 3), recursionDepth+1));
            }

        }
        // zp
        if((propagationFlag & 8) != 0){
            neighbor = new ChunkPos(pos.x, pos.z+1, pos.y);

            if(this.chunks.ContainsKey(neighbor)){
                updateCode = VoxelData.PropagateLight(this.chunks[pos].data, this.chunks[pos].metadata, this.chunks[neighbor].data, this.chunks[neighbor].metadata, 3);

                if((updateCode & 4) == 4)
                    AddToUpdate(neighbor, noLight:false);
                if(((updateCode & 7) == 2 || (updateCode & 7) == 3) && (updateCode & 4) != 4)
                    AddToUpdate(neighbor, noLight:true);
                if((updateCode & 7) == 1 || (updateCode & 7) == 3)
                    AddToUpdate(pos, noLight:true);
                if(updateCode >= 8)
                    toCallLightCascade.Add(new ChunkLightPropagInfo(neighbor, (byte)(updateCode >> 3), recursionDepth+1));
            }
        }
        // ym
        if((propagationFlag & 16) != 0){
            neighbor = new ChunkPos(pos.x, pos.z, pos.y-1);

            if(this.chunks.ContainsKey(neighbor)){
                updateCode = VoxelData.PropagateLight(this.chunks[pos].data, this.chunks[pos].metadata, this.chunks[neighbor].data, this.chunks[neighbor].metadata, 4);

                if((updateCode & 4) == 4)
                    AddToUpdate(neighbor, noLight:false);
                if(((updateCode & 7) == 2 || (updateCode & 7) == 3) && (updateCode & 4) != 4)
                    AddToUpdate(neighbor, noLight:true);
                if((updateCode & 7) == 1 || (updateCode & 7) == 3)
                    AddToUpdate(pos, noLight:true);
                if(updateCode >= 8)
                    toCallLightCascade.Add(new ChunkLightPropagInfo(neighbor, (byte)(updateCode >> 3), recursionDepth+1));
            }
        }
        // yp
        if((propagationFlag & 32) != 0){
            neighbor = new ChunkPos(pos.x, pos.z, pos.y+1);

            if(this.chunks.ContainsKey(neighbor)){
                updateCode = VoxelData.PropagateLight(this.chunks[pos].data, this.chunks[pos].metadata, this.chunks[neighbor].data, this.chunks[neighbor].metadata, 5);

                if((updateCode & 4) == 4)
                    AddToUpdate(neighbor, noLight:false);
                if(((updateCode & 7) == 2 || (updateCode & 7) == 3) && (updateCode & 4) != 4)
                    AddToUpdate(neighbor, noLight:true);
                if((updateCode & 7) == 1 || (updateCode & 7) == 3)
                    AddToUpdate(pos, noLight:true);
                if(updateCode >= 8)
                    toCallLightCascade.Add(new ChunkLightPropagInfo(neighbor, (byte)(updateCode >> 3), recursionDepth+1));
            }
        }

        while(toCallLightCascade.Count > 0){
            ChunkLightPropagInfo info = toCallLightCascade[0];
            toCallLightCascade.RemoveAt(0);

            CheckLightPropagation(info);
        }

        return updateCurrent;
    }
    private bool CheckLightPropagation(ChunkLightPropagInfo info){
        return CheckLightPropagation(info.pos, info.propagationFlag, info.recursionDepth);
    }


    // Gets all chunks around player's render distance
    // GetChunks automatically rebuilds chunks if reload=True
    public void GetChunks(bool reload){
        int verticalChunkValue = this.playerPositionHandler.GetPlayerVerticalChunk();

        ChunkPos popChunk;
        ChunkPos addChunk;
		newChunk = this.playerPositionHandler.GetCurrentChunk();

    	// Reload all Chunks nearby
    	if(reload){
            this.lastChunkYLayer = 0;
    		ClearAllChunks();
    		requestPriorityQueue.Clear();
    		toUnload.Clear();

            requestPriorityQueue.SetPlayerPosition(newChunk);
            drawPriorityQueue.SetPlayerPosition(newChunk);
            updatePriorityQueue.SetPlayerPosition(newChunk);
            updateNoLightPriorityQueue.SetPlayerPosition(newChunk);
    		
	        for(int x=-renderDistance; x<=renderDistance;x++){
	        	for(int z=-renderDistance; z<=renderDistance;z++){
	        		requestPriorityQueue.Add(new ChunkPos(newChunk.x+x, newChunk.z+z, newChunk.y), initial:true);
	        	}
	        }

            if(verticalChunkValue != 0){
                for(int x=-renderDistance; x<=renderDistance;x++){
                    for(int z=-renderDistance; z<=renderDistance;z++){
                        requestPriorityQueue.Add(new ChunkPos(newChunk.x+x, newChunk.z+z, newChunk.y+verticalChunkValue), initial:true);
                    }
                }                
            }
	        
            this.lastChunkYLayer = verticalChunkValue;
	        this.playerCurrentChunk = newChunk;
	        return;
	    }

        // Delete chunk layer
        if(lastChunkYLayer != 0 && verticalChunkValue == 0){
            for(int x=-renderDistance; x <= renderDistance; x++){
                for(int z=-renderDistance; z <= renderDistance; z++){
                    popChunk = new ChunkPos(newChunk.x+x, newChunk.z+z, newChunk.y+lastChunkYLayer);
                    toUnload.Add(popChunk);
                    requestPriorityQueue.Remove(popChunk);
                }           
            }
        }

        // Adds chunk layer
        if(lastChunkYLayer == 0 && verticalChunkValue != 0){
            for(int x=-renderDistance; x <= renderDistance; x++){
                for(int z=-renderDistance; z <= renderDistance; z++){
                    addChunk = new ChunkPos(newChunk.x+x, newChunk.z+z, newChunk.y+verticalChunkValue);
                    toUnload.Remove(addChunk);
                    requestPriorityQueue.Add(addChunk);
                }
            }            
        }

    	// If didn't move to another chunk
    	if(this.playerCurrentChunk == newChunk){
            this.lastChunkYLayer = verticalChunkValue;
    		return;
    	}

    	int diff = (newChunk - this.playerCurrentChunk).dir();

        requestPriorityQueue.SetPlayerPosition(newChunk);
        drawPriorityQueue.SetPlayerPosition(newChunk);
        updatePriorityQueue.SetPlayerPosition(newChunk);
        updateNoLightPriorityQueue.SetPlayerPosition(newChunk);

        this.playerCurrentChunk = newChunk;

    	if(diff == 0){ // East
    		for(int i=-renderDistance; i <=renderDistance;i++){
    			popChunk = new ChunkPos(newChunk.x-renderDistance-1, newChunk.z+i, newChunk.y);
    			toUnload.Add(popChunk);
                requestPriorityQueue.Remove(popChunk);
    			addChunk = new ChunkPos(newChunk.x+renderDistance, newChunk.z+i, newChunk.y);
    			requestPriorityQueue.Add(addChunk);

                if(verticalChunkValue != 0){
                    popChunk = new ChunkPos(newChunk.x-renderDistance-1, newChunk.z+i, newChunk.y+verticalChunkValue);
                    toUnload.Add(popChunk);
                    requestPriorityQueue.Remove(popChunk);
                    addChunk = new ChunkPos(newChunk.x+renderDistance, newChunk.z+i, newChunk.y+verticalChunkValue);
                    requestPriorityQueue.Add(addChunk);                    
                }
    		}
    	}
    	else if(diff == 2){ // West
    		for(int i=-renderDistance; i <=renderDistance;i++){
    			popChunk = new ChunkPos(newChunk.x+renderDistance+1, newChunk.z+i, newChunk.y);
    			toUnload.Add(popChunk);
                requestPriorityQueue.Remove(popChunk);
    			addChunk = new ChunkPos(newChunk.x-renderDistance, newChunk.z+i, newChunk.y);
    			requestPriorityQueue.Add(addChunk);

                if(verticalChunkValue != 0){
                    popChunk = new ChunkPos(newChunk.x+renderDistance+1, newChunk.z+i, newChunk.y+verticalChunkValue);
                    toUnload.Add(popChunk);
                    requestPriorityQueue.Remove(popChunk);
                    addChunk = new ChunkPos(newChunk.x-renderDistance, newChunk.z+i, newChunk.y+verticalChunkValue);
                    requestPriorityQueue.Add(addChunk);                 
                }
    		}
    	}
    	else if(diff == 1){ // South
    		for(int i=-renderDistance; i <=renderDistance;i++){
    			popChunk = new ChunkPos(newChunk.x+i, newChunk.z+renderDistance+1, newChunk.y);
    			toUnload.Add(popChunk);
                requestPriorityQueue.Remove(popChunk);
     			addChunk = new ChunkPos(newChunk.x+i, newChunk.z-renderDistance, newChunk.y);
    			requestPriorityQueue.Add(addChunk);

                if(verticalChunkValue != 0){
                    popChunk = new ChunkPos(newChunk.x+i, newChunk.z+renderDistance+1, newChunk.y+verticalChunkValue);
                    toUnload.Add(popChunk);
                    requestPriorityQueue.Remove(popChunk);
                    addChunk = new ChunkPos(newChunk.x+i, newChunk.z-renderDistance, newChunk.y+verticalChunkValue);
                    requestPriorityQueue.Add(addChunk);                  
                }
      		}
    	}
    	else if(diff == 3){ // North
    		for(int i=-renderDistance; i <=renderDistance;i++){
    			popChunk = new ChunkPos(newChunk.x+i, newChunk.z-renderDistance-1, newChunk.y);
    			toUnload.Add(popChunk);
                requestPriorityQueue.Remove(popChunk);
      			addChunk = new ChunkPos(newChunk.x+i, newChunk.z+renderDistance, newChunk.y);
    			requestPriorityQueue.Add(addChunk);

                if(verticalChunkValue != 0){
                    popChunk = new ChunkPos(newChunk.x+i, newChunk.z-renderDistance-1, newChunk.y+verticalChunkValue);
                    toUnload.Add(popChunk);
                    requestPriorityQueue.Remove(popChunk);
                    addChunk = new ChunkPos(newChunk.x+i, newChunk.z+renderDistance, newChunk.y+verticalChunkValue);
                    requestPriorityQueue.Add(addChunk);               
                }
       		}	
    	}
    	else if(diff == 5){ // Southeast
    		for(int i=-renderDistance; i <= renderDistance; i++){
    			popChunk = new ChunkPos(currentChunk.x-renderDistance, currentChunk.z+i, newChunk.y);
    			toUnload.Add(popChunk);
                requestPriorityQueue.Remove(popChunk);
    			addChunk = new ChunkPos(newChunk.x+renderDistance, newChunk.z+i, newChunk.y);
       			requestPriorityQueue.Add(addChunk);

                if(verticalChunkValue != 0){
                    popChunk = new ChunkPos(currentChunk.x-renderDistance, currentChunk.z+i, newChunk.y+verticalChunkValue);
                    toUnload.Add(popChunk);
                    requestPriorityQueue.Remove(popChunk);
                    addChunk = new ChunkPos(newChunk.x+renderDistance, newChunk.z+i, newChunk.y+verticalChunkValue);
                    requestPriorityQueue.Add(addChunk);             
                }
    		}
    		for(int i=-renderDistance+1; i < renderDistance; i++){
    			popChunk = new ChunkPos(currentChunk.x+i, currentChunk.z+renderDistance, newChunk.y);
    			toUnload.Add(popChunk);
                requestPriorityQueue.Remove(popChunk);
     			addChunk = new ChunkPos(newChunk.x+i-1, newChunk.z-renderDistance, newChunk.y);
    			requestPriorityQueue.Add(addChunk);

                if(verticalChunkValue != 0){
                    popChunk = new ChunkPos(currentChunk.x+i, currentChunk.z+renderDistance, newChunk.y+verticalChunkValue);
                    toUnload.Add(popChunk);
                    requestPriorityQueue.Remove(popChunk);
                    addChunk = new ChunkPos(newChunk.x+i-1, newChunk.z-renderDistance, newChunk.y+verticalChunkValue);
                    requestPriorityQueue.Add(addChunk);           
                }
    		}
    	}
    	else if(diff == 6){ // Southwest
    		for(int i=-renderDistance; i <= renderDistance; i++){
    			popChunk = new ChunkPos(currentChunk.x+renderDistance, currentChunk.z+i, newChunk.y);
    			toUnload.Add(popChunk);
                requestPriorityQueue.Remove(popChunk);
    			addChunk = new ChunkPos(newChunk.x-renderDistance, newChunk.z+i, newChunk.y);
    			requestPriorityQueue.Add(addChunk);

                if(verticalChunkValue != 0){
                    popChunk = new ChunkPos(currentChunk.x+renderDistance, currentChunk.z+i, newChunk.y+verticalChunkValue);
                    toUnload.Add(popChunk);
                    requestPriorityQueue.Remove(popChunk);
                    addChunk = new ChunkPos(newChunk.x-renderDistance, newChunk.z+i, newChunk.y+verticalChunkValue);
                    requestPriorityQueue.Add(addChunk);          
                }
    		}
    		for(int i=-renderDistance; i < renderDistance; i++){
    			popChunk = new ChunkPos(currentChunk.x+i, currentChunk.z+renderDistance, newChunk.y);
    			toUnload.Add(popChunk);
                requestPriorityQueue.Remove(popChunk);
     			addChunk = new ChunkPos(newChunk.x+i+1, newChunk.z-renderDistance, newChunk.y);
    			requestPriorityQueue.Add(addChunk);

                if(verticalChunkValue != 0){
                    popChunk = new ChunkPos(currentChunk.x+i, currentChunk.z+renderDistance, newChunk.y+verticalChunkValue);
                    toUnload.Add(popChunk);
                    requestPriorityQueue.Remove(popChunk);
                    addChunk = new ChunkPos(newChunk.x+i+1, newChunk.z-renderDistance, newChunk.y+verticalChunkValue);
                    requestPriorityQueue.Add(addChunk);        
                }
    		}
    	}
    	else if(diff == 7){ // Northwest
    		for(int i=-renderDistance; i <= renderDistance; i++){
    			popChunk = new ChunkPos(currentChunk.x+renderDistance, currentChunk.z+i, newChunk.y);
    			toUnload.Add(popChunk);
                requestPriorityQueue.Remove(popChunk);
    			addChunk = new ChunkPos(newChunk.x-renderDistance, newChunk.z+i, newChunk.y);
    			requestPriorityQueue.Add(addChunk);

                if(verticalChunkValue != 0){
                    popChunk = new ChunkPos(currentChunk.x+renderDistance, currentChunk.z+i, newChunk.y+verticalChunkValue);
                    toUnload.Add(popChunk);
                    requestPriorityQueue.Remove(popChunk);
                    addChunk = new ChunkPos(newChunk.x-renderDistance, newChunk.z+i, newChunk.y+verticalChunkValue);
                    requestPriorityQueue.Add(addChunk);       
                }
    		}
    		for(int i=-renderDistance; i < renderDistance; i++){
    			popChunk = new ChunkPos(currentChunk.x+i, currentChunk.z-renderDistance, newChunk.y);
    			toUnload.Add(popChunk);
                requestPriorityQueue.Remove(popChunk);
     			addChunk = new ChunkPos(newChunk.x+i+1, newChunk.z+renderDistance, newChunk.y);
    			requestPriorityQueue.Add(addChunk);

                if(verticalChunkValue != 0){
                    popChunk = new ChunkPos(currentChunk.x+i, currentChunk.z-renderDistance, newChunk.y+verticalChunkValue);
                    toUnload.Add(popChunk);
                    requestPriorityQueue.Remove(popChunk);
                    addChunk = new ChunkPos(newChunk.x+i+1, newChunk.z+renderDistance, newChunk.y+verticalChunkValue);
                    requestPriorityQueue.Add(addChunk);     
                }
    		}
    	}
    	else if(diff == 4){ // Northeast
    		for(int i=-renderDistance; i <= renderDistance; i++){
    			popChunk = new ChunkPos(currentChunk.x-renderDistance, currentChunk.z+i, newChunk.y);
    			toUnload.Add(popChunk);
                requestPriorityQueue.Remove(popChunk);
    			addChunk = new ChunkPos(newChunk.x+renderDistance, newChunk.z+i, newChunk.y);
    			requestPriorityQueue.Add(addChunk);

                if(verticalChunkValue != 0){
                    popChunk = new ChunkPos(currentChunk.x-renderDistance, currentChunk.z+i, newChunk.y+verticalChunkValue);
                    toUnload.Add(popChunk);
                    requestPriorityQueue.Remove(popChunk);
                    addChunk = new ChunkPos(newChunk.x+renderDistance, newChunk.z+i, newChunk.y+verticalChunkValue);
                    requestPriorityQueue.Add(addChunk);    
                }
    		}
    		for(int i=-renderDistance; i < renderDistance; i++){
    			popChunk = new ChunkPos(currentChunk.x+i+1, currentChunk.z-renderDistance, newChunk.y);
    			toUnload.Add(popChunk);
                requestPriorityQueue.Remove(popChunk);
     			addChunk = new ChunkPos(newChunk.x+i, newChunk.z+renderDistance, newChunk.y);
    			requestPriorityQueue.Add(addChunk);

                if(verticalChunkValue != 0){
                    popChunk = new ChunkPos(currentChunk.x+i+1, currentChunk.z-renderDistance, newChunk.y+verticalChunkValue);
                    toUnload.Add(popChunk);
                    requestPriorityQueue.Remove(popChunk);
                    addChunk = new ChunkPos(newChunk.x+i, newChunk.z+renderDistance, newChunk.y+verticalChunkValue);
                    requestPriorityQueue.Add(addChunk); 
                }
    		}
    	}

        this.lastChunkYLayer = verticalChunkValue;
	    currentChunk = newChunk;
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

    // Returns the heightmap value of a generated chunk in block position
    private int GetBlockHeight(ChunkPos pos, int blockX, int blockZ){
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

    // Attaches the AudioManager that has the "DontDestroyOnLoad" flag on
    private void SetAudioManager(){
        this.audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
    }

    // Re-acquires every chunk that is possibly not loaded and adds all chunks that need redraw (except borders) to redraw list
    private void FixUnloaded(){
        ChunkPos newChunk;
        int chunkLayer = this.playerPositionHandler.GetPlayerVerticalChunk();
        int actualRenderDistance = this.renderDistance - 1;
        this.message = new NetMessage(NetCode.REQUESTCHUNKLOAD);

        for(int x=-actualRenderDistance; x < actualRenderDistance; x++){
            for(int z=actualRenderDistance; z < actualRenderDistance; z++){
                newChunk = new ChunkPos(this.currentChunk.x+x, this.currentChunk.z+z, this.currentChunk.y);

                if(!SkipNotImplemented(newChunk))
                    continue;

                if(!this.chunks.ContainsKey(newChunk) && !this.toLoadChunk.Contains(newChunk) && !this.requestPriorityQueue.Contains(newChunk) && !this.drawPriorityQueue.Contains(newChunk)){
                    this.message.RequestChunkLoad(newChunk);
                    client.Send(this.message);
                    continue;
                }

                if(this.chunks.ContainsKey(newChunk)){
                    if(!this.chunks[newChunk].drawMain)
                        this.drawPriorityQueue.Add(newChunk);
                }

                // If there's vertical chunks as well
                if(chunkLayer != 0){
                    newChunk = new ChunkPos(this.currentChunk.x+x, this.currentChunk.z+z, this.currentChunk.y+chunkLayer);

                    if(!SkipNotImplemented(newChunk))
                        continue;

                    if(!this.chunks.ContainsKey(newChunk) && !this.toLoadChunk.Contains(newChunk) && !this.requestPriorityQueue.Contains(newChunk) && !this.drawPriorityQueue.Contains(newChunk)){
                        this.message.RequestChunkLoad(newChunk);
                        client.Send(this.message);
                        continue;
                    }

                    if(this.chunks.ContainsKey(newChunk)){
                        if(!this.chunks[newChunk].drawMain)
                            this.drawPriorityQueue.Add(newChunk);
                    }                    
                }
            }
        }
    }

    // Unloads all chunks that are loaded but shouldn't
    private void UnloadUnwanted(){
        foreach(ChunkPos pos in this.chunks.Keys){
            if(!ShouldBeDrawn(pos)){
                this.toUnload.Add(pos);
            }
        }
    }

    // Returns whether a ChunkPos is within Render Distance
    private bool ShouldBeDrawn(ChunkPos pos){
        ChunkPos playerChunk = this.playerPositionHandler.GetCurrentChunk();

        if(pos.x >= playerChunk.x - (this.renderDistance-1) && pos.x <= playerChunk.x + (this.renderDistance-1)){
            if(pos.z >= playerChunk.z - (this.renderDistance-1) && pos.z <= playerChunk.z + (this.renderDistance-1)){
                if(pos.y == playerChunk.y)
                    return true;
                else if(pos.y == playerChunk.y + this.playerPositionHandler.GetPlayerVerticalChunk())
                    return true;
            }
        }
        return false;
    }

    // Returns false if chunk.y is not implemented yet
    private bool SkipNotImplemented(ChunkPos pos){
        return pos.y >= 0 && pos.y <= Chunk.chunkMaxY;
    }

    // Goes through all Chunks and checks if they should've been deleted already
    private void ForceUnload(){
        ChunkPos currentChunk = this.playerPositionHandler.GetCurrentChunk();

        foreach(ChunkPos pos in this.chunks.Keys){
            if(Mathf.Abs(pos.x - currentChunk.x) > renderDistance){
                toUnload.Add(pos);
            }

            if(Mathf.Abs(pos.z - currentChunk.z) > renderDistance){
                toUnload.Add(pos);
            }
        }
    }

    // Waits the end of execution of multiple Task Pools
    private void WaitTaskPool(Task[] taskPool){
        for(int i=0; i < taskPool.Length; i++){
            if(taskPool[i] == null)
                continue;

            taskPool[i].Wait();
        }
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
