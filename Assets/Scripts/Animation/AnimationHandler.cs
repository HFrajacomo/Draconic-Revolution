using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

public class AnimationHandler : MonoBehaviour {
	private bool INIT = false;
	private bool isPlayer = false;
	private string controllerName;
	private string controllerNameFP;

	private Animator tpAnimator;
	private Animator fpAnimator;
	private ShapeKeyAnimator shapeKeyAnimator;
	private ProceduralAnimationRigController rigControllerTP;
	private ProceduralAnimationRigController rigControllerFP;
	private BattleStyleData battleStyle;
	private BattleStyleData battleStyleFP;
	private float animationCrossfadeTime = 0.06f;

	private Dictionary<BoneAnchorType, GameObject> attachmentsTP = new Dictionary<BoneAnchorType, GameObject>();
	private Dictionary<BoneAnchorType, int> attachmentDataIndexTP = new Dictionary<BoneAnchorType, int>();
	private Dictionary<BoneAnchorType, AttachmentMaterialData> attachmentSkinTP = new Dictionary<BoneAnchorType, AttachmentMaterialData>();
	private Dictionary<BoneAnchorType, GameObject> attachmentsFP = new Dictionary<BoneAnchorType, GameObject>();
	private Dictionary<BoneAnchorType, int> attachmentDataIndexFP = new Dictionary<BoneAnchorType, int>();
	private Dictionary<BoneAnchorType, AttachmentMaterialData> attachmentSkinFP = new Dictionary<BoneAnchorType, AttachmentMaterialData>();
	private List<BoneAnchorType> cachedAnchors = new List<BoneAnchorType>();

	private static Dictionary<string, Dictionary<string, AnimationStateMapping>> stateMappings;
	private static Dictionary<string, Dictionary<BoneAnchorType, string>> anchorMappings;
	private static Dictionary<string, Dictionary<int, string>> hashToName;


	public void Init(string controllerName, CharacterBuilder firstPersonBuilder, bool isUserCharacter=false){
		this.controllerName = controllerName;
		this.controllerNameFP = "";

		Transform tpParent = this.transform.Find("TP-Rig");
		Transform tpAnimObj = tpParent.Find("Animator");
			
		if(AnimationHandler.hashToName == null)
			AnimationHandler.hashToName = new Dictionary<string, Dictionary<int, string>>();
		
		LoadMapping();
		LoadAnchors();
		this.isPlayer = isUserCharacter;

		this.tpAnimator = tpAnimObj.GetComponent<Animator>();
		this.shapeKeyAnimator = tpParent.GetComponent<ShapeKeyAnimator>();
		this.rigControllerTP = new ProceduralAnimationRigController(tpParent.gameObject, tpAnimObj.gameObject, this.controllerName);
		this.rigControllerTP.Build();

		if(this.isPlayer){
			Transform fpParent = this.transform.Find("Camera/FP-Rig");
			Transform fpAnimObj = fpParent.Find("Animator");
			
			this.fpAnimator = fpAnimObj.GetComponent<Animator>();
			this.rigControllerFP = new ProceduralAnimationRigController(fpParent.gameObject, fpAnimObj.gameObject, $"{this.controllerName}_FP");
			this.rigControllerFP.Build();
			LoadMappingFP();
			LoadAnchorsFP();
		}

		this.INIT = true;
	}


