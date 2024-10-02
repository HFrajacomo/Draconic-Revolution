using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioTrackMusic3D : MonoBehaviour
{
    private Dictionary<ulong, AudioSource> audioMap = new Dictionary<ulong, AudioSource>();
    private AudioSource cachedSource;

    private const float HARD_VOLUME_LIMIT = 0.2f;
    private static float MAX_VOLUME = 0.2f;

    public void Awake(){
        ChangeVolume();
    }


    public void Play(Sound sound, AudioClip clip, ulong entityCode){
        if(!audioMap.ContainsKey(entityCode))
            return;

        cachedSource = audioMap[entityCode];

        cachedSource.maxDistance = (float)sound.GetVolume();
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
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Custom;
        source.loop = false;
    }

    public void ChangeVolume(){
        MAX_VOLUME = HARD_VOLUME_LIMIT * (Configurations.music3DVolume/100f);
    }

    public void DestroyTrackInfo(){
        List<ulong> removeList = new List<ulong>(audioMap.Keys);

        foreach(ulong entity in removeList){
            UnregisterAudioSource(entity);
        }
    }
}
