using System.Text;
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
    private StringBuilder sb = new StringBuilder();

    // Update is called once per frame
    void Update()
    {
    	str.text = timer.ToString();
    	if(chunkLoader.chunks.ContainsKey(chunkLoader.currentChunk)){
            // Biome, ChunkPos and XYZ 
            /*
            sb.Append(Mathf.RoundToInt(character.position.x));
            sb.Append(", ");
            sb.Append(Mathf.RoundToInt(character.position.z));
            sb.Append("    (");
            sb.Append(Mathf.FloorToInt(character.position.x/Chunk.chunkWidth));
            sb.Append(", ");
            sb.Append(Mathf.FloorToInt(character.position.z/Chunk.chunkWidth));
            sb.Append(")");
            */
            
            // XYZ only
            sb.Append(Mathf.RoundToInt(character.position.x));
            sb.Append(", ");
            sb.Append(Mathf.CeilToInt(character.position.y));
            sb.Append(", ");
            sb.Append(Mathf.RoundToInt(character.position.z));
            

            biome.text = sb.ToString();
            sb.Clear();
        }
    }
}
