using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainControllerManager : MonoBehaviour {
	// Public passed variables
	public float movementX = 0f;
	public float movementZ = 0f;
	public bool jumping = false;
	public float mouseX = 0f;
	public float mouseY = 0f;

    public bool gravityHack = false;
    public bool freecam = false;
    private bool HUDActive = true;

    // Static
    public static bool shifting = false;
    public static bool inventory = false;
    public static bool ctrl = false;

    // Player State
    public static bool InUI = false;

    // Unity Reference
    public GameObject mainHUD;
    public PlayerHotbarHandler hotbarHandler;
    public GameObject inventoryGUI;
    public PlayerInventoryManager playerInventoryManager;
    public GameObject hotbar;
    public PlayerRaycast raycast;
    public Transform playerCamera;
    public ChunkLoader cl;
    public PlayerMovement playerMovement;
    public PlayerActionController playerActionController;


    // Locks
    private bool LOCK_MOUSE1 = false;
    private bool LOCK_MOUSE2 = false;
    private bool LOCK_INTERACT = false;
    private bool LOCK_DROP = false;
    public static bool DEBUG = false;
    private static readonly byte EQUIPMENT_INVENTORY_CODE = 3; // Position of the equipment in PlayerInventoryManager's inventories

    public void Update(){
        LOCK_DROP = false;
        LOCK_INTERACT = false;
        LOCK_MOUSE2 = false;
        LOCK_MOUSE1 = false;
    }

	// Jumping
    public void OnJump(){
    	// If it's press
    	if(!jumping){
       		jumping = true;
    	}
    	// If it's release
		else
			jumping = false;
    }

    // Mouse Camera Look
    public void OnMouseLook(InputValue val){
        mouseX = val.Get<Vector2>().x;
        mouseY = val.Get<Vector2>().y;
    }

    public void OnMovement(InputValue val){
        movementX = val.Get<Vector2>().x;
        movementZ = val.Get<Vector2>().y;
    }

    public void OnPrimaryAction(){
    	if(!MainControllerManager.InUI && !LOCK_MOUSE1){
            playerActionController.RegisterPrimaryAction();
            raycast.BreakBlock();
            LOCK_MOUSE1 = true;
        }
    }

    public void OnSecondaryAction(){
    	if(!MainControllerManager.InUI && !LOCK_MOUSE2){
            raycast.UseItem();
            LOCK_MOUSE2 = true;
        }
    }

    public void OnSheathe(){
        if(!MainControllerManager.InUI){
            if(!this.playerActionController.HasRestriction(PlayerActionRestriction.SHEATHE) && !this.playerActionController.HasRestriction(PlayerActionRestriction.STUNNED) && !PlayerHotbarHandler.IS_SWITCHING){
                this.hotbarHandler.SwitchHotbars();
            }
        }
    }

    public void OnInteract(){
        if(!MainControllerManager.InUI && !LOCK_INTERACT){
            raycast.Interact();
            LOCK_INTERACT = true;
        }
    }

    public void OnToggleGravity(){
        gravityHack = !gravityHack;
    }

    public void OnPrefabRead(){
        raycast.PrefabRead(true);
    }

    public void OnPrefabReadAir(){
        raycast.PrefabRead(false);
    }

    public void OnToggleFreeCam(){
        freecam = !freecam;

        if(freecam)
            this.playerMovement.ChangeMoveset(Moveset.FREECAM);
        else
            this.playerMovement.ChangeMoveset(Moveset.NORMAL);
    }

    public void OnShifting(){
        if(!MainControllerManager.shifting){
            MainControllerManager.shifting = true;
        }
        // If it's release
        else
            MainControllerManager.shifting = false;       
    }

    public void OnDebugKey(){
        if(World.isClient){
            this.raycast.TakeWorldScreenshot();
        }

        this.cl.Cleanup();
        DebugCube.Clear();
        SceneManager.LoadScene("Blank");
    }

    // Numpad 1: replaces the player inventory with a test kit
    public void OnDebugKey2(){
        GiveTestInventory();
    }

    /*
    DEBUG: replaces the player inventory with a test kit (weapons, tools, torches, stone and chests in the hotbar)
    The new inventory is sent to the Server like any other inventory change
    */
    public void GiveTestInventory(){
        if(MainControllerManager.InUI)
            return;

        byte actionHotbarID = InventoryLoader.GetActionInventoryID("ACTION_HOTBAR");
        byte hotbarID = InventoryLoader.GetInventoryID("HOTBAR");
        byte playerID = InventoryLoader.GetInventoryID("PLAYER");
        byte equipmentID = InventoryLoader.GetInventoryID("EQUIPMENT");
        byte[] buffer = new byte[4096];
        List<ItemStack> previousEquipment = new List<ItemStack>();
        int length = 0;

        for(ushort i=0; i < InventoryLoader.GetInventorySize(equipmentID); i++)
            previousEquipment.Add(this.playerInventoryManager.GetItemStack(EQUIPMENT_INVENTORY_CODE, i));

        // Action Hotbar
        buffer[length++] = actionHotbarID;
        length += new ActionInventorySlot(ActionLoader.GetID("BASE_EquipmentAttack"), false, 0, 0, 0, 0, actionHotbarID, 0).SaveToBuffer(buffer, length);

        for(int i=1; i < InventoryLoader.GetInventorySize(actionHotbarID); i++)
            length += EmptyPlayerInventorySlot.WriteBlank(buffer, length);

        // Hotbar
        buffer[length++] = hotbarID;
        length += EmptyPlayerInventorySlot.WriteBlank(buffer, length);
        length += new WeaponPlayerInventorySlot(ItemLoader.GetID("BASE_Bastard_Sword"), 1000, 0, EnchantmentType.NONE, hotbarID, 1).SaveToBuffer(buffer, length);
        length += new WeaponPlayerInventorySlot(ItemLoader.GetID("BASE_Pickaxe"), 1000, 0, EnchantmentType.NONE, hotbarID, 2).SaveToBuffer(buffer, length);
        length += new SubWeaponPlayerInventorySlot(ItemLoader.GetID("BASE_Torch"), 0, 50, hotbarID, 3).SaveToBuffer(buffer, length);
        length += new ItemPlayerInventorySlot(ItemLoader.GetID("BASE_Stone"), 50, hotbarID, 4).SaveToBuffer(buffer, length);
        length += new ItemPlayerInventorySlot(ItemLoader.GetID("BASE_Chest"), 50, hotbarID, 5).SaveToBuffer(buffer, length);

        for(int i=6; i < InventoryLoader.GetInventorySize(hotbarID); i++)
            length += EmptyPlayerInventorySlot.WriteBlank(buffer, length);

        // Bag
        buffer[length++] = playerID;

        for(int i=0; i < InventoryLoader.GetInventorySize(playerID); i++)
            length += EmptyPlayerInventorySlot.WriteBlank(buffer, length);

        // Equipment
        buffer[length++] = equipmentID;

        for(int i=0; i < InventoryLoader.GetInventorySize(equipmentID); i++)
            length += EmptyPlayerInventorySlot.WriteBlank(buffer, length);

        // Applied like an inventory received from the Server
        this.playerInventoryManager.LoadFromBytes(buffer, 0, length);
        this.hotbarHandler.SetHotbar(this.playerInventoryManager.GetMainInventory());
        this.hotbarHandler.SetActionHotbar(this.playerInventoryManager.GetActionInventory());
        this.hotbarHandler.DrawActionHotbar();

        this.playerInventoryManager.SendInventoryDataToServer();

        for(int i=0; i < previousEquipment.Count; i++){
            if(previousEquipment[i] != null)
                this.playerInventoryManager.SendEquipDataToServer(EQUIPMENT_INVENTORY_CODE, previousEquipment[i], null);
        }
    }

    public void OnToggleHUD(){
        if(MainControllerManager.InUI)
            return;

        this.HUDActive = !this.HUDActive;
        this.mainHUD.SetActive(this.HUDActive);
    }

    public void OnScroll1(){
        if(MainControllerManager.InUI)
            return;
        hotbarHandler.Scroll1();
    }
    public void OnScroll2(){
        if(MainControllerManager.InUI)
            return;
        hotbarHandler.Scroll2();
    }
    public void OnScroll3(){
        if(MainControllerManager.InUI)
            return;
        hotbarHandler.Scroll3();
    }
    public void OnScroll4(){
        if(MainControllerManager.InUI)
            return;
        hotbarHandler.Scroll4();
    }
    public void OnScroll5(){
        if(MainControllerManager.InUI)
            return;
        hotbarHandler.Scroll5();
    }
    public void OnScroll6(){
        if(MainControllerManager.InUI)
            return;
        hotbarHandler.Scroll6();
    }
    public void OnScroll7(){
        if(MainControllerManager.InUI)
            return;
        hotbarHandler.Scroll7();
    }
    public void OnScroll8(){
        if(MainControllerManager.InUI)
            return;
        hotbarHandler.Scroll8();
    }
    public void OnScroll9(){
        if(MainControllerManager.InUI)
            return;
        hotbarHandler.Scroll9();
    }
    public void OnMouseScroll(InputValue val){
        if(MainControllerManager.InUI)
            return;
        hotbarHandler.MouseScroll((int)val.Get<Vector2>().y);
    }
    public void OnOpenInventory(){
        bool newState = !MainControllerManager.InUI;

        // Closing the inventory also closes the container being looked at
        if(!newState && this.playerInventoryManager.containerManager != null)
            this.playerInventoryManager.containerManager.Close(notifyServer:true);

        this.inventoryGUI.SetActive(newState);
        MainControllerManager.InUI = newState;

        if(newState){
            playerInventoryManager.ReloadInventory();
            this.HUDActive = false;
            this.mainHUD.SetActive(this.HUDActive);
        }

        this.playerInventoryManager.ResetSelection();

        MouseLook.ToggleMouseCursor(newState);

        // If closing, refresh the hotbar
        if(newState == false){
            this.HUDActive = true;
            this.mainHUD.SetActive(this.HUDActive);
            hotbarHandler.DrawHotbar();
        }
    }
    // ESC closes the inventory and the container being looked at, if they're open
    public void OnCloseUI(){
        if(!MainControllerManager.InUI)
            return;

        // Closing the inventory also closes the container being looked at
        if(this.playerInventoryManager.containerManager != null)
            this.playerInventoryManager.containerManager.Close(notifyServer:true);

        this.inventoryGUI.SetActive(false);
        MainControllerManager.InUI = false;

        this.playerInventoryManager.ResetSelection();

        MouseLook.ToggleMouseCursor(false);

        this.HUDActive = true;
        this.mainHUD.SetActive(this.HUDActive);
        hotbarHandler.DrawHotbar();
    }

    // Opens the inventory with the container panel, after the Server sent the contents of a container the player interacted with
    // If the inventory was already open, the held stack is kept
    public void OpenContainer(){
        if(!MainControllerManager.InUI){
            this.inventoryGUI.SetActive(true);
            MainControllerManager.InUI = true;
            this.playerInventoryManager.ReloadInventory();
            this.playerInventoryManager.ResetSelection();
            MouseLook.ToggleMouseCursor(true);

            this.HUDActive = false;
            this.mainHUD.SetActive(this.HUDActive);
        }

        this.playerInventoryManager.SetContainerMode(true);
    }

    public void OnCtrl(){
        if(!MainControllerManager.ctrl){
            MainControllerManager.ctrl = true;
        }
        // If it's release
        else
            MainControllerManager.ctrl = false;    
    }
    public void OnDrop(){
        if(LOCK_DROP || MainControllerManager.InUI)
            return;

        if(!PlayerHotbarHandler.IS_NORMAL_HOTBAR || PlayerHotbarHandler.IS_SWITCHING)
            return;

        LOCK_DROP = true;

        if(hotbarHandler.hotbar.GetSlot(PlayerHotbarHandler.hotbarSlot) == null)
            return;

        ushort id = hotbarHandler.hotbar.GetSlot(PlayerHotbarHandler.hotbarSlot).GetID();
        ItemStack its;
        byte amount;

        if(!MainControllerManager.ctrl){
            amount = 1;
            playerInventoryManager.SubToCounter(hotbarHandler.hotbar.GetSlot(PlayerHotbarHandler.hotbarSlot).GetID(), amount);

            if(hotbarHandler.hotbar.GetSlot(PlayerHotbarHandler.hotbarSlot).Decrement()){
                playerInventoryManager.SetNull(1, PlayerHotbarHandler.hotbarSlot);
            }
            else{
                hotbarHandler.DrawHotbarSlot(PlayerHotbarHandler.hotbarSlot);
            }

            its = new ItemStack(id, amount);
            hotbarHandler.DrawActionHotbar();
        }
        else{
            amount = hotbarHandler.hotbar.GetSlot(PlayerHotbarHandler.hotbarSlot).GetAmount();
            playerInventoryManager.SubToCounter(hotbarHandler.hotbar.GetSlot(PlayerHotbarHandler.hotbarSlot).GetID(), amount);
            playerInventoryManager.SetNull(1, PlayerHotbarHandler.hotbarSlot);

            its = new ItemStack(id, amount);
            hotbarHandler.DrawActionHotbar();
        }

        playerInventoryManager.SendInventoryDataToServer();

        DropItem(its, (byte)(PlayerHotbarHandler.hotbarSlot + InventoryLoader.GetInventorySize("ACTION_HOTBAR")));
    }

    public void DropItem(ItemStack its, byte hotbarSlot){
        Vector3 force = this.playerCamera.forward / 5f;
        Vector3 origin = WorldOrigin.ToAbsolute(this.playerCamera.position);

        NetMessage message = new NetMessage(NetCode.DROPITEM);
        message.DropItem(origin.x, origin.y, origin.z, force.x, force.y, force.z, its.GetID(), its.GetAmount(), hotbarSlot);       
        this.cl.client.Send(message);

        if(hotbarSlot != byte.MaxValue){
            hotbarSlot = (byte)(hotbarSlot - InventoryLoader.GetInventorySize("ACTION_HOTBAR"));
            hotbarHandler.DrawHotbarSlot(hotbarSlot);
            hotbarHandler.playerInventoryManager.DrawSlot(1, hotbarSlot);
        }
    }
    public void DropItem(ItemStack its){DropItem(its, byte.MaxValue);}
}
