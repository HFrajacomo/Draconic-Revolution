using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHotbarHandler : MonoBehaviour
{
	// Unity Reference
	public ChunkLoader cl;

	// Animation
	public Animator animator;
	public static bool IS_SWITCHING = false;
	public static bool IS_NORMAL_HOTBAR = true;
	private float animationTime = 0.25f;

	// Inventory
	public Inventory hotbar = InventoryLoader.GetInventory("HOTBAR");
	public ActionInventory actionHotbar = InventoryLoader.GetActionInventory("ACTION_HOTBAR");

	// UI
	public Image hotbarImage;
	public Image attackHotbarImage;
	public Image[] hotbarIcon;
	public Image[] attackHotbarIcon;
	public TextMeshProUGUI[] hotbarText;
	public TextMeshProUGUI[] attackHotbarText;
	public RectTransform hotbar_selected;
	public RectTransform attackHotbar_selected;
	public PlayerInventoryManager playerInventoryManager;

	public Material hotbarMaterial;
	public Material actionMaterial;

	// Constant colors
	private readonly Color HIDDEN = new Color(.7f, .7f, .7f, .7f);
	private readonly Color TRANSPARENT = new Color(1f, 1f, 1f, 0f);
	private readonly Color WHITE = new Color(1f, 1f, 1f, 1f);

	// Hotbar
	public static byte hotbarSlot = 0;
	public static byte attackHotbarSlot = 0;
	private static ClickableSlot previousSlot = null;

	private static bool STARTED = false;
	private static float HOTBAR_SELECTION_CHANGE_DOWNTIME = 0.08f;
	private static bool HOTBAR_SELECTED_VALID = false;
	private static float HOTBAR_SELECTION_TIME = 0f;
	private static byte LAST_HOTBAR_SENT = 9;
	private static byte LAST_ATTACK_HOTBAR_SENT = 9;



    // Start is called before the first frame update
    void Start()
    {
    	IS_SWITCHING = false;
    	IS_NORMAL_HOTBAR = true;

    	foreach(Image img in hotbarIcon){
    		img.material = Instantiate(this.hotbarMaterial);
    	}

    	foreach(Image img in attackHotbarIcon){
    		img.material = Instantiate(this.actionMaterial);
    	}

    	InitiateHotbar();

        this.DrawHotbar();
    }

    void Update(){
    	RefreshItemEffects();

    	if(PlayerHotbarHandler.HOTBAR_SELECTED_VALID){
    		PlayerHotbarHandler.HOTBAR_SELECTION_TIME += Time.deltaTime;

    		if(PlayerHotbarHandler.HOTBAR_SELECTION_TIME >= PlayerHotbarHandler.HOTBAR_SELECTION_CHANGE_DOWNTIME){
    			SendHotbarInfoToServer();
    			PlayerHotbarHandler.HOTBAR_SELECTED_VALID = false;
    			PlayerHotbarHandler.HOTBAR_SELECTION_TIME = 0f;
    		}
    	}
    }

    void OnDisable(){
    	PlayerHotbarHandler.STARTED = false;
		PlayerHotbarHandler.HOTBAR_SELECTED_VALID = false;
		PlayerHotbarHandler.HOTBAR_SELECTION_TIME = 0f;
    }

    public void SwitchHotbars(){
    	StartCoroutine(SwitchCoroutine());
    }
    private IEnumerator SwitchCoroutine(){
    	IS_NORMAL_HOTBAR = !IS_NORMAL_HOTBAR;
    	IS_SWITCHING = true;

    	if(IS_NORMAL_HOTBAR)
    		this.animator.Play("HotbarSwitch-AttackToNormal");
    	else
    		this.animator.Play("HotbarSwitch-NormalToAttack");

		yield return new WaitForSeconds(this.animationTime/2);

		if(IS_NORMAL_HOTBAR){
			SetHotbarParameterToHidden(false);
		}
		else{
			SetHotbarParameterToHidden(true);
		}

		yield return new WaitForSeconds(this.animationTime/2);

		IS_SWITCHING = false;
    }

    // Checks if the current ItemStack selected has a different item from the last and run
    public void RefreshItemEffects(){
    	if(PlayerHotbarHandler.IS_NORMAL_HOTBAR){
	    	ClickableSlot current = GetSlotStack();
	    	if(!ClickableSlot.IsEqual(current, previousSlot)){
	    		// OnUnhold
				if(previousSlot != null && previousSlot.IsItemStack())
	    			((ItemStack)previousSlot).GetItem().OnUnholdPlayer(this.cl, (ItemStack)previousSlot, Configurations.accountID);
	    		else if(previousSlot != null)
	    			((EntityAction)previousSlot).OnUnholdPlayer(this.cl, ((EntityAction)previousSlot).GetItemStack(playerInventoryManager), Configurations.accountID);

	    		// OnHold
	    		if(current != null && current.IsItemStack())
	    			((ItemStack)current).GetItem().OnHoldPlayer(this.cl, (ItemStack)current, Configurations.accountID);
	    		else if(current != null)
	    			((EntityAction)current).OnHoldPlayer(this.cl, ((EntityAction)current).GetItemStack(playerInventoryManager), Configurations.accountID);

	    		previousSlot = current;
	    	}
	    }
    }

    public void SetHotbar(Inventory hotbar){
    	this.hotbar = hotbar;
        this.DrawHotbar();
    }

    public void SetActionHotbar(ActionInventory hotbar){
    	this.actionHotbar = hotbar;
    	this.DrawHotbar();
    }

	// Selects a new item in hotbar
	public void Scroll1(){
		if(PlayerHotbarHandler.IS_SWITCHING)
			return;
		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.hotbarSlot == 0)
			return;
		if(!PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.attackHotbarSlot == 0)
			return;

		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR){
			PlayerHotbarHandler.hotbarSlot = 0;
			this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(0), 48);
		}
		else{
			PlayerHotbarHandler.attackHotbarSlot = 0;
			this.attackHotbar_selected.anchoredPosition = new Vector2(GetSelectionX(0), 48);			
		}

		TriggerHotbarDelay();
	}
	public void Scroll2(){
		if(PlayerHotbarHandler.IS_SWITCHING)
			return;
		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.hotbarSlot == 1)
			return;
		if(!PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.attackHotbarSlot == 1)
			return;

		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR){
			PlayerHotbarHandler.hotbarSlot = 1;
			this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(1), 48);
		}
		else{
			PlayerHotbarHandler.attackHotbarSlot = 1;
			this.attackHotbar_selected.anchoredPosition = new Vector2(GetSelectionX(1), 48);			
		}
		
		TriggerHotbarDelay();
	}
	public void Scroll3(){
		if(PlayerHotbarHandler.IS_SWITCHING)
			return;
		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.hotbarSlot == 2)
			return;
		if(!PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.attackHotbarSlot == 2)
			return;

		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR){
			PlayerHotbarHandler.hotbarSlot = 2;
			this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(2), 48);
		}
		else{
			PlayerHotbarHandler.attackHotbarSlot = 2;
			this.attackHotbar_selected.anchoredPosition = new Vector2(GetSelectionX(2), 48);			
		}
		
		TriggerHotbarDelay();
	}
	public void Scroll4(){
		if(PlayerHotbarHandler.IS_SWITCHING)
			return;
		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.hotbarSlot == 3)
			return;
		if(!PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.attackHotbarSlot == 3)
			return;

		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR){
			PlayerHotbarHandler.hotbarSlot = 3;
			this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(3), 48);
		}
		else{
			PlayerHotbarHandler.attackHotbarSlot = 3;
			this.attackHotbar_selected.anchoredPosition = new Vector2(GetSelectionX(3), 48);			
		}
		
		TriggerHotbarDelay();
	}
	public void Scroll5(){
		if(PlayerHotbarHandler.IS_SWITCHING)
			return;
		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.hotbarSlot == 4)
			return;
		if(!PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.attackHotbarSlot == 4)
			return;

		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR){
			PlayerHotbarHandler.hotbarSlot = 4;
			this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(4), 48);
		}
		else{
			PlayerHotbarHandler.attackHotbarSlot = 4;
			this.attackHotbar_selected.anchoredPosition = new Vector2(GetSelectionX(4), 48);			
		}
		
		TriggerHotbarDelay();
	}
	public void Scroll6(){
		if(PlayerHotbarHandler.IS_SWITCHING)
			return;
		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.hotbarSlot == 5)
			return;
		if(!PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.attackHotbarSlot == 5)
			return;

		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR){
			PlayerHotbarHandler.hotbarSlot = 5;
			this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(5), 48);
		}
		else{
			PlayerHotbarHandler.attackHotbarSlot = 5;
			this.attackHotbar_selected.anchoredPosition = new Vector2(GetSelectionX(5), 48);			
		}
		
		TriggerHotbarDelay();
	}
	public void Scroll7(){
		if(PlayerHotbarHandler.IS_SWITCHING)
			return;
		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.hotbarSlot == 6)
			return;
		if(!PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.attackHotbarSlot == 6)
			return;

		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR){
			PlayerHotbarHandler.hotbarSlot = 6;
			this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(6), 48);
		}
		else{
			PlayerHotbarHandler.attackHotbarSlot = 6;
			this.attackHotbar_selected.anchoredPosition = new Vector2(GetSelectionX(6), 48);			
		}
		
		TriggerHotbarDelay();
	}
	public void Scroll8(){
		if(PlayerHotbarHandler.IS_SWITCHING)
			return;
		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.hotbarSlot == 7)
			return;
		if(!PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.attackHotbarSlot == 7)
			return;

		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR){
			PlayerHotbarHandler.hotbarSlot = 7;
			this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(7), 48);
		}
		else{
			PlayerHotbarHandler.attackHotbarSlot = 7;
			this.attackHotbar_selected.anchoredPosition = new Vector2(GetSelectionX(7), 48);			
		}
		
		TriggerHotbarDelay();
	}
	public void Scroll9(){
		if(PlayerHotbarHandler.IS_SWITCHING)
			return;
		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.hotbarSlot == 8)
			return;
		if(!PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.attackHotbarSlot == 8)
			return;

		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR){
			PlayerHotbarHandler.hotbarSlot = 8;
			this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(8), 48);
		}
		else{
			PlayerHotbarHandler.attackHotbarSlot = 8;
			this.attackHotbar_selected.anchoredPosition = new Vector2(GetSelectionX(8), 48);		
		}
		
		TriggerHotbarDelay();
	}
	public void MouseScroll(int val){
		if(PlayerHotbarHandler.IS_SWITCHING)
			return;

		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR){
			if(val < 0){
				if(PlayerHotbarHandler.hotbarSlot == 8)
					PlayerHotbarHandler.hotbarSlot = 0;
				else
					PlayerHotbarHandler.hotbarSlot++;
			}
			else if(val > 0){
				if(PlayerHotbarHandler.hotbarSlot == 0)
					PlayerHotbarHandler.hotbarSlot = 8;
				else
					PlayerHotbarHandler.hotbarSlot--;
			}
			else
				return;

			this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(PlayerHotbarHandler.hotbarSlot), 48);
		}
		else{
			if(val < 0){
				if(PlayerHotbarHandler.attackHotbarSlot == 8)
					PlayerHotbarHandler.attackHotbarSlot = 0;
				else
					PlayerHotbarHandler.attackHotbarSlot++;
			}
			else if(val > 0){
				if(PlayerHotbarHandler.attackHotbarSlot == 0)
					PlayerHotbarHandler.attackHotbarSlot = 8;
				else
					PlayerHotbarHandler.attackHotbarSlot--;
			}
			else
				return;

			this.attackHotbar_selected.anchoredPosition = new Vector2(GetSelectionX(PlayerHotbarHandler.attackHotbarSlot), 48);
		}
		
		TriggerHotbarDelay();
	}

	// Scrolls to a given slot. Only works once when receiving Player Character information to set the hotbar position
	public void ScrollToNormalSlot(byte slot){
		if(!PlayerHotbarHandler.STARTED){
			PlayerHotbarHandler.hotbarSlot = slot;
			this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(slot), 48);
			
			SendHotbarInfoToServer();
			PlayerHotbarHandler.STARTED = true;
		}
	}

	public void ScrollToActionSlot(byte slot){
		if(!PlayerHotbarHandler.STARTED){
			PlayerHotbarHandler.attackHotbarSlot = slot;
			this.attackHotbar_selected.anchoredPosition = new Vector2(GetSelectionX(slot), 48);
			
			SendHotbarInfoToServer();
			PlayerHotbarHandler.STARTED = true;
		}
	}

	private void TriggerHotbarDelay(){
		PlayerHotbarHandler.HOTBAR_SELECTION_TIME = 0f;
		PlayerHotbarHandler.HOTBAR_SELECTED_VALID = true;
	}

	// Returns the ItemStack selected in hotbar
	public ClickableSlot GetSlotStack(){
		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR){
			if(PlayerHotbarHandler.hotbarSlot == 9)
				return null;

			return (ItemStack)hotbar.GetSlot(PlayerHotbarHandler.hotbarSlot);
		}
		else{
			if(PlayerHotbarHandler.attackHotbarSlot == 9){

				return null;
			}

			return (EntityAction)actionHotbar.GetPos(PlayerHotbarHandler.attackHotbarSlot);
		}
		
	}

	// Calculates correct X position for the selected hotbar spot
	public int GetSelectionX(int pos){
		return 107*pos-428;
	}

	// Draws a hotbar slot
	public void DrawHotbarSlot(byte slot){
		ItemStack its = hotbar.GetSlot(slot);

		if(its == null){
			hotbarIcon[slot].material.SetTexture("_Texture", null);
			hotbarIcon[slot].color = TRANSPARENT;
			hotbarText[slot].text = "";
		}
		else{
			hotbarIcon[slot].material.SetTexture("_Texture", ItemLoader.GetSprite(its));
			hotbarIcon[slot].color = WHITE;

			if(its.GetStacksize() > 1)		
				hotbarText[slot].text = its.GetAmount().ToString();
			else
				hotbarText[slot].text = "";
		}
	}

	// Draws an action hotbar slot
	public void DrawActionSlot(byte slot){
		EntityAction ea = actionHotbar.GetPos(slot);
		Texture2D icon, underlay;
		string text;

		if(ea == null){
			attackHotbarIcon[slot].color = TRANSPARENT;
			attackHotbarIcon[slot].material.SetTexture("_ItemIcon", null);
			attackHotbarIcon[slot].material.SetTexture("_Underlay", null);
			attackHotbarText[slot].text = "";
		}
		else{
    		ea.OnIconDraw(this.cl, ea.GetItemStack(playerInventoryManager), out underlay, out icon);
    		text = ea.OnStackDraw(this.cl, ea.GetItemStack(playerInventoryManager));
    		attackHotbarIcon[slot].color = WHITE;
    		attackHotbarIcon[slot].material.SetTexture("_ItemIcon", icon);
    		attackHotbarIcon[slot].material.SetTexture("_Underlay", underlay);
    		attackHotbarText[slot].text = text;
		}
	}

	// Redraws the entire hotbar
	public void DrawHotbar(){
		for(byte i=0; i < hotbar.GetLimit(); i++){
			this.DrawHotbarSlot(i);
			this.DrawActionSlot(i);
		}
	}

	// Redraws the Action hotbar
	public void DrawActionHotbar(){
		for(byte i=0; i < actionHotbar.GetLimit(); i++){
			this.DrawActionSlot(i);
		}
	}

	private void SendHotbarInfoToServer(){
		if(PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.hotbarSlot == PlayerHotbarHandler.LAST_HOTBAR_SENT)
			return;
		if(!PlayerHotbarHandler.IS_NORMAL_HOTBAR && PlayerHotbarHandler.attackHotbarSlot == PlayerHotbarHandler.LAST_ATTACK_HOTBAR_SENT)
			return;

		NetMessage message = new NetMessage(NetCode.SENDHOTBARPOSITION);
		message.SendHotbarPosition(PlayerHotbarHandler.IS_NORMAL_HOTBAR, (byte)(PlayerHotbarHandler.hotbarSlot + InventoryLoader.GetInventorySize("ACTION_HOTBAR")), PlayerHotbarHandler.attackHotbarSlot);
		this.cl.client.Send(message);
		PlayerHotbarHandler.LAST_HOTBAR_SENT = PlayerHotbarHandler.hotbarSlot;
	}

	private void InitiateHotbar(){
		Texture2D texNormal = Resources.Load<Texture2D>("UI/hotbar");
		Texture2D texAttack = Resources.Load<Texture2D>("UI/attack_hotbar");

		if(texNormal == null || texAttack == null)
			Debug.LogWarning("[PlayerHotbarHandler] Failed to load hotbar texture");

		this.hotbarImage.material = Instantiate(this.hotbarMaterial);
		this.hotbarImage.material.SetTexture("_Texture", texNormal);
		
		this.attackHotbarImage.material = Instantiate(this.hotbarMaterial);
		this.attackHotbarImage.material.SetTexture("_Texture", texAttack);

		SetHotbarParameterToHidden(false);
	}

	private void SetHotbarParameterToHidden(bool normalHotbar){
		if(normalHotbar){
			this.hotbarImage.material.SetFloat("_Hidden", 1f);
			this.attackHotbarImage.material.SetFloat("_Hidden", 0f);

			for(int i=0; i < this.hotbarIcon.Length; i++){
				this.hotbarIcon[i].material.SetFloat("_Hidden", 1f);
				this.hotbarText[i].color = HIDDEN;
				this.attackHotbarIcon[i].material.SetFloat("_Hidden", 0f);
				this.attackHotbarText[i].color = WHITE;
			}
		}
		else{
			this.hotbarImage.material.SetFloat("_Hidden", 0f);
			this.attackHotbarImage.material.SetFloat("_Hidden", 1f);

			for(int i=0; i < this.hotbarIcon.Length; i++){
				this.hotbarIcon[i].material.SetFloat("_Hidden", 0f);
				this.hotbarText[i].color = WHITE;
				this.attackHotbarIcon[i].material.SetFloat("_Hidden", 1f);
				this.attackHotbarText[i].color = HIDDEN;
			}
		}
	}
}
