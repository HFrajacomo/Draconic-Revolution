#ifndef LEAVES_WIND_INCLUDED
#define LEAVES_WIND_INCLUDED

#include "Assets/Scripts/Rendering/ShaderFunctions/WorldOrigin.hlsl"

/*
Phase of the wind wave that travels diagonally over the XZ plane at `speed` world units per second with `wavelength` units between gusts on each axis.
Every term is wrapped to [0, 1) before being combined, which is exact for any 2PI-periodic consumer (sin/cos), so the sway frequency and
precision do not depend on the distance from the world origin nor on how long the session has been running.
worldPos is the Unity position. The floating origin is added as its own wrapped term, so the wave continues seamlessly when the origin is shifted
*/
void LeavesWindPhase_float(float3 worldPos, float time, float speed, float wavelength, out float phase){
	float invWavelength = 1.0 / max(wavelength, 0.001);
	float2 spatial = frac(worldPos.xz * invWavelength) + frac(_WorldOrigin.xz * invWavelength);
	float temporal = frac(time * speed * invWavelength);

	phase = 6.28318530718 * (spatial.x + spatial.y - temporal);
}

#endif
