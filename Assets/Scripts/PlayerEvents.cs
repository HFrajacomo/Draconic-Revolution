using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;

public class PlayerEvents : MonoBehaviour
{
	// Inventory
	public Inventory inventory = new Inventory(InventoryType.PLAYER);
	public Inventory hotbar = new Inventory(InventoryType.HOTBAR);
	public Image[] hotbarIcon;
	public Text[] hotbarText;
	public SpriteAtlas iconAtlas;
	public RectTransform hotbar_selected;
	public InventoryUIPlayer invUIPlayer;

	// Constant colors
	private readonly Color TRANSPARENT = new Color(1f, 1f, 1f, 0f);
	private readonly Color WHITE = new Color(1f, 1f, 1f, 1f);

	// Hotbar
	public static byte hotbarSlot = 0;
	public static ItemEntityHand itemInHand;

	// Unity Reference
	private GameObject character;
	private GameObject handItem;
	public ChunkRenderer iconRenderer;


    // Start is called before the first frame update
    void Start()
    {
    	/*
    	ItemStack its = new ItemStack(ItemID.STONEBLOCK, 40);
    	ItemStack its2 = new ItemStack(ItemID.WOODBLOCK, 40);
    	ItemStack its3 = new ItemStack(ItemID.TORCH, 50);
    	inventory.AddStack(its, inventory.CanFit(its));
    	hotbar.AddStack(its, hotbar.CanFit(its));
       	hotbar.AddStack(its2, hotbar.CanFit(its2));
       	hotbar.AddStack(its3, hotbar.CanFit(its3));
       	*/
    	this.Scroll1();


        InventoryStaticMessage.SetPlayerInventory(inventory);
        InventoryStaticMessage.SetInventory(hotbar);
        invUIPlayer.OpenInventory(inventory, hotbar);

        this.DrawHotbar();
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
	public void Scroll1(){
		PlayerEvents.hotbarSlot = 0;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(-1), 34);
		DrawItemEntity(GetSlotStack());
	}
	public void Scroll2(){
		PlayerEvents.hotbarSlot = 1;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(0), 34);
		DrawItemEntity(GetSlotStack());
	}
	public void Scroll3(){
		PlayerEvents.hotbarSlot = 2;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(1), 34);
		DrawItemEntity(GetSlotStack());
	}
	public void Scroll4(){
		PlayerEvents.hotbarSlot = 3;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(2), 34);
		DrawItemEntity(GetSlotStack());
	}
	public void Scroll5(){
		PlayerEvents.hotbarSlot = 4;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(3), 34);
		DrawItemEntity(GetSlotStack());
	}
	public void Scroll6(){
		PlayerEvents.hotbarSlot = 5;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(4), 34);
		DrawItemEntity(GetSlotStack());
	}
	public void Scroll7(){
		PlayerEvents.hotbarSlot = 6;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(5), 34);
		DrawItemEntity(GetSlotStack());
	}
	public void Scroll8(){
		PlayerEvents.hotbarSlot = 7;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(6), 34);
		DrawItemEntity(GetSlotStack());
	}
	public void Scroll9(){
		PlayerEvents.hotbarSlot = 8;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(7), 34);
		DrawItemEntity(GetSlotStack());
	}
	public void MouseScroll(int val){
		if(val < 0){
			if(PlayerEvents.hotbarSlot == 8)
				PlayerEvents.hotbarSlot = 0;
			else
				PlayerEvents.hotbarSlot++;
		}
		else if(val > 0){
			if(PlayerEvents.hotbarSlot == 0)
				PlayerEvents.hotbarSlot = 8;
			else
				PlayerEvents.hotbarSlot--;
		}
		else
			return;

		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(PlayerEvents.hotbarSlot-1), 34);
		DrawItemEntity(GetSlotStack());
	}

	// Returns the ItemStack selected in hotbar
	public ItemStack GetSlotStack(){
		return hotbar.GetSlot(PlayerEvents.hotbarSlot);
	}

	// Calculates correct X position for the selected hotbar spot
	public int GetSelectionX(int pos){
		return 78*pos-234;
	}

	// Draws a hotbar slot
	public void DrawHotbarSlot(byte slot){
		ItemStack its = hotbar.GetSlot(slot);

		if(its == null){
			hotbarIcon[slot].sprite = null;
			hotbarIcon[slot].color = this.TRANSPARENT;		
			hotbarText[slot].text = "";
		}
		else{
			Debug.Log(its.GetItemIconName());
			hotbarIcon[slot].sprite = iconAtlas.GetSprite(its.GetItemIconName());
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

	public void DestroyItemEntity(){
		PlayerEvents.itemInHand.Destroy();
		PlayerEvents.itemInHand = null;	
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
			PlayerEvents.itemInHand = new ItemEntityHand(its.GetID(), its.GetIconID(), this.iconRenderer);
			this.handItem = PlayerEvents.itemInHand.go;
			this.handItem.name = "HandItem";
			this.handItem.transform.parent = this.character.transform;
			SetItemEntityPosition();
			return;
		}
		// If had item and switched to same
		if(its.GetIconID() == PlayerEvents.itemInHand.iconID)
			return;

		// Else if switched from something to something else
		PlayerEvents.itemInHand.ChangeItem(its.GetItem());
	}

	public void SetPlayerObject(GameObject go){
		this.character = go;
		DrawItemEntity(GetSlotStack());
	}

	public void SetItemEntityPosition(){
		if(this.handItem == null)
			return;

		this.handItem.transform.localPosition = new Vector3(0.34f, 0.1f, 0.5f);
		this.handItem.transform.localEulerAngles = new Vector3(130f, 0f, 0f);
		this.handItem.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
	}
}
