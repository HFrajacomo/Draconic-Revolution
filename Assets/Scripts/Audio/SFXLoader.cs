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

    // Sound Dictionaries
    private Dictionary<ChunkPos, Dictionary<ulong, GameObject>> blockSFX = new Dictionary<ChunkPos, Dictionary<ulong, GameObject>>();


    /*
    Adds a block into the SFXLoader
    */
    public void LoadBlockSFX(AudioName name, ChunkPos pos, int x, int y, int z){
        CastCoord coord = new CastCoord(pos, x, y, z);
        GameObject go = GameObject.Instantiate(prefab, new Vector3(coord.GetWorldX(), coord.GetWorldY(), coord.GetWorldZ()), Quaternion.identity);
        go.transform.parent = prefabCategory.transform;
        AudioSource source = go.GetComponent<AudioSource>();
        ulong entityCode = (ulong)(x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z);

        if(!blockSFX.ContainsKey(pos))
            blockSFX.Add(pos, new Dictionary<ulong, GameObject>());

        if(blockSFX[pos].ContainsKey(entityCode)){
            blockSFX[pos][entityCode] = go;
            RemoveBlockSFX(pos, x, y, z);
        }
        else
            blockSFX[pos].Add(entityCode, go);

        audioManager.RegisterAudioSource(source, AudioUsecase.SFX_3D, entityCode, pos:pos);
        audioManager.Play(name, entity:entityCode, chunk:pos);
    }

    /*
    Removes a single block from SFXLoader
    */
    public void RemoveBlockSFX(ChunkPos pos, int x, int y, int z){
        ulong code = (ulong)(x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z);

        if(!blockSFX.ContainsKey(pos))
            return;

        if(!blockSFX[pos].ContainsKey(code))
            return;

        GameObject.Destroy(blockSFX[pos][code]);
        blockSFX[pos].Remove(code);
        audioManager.UnregisterAudioSource(AudioUsecase.SFX_3D, code, pos:pos);

        if(blockSFX[pos].Count == 0)
            blockSFX.Remove(pos);
    }

    /*
    Removes an entire chunk from the SFXLoader
    */
    public void RemoveChunkSFX(ChunkPos pos){
        if(!blockSFX.ContainsKey(pos))
            return;

        foreach(ulong entity in blockSFX[pos].Keys){
            GameObject.Destroy(blockSFX[pos][entity]);
            audioManager.UnregisterAudioSource(AudioUsecase.SFX_3D, entity, pos:pos);
        }

        blockSFX.Remove(pos);
    }

    public void SetAudioManager(AudioManager manager){this.audioManager = manager;}
}
