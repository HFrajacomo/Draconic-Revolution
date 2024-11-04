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
    private Dictionary<EntityID, GameObject> entitySFX = new Dictionary<EntityID, GameObject>();

    // Adds an SFX to an Entity
    public void LoadEntitySFX(string name, EntityID entity){
        GameObject entityObj = cl.client.entityHandler.GetEntityObject(entity);

        GameObject go = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        go.transform.parent = entityObj.transform;
        go.transform.localPosition = Vector3.zero;
        go.name = $"SFX_{name}";
        AudioSource source = go.GetComponent<AudioSource>();

        if(this.entitySFX.ContainsKey(entity)){
            GameObject.Destroy(this.entitySFX[entity]);
            this.entitySFX[entity] = go;
        }
        else{
            this.entitySFX.Add(entity, go);            
        }

        if(AudioLoader.IsLoop(name))
            audioManager.RegisterAudioSource(source, AudioUsecase.SFX_3D_LOOP, entity);
        else
            audioManager.RegisterAudioSource(source, AudioUsecase.SFX_3D, entity);

        audioManager.Play(name, entity);
    }

    // Remove an SFX from an Entity
    public void RemoveEntitySFX(EntityID entity){
        if(!entitySFX.ContainsKey(entity))
            return;

        GameObject.Destroy(entitySFX[entity]);
        entitySFX.Remove(entity);
        audioManager.UnregisterAudioSource(AudioUsecase.SFX_3D, entity);
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