	// Plays bone animation
	public void Play(string stateName, bool overrideState=false, bool ignoreFP=false){
		if(!this.INIT)
			return;

		bool skipThirdPerson = false;
		AnimationStateMapping givenMap, currentMap, currentMapFP;

		givenMap = AnimationHandler.stateMappings[this.controllerName][stateName];

		if(!overrideState){
			currentMap = AnimationHandler.stateMappings[this.controllerName][AnimationHandler.hashToName[this.controllerName][GetState(this.tpAnimator.GetLayerIndex(givenMap.layers[0])).shortNameHash]];

			if(this.isPlayer){
				currentMapFP = AnimationHandler.stateMappings[this.controllerNameFP][AnimationHandler.hashToName[this.controllerNameFP][GetStateFP(0).shortNameHash]];
			}

			if(givenMap.state == currentMap.state){
				StopLayer(givenMap.stopLayer);
				skipThirdPerson = true;
			}
		}
		else{
			currentMap = givenMap;
		}

		if(skipThirdPerson){}
		else if(VerifyLayerStates(givenMap.layers[0], currentMap.priority)){}
		else if(overrideState){
			StopLayer(givenMap.stopLayer);
			this.tpAnimator.CrossFade(stateName, this.animationCrossfadeTime, layer:this.tpAnimator.GetLayerIndex(givenMap.layers[0]));

			if(this.isPlayer && !ignoreFP){
				if(this.fpAnimator.HasState(0, Animator.StringToHash(stateName))){
					this.fpAnimator.CrossFade(stateName, this.animationCrossfadeTime);
				}
				else{
					this.fpAnimator.CrossFade("Empty", this.animationCrossfadeTime);
				}
			}
		}
		else{
			for(int i=0; i < givenMap.layers.Length; i++){
				currentMap = AnimationHandler.stateMappings[this.controllerName][AnimationHandler.hashToName[this.controllerName][GetState(this.tpAnimator.GetLayerIndex(givenMap.layers[i])).shortNameHash]];

				if(givenMap.priority <= currentMap.priority){
					StopLayer(givenMap.stopLayer);
					this.tpAnimator.CrossFade(stateName, this.animationCrossfadeTime, layer:this.tpAnimator.GetLayerIndex(givenMap.layers[i]));

					break;
				}
			}
		}

		// Handling First Person
		if(this.isPlayer && !ignoreFP && !overrideState){
			currentMapFP = AnimationHandler.stateMappings[this.controllerNameFP][AnimationHandler.hashToName[this.controllerNameFP][GetStateFP(0).shortNameHash]];

			if(!this.fpAnimator.HasState(0, Animator.StringToHash(stateName))){
				givenMap = AnimationHandler.stateMappings[this.controllerNameFP]["Empty"];
			}

			if(givenMap.state != currentMapFP.state){
				if(givenMap.priority <= currentMapFP.priority){
					this.fpAnimator.CrossFade(givenMap.state, this.animationCrossfadeTime);
				}
			}
		}
	}

	// Force plays a state for non-player characters (Used by Client)
	public void Play(string state, int layer){
		this.tpAnimator.CrossFade(state, this.animationCrossfadeTime, layer:layer);
	}

	// Plays/Stops/Registers ShapeKey Animations based on the settings inputted
	public void Play(string shapeKey, CustomAnimationSettings settings){
		if(!this.INIT)
			return;

		this.shapeKeyAnimator.Play(shapeKey, settings);
	}


	// Looks for every Layer to find if the current playing State is StateName and return the normalizedTime
	// Return -1 if no state like that is found
	public float GetAnimationTime(string stateName){
		AnimatorStateInfo stateInfo;

		for(int i=0; i < this.tpAnimator.layerCount; i++){
			stateInfo = this.tpAnimator.GetCurrentAnimatorStateInfo(i);

			if(stateInfo.IsName(stateName)){
				return stateInfo.normalizedTime;
			}

			stateInfo = this.tpAnimator.GetNextAnimatorStateInfo(i);

			if(stateInfo.IsName(stateName)){
				return stateInfo.normalizedTime;
			}
		}

		return -1f;
	}

	// Looks for a given state clip in an ThirdPerson Animator and returns its clip length in seconds
	// Returns a default value if clip wasn't found
	public float GetClipLength(string stateName, float defaultValue=0.01f){
		AnimatorOverrideController overrideController = tpAnimator.runtimeAnimatorController as AnimatorOverrideController;
		if(overrideController != null){
			List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
			overrideController.GetOverrides(overrides);

			for(int i = 0; i < overrides.Count; i++){
				KeyValuePair<AnimationClip, AnimationClip> pair = overrides[i];
				AnimationClip originalClip = pair.Key;
				AnimationClip overrideClip = pair.Value;

				if(originalClip != null && originalClip.name == stateName){
					return overrideClip.length;
				}
			}
		}

		return defaultValue;
	}

