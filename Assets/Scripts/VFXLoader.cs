using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXLoader : MonoBehaviour
{
	public Dictionary<ChunkPos, Dictionary<string, GameObject>> data;
    public Dictionary<ChunkPos, Dictionary<string, NetMessage>> serverVFX;

    // Start is called before the first frame update
    void Start()
    {
		data = new Dictionary<ChunkPos, Dictionary<string, GameObject>>(); 
        serverVFX = new Dictionary<ChunkPos, Dictionary<string, NetMessage>>();
    }

    // Registers a new chunk to VFXLoader
    public void NewChunk(ChunkPos pos, bool isServer=false){
        if(isServer)
            serverVFX.Add(pos, new Dictionary<string, NetMessage>());
        else
    	   data.Add(pos, new Dictionary<string, GameObject>());
    }

    // Unregisters a chunk from VFXLoader
    public void RemoveChunk(ChunkPos pos, bool isServer=false){
        // Destroy all VFX once chunk is unloaded
        if(isServer){
            serverVFX.Remove(pos);
        }
        else{
            foreach(string key in this.data[pos].Keys)
                Destroy(this.data[pos][key]);

        	data.Remove(pos);
        }
    }

    // Adds GameObject to Chunk Dict and sets it to active
    public void Add(ChunkPos pos, GameObject go, bool active=true){
    	if(data[pos].ContainsKey(go.name)){
            Destroy(this.data[pos][go.name]);
            data[pos].Remove(go.name);
        }

        data[pos].Add(go.name, go);

    	if(!active){
    		data[pos][go.name].SetActive(false);
    	}
    }

    // Adds NetMessage to Server buffer
    public void Add(ChunkPos pos, string name, NetMessage message){
        if(this.serverVFX[pos].ContainsKey(name))
            this.serverVFX[pos].Remove(name);

        this.serverVFX[pos][name] = message;
    }

    // Removes an GameObject from Chunk Dict
    public void Remove(ChunkPos pos, string name, bool isServer=false){
        if(isServer){
            this.serverVFX[pos].Remove(name);
        }
        else{
            Destroy(this.data[pos][name]);
        	data[pos].Remove(name);
        }
    }



}