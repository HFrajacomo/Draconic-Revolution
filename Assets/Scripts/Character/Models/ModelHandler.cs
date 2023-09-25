using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ModelHandler{
	private static AssetBundle assets;

	private static Dictionary<ModelType, Dictionary<string, ModelInfo>> models = new Dictionary<ModelType, Dictionary<string, ModelInfo>>();

	private static readonly string ASSET_BUNDLE_RESPATH = "CharacterModels/characters";
	private static readonly string CLOTHES_DB = "CharacterModels/clothes_db";
	private static readonly string LEGS_DB = "CharacterModels/legs_db";
	private static readonly string BOOTS_DB = "CharacterModels/boots_db";
	private static readonly string HATS_DB = "CharacterModels/hats_db";
	private static readonly string HAIR_DB = "CharacterModels/hair_db";

	private static TextAsset cachedText;


	static ModelHandler(){
		assets = Resources.Load<AssetBundle>(ASSET_BUNDLE_RESPATH);
		LoadModelInfo();
	}

	public static void Run(){}

	public static GameObject GetModelObject(ModelType type, string name){
		return GameObject.Instantiate(assets.LoadAsset<GameObject>(name));
	}

	public static ModelInfo GetModelInfo(ModelType type, string name){
		return models[type][name];
	}

	public static List<ModelInfo> GetModelInfoList(ModelType t){
		List<ModelInfo> outList = new List<ModelInfo>();
		
		foreach(ModelInfo mi in models[t].Values){
			outList.Add(mi);
		}

		return outList;
	}

	private static void LoadModelInfo(){
		cachedText = Resources.Load<TextAsset>(CLOTHES_DB);
		ProcessTextAsset(ModelType.CLOTHES, cachedText.ToString());
		cachedText = Resources.Load<TextAsset>(LEGS_DB);
		ProcessTextAsset(ModelType.LEGS, cachedText.ToString());
		cachedText = Resources.Load<TextAsset>(BOOTS_DB);
		ProcessTextAsset(ModelType.FOOTGEAR, cachedText.ToString());
		cachedText = Resources.Load<TextAsset>(HATS_DB);
		ProcessTextAsset(ModelType.HEADGEAR, cachedText.ToString());
		cachedText = Resources.Load<TextAsset>(HAIR_DB);
		ProcessTextAsset(ModelType.HAIR, cachedText.ToString());
	}

	private static void ProcessTextAsset(ModelType t, string text){
		string[] lines = text.Split("\r\n");
		string[] lineElements;

		foreach(string line in lines){
			if(line.Length == 0)
				continue;
			if(line[0] == '#')
				continue;


			lineElements = line.Split('\t');

			if(!models.ContainsKey(t))
				models.Add(t, new Dictionary<string, ModelInfo>());

			models[t].Add(BuildName(lineElements), new ModelInfo(t, lineElements[0], lineElements[1], lineElements[2][0]));
		}
	}

	private static string BuildName(string[] miSerialized){
		return miSerialized[0] + "/" + miSerialized[2][0];
	}
}