using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioTrackMusic2D : MonoBehaviour
{
    public AudioSource audioSourceOutput;
    public AudioSource switchSourceOutput;
    public AudioClip currentClip;
    public AudioClip switchClip;

    private AudioName currentMusic;
    private bool playingComposite;
    //private float pausedTime = 0;

    private const float MAX_VOLUME = 0.2f;


    public void Awake(){
        this.audioSourceOutput = this.gameObject.AddComponent<AudioSource>();
        this.audioSourceOutput.spatialBlend = 0f;
        this.audioSourceOutput.volume = MAX_VOLUME;
        this.audioSourceOutput.spread = 180f;
        this.audioSourceOutput.loop = true;

        this.switchSourceOutput = this.gameObject.AddComponent<AudioSource>();
        this.switchSourceOutput.spatialBlend = 0f;
        this.switchSourceOutput.volume = MAX_VOLUME;
        this.switchSourceOutput.spread = 180f;
        this.switchSourceOutput.loop = true;
    }


    public void StartPlay(Sound sound, AudioClip clip){
        if(!audioSourceOutput.isPlaying){
            this.currentMusic = sound.name;
            this.audioSourceOutput.clip = clip;
            this.audioSourceOutput.Play();
        }
    }

    public void ResumePlay(){return;}
    public void Pause(){return;}
    public void Stop(){return;}
    public void ChangeDynamic(int dynamic){return;}
    public void ChangeVolume(float volume){return;}
}
