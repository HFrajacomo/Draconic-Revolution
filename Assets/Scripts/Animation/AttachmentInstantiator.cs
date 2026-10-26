using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/*
Instantiates attachment models and assigns them to "--- LoadedAttachments ---" organizer object.
Models are preloaded by AttachmentLoader. Item-specific Material values are applied through MaterialPropertyBlocks
*/

public static class AttachmentInstantiator {
	private static readonly Transform organizerObject;
	private static Dictionary<string, int> instanceCounter = new Dictionary<string, int>();
	private static Vector3 SCALING = new Vector3(.01f, .01f, .01f);
	private static MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

	static AttachmentInstantiator(){
		organizerObject = GameObject.Find("----- LoadedAttachments -----").transform;
	}

	public static GameObject Instantiate(string name, bool castShadows){
		GameObject instance;
		MeshRenderer renderer;

		if(!instanceCounter.ContainsKey(name)){
			instanceCounter.Add(name, 1);			
		}

		instance = GameObject.Instantiate(AttachmentLoader.GetModel(name));
		instance.transform.parent = organizerObject;
		instance.transform.localScale = SCALING;
		instance.name = $"{name}_{instanceCounter[name]}";
		instanceCounter[name] += 1;

		if(!castShadows){
			renderer = instance.GetComponent<MeshRenderer>();

			if(renderer != null){
				renderer.shadowCastingMode = ShadowCastingMode.Off;
			}
		}

		return instance;
	}

	public static void ApplyTransform(GameObject go, bool flip, float heightOffset=0f){
		go.transform.localPosition = Vector3.up * heightOffset;
		go.transform.localScale = SCALING;
		
		if(flip){
			go.transform.localRotation = Quaternion.Euler(Vector3.right * 180f);
		}
		else{
			go.transform.localRotation = Quaternion.Euler(Vector3.zero);

		}
	}

	/*
	Applies a material descriptor to an attachment instance and adds a reference to its textures
		Slots not described (or a null descriptor) go back to the model's imported materials with no overrides
		Must be paired with a ReleaseSkin(skin) call when the skin is replaced or the attachment is destroyed
	*/
	public static void ApplySkin(GameObject go, string fbxName, AttachmentMaterialData skin){
		Renderer renderer = go.GetComponentInChildren<Renderer>();
		Material[] materials;
		AttachmentMaterialProperty property;
		Texture2D texture;

		AttachmentTextureCache.Acquire(skin);

		if(renderer == null)
			return;

		materials = AttachmentLoader.GetModel(fbxName).GetComponentInChildren<Renderer>().sharedMaterials;

		if(skin != null){
			if(skin.materials.Length > materials.Length)
				Debug.LogWarning($"[AttachmentInstantiator] Skin defines {skin.materials.Length} materials, but {fbxName} only has {materials.Length} material slots");

			for(int i=0; i < skin.materials.Length && i < materials.Length; i++){
				materials[i] = AttachmentLoader.GetBaseMaterial(skin.materials[i].material);
			}
		}

		renderer.sharedMaterials = materials;

		for(int i=0; i < materials.Length; i++){
			if(skin == null || i >= skin.materials.Length || skin.materials[i].properties.Length == 0){
				renderer.SetPropertyBlock(null, i);
				continue;
			}

			propertyBlock.Clear();

			for(int j=0; j < skin.materials[i].properties.Length; j++){
				property = skin.materials[i].properties[j];

				switch(property.GetPropertyType()){
					case AttachmentMaterialPropertyType.FLOAT:
						propertyBlock.SetFloat(property.GetPropertyID(), property.floatValue);
						break;
					case AttachmentMaterialPropertyType.COLOR:
						propertyBlock.SetColor(property.GetPropertyID(), property.colorValue);
						break;
					case AttachmentMaterialPropertyType.TEXTURE:
						texture = AttachmentTextureCache.Get(property.textureValue);

						if(texture != null)
							propertyBlock.SetTexture(property.GetPropertyID(), texture);
						break;
				}
			}

			renderer.SetPropertyBlock(propertyBlock, i);
		}
	}

	// Removes the texture references added by ApplySkin
	public static void ReleaseSkin(AttachmentMaterialData skin){
		AttachmentTextureCache.Release(skin);
	}
}