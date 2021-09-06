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

    // Start is called before the first frame update
    void Start()
    {
    	ItemStack its = new ItemStack(ItemID.STONEBLOCK, 40);
    	inventory.AddStack(its, inventory.CanFit(its));
    	hotbar.AddStack(its, hotbar.CanFit(its));


        InventoryStaticMessage.SetPlayerInventory(inventory);
        InventoryStaticMessage.SetInventory(hotbar);
        invUIPlayer.OpenInventory();

        this.DrawHotbar();
    }


	// Selects a new item in hotbar
	public void Scroll1(){
		PlayerEvents.hotbarSlot = 0;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(0), 34);
	}
	public void Scroll2(){
		PlayerEvents.hotbarSlot = 1;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(1), 34);
	}
	public void Scroll3(){
		PlayerEvents.hotbarSlot = 2;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(2), 34);
	}
	public void Scroll4(){
		PlayerEvents.hotbarSlot = 3;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(3), 34);
	}
	public void Scroll5(){
		PlayerEvents.hotbarSlot = 4;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(4), 34);
	}
	public void Scroll6(){
		PlayerEvents.hotbarSlot = 5;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(5), 34);
	}
	public void Scroll7(){
		PlayerEvents.hotbarSlot = 6;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(6), 34);
	}
	public void Scroll8(){
		PlayerEvents.hotbarSlot = 7;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(7), 34);
	}
	public void Scroll9(){
		PlayerEvents.hotbarSlot = 8;
		this.hotbar_selected.anchoredPosition = new Vector2(GetSelectionX(8), 34);
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
	}
}
