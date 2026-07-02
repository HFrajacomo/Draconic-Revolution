using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationLoader : BaseLoader {
	private static Dictionary<string, RuntimeAnimatorController> controllers = new Dictionary<string, RuntimeAnimatorController>();
	private static Dictionary<string, AnimationStateMapping[]> stateMappings = new Dictionary<string, AnimationStateMapping[]>();
	private static Dictionary<string, BoneAnchorPoint[]> anchorMappings = new Dictionary<string, BoneAnchorPoint[]>();
	private static Dictionary<string, MultiAimData[]> rigs = new Dictionary<string, MultiAimData[]>();
	private static Dictionary<int, BattleStyleData> battleStyles = new Dictionary<int, BattleStyleData>();
	private static Dictionary<string, BattleStyleData> nameToBattleStyle = new Dictionary<string, BattleStyleData>();
	private static Dictionary<string, string> armatureName = new Dictionary<string, string>();
	private static bool isClient;

	private static readonly string CONTROLLERS_PATHS = "SerializedData/AnimatorControllers";
	private static readonly string ANIMATION_RESFOLDER = "Animations/";
	private static readonly string BATTLE_STYLE_RESFOLDER = "BattleStyles/";
	public static readonly string ANIMATION_CLIP_RESFOLDER = "AnimationClips/";

	
	public AnimationLoader(bool isClient){AnimationLoader.isClient = isClient;}

	public override bool Load(){
		if(isClient){
			LoadCharacterControllers();
			LoadAnchorBones();
			LoadStateMappings();
			LoadRigs();
			LoadArmatureName();
			LoadBattleStyles();
		}

		return true;
	}

	public static RuntimeAnimatorController GetController(string controller){return controllers[controller];}
	public static AnimationStateMapping[] GetAnimationMapping(string controller){return stateMappings[controller];}
	public static bool ContainsMapping(string controller){return stateMappings.ContainsKey(controller);}
	public static BoneAnchorPoint[] GetAnchorMapping(string controller){return anchorMappings[controller];}
	public static bool ContainsAnchor(string controller){return anchorMappings.ContainsKey(controller);}
	public static MultiAimData[] GetRig(string controller){return rigs[controller];}
	public static bool ContainsRig(string controller){return rigs.ContainsKey(controller);}
	public static string GetArmatureName(string controller){return armatureName[controller];}
	public static BattleStyleData GetBattleStyle(int style){return battleStyles[style];}
	public static BattleStyleData GetBattleStyle(string style){return nameToBattleStyle[style];}

	private void LoadArmatureName(){
		string respath;

		foreach(string controllerName in controllers.Keys){
			respath = $"{ANIMATION_RESFOLDER}{controllerName}/armature";

			TextAsset armature = Resources.Load<TextAsset>(respath);

			if(armature == null){
				throw new AnimationImportException($"Couldn't locate the Armature Name: {respath} while loading Animations");
			}

			armatureName.Add(controllerName, armature.text);
		}	
	}

	private void LoadAnchorBones(){
		string respath;
		Wrapper<BoneAnchorPoint> wrapper;

		foreach(string controllerName in controllers.Keys){
			respath = $"{ANIMATION_RESFOLDER}{controllerName}/anchors";

			TextAsset anchorJson = Resources.Load<TextAsset>(respath);

			if(anchorJson == null){
				throw new AnimationImportException($"Couldn't locate the Anchor Mapping: {respath} while loading Animations");
			}

			wrapper = JsonUtility.FromJson<Wrapper<BoneAnchorPoint>>(JsonFormatter.RemoveComments(anchorJson.text));

			foreach(BoneAnchorPoint anchor in wrapper.data){
				anchor.PostDeserializationSetup();
			}

			anchorMappings.Add(controllerName, wrapper.data);
		}
	}

	private void LoadRigs(){
		string respath;
		Wrapper<MultiAimData> wrapper;

		foreach(string controllerName in controllers.Keys){
			respath = $"{ANIMATION_RESFOLDER}{controllerName}/rigs";

			TextAsset rigsJson = Resources.Load<TextAsset>(respath);

			if(rigsJson == null){
				throw new AnimationImportException($"Couldn't locate the Rigs: {respath} while loading Animations");
			}

			wrapper = JsonUtility.FromJson<Wrapper<MultiAimData>>(JsonFormatter.RemoveComments(rigsJson.text));

			foreach(MultiAimData rig in wrapper.data){
				rig.PostDeserializationSetup();
			}

			rigs.Add(controllerName, wrapper.data);
		}		
	}

	private void LoadCharacterControllers(){
		RuntimeAnimatorController currentController;

		TextAsset controllerJson = Resources.Load<TextAsset>(CONTROLLERS_PATHS);

		if(controllerJson == null){
			throw new AnimationImportException($"Couldn't locate the AnimatorController Mappings in RESPATH: {CONTROLLERS_PATHS} while loading RuntimeAnimatorController");
		}

		Wrapper<ValuePair<string, string>> wrapper = JsonUtility.FromJson<Wrapper<ValuePair<string, string>>>(JsonFormatter.RemoveComments(controllerJson.text));

		foreach(ValuePair<string, string> vp in wrapper.data){
			currentController = Resources.Load<RuntimeAnimatorController>(vp.value);

			if(currentController == null){
				throw new AnimationImportException($"AnimatorController was not found in Resources Path: {vp.value}");
			}

			controllers.Add(vp.key, currentController);
		}
	}

	private void LoadStateMappings(){
		string respath;
		Wrapper<AnimationStateMapping> wrapper;

		foreach(string controllerName in controllers.Keys){
			respath = $"{ANIMATION_RESFOLDER}{controllerName}/mappings";

			TextAsset mappingJson = Resources.Load<TextAsset>(respath);

			if(mappingJson == null){
				throw new AnimationImportException($"Couldn't locate the AnimationMapping: {respath} while loading Animations");
			}

			wrapper = JsonUtility.FromJson<Wrapper<AnimationStateMapping>>(JsonFormatter.RemoveComments(mappingJson.text));

			foreach(AnimationStateMapping mapping in wrapper.data){
				mapping.PostDeserializationSetup();
			}

			stateMappings.Add(controllerName, wrapper.data);
		}
	}

	private void LoadBattleStyles(){
		BattleStyleData bsd;
        TextAsset[] assets = Resources.LoadAll<TextAsset>(BATTLE_STYLE_RESFOLDER);
        int styleCode = 0;

        foreach(TextAsset asset in assets){
        	bsd = JsonUtility.FromJson<BattleStyleData>(JsonFormatter.RemoveComments(asset.text));
        	bsd.PostDeserializationSetup(asset.name, styleCode);
			battleStyles.Add(styleCode, bsd);
			nameToBattleStyle.Add(bsd.GetName(), bsd);

			styleCode++;
        }
	}
}