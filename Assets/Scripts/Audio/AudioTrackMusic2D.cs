using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioTrackMusic2D : MonoBehaviour
{
    // AudioSources reference
    public AudioSource audioSourceOutput;
    public AudioSource switchSourceOutput;
    private AudioSource auxSwapSource;

    // AudioClip references 
    public AudioClip currentClip;
    public AudioClip switchClip;

    // Last command info
    private AudioName? currentMusic = null;
    private bool playOperationWasLast = false;

    // Crossfading
    private bool isCrossfading = false;
    private float crossfadeTimer = 0;

    // Downfade
    private bool isFading = false;
    private float fadeTimer = 0;

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
        if(playOperationWasLast){
            CrossfadeTransition();
            DownfadeTransition();
        }
        else{
            DownfadeTransition();
            CrossfadeTransition();
        }
    }


    /*
    Plays the given sound AudioClip
    If something is already playing, crossfade between the two
    */
    public void Play(Sound sound, AudioClip clip, bool isDynamic=false){
        if(this.currentMusic == sound.name && !isDynamic)
            return;

        if(isDynamic && this.currentMusic != sound.name)
            isDynamic = false;

        this.currentMusic = sound.name;
        this.playOperationWasLast = true;

        if(!audioSourceOutput.isPlaying){
            this.audioSourceOutput.volume = MAX_VOLUME;
            this.audioSourceOutput.clip = clip;
            this.audioSourceOutput.Play();
        }
        else if(audioSourceOutput.isPlaying && !switchSourceOutput.isPlaying){
            this.switchSourceOutput.clip = clip;
            isCrossfading = true;

            if(isDynamic)
                this.switchSourceOutput.timeSamples = this.audioSourceOutput.timeSamples;

            this.switchSourceOutput.Play();
        }
        else{
            this.switchSourceOutput.clip = clip;

            if(isDynamic)
                this.switchSourceOutput.timeSamples = this.audioSourceOutput.timeSamples;

            this.switchSourceOutput.Play();
        }
    }


    public void Stop(bool fade=false){
        if(!fade){
            if(this.currentMusic == null)
                return;

            this.playOperationWasLast = false;

            if(this.audioSourceOutput.isPlaying)
                this.audioSourceOutput.Stop();
            if(this.switchSourceOutput.isPlaying)
                this.switchSourceOutput.Stop();
        }
        else{
            if(this.currentMusic == null)
                return;

            this.playOperationWasLast = false;

            isFading = true;
        }
    }

    public void ChangeVolume(float volume){
        float v;

        if(volume < 0)
            v = 0f;
        else if(volume > 1)
            v = 1f;
        else
            v = volume;

        this.MAX_VOLUME = v;
        this.audioSourceOutput.volume = this.MAX_VOLUME;
        this.switchSourceOutput.volume = this.MAX_VOLUME;
    }


    private void CrossfadeTransition(){
        // If Downfade operation has been issued in the middle of crossfade
        if(isCrossfading && isFading && !playOperationWasLast){
            this.switchSourceOutput.volume = Mathf.Lerp(0f, MAX_VOLUME, FRAMES_MULTIPLIER*crossfadeTimer);

            if(crossfadeTimer == 0){
                this.auxSwapSource = this.audioSourceOutput;
                this.audioSourceOutput = this.switchSourceOutput;
                this.switchSourceOutput = this.auxSwapSource;
                this.auxSwapSource = null;

                if(this.switchSourceOutput.isPlaying)
                    this.switchSourceOutput.Stop();

                this.switchSourceOutput.clip = null;
                this.switchSourceOutput.volume = 0f;

                crossfadeTimer = 0;
                isCrossfading = false;
                return;                
            }

            crossfadeTimer--;
        }
        // If is simply crossfading
        else if(isCrossfading){
            if(this.audioSourceOutput.volume > 0f)
                this.audioSourceOutput.volume = Mathf.Lerp(0f, MAX_VOLUME, 1-(FRAMES_MULTIPLIER*crossfadeTimer));

            this.switchSourceOutput.volume = Mathf.Lerp(0f, MAX_VOLUME, FRAMES_MULTIPLIER*crossfadeTimer);

            if(crossfadeTimer == FRAMES_IN_TRANSITION){
                this.auxSwapSource = this.audioSourceOutput;
                this.audioSourceOutput = this.switchSourceOutput;
                this.switchSourceOutput = this.auxSwapSource;
                this.auxSwapSource = null;

                if(this.switchSourceOutput.isPlaying)
                    this.switchSourceOutput.Stop();

                this.switchSourceOutput.clip = null;
                this.switchSourceOutput.volume = 0f;

                crossfadeTimer = 0;
                isCrossfading = false;
                return;
            }

            crossfadeTimer++;
        }

    }

    private void DownfadeTransition(){
        if(isFading){
            this.audioSourceOutput.volume = Mathf.Lerp(0f, MAX_VOLUME, 1-(FRAMES_MULTIPLIER*fadeTimer));

            if(fadeTimer == FRAMES_IN_TRANSITION){
                if(this.audioSourceOutput.isPlaying)
                    this.audioSourceOutput.Stop();

                this.audioSourceOutput.clip = null;
                this.audioSourceOutput.volume = 0f;
                this.currentMusic = null;

                fadeTimer = 0;
                isFading = false;
                return;
            }

            fadeTimer++;
        }
    }
}
