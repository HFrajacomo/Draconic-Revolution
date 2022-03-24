//
// Perlin noise generator for Unity
// Keijiro Takahashi, 2013, 2015
// https://github.com/keijiro/PerlinNoise
//
// Based on the original implementation by Ken Perlin
// http://mrl.nyu.edu/~perlin/noise/
//
using UnityEngine;
using System.IO;

public static class Perlin
{
    /*
    Testing purposes only
    */
    /*
    public static void FillImage(){
        Texture2D noiseImage = new Texture2D(512, 512);
        float noise;
        Color c;
        for(int x = 0; x < 512; x++){
            for(int z = 0; z < 512; z++){
                noise = Noise(x*0.01f, z*0.01f, NoiseMap.BASE);
                noise = FindSplineHeight(noise);
                c = new Color(noise/256f, noise/256f, noise/256f);
                noiseImage.SetPixel(x, z, c);
            }
        }

        noiseImage.Apply();

        File.WriteAllBytes("noise.png", ImageConversion.EncodeToPNG(noiseImage));
    }
    */

    private static float Normalize(float val){
        return (val+1f)/2f;
    }

    /*
    For Debbuging purposes
    */
    /*
    private static int FindSplineHeight(float noiseValue){
        int index = World.baseNoiseSplineX.Length-2;
        
        for(int i=1; i < World.baseNoiseSplineX.Length; i++){
            if(World.baseNoiseSplineX[i] >= noiseValue){
                index = i-1;
                break;
            }
        }

        float inverseLerp = (noiseValue - World.baseNoiseSplineX[index])/(World.baseNoiseSplineX[index+1] - World.baseNoiseSplineX[index]);

        if(World.baseNoiseSplineY[index] > World.baseNoiseSplineY[index+1])
            return Mathf.CeilToInt(Mathf.Lerp(World.baseNoiseSplineY[index], World.baseNoiseSplineY[index+1], Mathf.Pow(Mathf.Abs(inverseLerp), 2)));
        else
            return Mathf.CeilToInt(Mathf.Lerp(World.baseNoiseSplineY[index], World.baseNoiseSplineY[index+1], Mathf.Pow(inverseLerp, 0.8f)));
    }
    */

    #region Noise functions

    public static float Noise(float x, NoiseMap type)
    {
        int X = Mathf.FloorToInt(x) & 0xff;
        x -= Mathf.Floor(x);
        float u = Fade(x);

        if(type == NoiseMap.BASE)
            return Lerp(u, Grad(World.baseNoise[X], x), Grad(World.baseNoise[X+1], x-1)) * 2;
        else
            return Lerp(u, Grad(World.baseNoise[X], x), Grad(World.baseNoise[X+1], x-1)) * 2;
    }

    public static float Noise(float x, float y, NoiseMap type)
    {
        int X = Mathf.FloorToInt(x) & 0xff;
        int Y = Mathf.FloorToInt(y) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);
        float u = Fade(x);
        float v = Fade(y);

        if(type == NoiseMap.BASE){
            int A = (World.baseNoise[X  ] + Y) & 0xff;
            int B = (World.baseNoise[X+1] + Y) & 0xff;
            return Lerp(v, Lerp(u, Grad(World.baseNoise[A  ], x, y  ), Grad(World.baseNoise[B  ], x-1, y  )),
                           Lerp(u, Grad(World.baseNoise[A+1], x, y-1), Grad(World.baseNoise[B+1], x-1, y-1)));
        }
        else{
            int A = (World.baseNoise[X  ] + Y) & 0xff;
            int B = (World.baseNoise[X+1] + Y) & 0xff;
            return Lerp(v, Lerp(u, Grad(World.baseNoise[A  ], x, y  ), Grad(World.baseNoise[B  ], x-1, y  )),
                           Lerp(u, Grad(World.baseNoise[A+1], x, y-1), Grad(World.baseNoise[B+1], x-1, y-1)));
        }
    }

    public static float Noise(float x, float y, float z, NoiseMap type)
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

        if(type == NoiseMap.BASE){        
            int A  = (World.baseNoise[X  ] + Y) & 0xff;
            int B  = (World.baseNoise[X+1] + Y) & 0xff;
            int AA = (World.baseNoise[A  ] + Z) & 0xff;
            int BA = (World.baseNoise[B  ] + Z) & 0xff;
            int AB = (World.baseNoise[A+1] + Z) & 0xff;
            int BB = (World.baseNoise[B+1] + Z) & 0xff;
            return Lerp(w, Lerp(v, Lerp(u, Grad(World.baseNoise[AA  ], x, y  , z  ), Grad(World.baseNoise[BA  ], x-1, y  , z  )),
                                   Lerp(u, Grad(World.baseNoise[AB  ], x, y-1, z  ), Grad(World.baseNoise[BB  ], x-1, y-1, z  ))),
                           Lerp(v, Lerp(u, Grad(World.baseNoise[AA+1], x, y  , z-1), Grad(World.baseNoise[BA+1], x-1, y  , z-1)),
                                   Lerp(u, Grad(World.baseNoise[AB+1], x, y-1, z-1), Grad(World.baseNoise[BB+1], x-1, y-1, z-1))));
        }
        else{
            int A  = (World.baseNoise[X  ] + Y) & 0xff;
            int B  = (World.baseNoise[X+1] + Y) & 0xff;
            int AA = (World.baseNoise[A  ] + Z) & 0xff;
            int BA = (World.baseNoise[B  ] + Z) & 0xff;
            int AB = (World.baseNoise[A+1] + Z) & 0xff;
            int BB = (World.baseNoise[B+1] + Z) & 0xff;
            return Lerp(w, Lerp(v, Lerp(u, Grad(World.baseNoise[AA  ], x, y  , z  ), Grad(World.baseNoise[BA  ], x-1, y  , z  )),
                                   Lerp(u, Grad(World.baseNoise[AB  ], x, y-1, z  ), Grad(World.baseNoise[BB  ], x-1, y-1, z  ))),
                           Lerp(v, Lerp(u, Grad(World.baseNoise[AA+1], x, y  , z-1), Grad(World.baseNoise[BA+1], x-1, y  , z-1)),
                                   Lerp(u, Grad(World.baseNoise[AB+1], x, y-1, z-1), Grad(World.baseNoise[BB+1], x-1, y-1, z-1))));            
        }
    }

    #endregion

    #region Private functions

    static float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    static float Lerp(float t, float a, float b)
    {
        return a + t * (b - a);
    }

    static float Grad(int hash, float x)
    {
        return (hash & 1) == 0 ? x : -x;
    }

    static float Grad(int hash, float x, float y)
    {
        return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
    }

    static float Grad(int hash, float x, float y, float z)
    {
        int h = hash & 15;
        float u = h < 8 ? x : y;
        float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    #endregion
}

