using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHotbarHandler : MonoBehaviour
{
	// Unity Reference
	public ChunkLoader cl;
	private GameObject character;
	private GameObject handItem;

	// Inventory
	public Inventory hotbar = InventoryLoader.GetInventory(InventoryType.HOTBAR);

	// UI
	public Image[] hotbarIcon;
	public TextMeshProUGUI[] hotbarText;
	public RectTransform hotbar_selected;
	public PlayerInventoryManager playerInventoryManager;
	public Material itemIconMaterial;

	// Constant colors
	private readonly Color TRANSPARENT = new Color(1f, 1f, 1f, 0f);
	private readonly Color WHITE = new Color(1f, 1f, 1f, 1f);

	// Hotbar
	public static byte hotbarSlot = 0;
	private static ItemStack previousItem = null;

	private static bool STARTED = false;
	private static float HOTBAR_SELECTION_CHANGE_DOWNTIME = 0.08f;
	private static bool HOTBAR_SELECTED_VALID = false;
	private static float HOTBAR_SELECTION_TIME = 0f;
	private static byte LAST_HOTBAR_SENT = 9;



    // Start is called before the first frame update
    void Start()
    {
    	foreach(Image img in hotbarIcon){
    		img.material = Instantiate(this.itemIconMaterial);
    	}

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

    // Checks if the current ItemStack selected has a different item from the last and run
    public void RefreshItemEffects(){
    	ItemStack current = GetSlotStack();
    	if(previousItem != current){
    		// OnUnhold
			if(previousItem != null)
    			previousItem.GetItem().OnUnholdPlayer(this.cl, previousItem, Configurations.accountID);

    		// OnHold
    		if(current != null)
    			current.GetItem().OnHoldPlayer(this.cl, current, Configurations.accountID);

    		previousItem = current;
    	}
    }

    public void SetHotbar(Inventory hotbar){
    	this.hotbar = hotbar;
        this.DrawHotbar();
    }

	// Selects a new item in hotbar
	public void Scroll1(){
		if(PlayerHotbarHandler.hotbarSlot == 0)
			return;

		PlayerHotbarHandler.hotbarSlot = 0;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(0), 48);
		TriggerHotbarDelay();
	}
	public void Scroll2(){
		if(PlayerHotbarHandler.hotbarSlot == 1)
			return;

		PlayerHotbarHandler.hotbarSlot = 1;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(1), 48);
		
		TriggerHotbarDelay();
	}
	public void Scroll3(){
		if(PlayerHotbarHandler.hotbarSlot == 2)
			return;

		PlayerHotbarHandler.hotbarSlot = 2;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(2), 48);
		
		TriggerHotbarDelay();
	}
	public void Scroll4(){
		if(PlayerHotbarHandler.hotbarSlot == 3)
			return;

		PlayerHotbarHandler.hotbarSlot = 3;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(3), 48);
		
		TriggerHotbarDelay();
	}
	public void Scroll5(){
		if(PlayerHotbarHandler.hotbarSlot == 4)
			return;

		PlayerHotbarHandler.hotbarSlot = 4;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(4), 48);
		
		TriggerHotbarDelay();
	}
	public void Scroll6(){
		if(PlayerHotbarHandler.hotbarSlot == 5)
			return;

		PlayerHotbarHandler.hotbarSlot = 5;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(5), 48);
		
		TriggerHotbarDelay();
	}
	public void Scroll7(){
		if(PlayerHotbarHandler.hotbarSlot == 6)
			return;

		PlayerHotbarHandler.hotbarSlot = 6;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(6), 48);
		
		TriggerHotbarDelay();
	}
	public void Scroll8(){
		if(PlayerHotbarHandler.hotbarSlot == 7)
			return;

		PlayerHotbarHandler.hotbarSlot = 7;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(7), 48);
		
		TriggerHotbarDelay();
	}
	public void Scroll9(){
		if(PlayerHotbarHandler.hotbarSlot == 8)
			return;

		PlayerHotbarHandler.hotbarSlot = 8;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(8), 48);
		
		TriggerHotbarDelay();
	}
	public void MouseScroll(int val){
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
		
		TriggerHotbarDelay();
	}

	// Scrolls to a given slot. Only works once when receiving Player Character information to set the hotbar position
	public void ScrollToSlot(byte slot){
		if(!PlayerHotbarHandler.STARTED){
			PlayerHotbarHandler.hotbarSlot = slot;
			this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(slot), 48);
			
			SendHotbarInfoToServer();
			PlayerHotbarHandler.STARTED = true;
		}
	}

	private void TriggerHotbarDelay(){
		PlayerHotbarHandler.HOTBAR_SELECTION_TIME = 0f;
		PlayerHotbarHandler.HOTBAR_SELECTED_VALID = true;
	}

	// Returns the ItemStack selected in hotbar
	public ItemStack GetSlotStack(){
		return hotbar.GetSlot(PlayerHotbarHandler.hotbarSlot);
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
			hotbarIcon[slot].color = this.TRANSPARENT;		
			hotbarText[slot].text = "";
		}
		else{
			hotbarIcon[slot].material.SetTexture("_Texture", ItemLoader.GetSprite(its));
			hotbarIcon[slot].color = this.WHITE;

			if(its.GetStacksize() > 1)		
				hotbarText[slot].text = its.GetAmount().ToString();
		}
	}

	// Redraws the entire hotbar
	public void DrawHotbar(){
		for(byte i=0; i < hotbar.GetLimit(); i++)
			this.DrawHotbarSlot(i);
	}

	private void SendHotbarInfoToServer(){
		if(PlayerHotbarHandler.hotbarSlot == PlayerHotbarHandler.LAST_HOTBAR_SENT)
			return;

		NetMessage message = new NetMessage(NetCode.SENDHOTBARPOSITION);
		message.SendHotbarPosition(PlayerHotbarHandler.hotbarSlot);
		this.cl.client.Send(message);
		PlayerHotbarHandler.LAST_HOTBAR_SENT = PlayerHotbarHandler.hotbarSlot;
	}
}
