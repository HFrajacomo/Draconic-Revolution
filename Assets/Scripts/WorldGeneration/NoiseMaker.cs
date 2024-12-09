using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public static class NoiseMaker
{

    public static float Noise1D(float x, NativeArray<byte> noiseMap)
    {
        int X = Mathf.FloorToInt(x) & 0xff;
        x -= Mathf.Floor(x);
        float u = Fade(x);

        return Lerp(u, Grad(noiseMap[X], x), Grad(noiseMap[X+1], x-1)) * 2;
    }

    public static float PatchNoise1D(float x, NativeArray<byte> noiseMap){
        int X = Mathf.FloorToInt(x) & 0xff;
        x -= Mathf.Floor(x);
        float u = Fade(x);

        return Lerp(u, Grad(noiseMap[X], x), Grad(noiseMap[X+1], x-1));
    }

    public static float PatchNoise1D(float x){
        int X = Mathf.FloorToInt(x) & 0xff;
        x -= Mathf.Floor(x);
        float u = Fade(x);

        return Lerp(u, Grad(GenerationSeed.patchNoise[X], x), Grad(GenerationSeed.patchNoise[X+1], x-1));
    }

    public static float NormalizedWeatherNoise1D(float x){
        int X = Mathf.FloorToInt(x) & 0xff;
        x -= Mathf.Floor(x);
        float u = Fade(x);

        return Normalize(Lerp(u, Grad(GenerationSeed.weatherNoise[X], x), Grad(GenerationSeed.weatherNoise[X+1], x-1)));
    }

    public static float WeatherNoise(float x, float y){
        int X = Mathf.FloorToInt(x) & 0xff;
        int Y = Mathf.FloorToInt(y) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);

        float u = Fade(x);
        float v = Fade(y);

        int A = (GenerationSeed.weatherNoise[X  ] + Y) & 0xff;
        int B = (GenerationSeed.weatherNoise[X+1] + Y) & 0xff;
        return Lerp(v, Lerp(u, Grad(GenerationSeed.weatherNoise[A  ], x, y  ), Grad(GenerationSeed.weatherNoise[B  ], x-1, y  )),
                       Lerp(u, Grad(GenerationSeed.weatherNoise[A+1], x, y-1), Grad(GenerationSeed.weatherNoise[B+1], x-1, y-1)));
    }

    public static float PatchNoise2D(float x, float y, NativeArray<byte> noiseMap){
        int X = Mathf.FloorToInt(x) & 0xff;
        int Y = Mathf.FloorToInt(y) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);

        float u = Fade(x);
        float v = Fade(y);

        int A = (noiseMap[X  ] + Y) & 0xff;
        int B = (noiseMap[X+1] + Y) & 0xff;
        return Lerp(v, Lerp(u, Grad(noiseMap[A  ], x, y  ), Grad(noiseMap[B  ], x-1, y  )),
                       Lerp(u, Grad(noiseMap[A+1], x, y-1), Grad(noiseMap[B+1], x-1, y-1)));
    }

    public static float NormalizedPatchNoise1D(float x)
    {
        int X = Mathf.FloorToInt(x) & 0xff;
        x -= Mathf.Floor(x);
        float u = Fade(x);

        return Normalize(Lerp(u, Grad(GenerationSeed.patchNoise[X], x), Grad(GenerationSeed.patchNoise[X+1], x-1)));
    }

    public static float NormalizedPatchNoise2D(float x, float y){
        int X = Mathf.FloorToInt(x) & 0xff;
        int Y = Mathf.FloorToInt(y) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);

        float u = Fade(x);
        float v = Fade(y);

        int A = (GenerationSeed.patchNoise[X  ] + Y) & 0xff;
        int B = (GenerationSeed.patchNoise[X+1] + Y) & 0xff;
        return Normalize(Lerp(v, Lerp(u, Grad(GenerationSeed.patchNoise[A  ], x, y  ), Grad(GenerationSeed.patchNoise[B  ], x-1, y  )),
                       Lerp(u, Grad(GenerationSeed.patchNoise[A+1], x, y-1), Grad(GenerationSeed.patchNoise[B+1], x-1, y-1))));
    }

    public static float Noise2D(float x, float y, NativeArray<byte> noiseMap)
    {
        int X = Mathf.FloorToInt(x) & 0xff;
        int Y = Mathf.FloorToInt(y) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);

        float u = Fade(x);
        float v = Fade(y);

        int A = (noiseMap[X  ] + Y) & 0xff;
        int B = (noiseMap[X+1] + Y) & 0xff;
        return Lerp(v, Lerp(u, Grad(noiseMap[A  ], x, y  ), Grad(noiseMap[B  ], x-1, y  )),
                       Lerp(u, Grad(noiseMap[A+1], x, y-1), Grad(noiseMap[B+1], x-1, y-1)));
        
    }

    public static float Noise3D(float x, float y, float z, NativeArray<byte> noiseMap)
    {
        int X = Mathf.FloorToInt(x) & 0xff;
        int Y = Mathf.FloorToInt(y) & 0xff;
        int Z = Mathf.FloorToInt(z) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);
        z -= Mathf.Floor(z);
        float u = Fade(x);
        float v = Fade(y);
        float w = Fade(z);
      
        int A  = (noiseMap[X  ] + Y) & 0xff;
        int B  = (noiseMap[X+1] + Y) & 0xff;
        int AA = (noiseMap[A  ] + Z) & 0xff;
        int BA = (noiseMap[B  ] + Z) & 0xff;
        int AB = (noiseMap[A+1] + Z) & 0xff;
        int BB = (noiseMap[B+1] + Z) & 0xff;
        return Lerp(w, Lerp(v, Lerp(u, Grad(noiseMap[AA  ], x, y  , z  ), Grad(noiseMap[BA  ], x-1, y  , z  )),
                               Lerp(u, Grad(noiseMap[AB  ], x, y-1, z  ), Grad(noiseMap[BB  ], x-1, y-1, z  ))),
                       Lerp(v, Lerp(u, Grad(noiseMap[AA+1], x, y  , z-1), Grad(noiseMap[BA+1], x-1, y  , z-1)),
                               Lerp(u, Grad(noiseMap[AB+1], x, y-1, z-1), Grad(noiseMap[BB+1], x-1, y-1, z-1))));
    }

    public static float Noise3DNormalized(float x, float y, float z, NativeArray<byte> noiseMap)
    {
        int X = Mathf.FloorToInt(x) & 0xff;
        int Y = Mathf.FloorToInt(y) & 0xff;
        int Z = Mathf.FloorToInt(z) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);
        z -= Mathf.Floor(z);
        float u = Fade(x);
        float v = Fade(y);
        float w = Fade(z);
      
        int A  = (noiseMap[X  ] + Y) & 0xff;
        int B  = (noiseMap[X+1] + Y) & 0xff;
        int AA = (noiseMap[A  ] + Z) & 0xff;
        int BA = (noiseMap[B  ] + Z) & 0xff;
        int AB = (noiseMap[A+1] + Z) & 0xff;
        int BB = (noiseMap[B+1] + Z) & 0xff;
        return Normalize(Lerp(w, Lerp(v, Lerp(u, Grad(noiseMap[AA  ], x, y  , z  ), Grad(noiseMap[BA  ], x-1, y  , z  )),
                               Lerp(u, Grad(noiseMap[AB  ], x, y-1, z  ), Grad(noiseMap[BB  ], x-1, y-1, z  ))),
                       Lerp(v, Lerp(u, Grad(noiseMap[AA+1], x, y  , z-1), Grad(noiseMap[BA+1], x-1, y  , z-1)),
                               Lerp(u, Grad(noiseMap[AB+1], x, y-1, z-1), Grad(noiseMap[BB+1], x-1, y-1, z-1)))));
    }

    public static float NoiseMask(float x, float y, float z, NativeArray<byte> noiseMap)
    {
        int X = Mathf.FloorToInt(x) & 0xff;
        int Y = Mathf.FloorToInt(y) & 0xff;
        int Z = Mathf.FloorToInt(z) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);
        z -= Mathf.Floor(z);
        float u = Fade(x);
        float v = Fade(y);
        float w = Fade(z);
      
        int A  = (noiseMap[X  ] + Y) & 0xff;
        int B  = (noiseMap[X+1] + Y) & 0xff;
        int AA = (noiseMap[A  ] + Z) & 0xff;
        int BA = (noiseMap[B  ] + Z) & 0xff;
        int AB = (noiseMap[A+1] + Z) & 0xff;
        int BB = (noiseMap[B+1] + Z) & 0xff;
        return Lerp(w, Lerp(v, Lerp(u, Grad(noiseMap[AA  ], x, y  , z  ), Grad(noiseMap[BA  ], x-1, y  , z  )),
                               Lerp(u, Grad(noiseMap[AB  ], x, y-1, z  ), Grad(noiseMap[BB  ], x-1, y-1, z  ))),
                       Lerp(v, Lerp(u, Grad(noiseMap[AA+1], x, y  , z-1), Grad(noiseMap[BA+1], x-1, y  , z-1)),
                               Lerp(u, Grad(noiseMap[AB+1], x, y-1, z-1), Grad(noiseMap[BB+1], x-1, y-1, z-1))));
    }

    public static float FindSplineHeight(float noiseValue, NoiseMap type, ChunkDepthID cdID){
        int index = GenerationSeed.baseNoiseSplineX.Length-2;

        if(type == NoiseMap.BASE){
            if(cdID == ChunkDepthID.SURFACE){
                for(int i=1; i < GenerationSeed.baseNoiseSplineX.Length; i++){
                    if(GenerationSeed.baseNoiseSplineX[i] >= noiseValue){
                        index = i-1;
                        break;
                    }
                }

                float inverseLerp = (noiseValue - GenerationSeed.baseNoiseSplineX[index])/(GenerationSeed.baseNoiseSplineX[index+1] - GenerationSeed.baseNoiseSplineX[index]);

                if(GenerationSeed.baseNoiseSplineY[index] > GenerationSeed.baseNoiseSplineY[index+1])
                    return Mathf.CeilToInt(Mathf.Lerp(GenerationSeed.baseNoiseSplineY[index], GenerationSeed.baseNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2)));
                else
                    return Mathf.CeilToInt(Mathf.Lerp(GenerationSeed.baseNoiseSplineY[index], GenerationSeed.baseNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f)));
            }
            else if(cdID == ChunkDepthID.HELL){
                for(int i=1; i < GenerationSeed.baseHellNoiseSplineX.Length; i++){
                    if(GenerationSeed.baseHellNoiseSplineX[i] >= noiseValue){
                        index = i-1;
                        break;
                    }
                }

                float inverseLerp = (noiseValue - GenerationSeed.baseHellNoiseSplineX[index])/(GenerationSeed.baseHellNoiseSplineX[index+1] - GenerationSeed.baseHellNoiseSplineX[index]);

                if(GenerationSeed.baseHellNoiseSplineY[index] > GenerationSeed.baseHellNoiseSplineY[index+1])
                    return Mathf.CeilToInt(Mathf.Lerp(GenerationSeed.baseHellNoiseSplineY[index], GenerationSeed.baseHellNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2)));
                else
                    return Mathf.CeilToInt(Mathf.Lerp(GenerationSeed.baseHellNoiseSplineY[index], GenerationSeed.baseHellNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f)));                
            }
            else if(cdID == ChunkDepthID.CORE){
                for(int i=1; i < GenerationSeed.baseCoreNoiseSplineX.Length; i++){
                    if(GenerationSeed.baseCoreNoiseSplineX[i] >= noiseValue){
                        index = i-1;
                        break;
                    }
                }

                float inverseLerp = (noiseValue - GenerationSeed.baseCoreNoiseSplineX[index])/(GenerationSeed.baseCoreNoiseSplineX[index+1] - GenerationSeed.baseCoreNoiseSplineX[index]);

                if(GenerationSeed.baseCoreNoiseSplineY[index] > GenerationSeed.baseCoreNoiseSplineY[index+1])
                    return Mathf.CeilToInt(Mathf.Lerp(GenerationSeed.baseCoreNoiseSplineY[index], GenerationSeed.baseCoreNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2)));
                else
                    return Mathf.CeilToInt(Mathf.Lerp(GenerationSeed.baseCoreNoiseSplineY[index], GenerationSeed.baseCoreNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f)));                 
            }
            else
                return 0;
        }
        else if(type == NoiseMap.EROSION){
            if(cdID == ChunkDepthID.SURFACE){
                for(int i=1; i < GenerationSeed.erosionNoiseSplineX.Length; i++){
                    if(GenerationSeed.erosionNoiseSplineX[i] >= noiseValue){
                        index = i-1;
                        break;
                    }
                }

                float inverseLerp = (noiseValue - GenerationSeed.erosionNoiseSplineX[index])/(GenerationSeed.erosionNoiseSplineX[index+1] - GenerationSeed.erosionNoiseSplineX[index]);

                if(GenerationSeed.erosionNoiseSplineY[index] > GenerationSeed.erosionNoiseSplineY[index+1])
                    return Mathf.Lerp(GenerationSeed.erosionNoiseSplineY[index], GenerationSeed.erosionNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2));
                else
                    return Mathf.Lerp(GenerationSeed.erosionNoiseSplineY[index], GenerationSeed.erosionNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f));
            }
            else if(cdID == ChunkDepthID.HELL){
                for(int i=1; i < GenerationSeed.erosionHellNoiseSplineX.Length; i++){
                    if(GenerationSeed.erosionHellNoiseSplineX[i] >= noiseValue){
                        index = i-1;
                        break;
                    }
                }

                float inverseLerp = (noiseValue - GenerationSeed.erosionHellNoiseSplineX[index])/(GenerationSeed.erosionHellNoiseSplineX[index+1] - GenerationSeed.erosionHellNoiseSplineX[index]);

                if(GenerationSeed.erosionHellNoiseSplineY[index] > GenerationSeed.erosionHellNoiseSplineY[index+1])
                    return Mathf.Lerp(GenerationSeed.erosionHellNoiseSplineY[index], GenerationSeed.erosionHellNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2));
                else
                    return Mathf.Lerp(GenerationSeed.erosionHellNoiseSplineY[index], GenerationSeed.erosionHellNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f));
            }
            else if(cdID == ChunkDepthID.CORE){
                for(int i=1; i < GenerationSeed.erosionCoreNoiseSplineX.Length; i++){
                    if(GenerationSeed.erosionCoreNoiseSplineX[i] >= noiseValue){
                        index = i-1;
                        break;
                    }
                }

                float inverseLerp = (noiseValue - GenerationSeed.erosionCoreNoiseSplineX[index])/(GenerationSeed.erosionCoreNoiseSplineX[index+1] - GenerationSeed.erosionCoreNoiseSplineX[index]);

                if(GenerationSeed.erosionCoreNoiseSplineY[index] > GenerationSeed.erosionCoreNoiseSplineY[index+1])
                    return Mathf.Lerp(GenerationSeed.erosionCoreNoiseSplineY[index], GenerationSeed.erosionCoreNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2));
                else
                    return Mathf.Lerp(GenerationSeed.erosionCoreNoiseSplineY[index], GenerationSeed.erosionCoreNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f));                
            }
            else
                return 0f;
        }
        else if(type == NoiseMap.PEAK){
            if(cdID == ChunkDepthID.SURFACE || cdID == ChunkDepthID.UNDERGROUND){
                for(int i=1; i < GenerationSeed.peakNoiseSplineX.Length; i++){
                    if(GenerationSeed.peakNoiseSplineX[i] >= noiseValue){
                        index = i-1;
                        break;
                    }
                }

                float inverseLerp = (noiseValue - GenerationSeed.peakNoiseSplineX[index])/(GenerationSeed.peakNoiseSplineX[index+1] - GenerationSeed.peakNoiseSplineX[index]);

                if(GenerationSeed.peakNoiseSplineY[index] > GenerationSeed.peakNoiseSplineY[index+1])
                    return Mathf.Lerp(GenerationSeed.peakNoiseSplineY[index], GenerationSeed.peakNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2));
                else
                    return Mathf.Lerp(GenerationSeed.peakNoiseSplineY[index], GenerationSeed.peakNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f));
            }
            else if(cdID == ChunkDepthID.HELL){
                for(int i=1; i < GenerationSeed.peakHellNoiseSplineX.Length; i++){
                    if(GenerationSeed.peakHellNoiseSplineX[i] >= noiseValue){
                        index = i-1;
                        break;
                    }
                }

                float inverseLerp = (noiseValue - GenerationSeed.peakHellNoiseSplineX[index])/(GenerationSeed.peakHellNoiseSplineX[index+1] - GenerationSeed.peakHellNoiseSplineX[index]);

                if(GenerationSeed.peakHellNoiseSplineY[index] > GenerationSeed.peakHellNoiseSplineY[index+1])
                    return Mathf.Lerp(GenerationSeed.peakHellNoiseSplineY[index], GenerationSeed.peakHellNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2));
                else
                    return Mathf.Lerp(GenerationSeed.peakHellNoiseSplineY[index], GenerationSeed.peakHellNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f));                
            }
            else if(cdID == ChunkDepthID.CORE){
                for(int i=1; i < GenerationSeed.peakCoreNoiseSplineX.Length; i++){
                    if(GenerationSeed.peakCoreNoiseSplineX[i] >= noiseValue){
                        index = i-1;
                        break;
                    }
                }

                float inverseLerp = (noiseValue - GenerationSeed.peakCoreNoiseSplineX[index])/(GenerationSeed.peakCoreNoiseSplineX[index+1] - GenerationSeed.peakCoreNoiseSplineX[index]);

                if(GenerationSeed.peakCoreNoiseSplineY[index] > GenerationSeed.peakCoreNoiseSplineY[index+1])
                    return Mathf.Lerp(GenerationSeed.peakCoreNoiseSplineY[index], GenerationSeed.peakCoreNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2));
                else
                    return Mathf.Lerp(GenerationSeed.peakCoreNoiseSplineY[index], GenerationSeed.peakCoreNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f));                    
            }
            else
                return 0f;
        }
        else{
            for(int i=1; i < GenerationSeed.baseNoiseSplineX.Length; i++){
                if(GenerationSeed.baseNoiseSplineX[i] >= noiseValue){
                    index = i-1;
                    break;
                }
            }

            float inverseLerp = (noiseValue - GenerationSeed.baseNoiseSplineX[index])/(GenerationSeed.baseNoiseSplineX[index+1] - GenerationSeed.baseNoiseSplineX[index]) ;

            if(GenerationSeed.baseNoiseSplineY[index] > GenerationSeed.baseNoiseSplineY[index+1])
                return Mathf.CeilToInt(Mathf.Lerp(GenerationSeed.baseNoiseSplineY[index], GenerationSeed.baseNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2)));
            else
                return Mathf.CeilToInt(Mathf.Lerp(GenerationSeed.baseNoiseSplineY[index], GenerationSeed.baseNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f)));            
        }
    }

    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private static float Lerp(float t, float a, float b)
    {
        return a + t * (b - a);
    }

    private static float Grad(int hash, float x)
    {
        return (hash & 1) == 0 ? x : -x;
    }

    private static float Grad(int hash, float x, float y)
    {
        return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
    }

    private static float Grad(int hash, float x, float y, float z)
    {
        int h = hash & 15;
        float u = h < 8 ? x : y;
        float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    private static float Normalize(float x){
        return (1 + x)/2;
    }
}
