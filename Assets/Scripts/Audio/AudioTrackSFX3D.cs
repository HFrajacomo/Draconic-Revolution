using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioTrackSFX3D : MonoBehaviour
{
    private Dictionary<ChunkPos, Dictionary<ulong, AudioSource>> audioMap = new Dictionary<ChunkPos, Dictionary<ulong, AudioSource>>();
    private Dictionary<EntityID, Dictionary<string, AudioSource>> entityMap = new Dictionary<EntityID, Dictionary<string, AudioSource>>();

    private AudioSource cachedSource;

    private const float HARD_VOLUME_LIMIT = 0.2f;
    private static float MAX_VOLUME = 0.2f;

    public void Awake(){
        ChangeVolume();
    }

    public void Play(Sound sound, AudioClip clip, EntityID entityID){
        if(entityID.type == EntityType.VOXEL){
            if(!audioMap.ContainsKey(entityID.pos))
                return;

            if(!audioMap[entityID.pos].ContainsKey(entityID.code))
                return;

            cachedSource = audioMap[entityID.pos][entityID.code];
        }
        else if(entityID.type == EntityType.PLAYER){
            if(!entityMap.ContainsKey(entityID))
                return;

            if(!entityMap[entityID].ContainsKey(sound.name))
                return;

            cachedSource = entityMap[entityID][sound.name];
        }

        // Source may have been destroyed together with its parent (e.g. model rebuilt)
        if(cachedSource == null)
            return;

        if(sound.GetUsecaseType() == AudioUsecase.SFX_3D)
            cachedSource.loop = false;
        else
            cachedSource.loop = true;

        cachedSource.maxDistance = (float)sound.GetVolume();
        cachedSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.EaseInOut(0f, 1f, cachedSource.maxDistance, 0f));

        cachedSource.clip = clip;
        cachedSource.clip.name = sound.name;
        cachedSource.Play();
    }

    /*
    Registers an AudioSource
        sfxName is required for Player entities, since a single entity can hold multiple SFX
    */
    public void RegisterAudioSource(EntityID entityID, AudioSource source, string sfxName=null){
        if(entityID.type == EntityType.VOXEL){
            if(!audioMap.ContainsKey(entityID.pos))
                audioMap.Add(entityID.pos, new Dictionary<ulong, AudioSource>());

            if(!audioMap[entityID.pos].ContainsKey(entityID.code)){
                SetupAudioSource(source);
                audioMap[entityID.pos].Add(entityID.code, source);
            }
        }
        else if(entityID.type == EntityType.PLAYER){
            if(sfxName == null){
                Debug.LogWarning($"[AudioTrackSFX3D] Tried to register a Player AudioSource without an SFX name: {entityID}");
                return;
            }

            if(!entityMap.ContainsKey(entityID))
                entityMap.Add(entityID, new Dictionary<string, AudioSource>());

            entityMap[entityID][sfxName] = source;
            SetupAudioSource(source);
        }
    }

    /*
    Unregisters an AudioSource
        If sfxName is null, all AudioSources of a Player entity are unregistered
    */
    public void UnregisterAudioSource(EntityID entityID, string sfxName=null){
        if(entityID.type == EntityType.VOXEL){
            if(!audioMap.ContainsKey(entityID.pos))
                return;

            if(audioMap[entityID.pos].ContainsKey(entityID.code))
                audioMap[entityID.pos].Remove(entityID.code);

            if(audioMap[entityID.pos].Count == 0)
                audioMap.Remove(entityID.pos);
        }
        else if(entityID.type == EntityType.PLAYER){
            if(!entityMap.ContainsKey(entityID))
                return;

            if(sfxName == null){
                entityMap.Remove(entityID);
                return;
            }

            entityMap[entityID].Remove(sfxName);

            if(entityMap[entityID].Count == 0)
                entityMap.Remove(entityID);
        }
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
        List<EntityID> removeIds = new List<EntityID>(entityMap.Keys);
        List<ulong> removeCodeList;

        // AudioMap
        foreach(ChunkPos pos in removeList){
            removeCodeList = new List<ulong>(audioMap[pos].Keys);

            foreach(ulong entity in removeCodeList){
                UnregisterAudioSource(new EntityID(EntityType.VOXEL, pos, entity));
            }
        }

        // EntityMap
        foreach(EntityID id in removeIds){
            UnregisterAudioSource(id);
        }
    }
}
