using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioTrackSFX3D : MonoBehaviour
{
    private Dictionary<ulong, AudioSource> audioMap = new Dictionary<ulong, AudioSource>();
    private AudioSource cachedSource;

    private static float MAX_VOLUME = 0.2f;


    public void Play(Sound sound, AudioClip clip, ulong entityCode){
        if(!audioMap.ContainsKey(entityCode))
            return;

        cachedSource = audioMap[entityCode];

        if(sound.type == AudioUsecase.SFX_3D)
            cachedSource.loop = false;
        // AudioUsecase.SFX_3D_LOOP
        else
            cachedSource.loop = true;

        cachedSource.maxDistance = (float)sound.volume;
        cachedSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.EaseInOut(0f, 1f, cachedSource.maxDistance, 0f));

        cachedSource.clip = clip;
        cachedSource.Play();
    }

    public void RegisterAudioSource(ulong entityCode, AudioSource source){
        if(!audioMap.ContainsKey(entityCode)){
            SetupAudioSource(source);
            audioMap.Add(entityCode, source);
        }
    }

    public void UnregisterAudioSource(ulong entityCode){
        if(audioMap.ContainsKey(entityCode))
            audioMap.Remove(entityCode);
    }

    public void SetupAudioSource(AudioSource source){
        source.spatialBlend = 1f;
        source.volume = MAX_VOLUME;
        source.spread = 60f;
        source.dopplerLevel = 0.1f;
        source.rolloffMode = AudioRolloffMode.Custom;
    }

    public void DestroyTrackInfo(){
        List<ulong> removeList = new List<ulong>(audioMap.Keys);

        foreach(ulong entity in removeList){
            UnregisterAudioSource(entity);
        }
    }
}
