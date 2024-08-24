float4 _AAAAA[32];

void CheckDistance_float(float3 fragPos, float4 redColor, out float4 color){
    color = float4(0, 0, 0, 0);

    float dist;
    float3 vectorXYZ;

    for (int i = 0; i < 32; i++){
        vectorXYZ = _AAAAA[i].xyz;

        if(vectorXYZ.y < -10){
            return;
        }

        dist = distance(fragPos, vectorXYZ);

        if (dist < 8.0){
            color = redColor;
            return;
        }
    }
}