#ifndef ANIMATED_OBJECTS_INCLUDED
#define ANIMATED_OBJECTS_INCLUDED

/*
Animated Objects (ShaderIndex.ANIMATED_OBJECTS). See ChunkAnimatedObjects and AnimatedObjectTables.
Vertices of the Animated Objects mesh are in model space, with UV2 = (part of the model, slot of the Object).
Their slot places them in the chunk and tells which animation plays, so animations never rebuild the mesh.
time must be the Time node (_TimeParameters.x), so HDRP gives the previous time to the motion vector pass
*/

// Must follow AnimatedSlot.cs
struct AnimatedObjectSlot{
    float4 row0;
    float4 row1;
    float4 row2;
    float4 animation; // x = global animation (-1 = rest pose), y = start time, z = normalized time at start, w = packed light
};

// Must follow AnimatedClipInfo.cs
struct AnimatedObjectClip{
    float4 data; // x = first row, y = frame count, z = part count, w = length
    float4 flags; // x = loop
};

// Set per chunk by ChunkAnimatedObjects
StructuredBuffer<AnimatedObjectSlot> _AnimatedSlots;
// Set by AnimatedObjectTables
StructuredBuffer<AnimatedObjectClip> _AnimatedObjectClips;
StructuredBuffer<float4> _AnimatedObjectFrames;

// Transform of a part at a normalized time of an animation, interpolated between its two closest frames
void SampleAnimatedPart(AnimatedObjectClip animationClip, uint part, float normalized, out float4 r0, out float4 r1, out float4 r2){
    float frameCount = animationClip.data.y;
    float frame = normalized * (frameCount - 1);
    float f0 = floor(frame);
    float f1 = min(f0 + 1, frameCount - 1);
    float blend = frame - f0;
    uint partCount = (uint)animationClip.data.z;
    uint first = (uint)animationClip.data.x;
    uint i0 = first + ((uint)f0 * partCount + part) * 3;
    uint i1 = first + ((uint)f1 * partCount + part) * 3;

    r0 = lerp(_AnimatedObjectFrames[i0], _AnimatedObjectFrames[i1], blend);
    r1 = lerp(_AnimatedObjectFrames[i0 + 1], _AnimatedObjectFrames[i1 + 1], blend);
    r2 = lerp(_AnimatedObjectFrames[i0 + 2], _AnimatedObjectFrames[i1 + 2], blend);
}

/*
Outputs the object space position, normal and tangent of a vertex of the Animated Objects mesh,
and the Natural Light, Extra Light and dampness of its Object with the same values the Light UV (UV3) of Objects in the chunk mesh has
*/
void AnimatedObjectVertex_float(float3 positionOS, float3 normalOS, float3 tangentOS, float2 partSlot, float time,
    out float3 position, out float3 normal, out float3 tangent, out float natural, out float extra, out float dampness){

#ifdef SHADERGRAPH_PREVIEW
    position = positionOS;
    normal = normalOS;
    tangent = tangentOS;
    natural = 15;
    extra = 0;
    dampness = 0;
#else
    AnimatedObjectSlot slot = _AnimatedSlots[(uint)(partSlot.y + 0.5)];
    float3 p = positionOS;
    float3 n = normalOS;
    float3 t = tangentOS;
    int clipIndex = (int)round(slot.animation.x);

    if(clipIndex >= 0){
        AnimatedObjectClip animationClip = _AnimatedObjectClips[clipIndex];
        float normalized = (time - slot.animation.y) / max(animationClip.data.w, 0.0001) + slot.animation.z;
        float4 r0, r1, r2;

        normalized = animationClip.flags.x > 0.5 ? frac(normalized) : saturate(normalized);
        SampleAnimatedPart(animationClip, (uint)(partSlot.x + 0.5), normalized, r0, r1, r2);

        float3x3 part = float3x3(r0.xyz, r1.xyz, r2.xyz);
        p = mul(part, p) + float3(r0.w, r1.w, r2.w);
        n = mul(part, n);
        t = mul(part, t);
    }

    // Normals go through the cofactor matrix (inverse transpose times the determinant), so scaled Objects keep correct normals
    float3x3 m = float3x3(slot.row0.xyz, slot.row1.xyz, slot.row2.xyz);
    float3 a0 = float3(m[0][0], m[1][0], m[2][0]);
    float3 a1 = float3(m[0][1], m[1][1], m[2][1]);
    float3 a2 = float3(m[0][2], m[1][2], m[2][2]);
    float determinant = dot(a0, cross(a1, a2));
    float3 transformedNormal = (n.x * cross(a1, a2) + n.y * cross(a2, a0) + n.z * cross(a0, a1)) * (determinant < 0 ? -1 : 1);
    float3 transformedTangent = mul(m, t);
    float normalLength = length(transformedNormal);
    float tangentLength = length(transformedTangent);

    // Removed slots have an all zero transform, collapsing their vertices until the mesh is rebuilt
    position = mul(m, p) + float3(slot.row0.w, slot.row1.w, slot.row2.w);
    normal = normalLength > 0.000001 ? transformedNormal / normalLength : float3(0, 1, 0);
    tangent = tangentLength > 0.000001 ? transformedTangent / tangentLength : float3(1, 0, 0);

    uint light = (uint)(slot.animation.w + 0.5);
    natural = (float)(light & 0x0F);
    extra = (float)((light >> 4) & 0x0F);
    dampness = (float)((light >> 8) & 0x01);
#endif
}

#endif
