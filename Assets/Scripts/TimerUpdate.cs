using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimerUpdate : MonoBehaviour
{
	public TimeOfDay timer;
	public TextMeshProUGUI str;
	public TextMeshProUGUI biome;
    public Transform character;
	public ChunkLoader cl;
    private StringBuilder sb = new StringBuilder();

    private CastCoord currentPos;

    // Update is called once per frame
    void Update()
    {
        if(!cl.PLAYERSPAWNED)
            return;

        this.currentPos = WorldOrigin.ToCastCoord(character.position);
        Vector3 absolutePos = WorldOrigin.ToAbsolute(character.position);

    	str.text = timer.ToString();

    	if(cl.Contains(this.currentPos.GetChunkPos())){
            // Biome
            sb.Append(BiomeHandler.ByteToBiome(cl.Get(this.currentPos.GetChunkPos()).biome));
            sb.Append(" | ");
            // ChunkPos
            sb.Append(this.currentPos.GetChunkPos().ToString());
            sb.Append("  //  ");
            
            // XYZ only
            sb.Append(Mathf.RoundToInt(absolutePos.x));
            sb.Append(", ");
            sb.Append(Mathf.CeilToInt(absolutePos.y%Chunk.chunkDepth));
            sb.Append(", ");
            sb.Append(Mathf.RoundToInt(absolutePos.z));
            

            biome.text = sb.ToString();
            sb.Clear();
        }
    }
}
