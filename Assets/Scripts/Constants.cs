using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    public static readonly float GRAVITY = 0.98f;
    public static readonly Vector3 GRAVITY_VECTOR = new Vector3(0, -Constants.GRAVITY, 0);

    public static readonly float BLOCK_SKIN = 0.5f;
    public static readonly float PHYSICS_ITEM_DRAG_MULTIPLIER = 0.2f;

    public static readonly float WORLD_COORDINATES_BLOCK_FLOATOFFSET = 0.5f;
    public static readonly int WORLD_WATER_LEVEL = 80;
    public static readonly int WORLD_CLAY_MIN_LEVEL = 50;
    public static readonly int WORLD_CLAY_MAX_LEVEL = 79;

    public static readonly float[] DECAL_STAGE_PERCENTAGE = new float[]{0.2f, 0.4f, 0.6f, 0.8f};
    public static readonly int DECAL_STAGE_SIZE = 4;
    public static readonly float DECAL_OFFSET = 0.001f;
}
