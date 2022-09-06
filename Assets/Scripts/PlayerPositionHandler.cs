using Random = System.Random;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPositionHandler : MonoBehaviour
{
    // Unity Reference
    public Transform playerTransform;
    public ChunkLoader cl;

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

    private Random rng = new Random();



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

    }

    public void SetAudioManager(AudioManager manager){this.audioManager = manager;}

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
}
