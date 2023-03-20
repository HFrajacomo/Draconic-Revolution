 using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

public static class CubeMeshData {

  public static readonly Vector3[] vertices = {
    new Vector3(1, 1, 1),
    new Vector3(-1, 1, 1),
    new Vector3(-1, -1, 1),
    new Vector3(1, -1, 1),
    new Vector3(-1, 1, -1),
    new Vector3(1, 1, -1),
    new Vector3(1, -1, -1),
    new Vector3(-1, -1, -1)
  };

  public static readonly float3[] floatverts = {
    new float3(1, 1, 1),
    new float3(-1, 1, 1),
    new float3(-1, -1, 1),
    new float3(1, -1, 1),
    new float3(-1, 1, -1),
    new float3(1, 1, -1),
    new float3(1, -1, -1),
    new float3(-1, -1, -1)
  };

  public static readonly float2[] floatUVs = {
    new float2(1, 1),
    new float2(-1, 1),
    new float2(-1, -1),
    new float2(1, -1),
    new float2(-1, 1),
    new float2(1, 1),
    new float2(1, -1),
    new float2(-1, -1),
  };

  public static readonly int[] faceTriangles = new int[]{0,1,2,3,5,0,3,6,4,5,6,7,1,4,7,2,5,4,1,0,3,2,7,6};
  // 3276
  public static readonly Vector2[] faceUVs = {
    new Vector2(1, 1),
    new Vector2(-1, 1),
    new Vector2(-1, -1),
    new Vector2(1, -1),
    new Vector2(-1, 1),
    new Vector2(1, 1),
    new Vector2(1, -1),
    new Vector2(-1, -1),
  };

  public static Vector3[] faceVertices(int dir, float scale, Vector3 pos)
  {
    Vector3[] fv = new Vector3[4];
    for (int i = 0; i < fv.Length; i++)
    {
      fv[i] = (vertices[faceTriangles[dir*4+i]] * scale) + pos;
    }
    return fv;
  }

  public static Vector3[] faceVertices(Direction dir, float scale, Vector3 pos)
  {
    return faceVertices((int)dir, scale, pos);
  }

  public static Vector2[] GetUVs(int dir, float scale, Vector2 pos)
  {
    Vector2[] fuv = new Vector2[4];
    for (int i = 0; i < fuv.Length; i++)
    {
      fuv[i] = (faceUVs[faceTriangles[dir*4+i]] * scale) + pos;
    }
    return fuv;
  }

  // MULTITHREADING SPECIFIC
  public static NativeArray<float2> GetUVs(int dir, float scale, float2 pos)
  {
    float2[] fuv = new float2[4];
    for (int i = 0; i < fuv.Length; i++)
    {
      fuv[i] = (floatUVs[faceTriangles[dir*4+i]] * scale) + pos;
    }
    return new NativeArray<float2>(fuv, Allocator.TempJob);
  }

  public static Vector2[] GetUVs(Direction dir, float scale, Vector2 pos)
  {
    return GetUVs((int)dir, scale, pos);
  }
}