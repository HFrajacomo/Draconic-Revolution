using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerEvents : MonoBehaviour
{
	// Unity Reference
	public ChunkLoader cl;
	private GameObject character;
	private GameObject handItem;
	public ChunkRenderer iconRenderer;

	// Inventory
	public Inventory inventory = new Inventory(InventoryType.PLAYER);
	public Inventory hotbar = new Inventory(InventoryType.HOTBAR);
	public Image[] hotbarIcon;
	public TextMeshProUGUI[] hotbarText;
	public RectTransform hotbar_selected;
	public InventoryUIPlayer invUIPlayer;
	public Material itemIconMaterial;

	// Constant colors
	private readonly Color TRANSPARENT = new Color(1f, 1f, 1f, 0f);
	private readonly Color WHITE = new Color(1f, 1f, 1f, 1f);

	// Hotbar
	public static byte hotbarSlot = 0;
	public static ItemEntityHand itemInHand;
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

        InventoryStaticMessage.SetPlayerInventory(inventory);
        InventoryStaticMessage.SetInventory(hotbar);
        invUIPlayer.OpenInventory(inventory, hotbar);

        this.DrawHotbar();
    }

    void Update(){
    	if(PlayerEvents.HOTBAR_SELECTED_VALID){
    		PlayerEvents.HOTBAR_SELECTION_TIME += Time.deltaTime;

    		if(PlayerEvents.HOTBAR_SELECTION_TIME >= PlayerEvents.HOTBAR_SELECTION_CHANGE_DOWNTIME){
    			SendHotbarInfoToServer();
    			PlayerEvents.HOTBAR_SELECTED_VALID = false;
    			PlayerEvents.HOTBAR_SELECTION_TIME = 0f;
    		}
    	}
    }

    void OnDisable(){
    	PlayerEvents.STARTED = false;
		PlayerEvents.HOTBAR_SELECTED_VALID = false;
		PlayerEvents.HOTBAR_SELECTION_TIME = 0f;
    }

    public void SetInventories(Inventory inv, Inventory hotbar){
    	this.inventory = inv;
    	this.hotbar = hotbar;
        InventoryStaticMessage.SetPlayerInventory(inventory);
        InventoryStaticMessage.SetInventory(hotbar);
        invUIPlayer.OpenInventory(inventory, hotbar);

        this.DrawHotbar();
    }

	// Selects a new item in hotbar
	public void Scroll1(bool skipOnUnhold=false){
		if(PlayerEvents.hotbarSlot == 0)
			return;
		if(!skipOnUnhold)
			OnUnholdPlayer();

		PlayerEvents.hotbarSlot = 0;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(0), 48);
		DrawItemEntity(GetSlotStack());
		OnHoldPlayer();
		TriggerHotbarDelay();
	}
	public void Scroll2(bool skipOnUnhold=false){
		if(PlayerEvents.hotbarSlot == 1)
			return;
		if(!skipOnUnhold)
			OnUnholdPlayer();

		PlayerEvents.hotbarSlot = 1;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(1), 48);
		DrawItemEntity(GetSlotStack());
		OnHoldPlayer();
		TriggerHotbarDelay();
	}
	public void Scroll3(bool skipOnUnhold=false){
		if(PlayerEvents.hotbarSlot == 2)
			return;
		if(!skipOnUnhold)
			OnUnholdPlayer();

		PlayerEvents.hotbarSlot = 2;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(2), 48);
		DrawItemEntity(GetSlotStack());
		OnHoldPlayer();
		TriggerHotbarDelay();
	}
	public void Scroll4(bool skipOnUnhold=false){
		if(PlayerEvents.hotbarSlot == 3)
			return;
		if(!skipOnUnhold)
			OnUnholdPlayer();

		PlayerEvents.hotbarSlot = 3;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(3), 48);
		DrawItemEntity(GetSlotStack());
		OnHoldPlayer();
		TriggerHotbarDelay();
	}
	public void Scroll5(bool skipOnUnhold=false){
		if(PlayerEvents.hotbarSlot == 4)
			return;
		if(!skipOnUnhold)
			OnUnholdPlayer();

		PlayerEvents.hotbarSlot = 4;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(4), 48);
		DrawItemEntity(GetSlotStack());
		OnHoldPlayer();
		TriggerHotbarDelay();
	}
	public void Scroll6(bool skipOnUnhold=false){
		if(PlayerEvents.hotbarSlot == 5)
			return;
		if(!skipOnUnhold)
			OnUnholdPlayer();

		PlayerEvents.hotbarSlot = 5;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(5), 48);
		DrawItemEntity(GetSlotStack());
		OnHoldPlayer();
		TriggerHotbarDelay();
	}
	public void Scroll7(bool skipOnUnhold=false){
		if(PlayerEvents.hotbarSlot == 6)
			return;
		if(!skipOnUnhold)
			OnUnholdPlayer();

		PlayerEvents.hotbarSlot = 6;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(6), 48);
		DrawItemEntity(GetSlotStack());
		OnHoldPlayer();
		TriggerHotbarDelay();
	}
	public void Scroll8(bool skipOnUnhold=false){
		if(PlayerEvents.hotbarSlot == 7)
			return;
		if(!skipOnUnhold)
			OnUnholdPlayer();

		PlayerEvents.hotbarSlot = 7;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(7), 48);
		DrawItemEntity(GetSlotStack());
		OnHoldPlayer();
		TriggerHotbarDelay();
	}
	public void Scroll9(bool skipOnUnhold=false){
		if(PlayerEvents.hotbarSlot == 8)
			return;
		if(!skipOnUnhold)
			OnUnholdPlayer();

		PlayerEvents.hotbarSlot = 8;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(8), 48);
		DrawItemEntity(GetSlotStack());
		OnHoldPlayer();
		TriggerHotbarDelay();
	}
	public void MouseScroll(int val){
		if(val < 0){
			OnUnholdPlayer();
			if(PlayerEvents.hotbarSlot == 8)
				PlayerEvents.hotbarSlot = 0;
			else
				PlayerEvents.hotbarSlot++;
		}
		else if(val > 0){
			OnUnholdPlayer();
			if(PlayerEvents.hotbarSlot == 0)
				PlayerEvents.hotbarSlot = 8;
			else
				PlayerEvents.hotbarSlot--;
		}
		else
			return;

		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(PlayerEvents.hotbarSlot), 48);
		DrawItemEntity(GetSlotStack());
		OnHoldPlayer();
		TriggerHotbarDelay();
	}

	// Scrolls to a given slot. Only works once when receiving Player Character information to set the hotbar position
	public void ScrollToSlot(byte slot){
		if(!PlayerEvents.STARTED){
			PlayerEvents.hotbarSlot = slot;
			this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(slot), 48);
			DrawItemEntity(GetSlotStack());
			OnHoldPlayer();
			SendHotbarInfoToServer();
			PlayerEvents.STARTED = true;
		}
	}

	private void TriggerHotbarDelay(){
		PlayerEvents.HOTBAR_SELECTION_TIME = 0f;
		PlayerEvents.HOTBAR_SELECTED_VALID = true;
	}

	// Returns the ItemStack selected in hotbar
	public ItemStack GetSlotStack(){
		return hotbar.GetSlot(PlayerEvents.hotbarSlot);
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

		DrawItemEntity(GetSlotStack());
	}

	// Updates inventory in UI
	public void UpdateInventory(){
		invUIPlayer.OpenInventory(this.inventory, this.hotbar);
	}

	// Runs Item OnHoldPlayer behaviour
	private void OnHoldPlayer(){
		ItemStack its = GetSlotStack();

		if(its == null)
			return;

		its.GetItem().OnHoldPlayer(this.cl, its, Configurations.accountID);
	}

	// Runs Item OnUnholdPlayer behaviour
	private void OnUnholdPlayer(){
		ItemStack its = GetSlotStack();

		if(its == null)
			return;

		its.GetItem().OnUnholdPlayer(this.cl, its, Configurations.accountID);
	}

	public void DestroyItemEntity(){
		//PlayerEvents.itemInHand.Destroy();
		//PlayerEvents.itemInHand = null;	
	}

	// Updates ItemEntity in Player's hand
	public void DrawItemEntity(ItemStack its){
		if(this.character == null)
			return;

		// If had nothing and switched to nothing
		if(its == null && PlayerEvents.itemInHand == null)
			return;
		// If had something and switched to nothing
		if(its == null){
			DestroyItemEntity();
			return;
		}
		// If had nothing and switched to something
		if(PlayerEvents.itemInHand == null){
			//PlayerEvents.itemInHand = new ItemEntityHand(its.GetID(), its.GetIconID(), this.iconRenderer);
			//this.handItem = PlayerEvents.itemInHand.go;
			//this.handItem.name = "HandItem";
			//this.handItem.transform.parent = this.character.transform;
			//SetItemEntityPosition();
			return;
		}
		// If had item and switched to same
		if(its.GetID() == PlayerEvents.itemInHand.GetID())
			return;

		// Else if switched from something to something else
		//PlayerEvents.itemInHand.ChangeItem(its.GetItem());
	}

	public void SetPlayerObject(GameObject go){
		this.character = go;
		DrawItemEntity(GetSlotStack());
	}

	/*
	public void SetItemEntityPosition(){
		if(this.handItem == null)
			return;

		this.handItem.transform.localPosition = new Vector3(0.34f, 0.1f, 0.5f);
		this.handItem.transform.localEulerAngles = new Vector3(130f, 0f, 0f);
		this.handItem.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
	}
	*/

	private void SendHotbarInfoToServer(){
		if(PlayerEvents.hotbarSlot == PlayerEvents.LAST_HOTBAR_SENT)
			return;

		NetMessage message = new NetMessage(NetCode.SENDHOTBARPOSITION);
		message.SendHotbarPosition(PlayerEvents.hotbarSlot);
		this.cl.client.Send(message);
		PlayerEvents.LAST_HOTBAR_SENT = PlayerEvents.hotbarSlot;
	}
}
