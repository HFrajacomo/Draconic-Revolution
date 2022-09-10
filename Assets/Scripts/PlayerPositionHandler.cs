using Random = System.Random;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPositionHandler : MonoBehaviour
{
    // Unity Reference
    public Transform playerTransform;
    public ChunkLoader cl;
    public AudioReverbZone reverb;

    // Position Stuff
    private CastCoord coord = new CastCoord(false);
    private CastCoord lastCoord = new CastCoord(false);
    private string currentBiome = "";
    private string lastBiome = "";

    // Audio Stuff
    public AudioManager audioManager;
    private AudioName? currentMusic;
    private const int minInterval = 0;
    private const int maxInterval = 2400;
    public int counter = -1;

    // Random Number Generator
    private Random rng = new Random();

    // Raytraced Geometric Reverb Generator
    private Vector3[] raytracingDirections;
    private float[] raytracingDistances;
    private RaycastHit cacheHit;
    private const int groundLayerMask = 1 << 8;

    /* Reverb Distances */
    // Maximum raytracing distances
    private float maxDistanceCardinal;
    private float maxDistanceDiagonal;
    private const float maxDistanceVertical = 800f;
    private const float bigEnoughAverageDistance = 30f;
    // Minimum distance to trigger reverb
    private const float minAvgReverbDistance = 4;


    // DEBUG MODE
    private bool isDebugMode = true;


    public void Awake(){
        InitRaytracingDirections();
        SetRaycastDistances();
    }

    // Update is called once per frame
    void Update(){
        RenewPositionalInformation();

        if(CheckBiomeChange()){
            StopMusic();
            
            if(GetBiomeMusic()){
                SetCounter();
            }
            else{
                counter = -1;
            }
        }

        if(counter > 0){
            counter--;
        }
        else if(counter == 0){
            PlayMusic();
            counter--;
        }

        CalculateDistances();
        SetReverbSpecs();
    }

    public void SetAudioManager(AudioManager manager){this.audioManager = manager;}

    /*
    Call this whenever Render Distance is changed
    */
    public void SetRaycastDistances(){
        maxDistanceCardinal = Chunk.chunkWidth*World.renderDistance;
        maxDistanceDiagonal = Mathf.Sqrt(2*(Chunk.chunkWidth*Chunk.chunkWidth));
    }

    private void RenewPositionalInformation(){
        if(this.coord.active)
            this.lastCoord = this.coord;

        this.coord = new CastCoord(playerTransform.position);

        if(this.currentBiome != "")
            this.lastBiome = this.currentBiome;

        if(cl.chunks.ContainsKey(this.coord.GetChunkPos()))
            this.currentBiome = cl.chunks[this.coord.GetChunkPos()].biomeName;    
    }

    private bool CheckBiomeChange(){return this.currentBiome != this.lastBiome;}

    private bool GetBiomeMusic(){
        this.currentMusic = AudioLibrary.GetBiomeMusic(this.currentBiome);

        if(this.currentMusic == null)
            return false;
        return true;
    }

    private void PlayMusic(){
        if(this.currentMusic == null)
            return;

        audioManager.Play((AudioName)this.currentMusic, dynamicLevel:MusicDynamicLevel.SOFT);
    }

    private void StopMusic(){audioManager.Stop(AudioUsecase.MUSIC_CLIP, fade:true);}

    private void SetCounter(){this.counter = rng.Next(minInterval, maxInterval);}

    private void InitRaytracingDirections(){
        this.raytracingDirections = new Vector3[10];
        this.raytracingDistances = new float[10];

        raytracingDirections[0] = Vector3.forward;
        raytracingDirections[1] = Vector3.back;
        raytracingDirections[2] = Vector3.left;
        raytracingDirections[3] = Vector3.right;
        raytracingDirections[4] = Vector3.up;
        raytracingDirections[5] = Vector3.down;
        raytracingDirections[6] = (Vector3.forward + Vector3.left).normalized;
        raytracingDirections[7] = (Vector3.forward + Vector3.right).normalized;
        raytracingDirections[8] = (Vector3.back + Vector3.left).normalized;
        raytracingDirections[9] = (Vector3.back + Vector3.right).normalized;

        this.reverb.minDistance = Chunk.chunkWidth*World.renderDistance;
        this.reverb.maxDistance = 0;
    }

    private void CalculateDistances(){
        // Front
        SendRaycast(raytracingDirections[0], maxDistanceCardinal, 0);
        if(isDebugMode) {Debug.DrawRay(this.playerTransform.position, raytracingDirections[0] * raytracingDistances[0], Color.red);}

        // Front-left
        SendRaycast(raytracingDirections[6], maxDistanceDiagonal, 1);
        if(isDebugMode) {Debug.DrawRay(this.playerTransform.position, raytracingDirections[6] * raytracingDistances[1], Color.red);}

        // Left
        SendRaycast(raytracingDirections[2], maxDistanceCardinal, 2);
        if(isDebugMode) {Debug.DrawRay(this.playerTransform.position, raytracingDirections[2] * raytracingDistances[2], Color.red);}

        // Back-left
        SendRaycast(raytracingDirections[8], maxDistanceDiagonal, 3);
        if(isDebugMode) {Debug.DrawRay(this.playerTransform.position, raytracingDirections[8] * raytracingDistances[3], Color.red);}

        // Back
        SendRaycast(raytracingDirections[1], maxDistanceCardinal, 4);
        if(isDebugMode) {Debug.DrawRay(this.playerTransform.position, raytracingDirections[1] * raytracingDistances[4], Color.red);}

        // Back-right
        SendRaycast(raytracingDirections[9], maxDistanceDiagonal, 5);
        if(isDebugMode) {Debug.DrawRay(this.playerTransform.position, raytracingDirections[9] * raytracingDistances[5], Color.red);}

        // Right
        SendRaycast(raytracingDirections[3], maxDistanceCardinal, 6);
        if(isDebugMode) {Debug.DrawRay(this.playerTransform.position, raytracingDirections[3] * raytracingDistances[6], Color.red);}

        // Front-right
        SendRaycast(raytracingDirections[7], maxDistanceDiagonal, 7);
        if(isDebugMode) {Debug.DrawRay(this.playerTransform.position, raytracingDirections[7] * raytracingDistances[7], Color.red);}

        // Up
        SendRaycast(raytracingDirections[4], maxDistanceVertical, 8);
        if(isDebugMode) {Debug.DrawRay(this.playerTransform.position, raytracingDirections[4] * raytracingDistances[8], Color.red);}

        // Down
        SendRaycast(raytracingDirections[5], maxDistanceVertical, 9);
        if(isDebugMode) {Debug.DrawRay(this.playerTransform.position, raytracingDirections[5] * raytracingDistances[9], Color.red);}
    }

    private void SendRaycast(Vector3 direction, float distance, int outputIndex){
        if(Physics.Raycast(this.playerTransform.position, direction, out this.cacheHit, distance, layerMask:groundLayerMask))
            raytracingDistances[outputIndex] = this.cacheHit.distance;
        else
            raytracingDistances[outputIndex] = 0f;
    }

    private void SetReverbSpecs(){
        float sumReverbDistance = GetSumReverbDistance();
        float averageReverbDistance = sumReverbDistance/8f;

        reverb.reflections = CalculateReflectionsFormula(averageReverbDistance);
        reverb.decayTime = CalculateDecay(averageReverbDistance);
        reverb.room = CalculateRoomFormula(averageReverbDistance);
        reverb.roomHF = CalculateRoomHigh(averageReverbDistance);
        reverb.reflectionsDelay = CalculateEarlyReflectionDelay(averageReverbDistance);
    }

    private float GetSumReverbDistance(){
        float sumReverbDistance = 0f;
        for(int i=0; i < 8; i++)
            sumReverbDistance += raytracingDistances[i];
        return sumReverbDistance;        
    }

    private int CalculateRoomFormula(float averageReverbDistance){
        float verticalVectorDistance = (this.raytracingDistances[8] + this.raytracingDistances[9])/2f;
        float val = Mathf.Log((averageReverbDistance/bigEnoughAverageDistance)+1, 2)*700 - 1000;
        val = val * Mathf.Clamp(5-(Mathf.Log(1+(verticalVectorDistance/8f), 1.4f)*4), 1, 5);

        return Mathf.CeilToInt(Mathf.Clamp(val, -1000, -300));
    }

    private int CalculateReflectionsFormula(float averageReverbDistance){
        float verticalVectorDistance = (this.raytracingDistances[8] + this.raytracingDistances[9])/2f;
        float val = Mathf.Log((averageReverbDistance/bigEnoughAverageDistance)+1, 1.58f)*3000 - 2600;
        val = val * Mathf.Clamp(5-(Mathf.Log(1+(verticalVectorDistance/8f), 1.4f)*4), 1, 5);

        return Mathf.CeilToInt(Mathf.Clamp(val, -2600, 400));
    }

    private float CalculateDecay(float averageReverbDistance){
        float verticalVectorDistance = this.raytracingDistances[8];
        float val;

        if(averageReverbDistance >= minAvgReverbDistance)
            val = Mathf.Log((averageReverbDistance/bigEnoughAverageDistance)+1, 2)*13.5f + 1.5f;
        else
            return 1.5f;

        val = val * Mathf.Clamp((Mathf.Log(1+(verticalVectorDistance/8f), 2f)*4), 0.1f, 1f);

        return Mathf.Clamp(val, 1.5f, 20f);
    }

    private int CalculateRoomHigh(float averageReverbDistance){
        float highestConsiderableDistance = 30f;
        float min, max, deltaMin, deltaMax;
        FindLowestAndHighest(out min, out max);

        if(min > highestConsiderableDistance)
            min = highestConsiderableDistance;
        if(max > highestConsiderableDistance)
            max = highestConsiderableDistance;

        deltaMin = averageReverbDistance - min;
        deltaMax = max - averageReverbDistance;

        return Mathf.CeilToInt((Mathf.Abs(deltaMax - deltaMin)/(max - min))*115 - 100);
    }

    private float CalculateEarlyReflectionDelay(float averageReverbDistance){
        float min, max;
        FindLowestAndHighest(out min, out max);

        return 0.017f + (min * 0.024f);
    }


    private void FindLowestAndHighest(out float min, out float max){
        min = 99999f;
        max = -9999f;

        for(int i=0; i < 8; i++){
            if(raytracingDistances[i] < min)
                min = raytracingDistances[i];
            if(raytracingDistances[i] > max)
                max = raytracingDistances[i];
        }
    }
}