	// Used when new style is set
	public void CreateAttachments(BattleStyleData style, AttachmentItems items){
		GameObject attachmentGO;
		Transform bone;
		AttachmentMaterialData skin;

		this.battleStyle = style;

		if(this.isPlayer)
			this.battleStyleFP = AnimationLoader.GetBattleStyle($"{style.GetName()}-FP");

		RemoveAttachments();

		if(this.battleStyle.attachments != null){
			for(int i=0; i < this.battleStyle.attachments.Length; i++){
				attachmentGO = AttachmentInstantiator.Instantiate(this.battleStyle.attachments[i].fbxName, !this.isPlayer);

				bone = this.tpAnimator.gameObject.transform.Find(anchorMappings[this.controllerName][this.battleStyle.attachments[i].GetAnchorType()]);
				attachmentGO.transform.parent = bone;

				AttachmentInstantiator.ApplyTransform(attachmentGO, this.battleStyle.attachments[i].flipModel, heightOffset:this.battleStyle.attachments[i].heightOffset);

				skin = AttachmentLoader.ResolveDescriptor(this.battleStyle.attachments, i, items);
				AttachmentInstantiator.ApplySkin(attachmentGO, this.battleStyle.attachments[i].fbxName, skin);

				if(this.isPlayer)
					attachmentGO.layer = 9;

				this.attachmentDataIndexTP[this.battleStyle.attachments[i].GetAnchorType()] = i;
				AddAttachment(this.battleStyle.GetName(), this.battleStyle.attachments[i].GetAnchorType(), attachmentGO, skin:skin);
			}
		}
		if(this.isPlayer){
			if(this.battleStyleFP.attachments != null){
				for(int i=0; i < this.battleStyleFP.attachments.Length; i++){
					attachmentGO = AttachmentInstantiator.Instantiate(this.battleStyleFP.attachments[i].fbxName, false);

					bone = this.fpAnimator.gameObject.transform.Find(anchorMappings[this.controllerName][this.battleStyleFP.attachments[i].GetAnchorType()]);
					attachmentGO.transform.parent = bone;

					AttachmentInstantiator.ApplyTransform(attachmentGO, this.battleStyleFP.attachments[i].flipModel, heightOffset:this.battleStyleFP.attachments[i].heightOffset);

					skin = AttachmentLoader.ResolveDescriptor(this.battleStyleFP.attachments, i, items);
					AttachmentInstantiator.ApplySkin(attachmentGO, this.battleStyleFP.attachments[i].fbxName, skin);

					if(this.isPlayer)
						attachmentGO.layer = 12;

					this.attachmentDataIndexFP[this.battleStyleFP.attachments[i].GetAnchorType()] = i;
					AddAttachment(this.battleStyleFP.GetName(), this.battleStyleFP.attachments[i].GetAnchorType(), attachmentGO, skin:skin);
				}
			}
		}
	}

	// Re-skins the current attachments after the equipped items changed without changing the BattleStyle
	public void RefreshAttachmentMaterials(AttachmentItems items){
		RefreshAttachmentMaterials(this.battleStyle.attachments, this.attachmentsTP, this.attachmentDataIndexTP, this.attachmentSkinTP, items);

		if(this.isPlayer)
			RefreshAttachmentMaterials(this.battleStyleFP.attachments, this.attachmentsFP, this.attachmentDataIndexFP, this.attachmentSkinFP, items);
	}

	private void RefreshAttachmentMaterials(AttachmentData[] attachments, Dictionary<BoneAnchorType, GameObject> gos, Dictionary<BoneAnchorType, int> dataIndexes, Dictionary<BoneAnchorType, AttachmentMaterialData> skins, AttachmentItems items){
		AttachmentMaterialData oldSkin, newSkin;
		int index;

		if(attachments == null)
			return;

		this.cachedAnchors.Clear();
		this.cachedAnchors.AddRange(gos.Keys);

		foreach(BoneAnchorType anchor in this.cachedAnchors){
			if(gos[anchor] == null || !dataIndexes.TryGetValue(anchor, out index))
				continue;

			skins.TryGetValue(anchor, out oldSkin);
			newSkin = AttachmentLoader.ResolveDescriptor(attachments, index, items);

			if(oldSkin == newSkin)
				continue;

			// New textures are acquired before the old ones are released, so shared textures are never unloaded in between
			AttachmentInstantiator.ApplySkin(gos[anchor], attachments[index].fbxName, newSkin);
			AttachmentInstantiator.ReleaseSkin(oldSkin);
			skins[anchor] = newSkin;
		}
	}

