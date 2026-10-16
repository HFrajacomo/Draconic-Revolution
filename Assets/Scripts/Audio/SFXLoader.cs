using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SFXLoader : MonoBehaviour
{
    // Unity Reference
    public AudioManager audioManager;
    public GameObject prefab;
    public GameObject prefabCategory;
    public ChunkLoader cl;

    // Sound Dictionaries
    private Dictionary<ChunkPos, Dictionary<EntityID, GameObject>> blockSFX = new Dictionary<ChunkPos, Dictionary<EntityID, GameObject>>();
    private Dictionary<EntityID, Dictionary<string, GameObject>> entitySFX = new Dictionary<EntityID, Dictionary<string, GameObject>>();

    // Non-looping Entity SFX that are destroyed once they finish playing
    private List<OneShotSFX> oneShotSFX = new List<OneShotSFX>();

    // Time a one-shot SFX has to start playing (clips are loaded asynchronously) before being discarded
    private static readonly float ONE_SHOT_START_TIMEOUT = 4f;

    void Update(){
        UpdateOneShotSFX();
    }

    /*
    Adds an SFX to an Entity
        parent: Transform the SFX will be attached to. Defaults to the Entity object
        localPosition: offset of the SFX relative to the parent
        If the Entity already has this SFX, it is replayed instead
    */
    public void LoadEntitySFX(string name, EntityID entity, Transform parent=null, Vector3 localPosition=default(Vector3)){
        if(ContainsEntitySFX(entity, name)){
            if(this.entitySFX[entity][name] != null){
                // Looping SFX that are already playing are not restarted
                if(AudioLoader.IsLoop(name) && this.entitySFX[entity][name].GetComponent<AudioSource>().isPlaying)
                    return;

                audioManager.Play(name, entity);
                return;
            }

            // GameObject was destroyed along with its parent
            RemoveEntitySFX(entity, name);
        }

        if(parent == null)
            parent = cl.client.entityHandler.GetEntityObject(entity).transform;

        GameObject go = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        go.transform.parent = parent;
        go.transform.localPosition = localPosition;
        go.name = $"SFX_{name}";
        AudioSource source = go.GetComponent<AudioSource>();

        if(!this.entitySFX.ContainsKey(entity))
            this.entitySFX.Add(entity, new Dictionary<string, GameObject>());

        this.entitySFX[entity].Add(name, go);

        if(AudioLoader.IsLoop(name)){
            audioManager.RegisterAudioSource(source, AudioUsecase.SFX_3D_LOOP, entity, sfxName:name);
        }
        else{
            audioManager.RegisterAudioSource(source, AudioUsecase.SFX_3D, entity, sfxName:name);
            this.oneShotSFX.Add(new OneShotSFX(entity, name, source, Time.time));
        }

        audioManager.Play(name, entity);
    }

    public bool ContainsEntitySFX(EntityID entity, string name){
        if(!this.entitySFX.ContainsKey(entity))
            return false;
        return this.entitySFX[entity].ContainsKey(name);
    }

    // Remove an SFX from an Entity
    public void RemoveEntitySFX(EntityID entity, string name){
        if(!ContainsEntitySFX(entity, name))
            return;

        if(this.entitySFX[entity][name] != null)
            GameObject.Destroy(this.entitySFX[entity][name]);

        this.entitySFX[entity].Remove(name);

        if(this.entitySFX[entity].Count == 0)
            this.entitySFX.Remove(entity);

        RemoveOneShotTracking(entity, name);
        audioManager.UnregisterAudioSource(AudioUsecase.SFX_3D, entity, sfxName:name);
    }

    // Removes all SFX from an Entity
    public void RemoveEntitySFX(EntityID entity){
        if(!this.entitySFX.ContainsKey(entity))
            return;

        foreach(GameObject go in this.entitySFX[entity].Values){
            if(go != null)
                GameObject.Destroy(go);
        }

        this.entitySFX.Remove(entity);

        RemoveOneShotTracking(entity);
        audioManager.UnregisterAudioSource(AudioUsecase.SFX_3D, entity);
    }

    // Destroys one-shot SFX that finished playing, were destroyed externally or never started
    private void UpdateOneShotSFX(){
        OneShotSFX shot;

        for(int i = this.oneShotSFX.Count-1; i >= 0; i--){
            if(i >= this.oneShotSFX.Count)
                continue;

            shot = this.oneShotSFX[i];

            if(shot.source != null && shot.source.isPlaying){
                if(!shot.started){
                    shot.started = true;
                    this.oneShotSFX[i] = shot;
                }
                continue;
            }

            if(shot.source == null || shot.started || Time.time - shot.creationTime >= ONE_SHOT_START_TIMEOUT)
                RemoveEntitySFX(shot.entity, shot.name);
        }
    }

    private void RemoveOneShotTracking(EntityID entity, string name){
        for(int i = this.oneShotSFX.Count-1; i >= 0; i--){
            if(this.oneShotSFX[i].entity.Equals(entity) && this.oneShotSFX[i].name == name)
                this.oneShotSFX.RemoveAt(i);
        }
    }

    private void RemoveOneShotTracking(EntityID entity){
        for(int i = this.oneShotSFX.Count-1; i >= 0; i--){
            if(this.oneShotSFX[i].entity.Equals(entity))
                this.oneShotSFX.RemoveAt(i);
        }
    }

    /*
    Adds a block into the SFXLoader
    */
    public void LoadBlockSFX(string name, ChunkPos pos, int x, int y, int z){
        CastCoord coord = new CastCoord(pos, x, y, z);
        GameObject go = GameObject.Instantiate(prefab, new Vector3(coord.GetWorldX(), coord.GetWorldY(), coord.GetWorldZ()), Quaternion.identity);
        go.transform.parent = prefabCategory.transform;
        AudioSource source = go.GetComponent<AudioSource>();
        EntityID id = new EntityID(EntityType.VOXEL, pos, (ulong)(x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z));

        if(!blockSFX.ContainsKey(pos))
            blockSFX.Add(pos, new Dictionary<EntityID, GameObject>());

        if(blockSFX[pos].ContainsKey(id)){
            blockSFX[pos][id] = go;
            RemoveBlockSFX(pos, x, y, z);
        }
        else
            blockSFX[pos].Add(id, go);

        audioManager.RegisterAudioSource(source, AudioUsecase.SFX_3D, id);
        audioManager.Play(name, id);
    }

    /*
    Removes a single block from SFXLoader
    */
    public void RemoveBlockSFX(ChunkPos pos, int x, int y, int z){
        EntityID id = new EntityID(EntityType.VOXEL, pos, (ulong)(x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z));

        if(!blockSFX.ContainsKey(pos))
            return;

        if(!blockSFX[pos].ContainsKey(id))
            return;

        GameObject.Destroy(blockSFX[pos][id]);
        blockSFX[pos].Remove(id);
        audioManager.UnregisterAudioSource(AudioUsecase.SFX_3D, id);

        if(blockSFX[pos].Count == 0)
            blockSFX.Remove(pos);
    }

    /*
    Removes an entire chunk from the SFXLoader
    */
    public void RemoveChunkSFX(ChunkPos pos){
        if(!blockSFX.ContainsKey(pos))
            return;

        foreach(EntityID entity in blockSFX[pos].Keys){
            GameObject.Destroy(blockSFX[pos][entity]);
            audioManager.UnregisterAudioSource(AudioUsecase.SFX_3D, entity);
        }

        blockSFX.Remove(pos);
    }

    public void SetAudioManager(AudioManager manager){this.audioManager = manager;}
}
