using System;
using System.Collections.Generic;
using UnityEngine;

/*
Loads everything that BattleStyle attachments need in order to be rendered (client only)
	Preloads every FBX model referenced by BattleStyles and Carry Styles, and every base Material referenced by Items and BattleStyle/Carry Style defaults
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

	/*
	Finds which material descriptor should be applied to a carry attachment
		Priority: off-hand item's Attachment section -> BattleStyle defaultMaterial -> null (FBX imported materials)
	*/
	public static AttachmentMaterialData ResolveCarryDescriptor(AttachmentData[] attachments, int attachmentIndex, AttachmentItems items){
		AttachmentMaterialData data;

		if(items.leftHand != 0){
			data = ItemLoader.GetItem(items.leftHand).GetAttachmentMaterials();

			if(data != null)
				return data;
		}

		return attachments[attachmentIndex].GetDefaultMaterial();
	}

	private void LoadModels(){
		BattleStyleData style;
		CarryStyleData carryStyle;

		for(int code=0; code < AnimationLoader.GetBattleStyleCount(); code++){
			style = AnimationLoader.GetBattleStyle(code);
			LoadModels(style.GetName(), style.attachments);
		}

		for(int code=0; code < AnimationLoader.GetCarryStyleCount(); code++){
			carryStyle = AnimationLoader.GetCarryStyle(code);
			LoadModels($"Carry Style {carryStyle.GetName()}", carryStyle.GetAttachments());
		}
	}

	private void LoadModels(string owner, AttachmentData[] attachments){
		GameObject prefab;

		if(attachments == null)
			return;

		for(int i=0; i < attachments.Length; i++){
			if(models.ContainsKey(attachments[i].fbxName))
				continue;

			prefab = Resources.Load<GameObject>($"{ITEM_MODEL_RESPATH}{attachments[i].fbxName}");

			if(prefab == null)
				throw new DeserializationErrorException($"[AttachmentLoader] {owner} references model {attachments[i].fbxName}, which wasn't found in Resources/{ITEM_MODEL_RESPATH}");

			models.Add(attachments[i].fbxName, prefab);
		}
	}

	private void LoadBaseMaterials(){
		HashSet<string> warnedProperties = new HashSet<string>();
		BattleStyleData style;
		CarryStyleData carryStyle;
		Item item;

		for(int id=0; id < ItemLoader.GetItemCount(); id++){
			item = ItemLoader.GetItem((ushort)id);
			RegisterDescriptor(item.GetAttachmentMaterials(), $"Item {item.codename}", warnedProperties);
		}

		for(int code=0; code < AnimationLoader.GetBattleStyleCount(); code++){
			style = AnimationLoader.GetBattleStyle(code);
			RegisterDescriptors(style.attachments, $"BattleStyle {style.GetName()}", warnedProperties);
		}

		for(int code=0; code < AnimationLoader.GetCarryStyleCount(); code++){
			carryStyle = AnimationLoader.GetCarryStyle(code);
			RegisterDescriptors(carryStyle.GetAttachments(), $"Carry Style {carryStyle.GetName()}", warnedProperties);
		}
	}

	private void RegisterDescriptors(AttachmentData[] attachments, string owner, HashSet<string> warnedProperties){
		if(attachments == null)
			return;

		for(int i=0; i < attachments.Length; i++){
			RegisterDescriptor(attachments[i].GetDefaultMaterial(), owner, warnedProperties);
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
