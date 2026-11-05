using System;
using System.Collections.Generic;
using UnityEngine;

/*
A Carry Style defines how a SubWeapon is held in the off-hand (Left Arm layer) on top of another BattleStyle
	Lives in Resources/CarryStyles and is referenced by SubWeapons through their carryStyle field
	Its overrides can only target the Carry states, and its attachments are always skinned by the off-hand item
	Like BattleStyles, the First Person version has the same name with the "-FP" suffix
	Only First Person Carry Styles have the Carry Run state, which falls back to the Carry Hold clip if not overridden
*/
[Serializable]
public class CarryStyleData {
	public bool firstPerson;
	public AttachmentData[] attachments;
	public Wrapper<StateClipPair> overrides;
	private StateClipPair[] clipPairs;
	private string name;
	private int code;
	private bool available = false;

	public static readonly string[] CARRY_STATES = new string[]{"Carry Draw", "Carry Hold", "Carry Stow", "Carry Run"};
	private static readonly string CARRY_HOLD_STATE = "Carry Hold";
	private static readonly string CARRY_RUN_STATE = "Carry Run";


	public void PostDeserializationSetup(string styleName, int styleCode, bool isClient){
		this.name = styleName;
		this.code = styleCode;

		if(this.overrides == null || this.overrides.data == null || this.overrides.data.Length == 0)
			throw new DeserializationErrorException($"[CarryStyleData] Carry Style {styleName} has no overrides");

		this.clipPairs = this.overrides.data;

		for(int i=0; i < this.clipPairs.Length; i++){
			if(Array.IndexOf(CARRY_STATES, this.clipPairs[i].state) < 0)
				throw new DeserializationErrorException($"[CarryStyleData] Carry Style {styleName} has an override for state '{this.clipPairs[i].state}', but carry overrides can only target: {string.Join(", ", CARRY_STATES)}");
			if(!this.firstPerson && this.clipPairs[i].state == CARRY_RUN_STATE)
				throw new DeserializationErrorException($"[CarryStyleData] Carry Style {styleName} overrides '{CARRY_RUN_STATE}', which only exists in First Person Carry Styles");

			this.clipPairs[i].SetFirstPerson(this.firstPerson);
		}

		if(this.firstPerson)
			AddRunFallback();

		if(this.attachments != null){
			for(int i=0; i < this.attachments.Length; i++){
				this.attachments[i].PostDeserializationSetup(this.firstPerson, isClient);
			}
		}

		this.available = !isClient || AreClipsLoadable();
	}

	public string GetName(){return this.name;}
	public int GetCode(){return this.code;}
	// On clients, a carry is only available once all of its clips (and the controller placeholder clips) exist
	public bool IsAvailable(){return this.available;}
	public AttachmentData[] GetAttachments(){return this.attachments;}
	public StateClipPair[] GetOverrides(){return this.clipPairs;}
	public override string ToString(){return this.name;}

	// First Person Carry Styles without a Carry Run clip keep holding the item while running
	private void AddRunFallback(){
		int holdIndex = -1;

		for(int i=0; i < this.clipPairs.Length; i++){
			if(this.clipPairs[i].state == CARRY_RUN_STATE)
				return;
			if(this.clipPairs[i].state == CARRY_HOLD_STATE)
				holdIndex = i;
		}

		if(holdIndex < 0)
			return;

		StateClipPair runPair = new StateClipPair();
		runPair.state = CARRY_RUN_STATE;
		runPair.clip = this.clipPairs[holdIndex].clip;
		runPair.events = new List<AnimationEventData>();
		runPair.SetFirstPerson(true);

		Array.Resize(ref this.clipPairs, this.clipPairs.Length + 1);
		this.clipPairs[this.clipPairs.Length - 1] = runPair;
	}

	private bool AreClipsLoadable(){
		for(int i=0; i < this.clipPairs.Length; i++){
			if(Resources.Load<AnimationClip>($"{AnimationLoader.ANIMATION_CLIP_RESFOLDER}{this.clipPairs[i].state}") == null){
				Debug.LogWarning($"[CarryStyleData] Carry Style {this.name} is disabled: the AnimatorControllers don't have the state '{this.clipPairs[i].state}'. Rebuild them with Editor Tools/Animations/Build Controllers");
				return false;
			}

			if(this.clipPairs[i].clip == "")
				continue;

			if(Resources.Load<AnimationClip>($"{AnimationLoader.ANIMATION_CLIP_RESFOLDER}{this.clipPairs[i].clip}") == null){
				Debug.LogWarning($"[CarryStyleData] Carry Style {this.name} is disabled: clip '{this.clipPairs[i].clip}' was not found");
				return false;
			}
		}

		return true;
	}
}
