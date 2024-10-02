using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioTrackSFX3D : MonoBehaviour
{
    private Dictionary<ChunkPos, Dictionary<ulong, AudioSource>> audioMap = new Dictionary<ChunkPos, Dictionary<ulong, AudioSource>>();
    private AudioSource cachedSource;

    private const float HARD_VOLUME_LIMIT = 0.2f;
    private static float MAX_VOLUME = 0.2f;

    public void Awake(){
        ChangeVolume();
    }

    public void Play(Sound sound, AudioClip clip, ulong entityCode, ChunkPos pos){
        if(!audioMap.ContainsKey(pos))
            return;

        if(!audioMap[pos].ContainsKey(entityCode))
            return;

        cachedSource = audioMap[pos][entityCode];

        if(sound.GetUsecaseType() == AudioUsecase.SFX_3D)
            cachedSource.loop = false;
        // AudioUsecase.SFX_3D_LOOP
        else
            cachedSource.loop = true;

        cachedSource.maxDistance = (float)sound.GetVolume();
        cachedSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.EaseInOut(0f, 1f, cachedSource.maxDistance, 0f));

        cachedSource.clip = clip;
        cachedSource.Play();
    }

    public void RegisterAudioSource(ulong entityCode, AudioSource source, ChunkPos pos){
        if(!audioMap.ContainsKey(pos))
            audioMap.Add(pos, new Dictionary<ulong, AudioSource>());

        if(!audioMap[pos].ContainsKey(entityCode)){
            SetupAudioSource(source);
            audioMap[pos].Add(entityCode, source);
        }
    }

    public void UnregisterAudioSource(ulong entityCode, ChunkPos pos){
        if(!audioMap.ContainsKey(pos))
            return;

        if(audioMap[pos].ContainsKey(entityCode))
            audioMap[pos].Remove(entityCode);

        if(audioMap[pos].Count == 0)
            audioMap.Remove(pos);
    }

    public void SetupAudioSource(AudioSource source){
        source.spatialBlend = 1f;
        source.volume = MAX_VOLUME;
        source.spread = 60f;
        source.dopplerLevel = 0.1f;
        source.rolloffMode = AudioRolloffMode.Custom;
    }

    public void ChangeVolume(){
        MAX_VOLUME = HARD_VOLUME_LIMIT * (Configurations.sfx3DVolume/100f);
    }

    public void DestroyTrackInfo(){
        List<ChunkPos> removeList = new List<ChunkPos>(audioMap.Keys);
        List<ulong> removeCodeList;

        foreach(ChunkPos pos in removeList){
            removeCodeList = new List<ulong>(audioMap[pos].Keys);

            foreach(ulong entity in removeCodeList){
                UnregisterAudioSource(entity, pos);
            }
        }
    }
}
