float4 _VOXEL_LIGHT_BUFFER[32];

void CalculateMovingVoxelLight_float(float3 fragPos, out float intensity){
    intensity = 0;

    float dist;
    float3 vectorXYZ;

    for (int i = 0; i < 32; i++){
        vectorXYZ = _VOXEL_LIGHT_BUFFER[i].xyz;

        if(vectorXYZ.y < -10){
            return;
        }

        dist = distance(fragPos, vectorXYZ);

        if (dist < _VOXEL_LIGHT_BUFFER[i].w){
            intensity = clamp(1 - (dist/_VOXEL_LIGHT_BUFFER[i].w), 0, 1);
            return;
        }
    }
}