using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public struct EntityHitbox
{
    public static EntityHitbox BLOCK = new EntityHitbox(new float3(1,1,1), 0f);
    public static EntityHitbox ITEM = new EntityHitbox(new float3(0.5f, 0.1f, 0.5f), 0.1f);
    public static EntityHitbox SAMPLE_PROJECTILE = new EntityHitbox(new float3(0.2f, 0.2f, 0.2f), 0.1f);
    public static EntityHitbox PLAYER = new EntityHitbox(new float3(0.8f, 1.8f, 0.8f), 0.1f); // just Sample

    public float3 hitboxDiameter;
    public float skinWidth;

    public EntityHitbox(float3 hitbox, float skin){
        this.hitboxDiameter = hitbox;
        this.skinWidth = skin;
    }

    public float3 GetDiameter(){
        return this.hitboxDiameter;
    }

    public float GetSkin(){
        return this.skinWidth;
    }
}
