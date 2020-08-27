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
    	if(chunkLoader.chunks.ContainsKey(chunkLoader.currentChunk))
    		biome.text = chunkLoader.chunks[chunkLoader.currentChunk].biomeName + " : " + Mathf.FloorToInt(character.position.x) + ", " + Mathf.FloorToInt(character.position.y) + ", " + Mathf.FloorToInt(character.position.z);
    }
}
