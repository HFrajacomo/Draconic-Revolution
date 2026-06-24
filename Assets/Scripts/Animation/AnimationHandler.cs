using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

public class AnimationHandler : MonoBehaviour {
	private bool INIT = false;
	private bool isPlayer = false;

	private Animator tpAnimator;
	private Animator fpAnimator;
	private ShapeKeyAnimator shapeKeyAnimator;
	private ProceduralAnimationRigController rigControllerTP;
	private ProceduralAnimationRigController rigControllerFP;
	private float animationCrossfadeTime = 0.06f;

	private static Dictionary<string, AnimationStateMapping> stateMappings;
	private static Dictionary<int, string> hashToName; 

	public void Init(string controllerName, CharacterBuilder firstPersonBuilder, bool isUserCharacter=false){
		Transform tpParent = this.transform.Find("TP-Rig");
		Transform tpAnimObj = tpParent.Find("Animator");

		LoadMapping(controllerName);
		this.isPlayer = isUserCharacter;

		this.tpAnimator = tpAnimObj.GetComponent<Animator>();
		this.shapeKeyAnimator = tpParent.GetComponent<ShapeKeyAnimator>();
		this.rigControllerTP = new ProceduralAnimationRigController(tpParent.gameObject, tpAnimObj.gameObject, controllerName);
		this.rigControllerTP.Build();

		if(this.isPlayer){
			Transform fpParent = this.transform.Find("Camera/FP-Rig");
			Transform fpAnimObj = fpParent.Find("Animator");
			
			this.fpAnimator = fpAnimObj.GetComponent<Animator>();
			this.rigControllerFP = new ProceduralAnimationRigController(fpParent.gameObject, fpAnimObj.gameObject, $"{controllerName}_FP");
			this.rigControllerFP.Build();
		}

		this.INIT = true;
	}


	// Plays bone animation
	public void Play(string stateName, bool overrideState=false, bool ignoreFP=false){
		if(!this.INIT)
			return;

		bool skipThirdPerson = false;
		AnimationStateMapping givenMap, currentMap, currentMapFP;

		givenMap = AnimationHandler.stateMappings[stateName];

		if(!overrideState){
			currentMap = AnimationHandler.stateMappings[AnimationHandler.hashToName[GetState(this.tpAnimator.GetLayerIndex(givenMap.layers[0])).shortNameHash]];

			if(this.isPlayer)
				currentMapFP = AnimationHandler.stateMappings[AnimationHandler.hashToName[GetStateFP(0).shortNameHash]];

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
				currentMap = AnimationHandler.stateMappings[AnimationHandler.hashToName[GetState(this.tpAnimator.GetLayerIndex(givenMap.layers[i])).shortNameHash]];

				if(givenMap.priority <= currentMap.priority){
					StopLayer(givenMap.stopLayer);
					this.tpAnimator.CrossFade(stateName, this.animationCrossfadeTime, layer:this.tpAnimator.GetLayerIndex(givenMap.layers[i]));

					break;
				}
			}
		}

		// Handling First Person
		if(this.isPlayer && !ignoreFP && !overrideState){
			currentMapFP = AnimationHandler.stateMappings[AnimationHandler.hashToName[GetStateFP(0).shortNameHash]];

			if(!this.fpAnimator.HasState(0, Animator.StringToHash(stateName))){
				givenMap = AnimationHandler.stateMappings["Empty"];
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

	// Sets a Third Person's Animator Parameter to a certain value
	public void SetParameterValue(string parameter, float val){
		this.tpAnimator.SetFloat(parameter, val);
	}

	public static string GetStateName(AnimatorStateInfo stateInfo){return hashToName[stateInfo.shortNameHash];}

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
			state = AnimationHandler.hashToName[GetState(i).shortNameHash];

			if(ArrayContains(layerName, AnimationHandler.stateMappings[state].layers)){
				if(priority > AnimationHandler.stateMappings[state].priority){
					StopLayer(i);
					this.tpAnimator.CrossFade(state, this.animationCrossfadeTime, layer:this.tpAnimator.GetLayerIndex(layerName));
					return true;
				} 
			}
		}

		return false;
	}

	private void LoadMapping(string controllerName){
		if(AnimationHandler.stateMappings != null)
			return;

		AnimationHandler.stateMappings = new Dictionary<string, AnimationStateMapping>();
		AnimationHandler.hashToName = new Dictionary<int, string>();

		foreach(AnimationStateMapping map in AnimationLoader.GetAnimationMapping(controllerName)){
			AnimationHandler.stateMappings.Add(map.state, map);
			AnimationHandler.hashToName.Add(Animator.StringToHash(map.state), map.state);
		}
	}

	private bool ArrayContains(string element, string[] arr){return Array.IndexOf(arr, element) >= 0;}
}