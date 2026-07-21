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
	private BattleStyleData currentStyle;
	private bool weaponSheathed = true;
	private int comboHit = 0;

	// Default Config
	private float hitWindowStart = .48f;
	private float attackExitTime = .8f;
	private HashSet<PlayerActionType> registeredAction;
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

		this.animator.SetBool("ISPLAYER", true);
		RunNetcodeList();
	}

	void LateUpdate(){
		if(!this.INIT)
			return;

		RunPlaylist();
		ResetCombo();
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
		this.playlist = new List<string>();
		this.overrideState = new List<bool>();
		this.ignoreFP = new List<bool>();
		this.restrictions = new HashSet<PlayerActionRestriction>();
		this.restrictionTimer = new Dictionary<PlayerActionRestriction, Coroutine>();
		this.currentlyQueuedState = new HashSet<string>();
		this.restrictions.Add(PlayerActionRestriction.PRIMARY);
		statesPlayed = new List<AnimationData>();
	}

	public void UseStyle(int style, bool updatePlayerDataAndServer=false){
		if(!this.INIT)
			Init();

		// Simple lock to avoid Style Switching without having weaponSheathed first
		if(!this.weaponSheathed){
			SyncCurrentStyleToServer();
			return;
		}

		if(this.currentStyleCode == style)
			return;

		this.currentStyle = AnimationLoader.GetBattleStyle(style);
		this.animationHandler.CreateAttachments(this.currentStyle);

		AnimatorOverrideController animationOverrideController = new AnimatorOverrideController(this.originalController);
		AnimatorOverrideController animationOverrideControllerFP = new AnimatorOverrideController(this.originalControllerFP);

		animationOverrideController = ApplyOverrides(animationOverrideController, this.currentStyle.GetOverrides());
		animationOverrideControllerFP = ApplyOverrides(animationOverrideControllerFP, AnimationLoader.GetBattleStyle($"{this.currentStyle.GetName()}-FP").GetOverrides());

		this.animator.runtimeAnimatorController = animationOverrideController;
		this.animatorFP.runtimeAnimatorController = animationOverrideControllerFP;
		this.animator.SetBool("ISPLAYER", true);
		this.animator.SetBool("Sheathed", true);
		this.animatorFP.SetBool("Sheathed", true);

		if(updatePlayerDataAndServer){
			NetMessage message = new NetMessage(NetCode.SENDBATTLESTYLE);
			message.SendBattleStyle(Configurations.accountID, style);
			this.cl.client.Send(message);

			this.cl.playerSheetController.GetSheet().SetBattleStyleCode(style);
			this.cl.playerSheetController.SendToServer();
		}
	}
	public void UseStyle(string style, bool updatePlayerDataAndServer=false){UseStyle(AnimationLoader.GetBattleStyle(style).GetCode(), updatePlayerDataAndServer:updatePlayerDataAndServer);}

	public void RemoveAllStyles(){
		this.animator.runtimeAnimatorController = this.originalController;
		this.animatorFP.runtimeAnimatorController = this.originalControllerFP;
	}

	public void Sheathe(){
		if(this.restrictions.Contains(PlayerActionRestriction.SHEATHE))
			return;

		RegisterRestriction(PlayerActionRestriction.SHEATHE, 0.9f);
		this.weaponSheathed = !this.weaponSheathed;
		this.animator.SetBool("Sheathed", this.weaponSheathed);
		this.animatorFP.SetBool("Sheathed", this.weaponSheathed);
		this.animator.SetBool("IsSheathing", true);
		this.animatorFP.SetBool("IsSheathing", true);

		if(this.weaponSheathed){
			this.comboHit = 0;
			RegisterRestriction(PlayerActionRestriction.PRIMARY, 0);
		}
		else{
			RemoveRestriction(PlayerActionRestriction.PRIMARY);
		}
	}

	public void RegisterRestriction(PlayerActionRestriction rest, float timeout){
		this.restrictions.Add(rest);

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

	// Registers a primary action
	public void RegisterPrimaryAction(){
		// Temporary check until Weapon Itemtypes are introduced into the game
		if(this.restrictions.Contains(PlayerActionRestriction.PRIMARY) || this.restrictions.Contains(PlayerActionRestriction.STUNNED) || this.restrictions.Contains(PlayerActionRestriction.SYSTEM))
			return;

		this.registeredAction.Add(PlayerActionType.PRIMARY_ACTION);
		RegisterRestriction(PlayerActionRestriction.SHEATHE, 1.5f);
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

	// Used when UseStyle is denied by Client
	private void SyncCurrentStyleToServer(){
		NetMessage message = new NetMessage(NetCode.SENDBATTLESTYLE);
		message.SendBattleStyle(Configurations.accountID, this.currentStyle.GetCode());
		this.cl.client.Send(message);
	}
}