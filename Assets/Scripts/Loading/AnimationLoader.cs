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
	private static Dictionary<int, CarryStyleData> carryStyles = new Dictionary<int, CarryStyleData>();
	private static Dictionary<string, CarryStyleData> nameToCarryStyle = new Dictionary<string, CarryStyleData>();
	private static Dictionary<string, string> armatureName = new Dictionary<string, string>();
	private static bool isClient;

	private static readonly string CONTROLLERS_PATHS = "SerializedData/AnimatorControllers";
	private static readonly string ANIMATION_RESFOLDER = "Animations/";
	private static readonly string BATTLE_STYLE_RESFOLDER = "BattleStyles/";
	private static readonly string CARRY_STYLE_RESFOLDER = "CarryStyles/";
	public static readonly string ANIMATION_CLIP_RESFOLDER = "AnimationClips/";

	
	public AnimationLoader(bool isClient){AnimationLoader.isClient = isClient;}

	public override bool Load(){
		if(isClient){
			LoadCharacterControllers();
			LoadAnchorBones();
			LoadStateMappings();
			LoadRigs();
			LoadArmatureName();
		}

		LoadBattleStyles();
		LoadCarryStyles();
		ValidateSubWeaponCarries();
		ValidateCarryAnchors();

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
	public static bool ContainsBattleStyle(string style){return nameToBattleStyle.ContainsKey(style);}
	public static int GetBattleStyleCount(){return battleStyles.Count;}
	public static CarryStyleData GetCarryStyle(int style){return carryStyles[style];}
	public static CarryStyleData GetCarryStyle(string style){return nameToCarryStyle[style];}
	public static bool ContainsCarryStyle(string style){return nameToCarryStyle.ContainsKey(style);}
	public static int GetCarryStyleCount(){return carryStyles.Count;}

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
        	bsd.PostDeserializationSetup(asset.name, styleCode, isClient);
			battleStyles.Add(styleCode, bsd);
			nameToBattleStyle.Add(bsd.GetName(), bsd);

			styleCode++;
        }
	}

	private void LoadCarryStyles(){
		CarryStyleData csd;
		TextAsset[] assets = Resources.LoadAll<TextAsset>(CARRY_STYLE_RESFOLDER);
		int styleCode = 0;

		foreach(TextAsset asset in assets){
			csd = JsonUtility.FromJson<CarryStyleData>(JsonFormatter.RemoveComments(asset.text));
			csd.PostDeserializationSetup(asset.name, styleCode, isClient);
			carryStyles.Add(styleCode, csd);
			nameToCarryStyle.Add(csd.GetName(), csd);

			styleCode++;
		}
	}

	// Every SubWeapon that names a Carry Style must reference an existing Third Person one
	private void ValidateSubWeaponCarries(){
		SubWeapon sub;

		for(int id=0; id < ItemLoader.GetItemCount(); id++){
			sub = ItemLoader.GetItem((ushort)id) as SubWeapon;

			if(sub == null || !sub.HasCarryStyle())
				continue;

			if(!nameToCarryStyle.ContainsKey(sub.GetCarryStyle()))
				throw new DeserializationErrorException($"[AnimationLoader] SubWeapon {sub.codename} references Carry Style {sub.GetCarryStyle()}, which wasn't found in Resources/{CARRY_STYLE_RESFOLDER}");

			if(nameToCarryStyle[sub.GetCarryStyle()].firstPerson)
				throw new DeserializationErrorException($"[AnimationLoader] SubWeapon {sub.codename} must reference a Third Person Carry Style, but {sub.GetCarryStyle()} is First Person");
		}
	}

	/*
	Carry attachments live together with the attachments of every style that allows carrying
		Since switching an attachment into an occupied anchor destroys the occupant, both sides must never share an anchor
		Carry attachments must also start stowed, so they can't default to a hand anchor
	*/
	private void ValidateCarryAnchors(){
		HashSet<BoneAnchorType> carryAnchors = new HashSet<BoneAnchorType>();
		HashSet<BoneAnchorType> mainAnchors = new HashSet<BoneAnchorType>();
		AttachmentData[] carryAttachments;

		foreach(CarryStyleData carryStyle in carryStyles.Values){
			carryAttachments = carryStyle.GetAttachments();
			carryAnchors.Clear();

			if(carryAttachments != null){
				for(int i=0; i < carryAttachments.Length; i++){
					if(IsHandAnchor(carryAttachments[i].GetAnchorType()))
						throw new DeserializationErrorException($"[AnimationLoader] Carry attachment {carryAttachments[i].fbxName} of Carry Style {carryStyle.GetName()} must start stowed, but is anchored to {carryAttachments[i].GetAnchorType()}");
				}
			}

			CollectAnchors(carryAttachments, carryStyle.GetOverrides(), carryAnchors);

			foreach(BattleStyleData mainStyle in battleStyles.Values){
				if(mainStyle.firstPerson != carryStyle.firstPerson)
					continue;

				if(!AllowsCarry(mainStyle))
					continue;

				mainAnchors.Clear();
				CollectAnchors(mainStyle.attachments, mainStyle.GetOverrides(), mainAnchors);
				mainAnchors.IntersectWith(carryAnchors);

				if(mainAnchors.Count > 0)
					throw new DeserializationErrorException($"[AnimationLoader] Carry Style {carryStyle.GetName()} shares the anchors [{string.Join(", ", mainAnchors)}] with BattleStyle {mainStyle.GetName()}, which allows carrying");
			}
		}
	}

	// First Person styles take the allowsCarry flag from their Third Person counterpart
	private bool AllowsCarry(BattleStyleData style){
		if(!style.firstPerson)
			return style.AllowsCarry();

		string tpName = style.GetName().Substring(0, style.GetName().Length - 3);

		if(!nameToBattleStyle.ContainsKey(tpName))
			return false;

		return nameToBattleStyle[tpName].AllowsCarry();
	}

	private void CollectAnchors(AttachmentData[] attachments, StateClipPair[] overrides, HashSet<BoneAnchorType> anchors){
		AN_SwitchAttachmentAnchorBehaviour switchBehaviour;

		if(attachments != null){
			for(int i=0; i < attachments.Length; i++){
				anchors.Add(attachments[i].GetAnchorType());
			}
		}

		if(overrides == null)
			return;

		for(int i=0; i < overrides.Length; i++){
			if(overrides[i].events == null)
				continue;

			foreach(AnimationEventData ev in overrides[i].events){
				if(ev.type != "AN_SwitchAttachmentAnchorBehaviour")
					continue;

				switchBehaviour = JsonUtility.FromJson<AN_SwitchAttachmentAnchorBehaviour>(JsonFormatter.RemoveComments(ev.json));
				AddAnchor(switchBehaviour.originAnchor, anchors);
				AddAnchor(switchBehaviour.targetAnchor, anchors);
			}
		}
	}

	private void AddAnchor(string anchorName, HashSet<BoneAnchorType> anchors){
		if(Enum.TryParse<BoneAnchorType>(anchorName, false, out BoneAnchorType parsed))
			anchors.Add(parsed);
	}

	private bool IsHandAnchor(BoneAnchorType anchor){return anchor == BoneAnchorType.BASE_CHARACTER_HAND_RIGHT || anchor == BoneAnchorType.BASE_CHARACTER_HAND_LEFT;}
}