using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureLoader : BaseLoader{
	private static readonly string STRUCTURE_RESPATH = "Structures/Standalone/";
	private static readonly string STRUCTURE_LIST_RESPATH = "Structures/Standalone/STRUCTURE_LIST";
	private static readonly string STRUCTUREGROUP_LIST_RESPATH = "Structures/Group/STRUCTUREGROUPS";

	private static bool isClient;
	private static Dictionary<string, Structure> structures = new Dictionary<string, Structure>();
	private static Dictionary<string, StructureGroup> structureGroups = new Dictionary<string, StructureGroup>();
	private static BiMap<string, ushort> codeBimap = new BiMap<string, ushort>();
	private static List<string> structureNames = new List<string>();

	public StructureLoader(bool client){
		isClient = client;
	}

	public override bool Load(){
		if(isClient)
			return false;

		ParseStructureList();
		LoadStructures();
		LoadStructureGroups();

		return true;
	}

	public static Structure GetStructure(string name){return structures[name];}
	public static ushort GetStructureID(string name){return codeBimap.Get(name);}
	public static string GetStructureName(ushort id){return codeBimap.Get(id);}
	public static StructureGroup GetStructureGroup(string name){return structureGroups[name];}

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

	private void LoadStructureGroups(){
		TextAsset textAsset = Resources.Load<TextAsset>(STRUCTUREGROUP_LIST_RESPATH);

		if(textAsset == null){
			Debug.Log("Couldn't Locate the STRUCTUREGROUP file while loading the StructureLoader");
			Application.Quit();
		}

		Wrapper<StructureGroup> wrapper = JsonUtility.FromJson<Wrapper<StructureGroup>>(textAsset.text);

		for(int i=0; i < wrapper.data.Length; i++){
			structureGroups.Add(wrapper.data[i].name, wrapper.data[i]);
		}
	}
}