	public GameObject GetAttachment(BoneAnchorType anchor, bool firstPerson){
		if(firstPerson && this.attachmentsFP.ContainsKey(anchor))
			return this.attachmentsFP[anchor];
		else if(!firstPerson && this.attachmentsTP.ContainsKey(anchor))
			return this.attachmentsTP[anchor];
		return null;
	}

	public void AddAttachment(string styleName, BoneAnchorType anchor, GameObject go, AttachmentMaterialData skin=null){
		bool isFirstPerson = IsFirstPersonString(styleName);

		if(ContainsAttachment(anchor, isFirstPerson))
			DestroyAttachment(anchor, isFirstPerson);

		if(isFirstPerson){
			this.attachmentsFP[anchor] = go;
			this.attachmentSkinFP[anchor] = skin;
		}
		else{
			this.attachmentsTP[anchor] = go;
			this.attachmentSkinTP[anchor] = skin;
		}
	}

	public void SwitchAttachments(bool firstPerson, BoneAnchorType originAnchor, BoneAnchorType targetAnchor, float heightOffset, bool flip){
		if(!ContainsAttachment(originAnchor, firstPerson:firstPerson)){
			return;
		}

		if(ContainsAttachment(targetAnchor, firstPerson:firstPerson)){
			DestroyAttachment(targetAnchor, firstPerson);
			GetAttachmentDataIndexes(firstPerson).Remove(targetAnchor);
		}

		if(!firstPerson){
			this.attachmentsTP[originAnchor].transform.parent = this.tpAnimator.gameObject.transform.Find(anchorMappings[this.controllerName][targetAnchor]);

			AttachmentInstantiator.ApplyTransform(this.attachmentsTP[originAnchor], flip, heightOffset:heightOffset);

			this.attachmentsTP.Add(targetAnchor, this.attachmentsTP[originAnchor]);
			this.attachmentsTP.Remove(originAnchor);
			this.attachmentDataIndexTP.Add(targetAnchor, this.attachmentDataIndexTP[originAnchor]);
			this.attachmentDataIndexTP.Remove(originAnchor);
		}
		else{
			this.attachmentsFP[originAnchor].transform.parent = this.fpAnimator.gameObject.transform.Find(anchorMappings[this.controllerName][targetAnchor]);

			AttachmentInstantiator.ApplyTransform(this.attachmentsFP[originAnchor], flip, heightOffset:heightOffset);

			this.attachmentsFP.Add(targetAnchor, this.attachmentsFP[originAnchor]);
			this.attachmentsFP.Remove(originAnchor);
			this.attachmentDataIndexFP.Add(targetAnchor, this.attachmentDataIndexFP[originAnchor]);
			this.attachmentDataIndexFP.Remove(originAnchor);
		}

		Dictionary<BoneAnchorType, AttachmentMaterialData> skins = GetAttachmentSkins(firstPerson);

		if(skins.TryGetValue(originAnchor, out AttachmentMaterialData movedSkin)){
			skins[targetAnchor] = movedSkin;
			skins.Remove(originAnchor);
		}
	}

	public void RemoveAttachments(){
		foreach(GameObject go in this.attachmentsTP.Values){
			GameObject.Destroy(go);
		}

		foreach(GameObject go in this.attachmentsFP.Values){
			GameObject.Destroy(go);
		}

		ReleaseSkins();

		this.attachmentsTP.Clear();
		this.attachmentsFP.Clear();
		this.attachmentDataIndexTP.Clear();
		this.attachmentDataIndexFP.Clear();
	}

