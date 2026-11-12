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
	private Dictionary<BoneAnchorType, AttachmentSource> attachmentSourceTP = new Dictionary<BoneAnchorType, AttachmentSource>();
	private Dictionary<BoneAnchorType, AttachmentMaterialData> attachmentSkinTP = new Dictionary<BoneAnchorType, AttachmentMaterialData>();
	private Dictionary<BoneAnchorType, GameObject> attachmentsFP = new Dictionary<BoneAnchorType, GameObject>();
	private Dictionary<BoneAnchorType, AttachmentSource> attachmentSourceFP = new Dictionary<BoneAnchorType, AttachmentSource>();
	private Dictionary<BoneAnchorType, AttachmentMaterialData> attachmentSkinFP = new Dictionary<BoneAnchorType, AttachmentMaterialData>();
	private List<BoneAnchorType> cachedAnchors = new List<BoneAnchorType>();

	private static Dictionary<string, Dictionary<string, AnimationStateMapping>> stateMappings;
	private static Dictionary<string, Dictionary<BoneAnchorType, string>> anchorMappings;
	private static Dictionary<string, Dictionary<int, string>> hashToName;

	private static readonly int EMPTY_STATE_HASH = Animator.StringToHash("Empty");


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
	public void Play(string stateName, bool overrideState=false, bool ignoreFP=false, bool ignoreTP=false){
		if(!this.INIT)
			return;

		bool skipThirdPerson = ignoreTP;
		AnimationStateMapping givenMap, currentMap, currentMapFP;
		int layerIndex;

		givenMap = AnimationHandler.stateMappings[this.controllerName][stateName];

		if(ignoreTP){
			currentMap = givenMap;
		}
		else if(!overrideState){
			currentMap = AnimationHandler.stateMappings[this.controllerName][AnimationHandler.hashToName[this.controllerName][GetState(this.tpAnimator.GetLayerIndex(givenMap.layers[0])).shortNameHash]];

			if(this.isPlayer){
				currentMapFP = AnimationHandler.stateMappings[this.controllerNameFP][AnimationHandler.hashToName[this.controllerNameFP][GetStateFP(0).shortNameHash]];
			}

			if(givenMap.state == currentMap.state){
				StopLayer(givenMap.stopLayer);
				StopStateInOtherLayers(givenMap, 0);
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
				layerIndex = this.tpAnimator.GetLayerIndex(givenMap.layers[i]);
				currentMap = AnimationHandler.stateMappings[this.controllerName][AnimationHandler.hashToName[this.controllerName][GetState(layerIndex).shortNameHash]];

				// Re-playing a state that is already in one of its layers must not restart it nor stop other layers
				if(givenMap.state == currentMap.state)
					break;

				if(givenMap.priority <= currentMap.priority){
					StopLayer(givenMap.stopLayer);
					this.tpAnimator.CrossFade(stateName, this.animationCrossfadeTime, layer:layerIndex);
					StopStateInOtherLayers(givenMap, i);

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

	// Force plays a state identified by its short name hash for non-player characters (Used by Client)
	public void Play(int stateHash, int layer){
		this.tpAnimator.CrossFade(stateHash, this.animationCrossfadeTime, layer);
	}

	// Plays/Stops/Registers ShapeKey Animations based on the settings inputted
	public void Play(string shapeKey, CustomAnimationSettings settings){
		if(!this.INIT)
			return;

		this.shapeKeyAnimator.Play(shapeKey, settings);
	}


	// Looks for every Layer to find if the current playing State is StateName and return the normalizedTime
	// Return -1 if no state like that is found
	public float GetAnimationTime(string stateName, bool firstPerson=false){
		AnimatorStateInfo stateInfo;
		Animator animator = firstPerson ? this.fpAnimator : this.tpAnimator;

		if(animator == null)
			return -1f;

		for(int i=0; i < animator.layerCount; i++){
			stateInfo = animator.GetCurrentAnimatorStateInfo(i);

			if(stateInfo.IsName(stateName)){
				return stateInfo.normalizedTime;
			}

			stateInfo = animator.GetNextAnimatorStateInfo(i);

			if(stateInfo.IsName(stateName)){
				return stateInfo.normalizedTime;
			}
		}

		return -1f;
	}

	// Looks for a given state clip in the ThirdPerson (or FirstPerson) Animator and returns its clip length in seconds
	// Returns a default value if clip wasn't found
	public float GetClipLength(string stateName, float defaultValue=0.01f, bool firstPerson=false){
		Animator animator = firstPerson ? this.fpAnimator : this.tpAnimator;

		if(animator == null)
			return defaultValue;

		AnimatorOverrideController overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;
		if(overrideController != null){
			List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
			overrideController.GetOverrides(overrides);

			for(int i = 0; i < overrides.Count; i++){
				KeyValuePair<AnimationClip, AnimationClip> pair = overrides[i];
				AnimationClip originalClip = pair.Key;
				AnimationClip overrideClip = pair.Value;

				if(originalClip != null && originalClip.name == stateName){
					if(overrideClip == null)
						return defaultValue;

					return overrideClip.length;
				}
			}
		}

		return defaultValue;
	}

	/*
	Used when a new style is set
		Creates the attachments of the main style and, if a Carry Style code is given, the attachments of that Carry Style
	*/
	public void CreateAttachments(BattleStyleData style, AttachmentItems items, int carryStyleCode){
		CarryStyleData carryStyle;
		string carryStyleFP;

		this.battleStyle = style;

		if(this.isPlayer)
			this.battleStyleFP = AnimationLoader.GetBattleStyle($"{style.GetName()}-FP");

		RemoveAttachments();

		BuildAttachments(this.battleStyle.attachments, false, false, items);

		if(this.isPlayer)
			BuildAttachments(this.battleStyleFP.attachments, true, false, items);

		if(carryStyleCode == BattleStyleDeterminator.NO_CARRY)
			return;

		carryStyle = AnimationLoader.GetCarryStyle(carryStyleCode);
		BuildAttachments(carryStyle.GetAttachments(), false, true, items);

		if(!this.isPlayer)
			return;

		carryStyleFP = $"{carryStyle.GetName()}-FP";

		if(AnimationLoader.ContainsCarryStyle(carryStyleFP) && AnimationLoader.GetCarryStyle(carryStyleFP).IsAvailable())
			BuildAttachments(AnimationLoader.GetCarryStyle(carryStyleFP).GetAttachments(), true, true, items);
	}

	private void BuildAttachments(AttachmentData[] attachments, bool firstPerson, bool isCarry, AttachmentItems items){
		GameObject attachmentGO;
		Transform bone;
		AttachmentMaterialData skin;
		AttachmentSource source;
		Animator animator = firstPerson ? this.fpAnimator : this.tpAnimator;

		if(attachments == null)
			return;

		for(int i=0; i < attachments.Length; i++){
			source = new AttachmentSource(attachments, i, isCarry);
			attachmentGO = AttachmentInstantiator.Instantiate(attachments[i].fbxName, !firstPerson && !this.isPlayer);

			bone = animator.gameObject.transform.Find(anchorMappings[this.controllerName][attachments[i].GetAnchorType()]);
			attachmentGO.transform.parent = bone;

			AttachmentInstantiator.ApplyTransform(attachmentGO, attachments[i].flipModel, heightOffset:attachments[i].heightOffset);

			skin = source.ResolveSkin(items);
			AttachmentInstantiator.ApplySkin(attachmentGO, attachments[i].fbxName, skin);

			if(this.isPlayer)
				attachmentGO.layer = firstPerson ? 12 : 9;

			GetAttachmentSources(firstPerson)[attachments[i].GetAnchorType()] = source;
			AddAttachment(firstPerson, attachments[i].GetAnchorType(), attachmentGO, skin);
		}
	}

	// Re-skins the current attachments after the equipped items changed without changing the BattleStyle or the carry
	public void RefreshAttachmentMaterials(AttachmentItems items){
		RefreshAttachmentMaterials(this.attachmentsTP, this.attachmentSourceTP, this.attachmentSkinTP, items);

		if(this.isPlayer)
			RefreshAttachmentMaterials(this.attachmentsFP, this.attachmentSourceFP, this.attachmentSkinFP, items);
	}

	private void RefreshAttachmentMaterials(Dictionary<BoneAnchorType, GameObject> gos, Dictionary<BoneAnchorType, AttachmentSource> sources, Dictionary<BoneAnchorType, AttachmentMaterialData> skins, AttachmentItems items){
		AttachmentMaterialData oldSkin, newSkin;
		AttachmentSource source;

		this.cachedAnchors.Clear();
		this.cachedAnchors.AddRange(gos.Keys);

		foreach(BoneAnchorType anchor in this.cachedAnchors){
			if(gos[anchor] == null || !sources.TryGetValue(anchor, out source))
				continue;

			skins.TryGetValue(anchor, out oldSkin);
			newSkin = source.ResolveSkin(items);

			if(oldSkin == newSkin)
				continue;

			// New textures are acquired before the old ones are released, so shared textures are never unloaded in between
			AttachmentInstantiator.ApplySkin(gos[anchor], source.GetData().fbxName, newSkin);
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
		AddAttachment(IsFirstPersonString(styleName), anchor, go, skin);
	}

	private void AddAttachment(bool isFirstPerson, BoneAnchorType anchor, GameObject go, AttachmentMaterialData skin){
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
			GetAttachmentSources(firstPerson).Remove(targetAnchor);
		}

		if(!firstPerson){
			this.attachmentsTP[originAnchor].transform.parent = this.tpAnimator.gameObject.transform.Find(anchorMappings[this.controllerName][targetAnchor]);

			AttachmentInstantiator.ApplyTransform(this.attachmentsTP[originAnchor], flip, heightOffset:heightOffset);

			this.attachmentsTP.Add(targetAnchor, this.attachmentsTP[originAnchor]);
			this.attachmentsTP.Remove(originAnchor);
			this.attachmentSourceTP.Add(targetAnchor, this.attachmentSourceTP[originAnchor]);
			this.attachmentSourceTP.Remove(originAnchor);
		}
		else{
			this.attachmentsFP[originAnchor].transform.parent = this.fpAnimator.gameObject.transform.Find(anchorMappings[this.controllerName][targetAnchor]);

			AttachmentInstantiator.ApplyTransform(this.attachmentsFP[originAnchor], flip, heightOffset:heightOffset);

			this.attachmentsFP.Add(targetAnchor, this.attachmentsFP[originAnchor]);
			this.attachmentsFP.Remove(originAnchor);
			this.attachmentSourceFP.Add(targetAnchor, this.attachmentSourceFP[originAnchor]);
			this.attachmentSourceFP.Remove(originAnchor);
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
		this.attachmentSourceTP.Clear();
		this.attachmentSourceFP.Clear();
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

	private Dictionary<BoneAnchorType, AttachmentSource> GetAttachmentSources(bool firstPerson){return firstPerson ? this.attachmentSourceFP : this.attachmentSourceTP;}
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

	// Parameter identified by Animator.StringToHash
	public void SetParameterValue(int parameterHash, float val, bool firstPersonAnimator){
		if(firstPersonAnimator)
			this.fpAnimator.SetFloat(parameterHash, val);
		else
			this.tpAnimator.SetFloat(parameterHash, val);
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
				// Avoids restarting Empty, which would also re-send it to other clients
				if(GetState(layerIndex).shortNameHash == EMPTY_STATE_HASH)
					continue;

				this.tpAnimator.CrossFade("Empty", this.animationCrossfadeTime, layer:layerIndex);
			}
			else{
				this.tpAnimator.CrossFade("Idle", this.animationCrossfadeTime, 0);
				this.fpAnimator.CrossFade("Empty", this.animationCrossfadeTime, 0);
			}
		}
	}

	// A state must only play in one layer at a time, so it's stopped in the other layers of its mapping after being placed in layers[placedIndex]
	private void StopStateInOtherLayers(AnimationStateMapping map, int placedIndex){
		int layerIndex;
		int stateHash = Animator.StringToHash(map.state);

		for(int i=0; i < map.layers.Length; i++){
			if(i == placedIndex)
				continue;

			layerIndex = this.tpAnimator.GetLayerIndex(map.layers[i]);

			if(layerIndex <= 0)
				continue;

			if(GetState(layerIndex).shortNameHash == stateHash)
				this.tpAnimator.CrossFade("Empty", this.animationCrossfadeTime, layer:layerIndex);
		}
	}

	// Returns the short name hash of the state a Third Person layer is playing or transitioning into
	public int GetTargetStateHash(int layer){
		if(!this.INIT || layer < 0)
			return 0;

		return GetState(layer).shortNameHash;
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