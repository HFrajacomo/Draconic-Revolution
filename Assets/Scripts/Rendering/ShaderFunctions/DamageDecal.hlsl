#ifndef DAMAGE_DECAL_INCLUDED
#define DAMAGE_DECAL_INCLUDED

/*
Damage decals of Objects, the cracks block faces get from the chunk decal mesh. See DamageDecal.cs
Uses the same texture as the block decals (Resources/decalTest.png, one stage per horizontal slice) and their opacity,
set as globals by ShaderLoader.

The decal is projected on the voxel grid by the main axis of the surface normal, so a face the size of a voxel gets
the same cracks a block face gets, and any Object shape gets cracks on its real surface.
Positions are relative to the chunk, where voxels are centered on integer coordinates (faces at +-0.5),
so they stay exact far from the world origin
*/

TEXTURE2D(_DamageDecalTexture);
float _DamageDecalOpacity;
float _DamageDecalStages;

/*
Blends the cracks of a damage stage over the albedo
voxelPosition and voxelNormal are relative to the chunk. decalCode is 0 when undamaged or the decal stage + 1
*/
void DamageDecal_float(float3 albedo, float3 voxelPosition, float3 voxelNormal, float decalCode, out float3 color){
    color = albedo;

#ifndef SHADERGRAPH_PREVIEW
    if(decalCode < 0.5)
        return;

    float stages = max(_DamageDecalStages, 1);
    float stage = clamp(round(decalCode) - 1, 0, stages - 1);
    float3 n = abs(voxelNormal);
    float3 p = voxelPosition + 0.5;
    float2 uv;

    // Hard pick of the projection axis, so pixel art stays crisp
    if(n.x >= n.y && n.x >= n.z)
        uv = p.zy;
    else if(n.y >= n.z)
        uv = p.xz;
    else
        uv = p.xy;

    uv = frac(uv);

    float4 decal = SAMPLE_TEXTURE2D_LOD(_DamageDecalTexture, s_point_clamp_sampler, float2((stage + uv.x) / stages, uv.y), 0);
    color = lerp(albedo, decal.rgb, decal.a * _DamageDecalOpacity);
#endif
}

// Static Objects of the chunk mesh: the decal code comes from the Light UV (UV3), w = 1 + decal code
void DamageDecalUV_float(float3 albedo, float4 lightUV, float3 positionOS, float3 normalOS, out float3 color){
    DamageDecal_float(albedo, positionOS, normalOS, lightUV.w - 1, color);
}

#endif