	// Destroys a single attachment and releases the textures of its skin
	private void DestroyAttachment(BoneAnchorType anchor, bool firstPerson){
		Dictionary<BoneAnchorType, GameObject> gos = firstPerson ? this.attachmentsFP : this.attachmentsTP;
		Dictionary<BoneAnchorType, AttachmentMaterialData> skins = GetAttachmentSkins(firstPerson);

		GameObject.Destroy(gos[anchor]);
		gos.Remove(anchor);

		if(skins.TryGetValue(anchor, out AttachmentMaterialData skin)){
			AttachmentInstantiator.ReleaseSkin(skin);
			skins.Remove(anchor);
		}
	}

	private void ReleaseSkins(){
		foreach(AttachmentMaterialData skin in this.attachmentSkinTP.Values){
			AttachmentInstantiator.ReleaseSkin(skin);
		}

		foreach(AttachmentMaterialData skin in this.attachmentSkinFP.Values){
			AttachmentInstantiator.ReleaseSkin(skin);
		}

		this.attachmentSkinTP.Clear();
		this.attachmentSkinFP.Clear();
	}

	private Dictionary<BoneAnchorType, int> GetAttachmentDataIndexes(bool firstPerson){return firstPerson ? this.attachmentDataIndexFP : this.attachmentDataIndexTP;}
	private Dictionary<BoneAnchorType, AttachmentMaterialData> GetAttachmentSkins(bool firstPerson){return firstPerson ? this.attachmentSkinFP : this.attachmentSkinTP;}

	// Attachments may be destroyed together with this character, so their texture references must be released
	void OnDestroy(){
		ReleaseSkins();
	}

	public bool ContainsAttachment(BoneAnchorType anchor, bool firstPerson){
		if(!firstPerson){
			return this.attachmentsTP.ContainsKey(anchor);
		}
		else{
			return this.attachmentsFP.ContainsKey(anchor);
		}
	}

	public void SetParameterValue(string parameter, float val, bool firstPersonAnimator){
		if(firstPersonAnimator)
			this.fpAnimator.SetFloat(parameter, val);
		else
			this.tpAnimator.SetFloat(parameter, val);
	}

	public static string GetStateName(string controllerName, AnimatorStateInfo stateInfo){return AnimationHandler.hashToName[controllerName][stateInfo.shortNameHash];}

	public Animator GetThirdPersonAnimator(){return this.tpAnimator;}
	public Animator GetFirstPersonAnimator(){return this.fpAnimator;}

	private AnimatorStateInfo GetState(int layer){
		AnimatorStateInfo stateInfo;

		stateInfo = this.tpAnimator.GetNextAnimatorStateInfo(layer);

		if(stateInfo.shortNameHash == 0){
			return this.tpAnimator.GetCurrentAnimatorStateInfo(layer);
		}

		return stateInfo;
	}

	private AnimatorStateInfo GetStateFP(int layer){
		AnimatorStateInfo stateInfo;

		stateInfo = this.fpAnimator.GetNextAnimatorStateInfo(layer);

		if(stateInfo.shortNameHash == 0){
			return this.fpAnimator.GetCurrentAnimatorStateInfo(layer);
		}

		return stateInfo;
	}

	private void StopLayer(int layer){
		if(layer != 0){
			this.tpAnimator.CrossFade("Empty", this.animationCrossfadeTime, layer:layer);
		}
		else{
			this.tpAnimator.CrossFade("Idle", this.animationCrossfadeTime, 0);
			this.fpAnimator.CrossFade("Empty", this.animationCrossfadeTime, 0);
		}
	}

	private void StopLayer(string[] layers){
		if(layers == null)
			return;

		int layerIndex;

		for(int i=0; i < layers.Length; i++){
			layerIndex = this.tpAnimator.GetLayerIndex(layers[i]);

			if(layerIndex != 0){
				this.tpAnimator.CrossFade("Empty", this.animationCrossfadeTime, layer:layerIndex);
			}
			else{
				this.tpAnimator.CrossFade("Idle", this.animationCrossfadeTime, 0);
				this.fpAnimator.CrossFade("Empty", this.animationCrossfadeTime, 0);
			}
		}
	}

