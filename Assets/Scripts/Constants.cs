using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    public static readonly float GRAVITY = 0.98f;
    public static readonly Vector3 GRAVITY_VECTOR = new Vector3(0, -Constants.GRAVITY, 0);
    public static readonly float BLOCK_SKIN = 0.5f;
    public static readonly float PHYSICS_ITEM_DRAG_MULTIPLIER = 0.2f;
}
