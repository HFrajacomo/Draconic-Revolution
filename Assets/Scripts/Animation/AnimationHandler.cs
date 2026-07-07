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
	private Dictionary<BoneAnchorType, GameObject> attachmentsFP = new Dictionary<BoneAnchorType, GameObject>();
	private Dictionary<BoneAnchorType, int> attachmentDataIndexFP = new Dictionary<BoneAnchorType, int>();

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

	// Used when new style is set
	public void CreateAttachments(BattleStyleData style){
		GameObject attachmentGO;
		Transform bone;

		this.battleStyle = style;

		if(this.isPlayer)
			this.battleStyleFP = AnimationLoader.GetBattleStyle($"{style.GetName()}-FP");

		RemoveAttachments();

		if(this.battleStyle.attachments != null){
			for(int i=0; i < this.battleStyle.attachments.Length; i++){
				attachmentGO = AttachmentInstantiator.Instantiate(this.battleStyle.attachments[i].fbxName, !this.isPlayer);

				bone = this.tpAnimator.gameObject.transform.Find(anchorMappings[this.controllerName][this.battleStyle.attachments[i].GetAnchorType()]);
				attachmentGO.transform.parent = bone;

				attachmentGO.transform.localPosition = this.battleStyle.attachments[i].offset;
		        attachmentGO.transform.localRotation = Quaternion.Euler(this.battleStyle.attachments[i].rotation);
		        AttachmentInstantiator.ApplyScaling(attachmentGO);

		        if(this.isPlayer)
		        	attachmentGO.layer = 9;

		        this.attachmentDataIndexTP.Add(this.battleStyle.attachments[i].GetAnchorType(), i);
		        AddAttachment(this.battleStyle.GetName(), this.battleStyle.attachments[i].GetAnchorType(), attachmentGO);
			}
		}
		if(this.isPlayer){
			if(this.battleStyleFP.attachments != null){
				for(int i=0; i < this.battleStyleFP.attachments.Length; i++){
					attachmentGO = AttachmentInstantiator.Instantiate(this.battleStyleFP.attachments[i].fbxName, false);

					bone = this.fpAnimator.gameObject.transform.Find(anchorMappings[this.controllerName][this.battleStyleFP.attachments[i].GetAnchorType()]);
					attachmentGO.transform.parent = bone;

					attachmentGO.transform.localPosition = this.battleStyleFP.attachments[i].offset;
			        attachmentGO.transform.localRotation = Quaternion.Euler(this.battleStyleFP.attachments[i].rotation);
			        AttachmentInstantiator.ApplyScaling(attachmentGO);

			        if(this.isPlayer)
			        	attachmentGO.layer = 12;

			        this.attachmentDataIndexFP.Add(this.battleStyleFP.attachments[i].GetAnchorType(), i);
			        AddAttachment(this.battleStyleFP.GetName(), this.battleStyleFP.attachments[i].GetAnchorType(), attachmentGO);
				}
			}
		}
	}

	public void AddAttachment(string styleName, BoneAnchorType anchor, GameObject go){
		bool isFirstPerson = IsFirstPersonString(styleName);

		if(ContainsAttachment(anchor, isFirstPerson)){
			if(isFirstPerson)
				GameObject.Destroy(this.attachmentsFP[anchor]);
			else
				GameObject.Destroy(this.attachmentsTP[anchor]);
		}

		if(isFirstPerson)
			this.attachmentsFP.Add(anchor, go);
		else
			this.attachmentsTP.Add(anchor, go);
	}

	public void SwitchAttachments(bool firstPerson, BoneAnchorType originAnchor, BoneAnchorType targetAnchor){
		if(!ContainsAttachment(originAnchor, firstPerson:firstPerson)){
			Debug.LogWarning($"No Attachment was found in {originAnchor}");
			return;
		}

		if(ContainsAttachment(targetAnchor, firstPerson:firstPerson)){
			if(firstPerson)
				GameObject.Destroy(this.attachmentsFP[targetAnchor]);
			else
				GameObject.Destroy(this.attachmentsTP[targetAnchor]);
		}

		if(!firstPerson){
			this.attachmentsTP[originAnchor].transform.parent = this.tpAnimator.gameObject.transform.Find(anchorMappings[this.controllerName][targetAnchor]);
			this.attachmentsTP[originAnchor].transform.localPosition = this.battleStyle.attachments[this.attachmentDataIndexTP[originAnchor]].offset;
			this.attachmentsTP[originAnchor].transform.localRotation = Quaternion.Euler(this.battleStyle.attachments[this.attachmentDataIndexTP[originAnchor]].rotation);
			AttachmentInstantiator.ApplyScaling(this.attachmentsTP[originAnchor]);

			this.attachmentsTP.Add(targetAnchor, this.attachmentsTP[originAnchor]);
			this.attachmentsTP.Remove(originAnchor);
			this.attachmentDataIndexTP.Add(targetAnchor, this.attachmentDataIndexTP[originAnchor]);
			this.attachmentDataIndexTP.Remove(originAnchor);
		}
		else{
			this.attachmentsFP[originAnchor].transform.parent = this.fpAnimator.gameObject.transform.Find(anchorMappings[this.controllerName][targetAnchor]);
			this.attachmentsFP[originAnchor].transform.localPosition = this.battleStyleFP.attachments[this.attachmentDataIndexFP[originAnchor]].offset;
			this.attachmentsFP[originAnchor].transform.localRotation = Quaternion.Euler(this.battleStyleFP.attachments[this.attachmentDataIndexFP[originAnchor]].rotation);
			AttachmentInstantiator.ApplyScaling(this.attachmentsFP[originAnchor]);

			this.attachmentsFP.Add(targetAnchor, this.attachmentsFP[originAnchor]);
			this.attachmentsFP.Remove(originAnchor);
			this.attachmentDataIndexFP.Add(targetAnchor, this.attachmentDataIndexFP[originAnchor]);
			this.attachmentDataIndexFP.Remove(originAnchor);
		}
	}

	public void RemoveAttachments(){
		if(this.attachmentsTP.Count != 0){
			foreach(BoneAnchorType bat in this.attachmentsTP.Keys){
				GameObject.Destroy(this.attachmentsTP[bat]);
			}

			if(this.isPlayer){
				foreach(BoneAnchorType bat in this.attachmentsFP.Keys){
					GameObject.Destroy(this.attachmentsFP[bat]);
				}
			}

			this.attachmentsTP.Clear();
			this.attachmentsFP.Clear();
			this.attachmentDataIndexTP.Clear();
			this.attachmentDataIndexFP.Clear();
		}
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