	private bool VerifyLayerStates(string layerName, int priority){
		string state;

		if(layerName == "")
			layerName = "Base Layer";

		for(int i=this.tpAnimator.GetLayerIndex(layerName)+1; i < this.tpAnimator.layerCount; i++){
			state = AnimationHandler.hashToName[this.controllerName][GetState(i).shortNameHash];

			if(ArrayContains(layerName, AnimationHandler.stateMappings[this.controllerName][state].layers)){
				if(priority > AnimationHandler.stateMappings[this.controllerName][state].priority){
					StopLayer(i);
					this.tpAnimator.CrossFade(state, this.animationCrossfadeTime, layer:this.tpAnimator.GetLayerIndex(layerName));
					return true;
				} 
			}
		}

		return false;
	}

	private void LoadMapping(){
		if(AnimationHandler.stateMappings == null)
			AnimationHandler.stateMappings = new Dictionary<string, Dictionary<string, AnimationStateMapping>>();

		if(AnimationHandler.stateMappings.ContainsKey(this.controllerName))
			return;

		AnimationHandler.hashToName.Add(this.controllerName, new Dictionary<int, string>());
		AnimationHandler.stateMappings.Add(this.controllerName, new Dictionary<string, AnimationStateMapping>());

		foreach(AnimationStateMapping map in AnimationLoader.GetAnimationMapping(this.controllerName)){
			AnimationHandler.stateMappings[this.controllerName].Add(map.state, map);
			AnimationHandler.hashToName[this.controllerName].Add(Animator.StringToHash(map.state), map.state);
		}
	}

	private void LoadMappingFP(){
		string fpControllerName = $"{this.controllerName}_FP";
		this.controllerNameFP = fpControllerName;

		if(AnimationHandler.stateMappings.ContainsKey(this.controllerNameFP))
			return;

		if(!AnimationLoader.ContainsMapping(this.controllerNameFP))
			return;


		AnimationHandler.hashToName.Add(this.controllerNameFP, new Dictionary<int, string>());
		AnimationHandler.stateMappings.Add(this.controllerNameFP, new Dictionary<string, AnimationStateMapping>());

		foreach(AnimationStateMapping map in AnimationLoader.GetAnimationMapping(this.controllerNameFP)){
			AnimationHandler.stateMappings[this.controllerNameFP].Add(map.state, map);
			AnimationHandler.hashToName[this.controllerNameFP].Add(Animator.StringToHash(map.state), map.state);
		}
	}

	private void LoadAnchors(){
		if(AnimationHandler.anchorMappings == null)
			AnimationHandler.anchorMappings = new Dictionary<string, Dictionary<BoneAnchorType, string>>();

		if(AnimationHandler.anchorMappings.ContainsKey(this.controllerName))
			return;

		AnimationHandler.anchorMappings.Add(this.controllerName, new Dictionary<BoneAnchorType, string>());

		foreach(BoneAnchorPoint anchor in AnimationLoader.GetAnchorMapping(this.controllerName)){
			AnimationHandler.anchorMappings[this.controllerName].Add(anchor.GetAnchorType(), anchor.bonePath);
		}
	}

	private void LoadAnchorsFP(){
		string fpControllerName = $"{this.controllerName}_FP";

		if(AnimationHandler.anchorMappings.ContainsKey(fpControllerName))
			return;

		if(!AnimationLoader.ContainsAnchor(fpControllerName))
			return;

		this.controllerNameFP = fpControllerName;

		AnimationHandler.anchorMappings.Add(this.controllerNameFP, new Dictionary<BoneAnchorType, string>());

		foreach(BoneAnchorPoint anchor in AnimationLoader.GetAnchorMapping(this.controllerNameFP)){
			AnimationHandler.anchorMappings[this.controllerNameFP].Add(anchor.GetAnchorType(), anchor.bonePath);
		}
	}

	private bool ArrayContains(string element, string[] arr){return Array.IndexOf(arr, element) >= 0;}
	private bool IsFirstPersonString(string text){
		if (string.IsNullOrEmpty(text) || text.Length < 3)
			return false;
		return text.EndsWith("-FP");
	}
}