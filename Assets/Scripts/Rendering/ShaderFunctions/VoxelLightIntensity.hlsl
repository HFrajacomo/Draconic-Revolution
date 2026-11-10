#ifndef VOXEL_LIGHT_INTENSITY_INCLUDED
#define VOXEL_LIGHT_INTENSITY_INCLUDED

// Set by VoxelLightHandler. xyz = position, w = radius. An element with y < -10 ends the list
float4 _VOXEL_LIGHT_BUFFER[32];

// Set by ShaderLoader and AmbientHandler
float _VoxelLightFalloff;
float _VoxelLightFloor;
float _ExtraLightIntensity;
// Set by AmbientHandler. Brightness (nits) of Dark Adaptation at the camera, and xyz = camera position, w = radius
float _DarkAdaptation;
float4 _DarkAdaptationCenter;

static const float VOXEL_LIGHT_DEFAULT_FALLOFF = 0.8;
static const float DARK_ADAPTATION_EDGE_ON = 0.35;

// Returns the strongest moving light (held torches, spells...) reaching fragPos, normalized to [0,1]
float GetMovingVoxelLight(float3 fragPos){
    float intensity = 0;
    float dist;

    for (int i = 0; i < 32; i++){
        if(_VOXEL_LIGHT_BUFFER[i].y < -10){
            break;
        }

        dist = distance(fragPos, _VOXEL_LIGHT_BUFFER[i].xyz);

        if (dist < _VOXEL_LIGHT_BUFFER[i].w){
            intensity = max(intensity, 1 - (dist/_VOXEL_LIGHT_BUFFER[i].w));
        }
    }

    return saturate(intensity);
}

// Maps a normalized light level (level/15) to brightness. Every level below 15 multiplies the light by the falloff,
// remapped so that level 15 is 1 and level 0 is 0
float VoxelLightCurve(float level){
    float falloff = _VoxelLightFalloff > 0 ? _VoxelLightFalloff : VOXEL_LIGHT_DEFAULT_FALLOFF;
    float minimum = pow(falloff, 15);

    return saturate((pow(falloff, 15 * (1 - saturate(level))) - minimum) / (1 - minimum));
}

void CalculateMovingVoxelLight_float(float3 fragPos, out float intensity){
    intensity = GetMovingVoxelLight(fragPos);
}

/*
Dark Adaptation: inside caves, unlit surfaces near the camera glow very faintly, like eyes getting used to the dark.
Fades to black at _DarkAdaptationCenter.w blocks, and faces pointing at the camera get more than faces seen edge-on,
so the floor and block edges read as a dark outline instead of a flat wash. Fragment stage only (uses derivatives)
*/
float GetDarkAdaptation(float3 fragPos){
    if(_DarkAdaptation <= 0 || _DarkAdaptationCenter.w <= 0)
        return 0;

    float3 toCamera = _DarkAdaptationCenter.xyz - fragPos;
    float dist = length(toCamera);
    float fade = saturate(1 - dist / _DarkAdaptationCenter.w);
    float3 faceNormal = normalize(cross(ddy(fragPos), ddx(fragPos)));
    float facing = abs(dot(faceNormal, toCamera / max(dist, 0.001)));

    return _DarkAdaptation * fade * fade * lerp(DARK_ADAPTATION_EDGE_ON, 1, facing);
}

// natural and extra are normalized light levels (level/15)
// albedoMask multiplies the Base Color, so realtime lights still shade the surface where there is voxel light
// extraEmission is the brightness given by Extra Lights (light blocks and moving lights), independent of the sky's ambient light.
// It only scales the surface's own color. The color of a light comes from the realtime Point Light of its source
// skyOcclusion goes into Occlusion: natural light is how much of the sky a voxel sees, so sky ambient and reflections fade without it
// An unlit voxel keeps _VoxelLightFloor of the sky ambient (albedoMask * skyOcclusion), so it is near-black instead of black
// Inside caves, unlit surfaces near the camera also get Dark Adaptation, so a very dark outline of the cave is visible
void VoxelLightShade_float(float natural, float extra, float3 fragPos, float3 albedo, out float albedoMask, out float3 extraEmission, out float skyOcclusion){
    float extraLight = VoxelLightCurve(max(extra, GetMovingVoxelLight(fragPos)));
    float naturalLight = VoxelLightCurve(natural);
    float floorRoot = sqrt(saturate(_VoxelLightFloor));
    float darkness = 1 - max(naturalLight, extraLight);

    albedoMask = lerp(floorRoot, 1, max(naturalLight, extraLight));
    skyOcclusion = lerp(floorRoot, 1, naturalLight);
    extraEmission = albedo * (extraLight * _ExtraLightIntensity + darkness * GetDarkAdaptation(fragPos));
}

// Ground Emission: up-facing blocks glow by their natural light (multiplied by _SkyLightMultiplier in the graph).
// Goes through the same curve, so dim cave floors barely glow
void VoxelLightGround_float(float natural, out float groundLight){
    groundLight = VoxelLightCurve(natural);
}
// Same as VoxelLightShade, reading the raw Light UV of chunk meshes (x = natural level, y = extra level)
void VoxelLightShadeUV_float(float4 lightUV, float3 fragPos, float3 albedo, out float albedoMask, out float3 extraEmission, out float skyOcclusion){
    VoxelLightShade_float(lightUV.x / 15, (lightUV.y - 1) / 15, fragPos, albedo, albedoMask, extraEmission, skyOcclusion);
}

#endif