using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class VFXLoader : MonoBehaviour
{
	public Dictionary<ChunkPos, Dictionary<string, GameObject>> data;
    public Dictionary<ChunkPos, List<HDAdditionalLightData>> lightReference;
    public Dictionary<ChunkPos, Dictionary<string, NetMessage>> serverVFX;

    public Dictionary<ulong, Dictionary<string, GameObject>> entityData;

    public static bool EXTRALIGHTSHADOWS = false;

    void OnDestroy(){
        RemoveAll();
        this.data = null;
        this.lightReference = null;
        this.serverVFX = null;
        this.entityData = null;
    }

    // Start is called before the first frame update
    void Start()
    {
		data = new Dictionary<ChunkPos, Dictionary<string, GameObject>>(); 
        serverVFX = new Dictionary<ChunkPos, Dictionary<string, NetMessage>>();
        lightReference = new Dictionary<ChunkPos, List<HDAdditionalLightData>>();
        entityData = new Dictionary<ulong, Dictionary<string, GameObject>>();
    }

    public bool Contains(ChunkPos pos, bool isServer=false){
        if(isServer)
            return serverVFX.ContainsKey(pos);
        else
            return data.ContainsKey(pos);
    }
    public bool Contains(ulong code){return entityData.ContainsKey(code);}

    public bool ContainsLight(ChunkPos pos){ 
        return lightReference.ContainsKey(pos);
    }

    // Registers a new chunk to VFXLoader
    public void NewChunk(ChunkPos pos, bool isServer=false){
        if(isServer)
            serverVFX.Add(pos, new Dictionary<string, NetMessage>());
        else{
    	   data.Add(pos, new Dictionary<string, GameObject>());
           lightReference.Add(pos, new List<HDAdditionalLightData>());
        }
    }

    // Unregisters a chunk from VFXLoader
    public void RemoveChunk(ChunkPos pos, bool isServer=false){
        if(this.data == null)
            return;

        // Destroy all VFX once chunk is unloaded
        if(isServer){
            serverVFX.Remove(pos);
        }
        else{
            foreach(string key in this.data[pos].Keys)
                Destroy(this.data[pos][key]);

        	data.Remove(pos);
            lightReference[pos] = null;
            lightReference.Remove(pos);
        }
    }

    // Removes all data from VFXLoader
    private void RemoveAll(){
        if(this.data == null)
            return;

        List<ChunkPos> removeChunks = new List<ChunkPos>();

        foreach(ChunkPos pos in this.data.Keys)
            removeChunks.Add(pos);

        foreach(ChunkPos pos in removeChunks)
            RemoveChunk(pos);
    }

    // Adds GameObject to Chunk Dict and sets it to active
    public void Add(ChunkPos pos, GameObject go, bool active=true, bool isOnDemandLight=false){
    	if(data[pos].ContainsKey(go.name)){
            Destroy(this.data[pos][go.name]);
            data[pos].Remove(go.name);
        }

        data[pos].Add(go.name, go);

        if(isOnDemandLight && VFXLoader.EXTRALIGHTSHADOWS){
            lightReference[pos].Add(go.GetComponent<HDAdditionalLightData>());
        }

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

    // Client adds a VFX object for a player entity itself
    public void Add(ulong code, GameObject go){
        if(!this.entityData.ContainsKey(code)){
            this.entityData.Add(code, new Dictionary<string, GameObject>());
        }

        if(this.entityData[code].ContainsKey(go.name)){
            Remove(code, go.name);
        }

        this.entityData[code].Add(go.name, go);
    }

    // Removes an GameObject from Chunk Dict
    public void Remove(ChunkPos pos, string name, bool isServer=false, bool isOnDemandLight=false){
        if(isServer){
            this.serverVFX[pos].Remove(name);
        }
        else{
            if(this.data[pos].ContainsKey(name)){

                if(isOnDemandLight && VFXLoader.EXTRALIGHTSHADOWS) 
                    lightReference[pos].Remove(this.data[pos][name].GetComponent<HDAdditionalLightData>());
                
                Destroy(this.data[pos][name]);
            	data[pos].Remove(name);
            }
        }
    }

    // Client removes a VFX based on an Entity
    public void Remove(ulong code, string name){
        if(this.entityData.ContainsKey(code)){
            if(this.entityData[code].ContainsKey(name)){
                GameObject.Destroy(this.entityData[code][name]);

                if(this.entityData[code].Count <= 1){
                    this.entityData.Remove(code);
                }
                else{
                    this.entityData[code].Remove(name);
                }
            }
        }
    }

    // Redraws lights in current and adjascent chunks in scene on Demand
    public void UpdateLights(ChunkPos pos, bool adjascent=true){
        if(!VFXLoader.EXTRALIGHTSHADOWS)
            return;

        // Updates all lights in current chunk
        foreach(HDAdditionalLightData light in this.lightReference[pos]){
            light.RequestShadowMapRendering();
        }

        if(adjascent){
            ChunkPos[] neighbors = {new ChunkPos(pos.x-1, pos.z-1, pos.y), new ChunkPos(pos.x-1, pos.z, pos.y), new ChunkPos(pos.x-1, pos.z+1, pos.y), new ChunkPos(pos.x, pos.z-1, pos.y), new ChunkPos(pos.x, pos.z+1, pos.y), new ChunkPos(pos.x+1, pos.z-1, pos.y), new ChunkPos(pos.x+1, pos.z, pos.y), new ChunkPos(pos.x+1, pos.z+1, pos.y)};
        
            // Updates all lights in neighbor chunks
            foreach(ChunkPos iterPos in neighbors){
                if(ContainsLight(iterPos)){
                    foreach(HDAdditionalLightData light in this.lightReference[iterPos]){
                        light.RequestShadowMapRendering();
                    }
                }
            }
        }
    }

}