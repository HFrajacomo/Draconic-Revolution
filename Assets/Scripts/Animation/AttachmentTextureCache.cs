using System;
using System.Collections.Generic;
using UnityEngine;

/*
Lazily loads and reference counts the textures used by attachment MaterialPropertyBlocks
	Textures with no references are moved to a FIFO pool of unused textures, so they can be reused without reloading
	Whenever the pool exceeds its capacity, the oldest unused texture is unloaded
	Textures handled here must never be assigned directly in a base Material, as they may be unloaded at any time
*/
public static class AttachmentTextureCache {
	private static readonly string TEXTURE_RESPATH = "ItemModels/Textures/";
	private static readonly int UNUSED_POOL_CAPACITY = 100;

	private static Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
	private static Dictionary<string, int> references = new Dictionary<string, int>();
	private static LinkedList<string> unusedPool = new LinkedList<string>();
	private static Dictionary<string, LinkedListNode<string>> unusedNodes = new Dictionary<string, LinkedListNode<string>>();
	private static HashSet<string> missingTextures = new HashSet<string>();


	// Adds a reference to every texture in the descriptor
	public static void Acquire(AttachmentMaterialData data){
		if(data == null)
			return;

		for(int i=0; i < data.materials.Length; i++){
			for(int j=0; j < data.materials[i].properties.Length; j++){
				if(data.materials[i].properties[j].GetPropertyType() == AttachmentMaterialPropertyType.TEXTURE)
					Acquire(data.materials[i].properties[j].textureValue);
			}
		}
	}

	// Removes a reference from every texture in the descriptor
	public static void Release(AttachmentMaterialData data){
		if(data == null)
			return;

		for(int i=0; i < data.materials.Length; i++){
			for(int j=0; j < data.materials[i].properties.Length; j++){
				if(data.materials[i].properties[j].GetPropertyType() == AttachmentMaterialPropertyType.TEXTURE)
					Release(data.materials[i].properties[j].textureValue);
			}
		}
	}

	// Returns an already acquired texture, or null if it couldn't be loaded
	public static Texture2D Get(string name){
		if(textures.TryGetValue(name, out Texture2D texture))
			return texture;
		return null;
	}

	public static Texture2D Acquire(string name){
		Texture2D texture;
		LinkedListNode<string> node;

		if(textures.TryGetValue(name, out texture)){
			if(unusedNodes.TryGetValue(name, out node)){
				unusedPool.Remove(node);
				unusedNodes.Remove(name);
			}

			references[name]++;
			return texture;
		}

		texture = Resources.Load<Texture2D>($"{TEXTURE_RESPATH}{name}");

		if(texture == null){
			if(missingTextures.Add(name))
				Debug.LogError($"[AttachmentTextureCache] Texture {name} wasn't found in Resources/{TEXTURE_RESPATH}");
			return null;
		}

		textures.Add(name, texture);
		references.Add(name, 1);

		return texture;
	}

	public static void Release(string name){
		if(!references.ContainsKey(name) || references[name] == 0)
			return;

		references[name]--;

		if(references[name] > 0)
			return;

		unusedNodes.Add(name, unusedPool.AddLast(name));

		if(unusedPool.Count > UNUSED_POOL_CAPACITY)
			Evict(unusedPool.First.Value);
	}

	private static void Evict(string name){
		Texture2D texture = textures[name];

		unusedPool.Remove(unusedNodes[name]);
		unusedNodes.Remove(name);
		references.Remove(name);
		textures.Remove(name);

		Resources.UnloadAsset(texture);
	}
}
