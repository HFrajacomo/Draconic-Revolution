using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class EntityHandler
{
	// Unity Reference
	private ChunkLoader cl;
	private PlayerModelHandler playerModelHandler;

	private Dictionary<ulong, CharacterSheet> playerSheet;
	private Dictionary<ulong, GameObject> playerObject;
	private Dictionary<ulong, Transform> playerHead;

	private Dictionary<ulong, AnimationHandler> playerAnimations;
	private Dictionary<ulong, Dictionary<int, AnimationData>> pendingPlayerAnimations;
	private Dictionary<ulong, Dictionary<string, float>> pendingPlayerAnimatorParameters;
	private Dictionary<ulong, ushort> playerItem;
	private Dictionary<ulong, bool> playerItemOrActionInHand;
	private Dictionary<ulong, DeltaMove> playerCurrentPositions;
	private Dictionary<ulong, ItemEntity> dropObject;
	private Dictionary<ulong, DeltaMove> dropCurrentPositions;
	private Dictionary<ulong, int> playerBattleStyle;
	private Dictionary<ulong, Dictionary<string, EntitySFXData>> playerActiveLoopSFX;
	private HashSet<ulong> playerSFXNeedsReapply;
	private Dictionary<ulong, Dictionary<BoneAnchorType, AttachmentAnchorState>> playerAttachmentAnchors;
	private Dictionary<ulong, AttachmentItems> playerAttachmentItems;
	private Dictionary<ulong, PendingItemHold> pendingItemHolds;
	private List<ulong> cachedPendingHoldCodes;
	private GameObject droppedItemBase = GameObject.Find("----- PrefabModels -----/DroppedItemBase");

	private int currentTogglingCounter;

	private static readonly int MAX_COUNTER_VALUE = 4;
	private static readonly string SHEATHE_STATE = "Weapon Sheathe";
	private static readonly float SHEATHE_START_GRACE_TIME = 0.5f;
	private static readonly float SHEATHE_MAX_WAIT_TIME = 3f;
	private static RuntimeAnimatorController BASE_CONTROLLER;


	public EntityHandler(ChunkLoader cl){
		this.playerSheet = new Dictionary<ulong, CharacterSheet>();
		this.playerObject = new Dictionary<ulong, GameObject>();
		this.playerHead = new Dictionary<ulong, Transform>();
		this.playerItem = new Dictionary<ulong, ushort>();
		this.playerCurrentPositions = new Dictionary<ulong, DeltaMove>();
		this.dropObject = new Dictionary<ulong, ItemEntity>();
		this.dropCurrentPositions = new Dictionary<ulong, DeltaMove>();
		this.playerAnimations = new Dictionary<ulong, AnimationHandler>();
		this.pendingPlayerAnimations = new Dictionary<ulong, Dictionary<int, AnimationData>>();
		this.pendingPlayerAnimatorParameters = new Dictionary<ulong, Dictionary<string, float>>();
		this.playerBattleStyle = new Dictionary<ulong, int>();
		this.playerActiveLoopSFX = new Dictionary<ulong, Dictionary<string, EntitySFXData>>();
		this.playerSFXNeedsReapply = new HashSet<ulong>();
		this.playerAttachmentAnchors = new Dictionary<ulong, Dictionary<BoneAnchorType, AttachmentAnchorState>>();
		this.playerAttachmentItems = new Dictionary<ulong, AttachmentItems>();
		this.playerItemOrActionInHand = new Dictionary<ulong, bool>();
		this.pendingItemHolds = new Dictionary<ulong, PendingItemHold>();
		this.cachedPendingHoldCodes = new List<ulong>();
		this.cl = cl;
		this.playerModelHandler = cl.playerModelHandler;
		BASE_CONTROLLER = AnimationLoader.GetController("BASE_Character");
	}

	/*
	Runs through all entities in Handler to verify which ones are bordering RenderDistance and cannot be rendered
	*/
	public void RunEntityActivation(){
		currentTogglingCounter = currentTogglingCounter % MAX_COUNTER_VALUE;

		if(currentTogglingCounter == 0){
			foreach(ulong u in playerObject.Keys){
				SetPlayerActive(u, this.cl.playerPositionHandler.IsInPlayerRenderDistance(this.playerObject[u].transform.position));
				ApplyPendingPlayerAnimations(u);
			}
		}
		else if(currentTogglingCounter == 1){
			foreach(ItemEntity ie in dropObject.Values){
				ie.SetVisible(this.cl.playerPositionHandler.IsInPlayerRenderDistance(ie.transform.position));
			}
		}

		currentTogglingCounter++;
	}

	public void RunSingleActivation(EntityType type, ulong code, float3 pos){
		if(!Contains(type, code))
			return;

		if(type == EntityType.PLAYER){
			SetPlayerActive(code, this.cl.playerPositionHandler.IsInPlayerRenderDistance(pos));
			ApplyPendingPlayerAnimations(code);
		}
		else if(type == EntityType.DROP){
			this.dropObject[code].SetVisible(this.cl.playerPositionHandler.IsInPlayerRenderDistance(pos));
		}
	}


	// Only works while there is no other EntityTypes here other than player
	public bool Contains(EntityType type, ulong code){
		if(type == EntityType.PLAYER)
			return this.playerObject.ContainsKey(code);
		else if(type == EntityType.DROP)
			return this.dropObject.ContainsKey(code);
		return false;
	}

	public void AddPlayer(ulong code, float3 pos, float3 dir){
		GameObject go = GameObject.Instantiate(GameObject.Find("----- PrefabModels -----/PlayerModel"), new Vector3(pos.x, pos.y, pos.z), Quaternion.Euler(0, dir.y, 0));

		go.name = "Player_" + code;
		this.playerObject.Add(code, go);
		this.playerHead.Add(code, go.transform.Find("Camera"));
		this.playerItem.Add(code, 0);
		this.playerItemOrActionInHand.Add(code, true);
		this.playerCurrentPositions.Add(code, new DeltaMove(pos, dir));
		this.playerAnimations.Add(code, null);
		this.pendingPlayerAnimations.Add(code, new Dictionary<int, AnimationData>());
		this.pendingPlayerAnimatorParameters.Add(code, new Dictionary<string, float>());
		this.playerBattleStyle.Add(code, -1);
		this.playerActiveLoopSFX.Add(code, new Dictionary<string, EntitySFXData>());
		this.playerAttachmentAnchors.Add(code, new Dictionary<BoneAnchorType, AttachmentAnchorState>());
		this.playerAttachmentItems.Add(code, AttachmentItems.NONE);
		RunSingleActivation(EntityType.PLAYER, code, pos);
	}

	public void AddPlayerSheet(ulong code, CharacterSheet sheet){
		if(this.playerSheet.ContainsKey(code)){
			this.playerSheet[code] = sheet;
		}
		else{
			this.playerSheet.Add(code, sheet);
		}
	}

	public bool UpdatePlayerModel(ulong code, CharacterAppearance app, bool isMale){
		if(this.playerObject.ContainsKey(code)){
			this.playerModelHandler.DeleteModel(this.playerObject[code]);

			this.playerObject[code] = this.playerModelHandler.BuildModel(this.playerObject[code], app, isMale, code);
			this.playerHead[code] = this.playerObject[code].transform.Find("Camera");
			this.playerAnimations[code] = this.playerObject[code].GetComponent<AnimationHandler>();

			if(code != Configurations.accountID){
				SetPlayerBattleStyle(code, this.playerSheet[code].GetBattleStyleCode());
			}

			// Model rebuild destroys SFX attached to bones
			this.playerSFXNeedsReapply.Add(code);
			ApplyPendingPlayerAnimations(code);

			return true;
		}
		return false;
	}

	public void SetPlayerBattleStyle(ulong code, int style){
		if(!this.playerBattleStyle.ContainsKey(code))
			return;

		if(this.playerBattleStyle[code] == style)
			return;

		bool isMale = this.playerSheet[code].GetGender();
		this.playerBattleStyle[code] = style;

		BattleStyleData bsd = AnimationLoader.GetBattleStyle(style);
		AnimatorOverrideController animationOverrideController;

		animationOverrideController = new AnimatorOverrideController(BASE_CONTROLLER);

		animationOverrideController = ApplyOverrides(animationOverrideController, bsd.GetOverrides());

		this.playerAnimations[code].GetThirdPersonAnimator().runtimeAnimatorController = animationOverrideController;
		this.playerSheet[code].SetBattleStyleCode(style);
		this.playerAnimations[code].CreateAttachments(bsd, this.playerAttachmentItems[code]);
		ApplyAttachmentAnchors(code);

		// Attachments are replaced, destroying SFX attached to them
		this.playerSFXNeedsReapply.Add(code);
		ApplyPendingPlayerAnimations(code);
	}

	/*
	Handles a BattleStyle change received from the Server, resetting attachments to their default anchors when the style differs
		If the style is the same, only the attachment materials are refreshed with the newly equipped items
		If the player model wasn't built yet, the data is kept and applied when the model is built
	*/
	public void ChangePlayerBattleStyle(ulong code, int style, AttachmentItems items){
		if(!this.playerBattleStyle.ContainsKey(code))
			return;

		bool itemsChanged = !this.playerAttachmentItems[code].Equals(items);
		this.playerAttachmentItems[code] = items;

		if(GetKnownBattleStyle(code) != style)
			this.playerAttachmentAnchors[code].Clear();

		if(this.playerAnimations[code] == null || !this.playerSheet.ContainsKey(code)){
			if(this.playerSheet.ContainsKey(code))
				this.playerSheet[code].SetBattleStyleCode(style);
			return;
		}

		if(this.playerBattleStyle[code] == style){
			if(itemsChanged)
				this.playerAnimations[code].RefreshAttachmentMaterials(items);
			return;
		}

		SetPlayerBattleStyle(code, style);
	}

	// Receives an attachment anchor switch for a remote player. Applied right away if the attachment exists, otherwise after attachments are created
	public void PlayerAttachmentAnchor(ulong code, BoneAnchorType origin, BoneAnchorType target, bool flip, float heightOffset){
		if(!this.playerAttachmentAnchors.ContainsKey(code))
			return;

		if(!AttachmentAnchorState.Apply(this.playerAttachmentAnchors[code], origin, target, flip, heightOffset))
			return;

		if(this.playerAnimations[code] != null && this.playerAnimations[code].ContainsAttachment(origin, false))
			this.playerAnimations[code].SwitchAttachments(false, origin, target, heightOffset, flip);
	}

	// Moves freshly created attachments from their default anchors to the ones informed by the Server
	private void ApplyAttachmentAnchors(ulong code){
		if(!this.playerAttachmentAnchors.ContainsKey(code))
			return;

		foreach(KeyValuePair<BoneAnchorType, AttachmentAnchorState> item in this.playerAttachmentAnchors[code]){
			this.playerAnimations[code].SwitchAttachments(false, item.Key, item.Value.anchor, item.Value.heightOffset, item.Value.flip);
		}
	}

	// Gets the last known BattleStyle of a player, even if the attachments were not created yet
	private int GetKnownBattleStyle(ulong code){
		if(this.playerBattleStyle[code] != -1)
			return this.playerBattleStyle[code];
		if(this.playerSheet.ContainsKey(code))
			return this.playerSheet[code].GetBattleStyleCode();
		return -1;
	}

	public void AddItem(ulong code, float3 pos, float3 dir, ItemStack its){
		ItemEntity newItemEntity = GameObject.Instantiate(this.droppedItemBase, pos, Quaternion.Euler(dir.x, dir.y, dir.z)).GetComponent<ItemEntity>();
		newItemEntity.SetItemStack(its);

		this.dropObject.Add(code, newItemEntity);
		this.dropCurrentPositions.Add(code, new DeltaMove(pos, dir));
		RunSingleActivation(EntityType.DROP, code, pos);
	}

	public void AnimateBone(ulong code, string stateName, int layer){
		if(!this.playerAnimations.ContainsKey(code))
			return;

		if(!CanProcessPlayerAnimations(code)){
			this.pendingPlayerAnimations[code][layer] = new AnimationData(stateName, layer);
			return;
		}

		this.playerAnimations[code].Play(stateName, layer);
	}

	// Triggers whenever a t(x) position is received. Moves entity to t(x-1) position received
	public void NudgeLastPos(EntityType type, ulong code, float3 pos, float3 dir){
		if(!Contains(type, code))
			return;

		if(type == EntityType.PLAYER){	
			this.playerObject[code].transform.position = this.playerCurrentPositions[code].deltaPos;
			this.playerObject[code].transform.eulerAngles = new Vector3(0, this.playerCurrentPositions[code].deltaRot.y, 0);
			this.playerHead[code].transform.localRotation = Quaternion.Euler(this.playerCurrentPositions[code].deltaRot.x, 0, 0);
		
			this.playerCurrentPositions[code] = new DeltaMove(pos, dir);
		}
		else if(type == EntityType.DROP){
			this.dropObject[code].gameObject.transform.position = this.dropCurrentPositions[code].deltaPos;

			this.dropCurrentPositions[code] = new DeltaMove(pos, dir);	
		}
	}

	// Fine movement of entity in frame deltas
	public void Nudge(EntityType type, ulong code, Vector3 dPos, Vector3 dRot){
		if(!Contains(type, code))
			return;

		EntityID id = new EntityID(type, code);

		if(type == EntityType.PLAYER){
			this.playerObject[code].transform.position += (dPos * (Time.deltaTime / TimeOfDay.timeRate));
			this.playerObject[code].transform.eulerAngles += new Vector3(0, (dRot * (Time.deltaTime / TimeOfDay.timeRate)).y, 0);
			this.playerHead[code].transform.eulerAngles += new Vector3((dRot * (Time.deltaTime / TimeOfDay.timeRate)).x, 0, 0);

			if(this.cl.voxelLightHandler.Contains(id)){
				this.cl.voxelLightHandler.UpdatePosition(id, this.playerObject[code].transform.position);
			}
		}
		else if(type == EntityType.DROP){
			this.dropObject[code].gameObject.transform.position += (dPos * (Time.deltaTime / TimeOfDay.timeRate));
		}
	}

	// Should only be used to hard set item rotation animation position
	public void SetItemPosition(ulong code, Vector3 newPos){
		if(!this.dropObject.ContainsKey(code))
			return;

		if(this.dropObject.ContainsKey(code)){
			this.dropObject[code].gameObject.transform.position = newPos;
		}
	}

	// ...
	public void Remove(EntityType type, ulong code){
		if(type == EntityType.PLAYER){
			UpdatePlayerItem(code, 0, 0, true, 1);
			
			this.playerObject[code].SetActive(false);
			GameObject.Destroy(this.playerObject[code]);
			this.playerHead.Remove(code);
			this.playerObject.Remove(code);
			this.playerItem.Remove(code);
			this.playerItemOrActionInHand.Remove(code);
			this.playerCurrentPositions.Remove(code);
			this.playerSheet.Remove(code);
			this.playerAnimations.Remove(code);
			this.pendingPlayerAnimations.Remove(code);
			this.pendingPlayerAnimatorParameters.Remove(code);
			this.playerBattleStyle.Remove(code);
			this.playerActiveLoopSFX.Remove(code);
			this.playerSFXNeedsReapply.Remove(code);
			this.playerAttachmentAnchors.Remove(code);
			this.playerAttachmentItems.Remove(code);
			this.pendingItemHolds.Remove(code);
			this.cl.sfx.RemoveEntitySFX(new EntityID(type, code));
		}
		else if(type == EntityType.DROP){
			this.dropObject[code].gameObject.SetActive(false);
			GameObject.Destroy(this.dropObject[code]);
			this.dropObject.Remove(code);
			this.dropCurrentPositions.Remove(code);
		}
	}

	public void ToggleItemAnimation(EntityType type, ulong code, float f){
		if(type == EntityType.DROP){
			if(this.dropObject.ContainsKey(code)){
				if(f == 1f){
					this.dropObject[code].PlayRotationAnimation();
				}
				else{
					this.dropObject[code].PlaySpinAnimation();
				}
			}
		}
	}

	public void SetAnimatorParameter(EntityType type, ulong code, string parameter, float val){
		if(!Contains(type, code))
			return;

		if(type == EntityType.PLAYER){
			if(!CanProcessPlayerAnimations(code)){
				this.pendingPlayerAnimatorParameters[code][parameter] = val;
				return;
			}

			this.playerAnimations[code].SetParameterValue(parameter, val, false);
		}
	}

	public Vector3 GetLastPosition(EntityType type, ulong code){
		if(!Contains(type, code))
			return Vector3.zero;

		if(type == EntityType.PLAYER)
			return this.playerCurrentPositions[code].deltaPos;
		else
			return this.dropCurrentPositions[code].deltaPos;
	}

	public Vector3 GetLastRotation(EntityType type, ulong code){
		if(!Contains(type, code))
			return Vector3.zero;

		if(type == EntityType.PLAYER)
			return this.playerCurrentPositions[code].deltaRot;
		else
			return this.dropCurrentPositions[code].deltaRot;
	}

	public CharacterSheet GetPlayerSheet(ulong code){
		return this.playerSheet[code];
	}

	public GameObject GetEntityObject(EntityID id){
		if(id.type == EntityType.PLAYER){
			if(id.code == this.cl.playerAccountID){
				return this.cl.audioListenerObject;
			}

			return this.playerObject[id.code];
		}
		return this.playerObject[id.code];
	}

	public GameObject GetEntityMiddle(EntityID id){
		if(id.type == EntityType.PLAYER){
			return GetEntityObject(id).transform.Find("MiddlePoint").gameObject;
		}
		else{
			return GetEntityObject(id);
		}
	}

	public Light GetPointLight(EntityType type, ulong code){
		if(type == EntityType.PLAYER){
			if(this.playerObject.ContainsKey(code)){
				return this.playerObject[code].GetComponentInChildren<Light>();
			}
		}

		return null;
	}

	private bool CanProcessPlayerAnimations(ulong code){
		return this.playerAnimations.ContainsKey(code) &&
			this.playerAnimations[code] != null &&
			this.playerObject[code].activeInHierarchy &&
			this.playerAnimations[code].GetThirdPersonAnimator().isActiveAndEnabled;
	}

	private void ApplyPendingPlayerAnimations(ulong code){
		if(!CanProcessPlayerAnimations(code))
			return;

		foreach(AnimationData animationData in this.pendingPlayerAnimations[code].Values){
			this.playerAnimations[code].Play(animationData.name, animationData.layer);
		}

		foreach(KeyValuePair<string, float> parameter in this.pendingPlayerAnimatorParameters[code]){
			this.playerAnimations[code].SetParameterValue(parameter.Key, parameter.Value, false);
		}

		this.pendingPlayerAnimations[code].Clear();
		this.pendingPlayerAnimatorParameters[code].Clear();

		if(this.playerSFXNeedsReapply.Contains(code)){
			this.playerSFXNeedsReapply.Remove(code);
			ReapplyPlayerLoopSFX(code);
		}
	}

	// Sets the active state of a player object, flagging its looping SFX to be restarted when it becomes visible again
	private void SetPlayerActive(ulong code, bool active){
		if(active && !this.playerObject[code].activeSelf)
			this.playerSFXNeedsReapply.Add(code);

		this.playerObject[code].SetActive(active);
	}

	/*
	Receives an SFX operation for a remote player
		Looping SFX are tracked so they can be rebuilt if the player model is not ready, rebuilt or out of sight
		One-shot SFX are dropped if the player can't currently be processed
	*/
	public void PlayerSFX(ulong code, EntitySFXOperation operation, EntitySFXData sfx){
		if(!this.playerObject.ContainsKey(code))
			return;

		EntityID id = new EntityID(EntityType.PLAYER, code);

		if(operation == EntitySFXOperation.DESTROY){
			this.playerActiveLoopSFX[code].Remove(sfx.name);
			this.cl.sfx.RemoveEntitySFX(id, sfx.name);
			return;
		}

		if(!AudioLoader.IsSound(sfx.name))
			return;

		if(sfx.isLoop)
			this.playerActiveLoopSFX[code][sfx.name] = sfx;

		if(!CanProcessPlayerAnimations(code)){
			if(sfx.isLoop)
				this.playerSFXNeedsReapply.Add(code);
			return;
		}

		LoadPlayerSFX(code, sfx);
	}

	// Rebuilds all looping SFX of a remote player
	private void ReapplyPlayerLoopSFX(ulong code){
		if(!this.playerActiveLoopSFX.ContainsKey(code))
			return;

		EntityID id = new EntityID(EntityType.PLAYER, code);

		foreach(EntitySFXData sfx in this.playerActiveLoopSFX[code].Values){
			this.cl.sfx.RemoveEntitySFX(id, sfx.name);
			LoadPlayerSFX(code, sfx);
		}
	}

	private void LoadPlayerSFX(ulong code, EntitySFXData sfx){
		Transform parent = null;

		if(sfx.hasAnchor){
			GameObject attachment = this.playerAnimations[code].GetAttachment(sfx.anchor, false);

			if(attachment != null)
				parent = attachment.transform;
		}

		this.cl.sfx.LoadEntitySFX(sfx.name, new EntityID(EntityType.PLAYER, code), parent:parent, localPosition:sfx.GetOffset());
	}

	/*
	Runs the Unhold event of the previous item/action and the Hold event of the new one for a remote player
		If the previous action sheathes the weapon, the Item's OnHoldClient is deferred until the Sheathe animation is over
		If a deferred Hold never ran, its Unhold is skipped
	*/
	public void UpdatePlayerItem(ulong playerCode, ushort id, ushort connectedItemId, bool isItem, byte quantity){
		if(!this.playerItem.ContainsKey(playerCode)){
			return;
		}

		if(this.playerItem[playerCode] == id && this.playerItemOrActionInHand[playerCode] == isItem){
			return;
		}

		ushort oldItem = this.playerItem[playerCode];
		bool oldIsItem = this.playerItemOrActionInHand[playerCode];
		bool oldHoldWasPending = this.pendingItemHolds.Remove(playerCode);
		bool isSheathing = false;

		if(oldIsItem){
			if(!oldHoldWasPending)
				ItemLoader.GetItem(oldItem).OnUnholdClient(this.cl, new ItemStack(oldItem, 1), playerCode);
		}
		else{
			EntityAction oldAction = ActionLoader.GetAction(oldItem);
			oldAction.OnUnholdClient(this.cl, new ItemStack(connectedItemId, 1), playerCode);
			isSheathing = oldAction.SheathesOnUnhold();
		}

		this.playerItem[playerCode] = id;
		this.playerItemOrActionInHand[playerCode] = isItem;

		if(isItem){
			if(isSheathing && CanProcessPlayerAnimations(playerCode))
				this.pendingItemHolds.Add(playerCode, new PendingItemHold(id, quantity));
			else
				ItemLoader.GetItem(id).OnHoldClient(this.cl, new ItemStack(id, quantity), playerCode);
		}
		else{
			ActionLoader.GetAction(id).OnHoldClient(this.cl, new ItemStack(connectedItemId, 1), playerCode);
		}
	}

	/*
	Runs every frame, releasing deferred Item Holds once the remote player's Sheathe animation is over
		The Sheathe animation state may arrive after the item change, so a grace time is given for it to start
	*/
	public void RunPendingItemHolds(){
		if(this.pendingItemHolds.Count == 0)
			return;

		PendingItemHold hold;

		this.cachedPendingHoldCodes.Clear();
		this.cachedPendingHoldCodes.AddRange(this.pendingItemHolds.Keys);

		foreach(ulong code in this.cachedPendingHoldCodes){
			hold = this.pendingItemHolds[code];
			hold.elapsed += Time.deltaTime;

			if(IsPendingHoldReady(code, ref hold)){
				this.pendingItemHolds.Remove(code);
				ItemLoader.GetItem(hold.id).OnHoldClient(this.cl, new ItemStack(hold.id, hold.quantity), code);
			}
			else{
				this.pendingItemHolds[code] = hold;
			}
		}
	}

	private bool IsPendingHoldReady(ulong code, ref PendingItemHold hold){
		if(hold.elapsed >= SHEATHE_MAX_WAIT_TIME || !CanProcessPlayerAnimations(code))
			return true;

		float time = this.playerAnimations[code].GetAnimationTime(SHEATHE_STATE);

		if(time >= 0f)
			hold.sawSheathe = true;
		if(time >= 1f)
			return true;
		if(hold.sawSheathe)
			return time < 0f;

		return hold.elapsed >= SHEATHE_START_GRACE_TIME;
	}

	private AnimatorOverrideController ApplyOverrides(AnimatorOverrideController controller, StateClipPair[] overrides){
		foreach(StateClipPair over in overrides){
			controller[over.FetchStateClip()] = over.FetchFinalClip();
		}

		return controller;
	}
}
