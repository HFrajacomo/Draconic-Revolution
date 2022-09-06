using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SFXLoader : MonoBehaviour
{
    // Unity Reference
    public AudioManager audioManager;
    public GameObject prefab;

    // Sound Dictionaries
    private Dictionary<ChunkPos, Dictionary<ulong, GameObject>> blockSFX = new Dictionary<ChunkPos, Dictionary<ulong, GameObject>>();


    /*
    Adds a block into the SFXLoader
    */
    public void LoadBlockSFX(AudioName name, ChunkPos pos, int x, int y, int z){
        GameObject go = GameObject.Instantiate(prefab, new Vector3(x, y, z), Quaternion.identity);
        go.transform.parent = prefab.transform;
        AudioSource source = go.GetComponent<AudioSource>();
        ulong entityCode = (ulong)(x*Chunk.chunkWidth*Chunk.chunkDepth+y*Chunk.chunkWidth+z);

        if(!blockSFX.ContainsKey(pos))
            blockSFX.Add(pos, new Dictionary<ulong, GameObject>());

        blockSFX[pos].Add(entityCode, go);

        audioManager.RegisterAudioSource(source, AudioUsecase.SFX_3D, entityCode);
        audioManager.Play(name, entity:entityCode);
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
