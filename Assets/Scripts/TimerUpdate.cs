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
	public ChunkLoader cl;
    private StringBuilder sb = new StringBuilder();

    private CastCoord currentPos;

    // Update is called once per frame
    void Update()
    {
        if(!cl.PLAYERSPAWNED)
            return;

        this.currentPos = new CastCoord(character.position.x, character.position.y, character.position.z);

    	str.text = timer.ToString();

    	if(cl.chunks.ContainsKey(this.currentPos.GetChunkPos())){
            // Biome, ChunkPos
            /*
            sb.Append(cl.chunks[this.currentPos.GetChunkPos()].biomeName);
            sb.Append(" | ");
            sb.Append(this.currentPos.GetChunkPos().ToString());
            sb.Append("  ");
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
