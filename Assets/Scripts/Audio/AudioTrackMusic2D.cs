using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioTrackMusic2D : MonoBehaviour
{
    public AudioSource audioSourceOutput;
    public AudioSource switchSourceOutput;
    private AudioSource auxSwapSource;

    public AudioClip currentClip;
    public AudioClip switchClip;

    private AudioName? currentMusic = null;
    private bool playingComposite;

    private bool isCrossfading = false;
    private float crossfadeTimer = 0;

    private float MAX_VOLUME = 0.2f;

    private const int FRAMES_IN_TRANSITION = 300;
    private const float FRAMES_MULTIPLIER = 1f/FRAMES_IN_TRANSITION;


    public void Awake(){
        this.audioSourceOutput = this.gameObject.AddComponent<AudioSource>();
        this.audioSourceOutput.spatialBlend = 0f;
        this.audioSourceOutput.volume = MAX_VOLUME;
        this.audioSourceOutput.spread = 180f;
        this.audioSourceOutput.loop = true;

        this.switchSourceOutput = this.gameObject.AddComponent<AudioSource>();
        this.switchSourceOutput.spatialBlend = 0f;
        this.switchSourceOutput.volume = 0f;
        this.switchSourceOutput.spread = 180f;
        this.switchSourceOutput.loop = true;
    }

    public void Update(){
        TransitionVolume();
    }


    public void StartPlay(Sound sound, AudioClip clip){
        if(this.currentMusic == sound.name)
            return;

        this.currentMusic = sound.name;

        if(!audioSourceOutput.isPlaying){
            this.audioSourceOutput.clip = clip;
            this.audioSourceOutput.Play();
        }
        else if(audioSourceOutput.isPlaying && !switchSourceOutput.isPlaying){
            this.switchSourceOutput.clip = clip;
            isCrossfading = true;
            this.switchSourceOutput.Play();
        }
        else{
            this.switchSourceOutput.clip = clip;
            this.switchSourceOutput.Play();
        }
    }


    public void Stop(){return;}
    public void ChangeDynamic(int dynamic){return;}
    public void ChangeVolume(float volume){return;}


    private void TransitionVolume(){
        if(isCrossfading){
            this.audioSourceOutput.volume = Mathf.Lerp(0f, MAX_VOLUME, 1-(FRAMES_MULTIPLIER*crossfadeTimer));
            this.switchSourceOutput.volume = Mathf.Lerp(0f, MAX_VOLUME, FRAMES_MULTIPLIER*crossfadeTimer);

            if(crossfadeTimer == FRAMES_IN_TRANSITION){
                this.auxSwapSource = this.audioSourceOutput;
                this.audioSourceOutput = this.switchSourceOutput;
                this.switchSourceOutput = this.auxSwapSource;
                this.auxSwapSource = null;

                this.switchSourceOutput.Stop();
                this.switchSourceOutput.clip = null;

                crossfadeTimer = 0;
                isCrossfading = false;
                return;
            }

            crossfadeTimer++;
        }
    }
}
