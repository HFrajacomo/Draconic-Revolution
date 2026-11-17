using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    // Physics
    public static readonly float GRAVITY = 0.98f;
    public static readonly Vector3 GRAVITY_VECTOR = new Vector3(0, -Constants.GRAVITY, 0);
    public static readonly float PHYSICS_MAXIMUM_GRAVITY_SPEED = -.6f;
    public static readonly float PHYSICS_ITEM_DRAG_MULTIPLIER = 0.2f;
    public static readonly float BUOYANCY = 0.3f;
    public static readonly Vector3 BUOYANCY_VECTOR = new Vector3(0, Constants.BUOYANCY, 0);
    public static readonly float PHYSICS_MAXIMUM_BUOYANCY_SPEED = .1f;
    public static readonly float PHYSICS_WATER_RESISTANCE_MULTIPLIER = .97f;

    // Hitboxes
    public static readonly float BLOCK_SKIN = 0.5f;

    // World
    public static readonly float WORLD_COORDINATES_BLOCK_FLOATOFFSET = 0.5f;
    public static readonly int WORLD_WATER_LEVEL = 80;
    public static readonly int WORLD_CLAY_MIN_LEVEL = 50;
    public static readonly int WORLD_CLAY_MAX_LEVEL = 79;
    public static readonly float WORLD_BLOCK_GRID_DISPLACEMENT = 0.5f;

    // Decals
    public static readonly float[] DECAL_STAGE_PERCENTAGE = new float[]{0.2f, 0.4f, 0.6f, 0.8f};
    public static readonly int DECAL_STAGE_SIZE = 4;
    public static readonly float DECAL_OFFSET = 0.001f;

    // Chunk Loading
    public static readonly int CHUNK_LOADING_VERTICAL_CHUNK_DISTANCE = 50;

    // Chunk Streaming (none of these depend on Render Distance)
    public static readonly int CHUNK_STREAM_WINDOW_MIN = 16; // Minimum amount of chunks sent but not yet consumed by a client
    public static readonly int CHUNK_STREAM_WINDOW_MAX = 512;
    public static readonly int CHUNK_STREAM_WINDOW_STEP = 8; // Window growth whenever a client keeps up with the server
    public static readonly int CHUNK_STREAM_GENERATION_LOOKAHEAD = 32; // Maximum amount of chunks a client can have waiting for generation
    public static readonly float SERVER_CHUNK_LOAD_BUDGET_MS = 10f;
    public static readonly int SERVER_GENERATION_BATCH = 4; // Chunks generated in parallel by the Server
    public static readonly float SERVER_CHUNK_SAVE_BUDGET_MS = 4f; // Time spent writing queued chunk saves to disk per tick
    public static readonly int SERVER_MAX_PENDING_READS = 64; // Chunks being read from disk by the ChunkReader thread at the same time
    public static readonly float SERVER_STRUCTURE_DRAIN_BUDGET_MS = 2f; // Time spent saving chunks changed by structures per tick
    public static readonly float SERVER_CHUNK_STREAM_BUDGET_MS = 4f;
    public static readonly float CLIENT_CHUNK_LOAD_BUDGET_MS = 4f;
    public static readonly float CLIENT_CHUNK_UNLOAD_BUDGET_MS = 2f;
    public static readonly float CLIENT_DRAW_FINISH_BUDGET_MS = 4f;
    // Maximum amount of jobs of each kind running at the same time. The actual amounts are limited by the job workers of the process (ChunkLoader.CalculateJobSlots)
    public static readonly int CLIENT_DRAW_BUILD_SLOTS = 3; // Chunk mesh builds of new chunks
    public static readonly int CLIENT_UPDATE_BUILD_SLOTS = 2; // Chunk mesh builds of changed chunks
    public static readonly int CLIENT_LIGHT_SLOTS = 12; // Chunk light calculations
    public static readonly float CLIENT_UNLOAD_ASSETS_INTERVAL_SECONDS = 20f; // Resources.UnloadUnusedAssets
    public static readonly float CLIENT_FORCE_UNLOAD_INTERVAL_SECONDS = 30f; // Unloads chunks left outside the render distance
    public static readonly float CHUNK_RESYNC_IDLE_SECONDS = 2f; // Time without receiving streamed chunks before looking for missing ones
    public static readonly float CHUNK_RESYNC_INTERVAL_SECONDS = 5f; // Pause after a full clean scan of the window
    public static readonly int CHUNK_RESYNC_CHECKS_PER_FRAME = 256;
    public static readonly int CHUNK_RESYNC_MAX_REQUESTS = 32;

    // Networking
    public static readonly int NET_MAX_FRAME_SIZE = 1024*1024; // Frames bigger than this close the connection
    public static readonly int NET_MAX_PENDING_BYTES = 64*1024*1024; // Connections that can't keep up with this much unsent data are closed
    public static readonly int NET_SOCKET_BUFFER_SIZE = 512*1024;
    public static readonly int NET_STREAM_MAX_PENDING_BYTES = 2*1024*1024; // Chunks aren't streamed to a client while it has this much unsent data
    public static readonly float NET_HEARTBEAT_INTERVAL_SECONDS = 1f; // A heartbeat is sent if nothing else was sent for this long
    public static readonly float NET_CLIENT_TIMEOUT_SECONDS = 15f;
    public static readonly float NET_SERVER_TIMEOUT_SECONDS = 15f;
    public static readonly float NET_TEMPORARY_TIMEOUT_SECONDS = 60f; // Connections that never identified themselves
    public static readonly int NET_CLOSE_FLUSH_TIMEOUT_MS = 500; // Time waiting for pending data to be sent before closing a connection
    public static readonly int BLOCKBATCH_MAX_CHANGES = 512; // Changed voxels of a chunk that are sent individually before a full chunk is sent instead
    public static readonly int SERVER_CHUNK_PAYLOAD_CACHE_MB = 64; // Compressed chunks kept ready to be sent
    public static readonly float SERVER_SHEET_FLUSH_SECONDS = 10f; // Interval for saving changed CharacterSheets to disk

    // Persistance
    public static readonly int MAXIMUM_REGION_FILE_POOL = 10;
    public static readonly float CHUNKS_IN_REGION_FILE = 32f;

    // Item Entity
    public static readonly int ITEM_ENTITY_LIFE_SPAN_TICKS = 19200; // 4 in-game hours
    public static readonly float ITEM_ENTITY_SPAWN_HEIGHT_BONUS = .5f;

    // Models
    public static readonly float PLAYER_MODEL_SCALING_FACTOR = .45f;
    public static readonly Vector3 CHARACTER_MODEL_EYE_Y_OFFSET = new Vector3(0, 1, 0);
}
