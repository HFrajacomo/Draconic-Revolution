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

    // Persistance
    public static readonly int MAXIMUM_REGION_FILE_POOL = 10;
    public static readonly float CHUNKS_IN_REGION_FILE = 32f;

    // Item Entity
    public static readonly int ITEM_ENTITY_LIFE_SPAN_TICKS = 19200; // 4 in-game hours
    public static readonly float ITEM_ENTITY_SPAWN_HEIGHT_BONUS = .5f;

    // Models
    public static readonly float PLAYER_MODEL_SCALING_FACTOR = .4f;
}
