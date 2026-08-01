#ifndef GETLIGHT_INCLUDED
#define GETLIGHT_INCLUDED

#include "UnityCG.cginc"
#include "AutoLight.cginc"
 
void GetLight_float(out float4 light)
{
#if SHADERGRAPH_PREVIEW
    light = float4(0, 1, 0, 1);
#else
    if (_DirectionalLightCount > 0)
    {
        light = _WorldSpaceLightPos0;
    }
    else
    {
        light = float4(0,0,0,1);
    }
#endif
}

#endif