using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class ActionLoader : BaseLoader {
	private static readonly string ACTION_LIST_RESPATH = "Actions/ACTION_LIST";
	private static readonly string ACTION_RESPATH = "Actions/";
	private static bool isClient;

	private static EntityAction[] actionBook;
	private static Dictionary<string, ushort> codenameToID = new Dictionary<string, ushort>();

	private static int amountOfActions = 0;
	private static List<string> actionEntries = new List<string>();

	public ActionLoader(bool client){
		isClient = client;
	}

	public override bool Load(){
		ParseActionList();
		LoadActions();

		return true;
	}

	public static ushort GetID(string codename){return codenameToID[codename];}
	public static EntityAction GetAction(ushort id){return actionBook[id];}
	public static EntityAction GetAction(string codename){return actionBook[codenameToID[codename]];}
	public static EntityAction GetCopy(ushort id){return actionBook[id].Copy();}
	public static EntityAction GetCopy(string codename){return actionBook[codenameToID[codename]].Copy();}

	public override void RunPostDeserializationRoutine(){
		foreach(EntityAction action in actionBook){
			action.PostDeserializationSetup();
		}
	}

	private void ParseActionList(){
		TextAsset textAsset = Resources.Load<TextAsset>(ACTION_LIST_RESPATH);

		if(textAsset == null){
			throw new DeserializationErrorException($"[ActionLoader] Couldn't locate ACTION_LIST while loading");
		}

		foreach(string line in textAsset.text.Replace("\r", "").Split("\n")){
			if(line.Length == 0)
				continue;
			if(line[0] == '#')
				continue;
			if(line[0] == ' ')
				continue;

			actionEntries.Add(line);
			amountOfActions++;
		}

		if(amountOfActions > ushort.MaxValue){
			throw new UshortLimitException($"[ActionLoader] Number of actions is bigger than ushort limitation. Draconic revolution cannot deal with that amount of actions");
		}
	}

	private void LoadActions(){
		TextAsset textAsset;
		EntityAction serializedAction;

		List<EntityAction> actionList = new List<EntityAction>();

		ushort i = 1;

		foreach(string action in actionEntries){
			textAsset = Resources.Load<TextAsset>($"{ACTION_RESPATH}{action}");

			if(textAsset != null){
				serializedAction = EntityActionDeserializer.DeserializeAction(textAsset.text);
				serializedAction.SetID(i);
				actionList.Add(serializedAction);
				codenameToID.Add(action, i);

				i++;
			}
			else{
				throw new DeserializationErrorException($"[ActionLoader] Action codename: {action} has no JSON information and wasn't loaded");
			}
		}

		actionBook = actionList.ToArray();
		actionList.Clear();
		actionList = null;
	}
}