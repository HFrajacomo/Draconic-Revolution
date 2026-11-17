#ifndef WORLD_ORIGIN_INCLUDED
#define WORLD_ORIGIN_INCLUDED

/*
Client-side Floating Origin (see WorldOrigin.cs)
	Every GameObject is placed at (absolute world position - _WorldOrigin), so Shader Graph's "Absolute World" position is near (0,0,0)
	_WorldOrigin is a multiple of 1024 on X and Z, and zero on Y
	Patterns anchored to the world (noise, voronoi, waves) must use the real world position, or they would jump whenever the origin is shifted
	Lighting must keep using the Unity position, since every light, camera and shader global is in Unity space
*/
float4 _WorldOrigin;

void WorldPatternPosition_float(float3 unityWS, out float3 worldPos){
	worldPos = unityWS + _WorldOrigin.xyz;
}

#endif
