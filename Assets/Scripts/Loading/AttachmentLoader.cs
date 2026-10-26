using System;
using System.Collections.Generic;
using UnityEngine;

/*
Loads everything that BattleStyle attachments need in order to be rendered (client only)
	Preloads every FBX model referenced by BattleStyles and every base Material referenced by Items and BattleStyle defaults
	Item-specific values are applied at runtime through MaterialPropertyBlocks, so no Material is ever created per item
	Item textures are not loaded here; they are lazily loaded by AttachmentTextureCache
*/
public class AttachmentLoader : BaseLoader {
	private static bool isClient;

	private static readonly string ITEM_MODEL_RESPATH = "ItemModels/";
	private static readonly string BASE_MATERIAL_RESPATH = "ItemModels/Materials/";

	private static Dictionary<string, GameObject> models = new Dictionary<string, GameObject>();
	private static Dictionary<string, Material> baseMaterials = new Dictionary<string, Material>();


	public AttachmentLoader(bool client){isClient = client;}

	public override bool Load(){
		if(!isClient)
			return true;

		LoadModels();
		LoadBaseMaterials();

		return true;
	}

	public static GameObject GetModel(string fbxName){return models[fbxName];}
	public static Material GetBaseMaterial(string name){return baseMaterials[name];}

	/*
	Finds which material descriptor should be applied to the attachment in the given index
		Priority: linked item's Attachment section -> BattleStyle defaultMaterial -> null (FBX imported materials)
	*/
	public static AttachmentMaterialData ResolveDescriptor(AttachmentData[] attachments, int attachmentIndex, AttachmentItems items){
		ushort itemID = items.GetItemFor(attachments, attachmentIndex);
		AttachmentMaterialData data;

		if(itemID != 0){
			data = ItemLoader.GetItem(itemID).GetAttachmentMaterials();

			if(data != null)
				return data;
		}

		return attachments[attachmentIndex].GetDefaultMaterial();
	}

	private void LoadModels(){
		BattleStyleData style;
		GameObject prefab;

		for(int code=0; code < AnimationLoader.GetBattleStyleCount(); code++){
			style = AnimationLoader.GetBattleStyle(code);

			if(style.attachments == null)
				continue;

			for(int i=0; i < style.attachments.Length; i++){
				if(models.ContainsKey(style.attachments[i].fbxName))
					continue;

				prefab = Resources.Load<GameObject>($"{ITEM_MODEL_RESPATH}{style.attachments[i].fbxName}");

				if(prefab == null)
					throw new DeserializationErrorException($"[AttachmentLoader] BattleStyle {style.GetName()} references model {style.attachments[i].fbxName}, which wasn't found in Resources/{ITEM_MODEL_RESPATH}");

				models.Add(style.attachments[i].fbxName, prefab);
			}
		}
	}

	private void LoadBaseMaterials(){
		HashSet<string> warnedProperties = new HashSet<string>();
		BattleStyleData style;
		Item item;

		for(int id=0; id < ItemLoader.GetItemCount(); id++){
			item = ItemLoader.GetItem((ushort)id);
			RegisterDescriptor(item.GetAttachmentMaterials(), $"Item {item.codename}", warnedProperties);
		}

		for(int code=0; code < AnimationLoader.GetBattleStyleCount(); code++){
			style = AnimationLoader.GetBattleStyle(code);

			if(style.attachments == null)
				continue;

			for(int i=0; i < style.attachments.Length; i++){
				RegisterDescriptor(style.attachments[i].GetDefaultMaterial(), $"BattleStyle {style.GetName()}", warnedProperties);
			}
		}
	}

	private void RegisterDescriptor(AttachmentMaterialData data, string owner, HashSet<string> warnedProperties){
		AttachmentMaterialEntry entry;
		Material material;

		if(data == null)
			return;

		for(int i=0; i < data.materials.Length; i++){
			entry = data.materials[i];

			if(!baseMaterials.TryGetValue(entry.material, out material)){
				material = Resources.Load<Material>($"{BASE_MATERIAL_RESPATH}{entry.material}");

				if(material == null)
					throw new DeserializationErrorException($"[AttachmentLoader] {owner} references base Material {entry.material}, which wasn't found in Resources/{BASE_MATERIAL_RESPATH}");

				baseMaterials.Add(entry.material, material);
			}

			for(int j=0; j < entry.properties.Length; j++){
				if(material.HasProperty(entry.properties[j].GetPropertyID()))
					continue;

				if(warnedProperties.Add($"{entry.material}/{entry.properties[j].name}"))
					Debug.LogWarning($"[AttachmentLoader] Base Material {entry.material} has no property {entry.properties[j].name} (first referenced by {owner})");
			}
		}
	}
}
