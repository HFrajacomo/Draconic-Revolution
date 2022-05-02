using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureHandler : MonoBehaviour
{
	private List<int> loadQueue = new List<int>();
	private Dictionary<int, Structure> structs = new Dictionary<int, Structure>();
	private List<byte> loadedBiomes = new List<byte>();
	public byte maxBiomesActive = 4;

    // Update is called once per frame
    void Update()
    {
    	// Loads up an element in loadQueue every frame
        if(loadQueue.Count > 0){
        	if(!structs.ContainsKey(loadQueue[0])){
        		structs.Add(loadQueue[0], Structure.Generate(loadQueue[0]));
        		loadQueue.RemoveAt(0);
        	}
        }
    }

    // Gets the requested Structure. Instantly loads it if still not loaded
    public Structure LoadStructure(int code){
    	// If struct is already loaded
    	if(structs.ContainsKey(code)){
    		return structs[code];
    	}

    	// If struct needs to be loaded
    	else{
    		structs.Add(code, Structure.Generate(code));
    		if(loadQueue.Contains(code)){
    			loadQueue.Remove(code);
    		}

    		return structs[code];
    	}
    }

    // Adds all Structs of a biome to the loadQueue
    public void LoadBiome(byte code){
    	if(!loadedBiomes.Contains(code)){
    		loadedBiomes.Add(code);
    		RemoveQueue();

    		foreach(int structCode in BiomeHandler.GetBiomeStructs((BiomeCode)code)){
    			if(loadQueue.Contains(structCode) || structs.ContainsKey(structCode)){
    				continue;
    			}
    			else{
    				loadQueue.Add(structCode);
    			}
    		}
    	}
    	else{
    		loadedBiomes.Remove(code);
    		loadedBiomes.Add(code);
    	}
    }

    // Removes structs from dict that are not in recent biomes
    private void RemoveQueue(){
    	byte biome;
    	bool found = false;

    	if(loadedBiomes.Count > this.maxBiomesActive){
    		biome = loadedBiomes[0];
    		loadedBiomes.RemoveAt(0);

    		// For every Struct in removed biome
    		foreach(int s in BiomeHandler.GetBiomeStructs((BiomeCode)biome)){
    			found = false;

    			// For every biome in loaded biomes
	    		foreach(byte b in loadedBiomes){
	    			if(BiomeHandler.GetBiomeStructs((BiomeCode)b).Contains(s)){
	    				found = true;
	    				break;
	    			}
	    		}
	    		// Removes if not found
	    		if(!found){
	    			structs.Remove(s);
	    		}
    		}
    	}
    }
}
