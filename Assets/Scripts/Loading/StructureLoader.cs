using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureLoader : BaseLoader{
	public static readonly string STRUCTURE_RESPATH = "Structures/";
	public static readonly string STRUCTURE_LIST_RESPATH = "Structures/STRUCTURE_LIST.txt";

	private static bool isClient;
	private static Dictionary<string, Structure> structures = new Dictionary<string, Structure>();
	private static List<string> structureNames = new List<string>();

	public StructureLoader(bool client){
		isClient = client;
	}

	public override bool Load(){
		if(isClient)
			return false;

		ParseStructureList();
		LoadStructures();

		return true;
	}

	public static Structure GetStructure(string name){return structures[name];}

	public override void RunPostDeserializationRoutine(){
		if(isClient)
			return;

		foreach(Structure st in structures.Values){
			st.SetupAfterSerialize();
		}
	}

	private void ParseStructureList(){
		TextAsset textAsset = Resources.Load<TextAsset>(STRUCTURE_LIST_RESPATH);

		if(textAsset == null){
			Debug.Log("Couldn't Locate the STRUCTURE_LIST while loading the StructureLoader");
			Application.Quit();
		}


		foreach(string line in textAsset.text.Replace("\r", "").Split("\n")){
			if(line.Length == 0)
				continue;
			if(line[0] == '#')
				continue;
			if(line[0] == ' ')
				continue;

			structureNames.Add(line);
		}
	}

	private void LoadStructures(){
		TextAsset textAsset;
		Structure serializedStruct;

		List<Structure> structureList = new List<Structure>();

		foreach(string structure in structureNames){
			textAsset = Resources.Load<TextAsset>($"{STRUCTURE_RESPATH}{structure}");

			if(textAsset != null){
				serializedStruct = JsonUtility.FromJson<Structure>(textAsset.text);
				structures.Add(structure, serializedStruct);
			}
			else{
				Debug.Log($"Structure codename: {structure} has no JSON information and wasn't loaded");
				Application.Quit();
			}
		}

		structureNames.Clear();
	}
}