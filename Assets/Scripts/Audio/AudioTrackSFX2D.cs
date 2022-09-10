using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioTrackSFX2D : MonoBehaviour
{
    // AudioSource references
    [SerializeField]
    private List<AudioSource> audioSources = new List<AudioSource>();

    // Cache reference
    private AudioSource sourceReference;

    private const float HARD_VOLUME_LIMIT = 0.2f;
    private static float MAX_VOLUME = 0.2f;
    private byte NUMBER_OF_SOURCES = 5;


    public void Awake(){
        ChangeVolume();
        CreateAudioSource();
    }


    public void Play(AudioClip clip){
        this.sourceReference = FindFreeAudioSouce();
        this.sourceReference.PlayOneShot(clip);
    }

    public void ChangeVolume(){
        MAX_VOLUME =  HARD_VOLUME_LIMIT * (Configurations.sfx2DVolume/100f);
    }

    private void CreateAudioSource(){
        for(int i=0; i < NUMBER_OF_SOURCES; i++){
            AudioSource source = this.gameObject.AddComponent<AudioSource>();
            this.audioSources.Add(source);

            source.spatialBlend = 0f;
            source.volume = MAX_VOLUME;
            source.spread = 180f;
            source.loop = false;
            source.bypassReverbZones = true;
        }
    }

    private AudioSource FindFreeAudioSouce(){
        for(int i=0; i < NUMBER_OF_SOURCES; i++){
            if(!audioSources[i].isPlaying)
                return audioSources[i];
        }

        return audioSources[0];
    }
}
