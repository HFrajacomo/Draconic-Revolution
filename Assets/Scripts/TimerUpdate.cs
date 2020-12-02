using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerUpdate : MonoBehaviour
{
	public TimeOfDay timer;
	public Text str;
	public Text biome;
    public Transform character;
	public ChunkLoader chunkLoader;

    // Update is called once per frame
    void Update()
    {
    	str.text = timer.ToString();
    	if(chunkLoader.chunks.ContainsKey(chunkLoader.currentChunk)){
            biome.text = chunkLoader.chunks[chunkLoader.currentChunk].biomeName + " : " + Mathf.RoundToInt(character.position.x) + ", " + Mathf.CeilToInt(character.position.y) + ", " + Mathf.RoundToInt(character.position.z) + "    (" + Mathf.RoundToInt(Mathf.RoundToInt(character.position.x)/Chunk.chunkWidth) + ", " + Mathf.RoundToInt(Mathf.RoundToInt(character.position.z)/Chunk.chunkWidth) + ")";
        }
    }
}
