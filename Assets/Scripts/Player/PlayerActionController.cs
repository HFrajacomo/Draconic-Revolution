using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActionController : MonoBehaviour {
	
	// Unity Reference
	public ChunkLoader cl;
	private AnimationHandler animationHandler;
	private PlayerMovement playerMovement;
	private Animator animator;
	private Animator animatorFP;

	// Flags
	private bool INIT = false;

	// Original Animators
	private RuntimeAnimatorController originalController;
	private RuntimeAnimatorController originalControllerFP;

	// Battle Style
	private int currentStyleCode;
	private int currentCarryCode = BattleStyleDeterminator.NO_CARRY;
	private bool currentCarryHasFP = false;
	private BattleStyleData currentStyle;
	private bool weaponSheathed = true;
	private int comboHit = 0;
	private AttachmentItems appliedAttachmentItems = AttachmentItems.NONE;
	private AttachmentItems pendingAttachmentItems = AttachmentItems.NONE;

	// Default Config
	private float hitWindowStart = .48f;
	private float attackExitTime = .8f;
	private HashSet<PlayerActionType> registeredAction;
	private Dictionary<PlayerActionType, int> registeredData;
	private HashSet<PlayerActionRestriction> restrictions;
	private Dictionary<PlayerActionRestriction, Coroutine> restrictionTimer;
	private HashSet<string> currentlyQueuedState;
	private List<string> playlist;
	private List<bool> overrideState;
	private List<bool> ignoreFP;

	// NetCode
	private static List<AnimationData> statesPlayed;
	private NetMessage animationLayerMessage = new NetMessage(NetCode.SENDANIMATIONLAYER);
	private NetMessage animatorParameterMessage = new NetMessage(NetCode.SENDANIMATORPARAMETER);
	private Dictionary<string, float> lastParameterValue = new Dictionary<string, float>();

	// Sheathe Tracking
	private bool sheatheInProgress = false;
	private bool sheatheSeenTP = false;
	private bool sheatheSeenFP = false;
	private bool carryStowSeenTP = false;
	private bool carryStowSeenFP = false;
	private float sheatheTimeout = 0f;
	private static readonly string SHEATHE_STATE = "Weapon Sheathe";
	private static readonly string UNSHEATHE_STATE = "Weapon Unsheathe";
	private static readonly string CARRY_STOW_STATE = "Carry Stow";
	private static readonly string CARRY_DRAW_STATE = "Carry Draw";
	private static readonly string HAS_CARRY_PARAMETER = "HasCarry";
	private static readonly float SHEATHE_TIMEOUT_MARGIN = 0.25f;

	// Animator Parameters
	private static readonly int IS_PLAYER_HASH = Animator.StringToHash("ISPLAYER");
	private static readonly int HAS_CARRY_HASH = Animator.StringToHash(HAS_CARRY_PARAMETER);
	private static readonly int SHEATHED_HASH = Animator.StringToHash("Sheathed");

	// Cache
	private float cachedTime;
	private PlayerMovementType lastMove;

	void Update(){
		if(!this.INIT)
			return;

		if(this.registeredAction.Contains(PlayerActionType.PRIMARY_ACTION)){
			if(this.comboHit >= 1){
				this.cachedTime = this.animationHandler.GetAnimationTime($"Attack {this.comboHit}");

				if(this.cachedTime != -1f){
					if(this.cachedTime >= this.hitWindowStart && this.cachedTime < this.attackExitTime && this.comboHit < this.currentStyle.GetComboHits()){
						this.comboHit++;
						this.restrictions.Add(PlayerActionRestriction.MOVEMENT);
						this.animator.SetInteger("Attack_Combo", this.comboHit);
						this.animatorFP.SetInteger("Attack_Combo", this.comboHit);
						this.registeredAction.Remove(PlayerActionType.PRIMARY_ACTION);
					}
				}
			}
			else if(this.comboHit == 0){
				this.comboHit++;
				this.animator.SetInteger("Attack_Combo", this.comboHit);
				this.animatorFP.SetInteger("Attack_Combo", this.comboHit);
				AddToPlaylist($"Attack {this.comboHit}");
				this.registeredAction.Remove(PlayerActionType.PRIMARY_ACTION);
				this.restrictions.Add(PlayerActionRestriction.MOVEMENT);
			}
		}

		SyncAnimatorParameters();
		RunNetcodeList();
	}

	/*
	Re-asserts the parameters that are only changed by state transitions of this controller
		Animators reset their parameters when (re)initialized, like when the Character is activated after joining,
		so values set while it was inactive would otherwise be lost
	*/
	private void SyncAnimatorParameters(){
		this.animator.SetBool(IS_PLAYER_HASH, true);
		this.animator.SetBool(HAS_CARRY_HASH, HasCarry());
		this.animator.SetBool(SHEATHED_HASH, this.weaponSheathed);
		this.animatorFP.SetBool(HAS_CARRY_HASH, this.currentCarryHasFP);
		this.animatorFP.SetBool(SHEATHED_HASH, this.weaponSheathed);
	}

	void LateUpdate(){
		if(!this.INIT)
			return;

		RunPlaylist();
		ResetCombo();
		TrackSheathe();
	}

	public void Init(){
		if(this.INIT)
			return;

		this.INIT = true;
		this.animationHandler = this.gameObject.GetComponent<AnimationHandler>();
		this.playerMovement = this.gameObject.GetComponent<PlayerMovement>();
		this.animator = this.animationHandler.GetThirdPersonAnimator();
		this.animatorFP = this.animationHandler.GetFirstPersonAnimator();
		this.originalController = this.animator.runtimeAnimatorController;
		this.originalControllerFP = this.animatorFP.runtimeAnimatorController;
		this.registeredAction = new HashSet<PlayerActionType>();
		this.registeredData = new Dictionary<PlayerActionType, int>();
		this.playlist = new List<string>();
		this.overrideState = new List<bool>();
		this.ignoreFP = new List<bool>();
		this.restrictions = new HashSet<PlayerActionRestriction>();
		this.restrictionTimer = new Dictionary<PlayerActionRestriction, Coroutine>();
		this.currentlyQueuedState = new HashSet<string>();
		statesPlayed = new List<AnimationData>();
	}

	public void UseStyle(int style, bool skipCheck=false){
		if(!this.INIT)
			Init();

		if(!skipCheck){
			if(IsCurrentStyle(style)){
				if(this.registeredAction.Contains(PlayerActionType.USE_STYLE)){
					this.registeredAction.Remove(PlayerActionType.USE_STYLE);
					this.registeredData.Remove(PlayerActionType.USE_STYLE);
				}
				return;
			}

			if(this.registeredAction.Contains(PlayerActionType.USE_STYLE)){
				this.registeredData[PlayerActionType.USE_STYLE] = style;
				return;
			}
		}

		if(HasRestriction(PlayerActionRestriction.USE_STYLE)){
			RegisterUseStyle(style);
			return;
		}

		if(!this.weaponSheathed){
			Sheathe(true);
			RegisterUseStyle(style);
			Sheathe(false);
			return;
		}
		
		ForceUseStyle(style);
	}
	public void UseStyle(string styleName){UseStyle(AnimationLoader.GetBattleStyle(styleName).GetCode());}

	/*
	Uses a style informed by the Server together with the items that skin its attachments
		If neither the style nor the carried off-hand style change, the current attachments are re-skinned right away
		Otherwise, the items are applied when the new style's attachments are created
	*/
	public void UseStyle(int style, AttachmentItems items){
		if(!this.INIT)
			Init();

		this.pendingAttachmentItems = items;

		if(IsCurrentStyle(style) && !this.appliedAttachmentItems.Equals(items)){
			this.appliedAttachmentItems = items;
			this.animationHandler.RefreshAttachmentMaterials(items);
		}

		UseStyle(style);
	}

	public void ForceUseStyle(int style){
		if(IsCurrentStyle(style)){
			this.registeredAction.Remove(PlayerActionType.USE_STYLE);
			this.registeredData.Remove(PlayerActionType.USE_STYLE);
			return;
		}


		this.currentStyle = AnimationLoader.GetBattleStyle(style);
		this.currentStyleCode = style;
		this.appliedAttachmentItems = this.pendingAttachmentItems;
		this.currentCarryCode = BattleStyleDeterminator.GetCarryStyleCode(style, this.appliedAttachmentItems);
		this.animationHandler.CreateAttachments(this.currentStyle, this.appliedAttachmentItems, this.currentCarryCode);

		Debug.Log($"{this.currentStyle} - {this.currentCarryCode}");

		AnimatorOverrideController animationOverrideController = new AnimatorOverrideController(this.originalController);
		AnimatorOverrideController animationOverrideControllerFP = new AnimatorOverrideController(this.originalControllerFP);

		animationOverrideController = ApplyOverrides(animationOverrideController, this.currentStyle.GetOverrides());
		animationOverrideControllerFP = ApplyOverrides(animationOverrideControllerFP, AnimationLoader.GetBattleStyle($"{this.currentStyle.GetName()}-FP").GetOverrides());

		this.currentCarryHasFP = false;

		if(HasCarry()){
			CarryStyleData carryStyle = AnimationLoader.GetCarryStyle(this.currentCarryCode);
			string carryStyleFP = $"{carryStyle.GetName()}-FP";

			animationOverrideController = ApplyOverrides(animationOverrideController, carryStyle.GetOverrides());

			if(AnimationLoader.ContainsCarryStyle(carryStyleFP) && AnimationLoader.GetCarryStyle(carryStyleFP).IsAvailable()){
				animationOverrideControllerFP = ApplyOverrides(animationOverrideControllerFP, AnimationLoader.GetCarryStyle(carryStyleFP).GetOverrides());
				this.currentCarryHasFP = true;
			}
		}

		this.animator.runtimeAnimatorController = animationOverrideController;
		this.animatorFP.runtimeAnimatorController = animationOverrideControllerFP;

		// Changing controllers resets the Animator parameters, so the carry flag is set right away. It's also re-asserted every frame by SyncAnimatorParameters
		if(HasCarry())
			this.animator.SetBool(HAS_CARRY_HASH, true);
		if(this.currentCarryHasFP)
			this.animatorFP.SetBool(HAS_CARRY_HASH, true);

		this.registeredAction.Remove(PlayerActionType.USE_STYLE);
		this.registeredData.Remove(PlayerActionType.USE_STYLE);
	}

	// A style is only the current one if the carried off-hand style that the pending items would produce is also the same
	private bool IsCurrentStyle(int style){
		return this.currentStyleCode == style && this.currentCarryCode == BattleStyleDeterminator.GetCarryStyleCode(style, this.pendingAttachmentItems);
	}

	private bool HasCarry(){return this.currentCarryCode != BattleStyleDeterminator.NO_CARRY;}

	// The carried off-hand item is stowed and drawn together with the kit, so the longest of both clips is used
	private float GetSheatheClipLength(bool sheathe, bool firstPerson=false){
		float length = this.animationHandler.GetClipLength(sheathe ? SHEATHE_STATE : UNSHEATHE_STATE, firstPerson:firstPerson);

		if(HasCarry())
			length = Mathf.Max(length, this.animationHandler.GetClipLength(sheathe ? CARRY_STOW_STATE : CARRY_DRAW_STATE, firstPerson:firstPerson));

		return length;
	}

	public void RemoveAllStyles(){
		this.animator.runtimeAnimatorController = this.originalController;
		this.animatorFP.runtimeAnimatorController = this.originalControllerFP;
	}

	public void Sheathe(bool flag, bool skipCheck=false){
		if(!skipCheck){
			if(this.registeredAction.Contains(PlayerActionType.SHEATHE_ON)){
				if(!flag){
					this.registeredAction.Remove(PlayerActionType.SHEATHE_ON);
				}
				return;
			}

			if(this.registeredAction.Contains(PlayerActionType.SHEATHE_OFF)){
				if(flag){
					this.registeredAction.Remove(PlayerActionType.SHEATHE_OFF);
				}
				return;
			}
		}

		if(this.weaponSheathed == flag){
			if(flag && this.registeredAction.Contains(PlayerActionType.SHEATHE_ON))
				this.registeredAction.Remove(PlayerActionType.SHEATHE_ON);
			else if(!flag && this.registeredAction.Contains(PlayerActionType.SHEATHE_OFF))
				this.registeredAction.Remove(PlayerActionType.SHEATHE_OFF);
			return;
		}

		if(HasRestriction(PlayerActionRestriction.SHEATHE)){
			RegisterSheathe(flag);
			return;
		}

		// Characters always join sheathed. Coroutines can't run while the Character is inactive, so requests made before the world is drawn are ignored
		if(!this.gameObject.activeInHierarchy)
			return;

		ForceSheathe(flag);
	}
	private void ForceSheathe(bool flag){
		float animationTime = GetSheatheClipLength(flag) - 0.05f;

		RegisterRestriction(PlayerActionRestriction.SHEATHE, animationTime);
		RegisterRestriction(PlayerActionRestriction.USE_STYLE, animationTime);
		this.weaponSheathed = flag;
		this.animator.SetBool("Sheathed", this.weaponSheathed);
		this.animatorFP.SetBool("Sheathed", this.weaponSheathed);
		this.animator.SetBool("IsSheathing", true);
		this.animatorFP.SetBool("IsSheathing", true);

		this.comboHit = 0;
		RegisterRestriction(PlayerActionRestriction.PRIMARY, animationTime + 0.05f);

		if(flag){
			// Discards buffered attacks so they can't start once the weapon is sheathed
			this.registeredAction.Remove(PlayerActionType.PRIMARY_ACTION);
			StartSheatheTracking();
		}
		else{
			this.sheatheInProgress = false;
		}

		// In case of this action being triggered by action queue
		if(flag){
			if(this.registeredAction.Contains(PlayerActionType.SHEATHE_ON)){
				this.registeredAction.Remove(PlayerActionType.SHEATHE_ON);
			}
		}
		else{
			if(this.registeredAction.Contains(PlayerActionType.SHEATHE_OFF)){
				this.registeredAction.Remove(PlayerActionType.SHEATHE_OFF);
			}
		}
	}

	/*
	Returns true while a Sheathe is queued or its animation hasn't fully finished in every animator
		The SHEATHE restriction can't be used for this, since it's lifted before the end of the clip and its events
	*/
	public bool IsSheathePending(){
		if(!this.INIT)
			return false;

		return this.sheatheInProgress || this.registeredAction.Contains(PlayerActionType.SHEATHE_ON);
	}

	private void StartSheatheTracking(){
		this.sheatheInProgress = true;
		this.sheatheSeenTP = false;
		this.sheatheSeenFP = false;
		this.carryStowSeenTP = false;
		this.carryStowSeenFP = false;
		this.sheatheTimeout = Mathf.Max(GetSheatheClipLength(true), GetSheatheClipLength(true, firstPerson:true)) + SHEATHE_TIMEOUT_MARGIN;
	}

	// Runs on LateUpdate, after the Animators have updated and dispatched their Animation Events for the frame
	private void TrackSheathe(){
		if(!this.sheatheInProgress)
			return;

		this.sheatheTimeout -= Time.deltaTime;

		bool tpDone = IsSheatheStateDone(this.animator, SHEATHE_STATE, false, ref this.sheatheSeenTP);
		bool fpDone = IsSheatheStateDone(this.animatorFP, SHEATHE_STATE, true, ref this.sheatheSeenFP);

		if(HasCarry()){
			tpDone &= IsSheatheStateDone(this.animator, CARRY_STOW_STATE, false, ref this.carryStowSeenTP);

			if(this.currentCarryHasFP)
				fpDone &= IsSheatheStateDone(this.animatorFP, CARRY_STOW_STATE, true, ref this.carryStowSeenFP);
		}

		if((tpDone && fpDone) || this.sheatheTimeout <= 0f)
			this.sheatheInProgress = false;
	}

	private bool IsSheatheStateDone(Animator anim, string state, bool firstPerson, ref bool seen){
		if(anim == null || !anim.isActiveAndEnabled)
			return true;

		float time = this.animationHandler.GetAnimationTime(state, firstPerson:firstPerson);

		if(time >= 0f)
			seen = true;
		if(time >= 1f)
			return true;

		return seen && time < 0f;
	}

	public void RegisterRestriction(PlayerActionRestriction rest, float timeout){
		this.restrictions.Add(rest);

		if(timeout < 0){
			Debug.LogWarning($"[PlayerActionController] Timeout provided was below zero: {timeout} -- Timeout will be changed to {timeout+1}");
			timeout += 1;
		}

		if(timeout > 0){
			if(this.restrictionTimer.ContainsKey(rest)){
				StopCoroutine(this.restrictionTimer[rest]);
				this.restrictionTimer.Remove(rest);
			}

			this.restrictionTimer.Add(rest, StartCoroutine(RestrictionRoutine(rest, timeout)));
		}
	}

	public void RemoveRestriction(PlayerActionRestriction rest){this.restrictions.Remove(rest);}
	public bool HasRestriction(PlayerActionRestriction rest){return this.restrictions.Contains(rest);}

	// Registers a primary action. Attacks are only possible with the weapon unsheathed
	public void RegisterPrimaryAction(){
		if(!this.INIT || this.weaponSheathed)
			return;

		if(this.restrictions.Contains(PlayerActionRestriction.PRIMARY) || this.restrictions.Contains(PlayerActionRestriction.STUNNED) || this.restrictions.Contains(PlayerActionRestriction.SYSTEM))
			return;

		this.registeredAction.Add(PlayerActionType.PRIMARY_ACTION);
		RegisterRestriction(PlayerActionRestriction.SHEATHE, 1.5f);
	}

	// Registers sheathing
	public void RegisterSheathe(bool flag){
		if(flag)
			this.registeredAction.Add(PlayerActionType.SHEATHE_ON);
		else
			this.registeredAction.Add(PlayerActionType.SHEATHE_OFF);
	}

	// Registers UseStyle
	public void RegisterUseStyle(int style){
		this.registeredAction.Add(PlayerActionType.USE_STYLE);
		this.registeredData.Add(PlayerActionType.USE_STYLE, style);
	}

	public void VerifyMovement(Vector3 facingDirection, Vector3 movementDirection, float runMomentum, float gravity, MovementFlags flags){
		float angle = Vector3.SignedAngle(facingDirection, movementDirection, Vector3.up);
		PlayerMovementType pmt;

		this.animator.SetFloat("Run", runMomentum);
		this.animatorFP.SetBool("Run", runMomentum > 0);
		this.animator.SetFloat("Gravity", gravity);
		this.animator.SetBool("IsGrounded", flags.isGrounded);
		this.animator.SetBool("ShouldMove", movementDirection.magnitude != 0);

		SendAnimatorValue("Run", runMomentum);

		if(!flags.isGrounded && this.weaponSheathed)
			pmt = PlayerMovementType.AIR;
		else if(!flags.isGrounded && !this.weaponSheathed && Mathf.Abs(angle) <= 50 && movementDirection != Vector3.zero)
			pmt = PlayerMovementType.AIR_AGGRO_FORWARD;
		else if(!flags.isGrounded && !this.weaponSheathed)
			pmt = PlayerMovementType.AIR_AGGRO;
		else if(movementDirection.magnitude == 0 && this.weaponSheathed)
			pmt = PlayerMovementType.STILL;
		else if(movementDirection.magnitude == 0 && !this.weaponSheathed)
			pmt = PlayerMovementType.STILL_AGGRO;
		else if(Mathf.Abs(angle) >= 180)
			pmt = PlayerMovementType.BACKWARD;
		else if(Mathf.Abs(angle) <= 50 && this.weaponSheathed)
			pmt = PlayerMovementType.FORWARD_SHEATHED;
		else if(Mathf.Abs(angle) <= 50)
			pmt = PlayerMovementType.FORWARD;
		else if(angle > 0)
			pmt = PlayerMovementType.RIGHT;
		else
			pmt = PlayerMovementType.LEFT;

		if(pmt != this.lastMove)
			ProcessMovement(pmt, runMomentum > 0f);

		this.lastMove = pmt;
	}

	public static void RegisterClientMessage(AnimationData data){PlayerActionController.statesPlayed.Insert(0, data);}

	// Used only in Menu
	public static void UseStyle(Animator animator, string styleName){
		AnimatorOverrideController animationOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
		animationOverrideController = ApplyOverrides(animationOverrideController, AnimationLoader.GetBattleStyle(styleName).GetOverrides());
		animator.runtimeAnimatorController = animationOverrideController;
	}

	private void RunNetcodeList(){
		for(int i = statesPlayed.Count - 1; i >= 0; i--){
			this.animationLayerMessage.SendAnimationLayer(Configurations.accountID, statesPlayed[i]);
			this.cl.client.Send(this.animationLayerMessage);
			statesPlayed.RemoveAt(i);
		} 
	}

	private void RunPlaylist(){
		for(int i=0; i < this.playlist.Count; i++){
			this.animationHandler.Play(this.playlist[i], overrideState:this.overrideState[i], ignoreFP:this.ignoreFP[i]);
			this.currentlyQueuedState.Add(this.playlist[i]);
		}

		this.playlist.Clear();
		this.overrideState.Clear();
		this.ignoreFP.Clear();
	}

    private IEnumerator RestrictionRoutine(PlayerActionRestriction rest, float timeout){
		yield return new WaitForSeconds(timeout);
		RemoveRestriction(rest);

		if(rest == PlayerActionRestriction.USE_STYLE){
			if(this.registeredAction.Contains(PlayerActionType.USE_STYLE)){
				ForceUseStyle(this.registeredData[PlayerActionType.USE_STYLE]);
			}
		}
		else if(rest == PlayerActionRestriction.SHEATHE){
			if(this.registeredAction.Contains(PlayerActionType.USE_STYLE)){
				ForceUseStyle(this.registeredData[PlayerActionType.USE_STYLE]);
			}
			if(this.registeredAction.Contains(PlayerActionType.SHEATHE_ON)){
				Sheathe(true, skipCheck:true);
			}
			if(this.registeredAction.Contains(PlayerActionType.SHEATHE_OFF)){
				Sheathe(false, skipCheck:true);
			}
		}
    }


	private static AnimatorOverrideController ApplyOverrides(AnimatorOverrideController controller, StateClipPair[] overrides){
		foreach(StateClipPair over in overrides){
			controller[over.FetchStateClip()] = over.FetchFinalClip();
		}

		return controller;
	}

	private void AddToPlaylist(string state, bool over=false, bool igFP=false){		
		this.playlist.Add(state);
		this.overrideState.Add(over);
		this.ignoreFP.Add(igFP);
	}

	private void ProcessMovement(PlayerMovementType pmt, bool isRunning){
		switch(pmt){
			case PlayerMovementType.STILL:
				AddToPlaylist("Idle");
				break;
			case PlayerMovementType.STILL_AGGRO:
				AddToPlaylist("Idle Weapon");
				break;
			case PlayerMovementType.FORWARD:
				AddToPlaylist("Moving Forward", igFP:!isRunning);
				break;
			case PlayerMovementType.FORWARD_SHEATHED:
				AddToPlaylist("Moving Forward", igFP:true);
				break;
			case PlayerMovementType.BACKWARD:
				AddToPlaylist("Walk Backward", igFP:!isRunning);
				break;
			case PlayerMovementType.LEFT:
				AddToPlaylist("Walk Left", igFP:!isRunning);
				break;
			case PlayerMovementType.RIGHT:
				AddToPlaylist("Walk Right", igFP:!isRunning);
				break;
			case PlayerMovementType.AIR:
				AddToPlaylist("On Air");
				break;
			case PlayerMovementType.AIR_AGGRO:
				AddToPlaylist("Idle Weapon");
				break;
			case PlayerMovementType.AIR_AGGRO_FORWARD:
				AddToPlaylist("Idle Weapon", igFP:true);
				AddToPlaylist("Moving Forward", igFP:true);
				break;
			default:
				break;
		}
	}

	private void ResetCombo(){
		if(this.comboHit >= 1){
			this.cachedTime = this.animationHandler.GetAnimationTime($"Attack {this.comboHit}");

			if(this.cachedTime == -1 && !this.currentlyQueuedState.Contains($"Attack {this.comboHit}")){
				this.comboHit = 0;
				this.registeredAction.Remove(PlayerActionType.PRIMARY_ACTION);
				RemoveRestriction(PlayerActionRestriction.MOVEMENT);
				this.animator.SetInteger("Attack_Combo", this.comboHit);
			}
		}

		this.currentlyQueuedState.Clear();
	}

	private void SendAnimatorValue(string parameter, float val){
		if(!this.lastParameterValue.ContainsKey(parameter))
			this.lastParameterValue.Add(parameter, -1);

		if(this.lastParameterValue[parameter] == val)
			return;

		this.lastParameterValue[parameter] = val;

		this.animatorParameterMessage.SendAnimatorParameter(Configurations.accountID, val, parameter);
		this.cl.client.Send(this.animatorParameterMessage);
	}
}