using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainControllerManager : MonoBehaviour
{
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
            this.playerActionController.Sheathe();
        }
    }

    public void OnInteract(){
        if(!MainControllerManager.InUI && !LOCK_INTERACT){
            raycast.Interact();
            LOCK_INTERACT = true;

            //this.cl.playerActionController.UseStyle("BASE_Sword", updatePlayerDataAndServer:true);
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

    public void OnDebugKey2(){
        MainControllerManager.DEBUG = !MainControllerManager.DEBUG;
    }

    public void OnToggleHUD(){
        if(MainControllerManager.InUI)
            return;

        this.HUDActive = !this.HUDActive;
        this.mainHUD.SetActive(this.HUDActive);
    }

    public void OnScroll1(){
        hotbarHandler.Scroll1();
    }
    public void OnScroll2(){
        hotbarHandler.Scroll2();
    }
    public void OnScroll3(){
        hotbarHandler.Scroll3();
    }
    public void OnScroll4(){
        hotbarHandler.Scroll4();
    }
    public void OnScroll5(){
        hotbarHandler.Scroll5();
    }
    public void OnScroll6(){
        hotbarHandler.Scroll6();
    }
    public void OnScroll7(){
        hotbarHandler.Scroll7();
    }
    public void OnScroll8(){
        hotbarHandler.Scroll8();
    }
    public void OnScroll9(){
        hotbarHandler.Scroll9();
    }
    public void OnMouseScroll(InputValue val){
        hotbarHandler.MouseScroll((int)val.Get<Vector2>().y);
    }
    public void OnOpenInventory(){
        bool newState = !MainControllerManager.InUI;
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
    public void CloseInventory(){
        this.inventoryGUI.SetActive(false);
        MainControllerManager.InUI = false;
        this.playerInventoryManager.ResetSelection();
        MouseLook.ToggleMouseCursor(false);

        this.HUDActive = true;
        this.mainHUD.SetActive(this.HUDActive);
        
        hotbarHandler.DrawHotbar();
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
        if(LOCK_DROP)
            return;

        LOCK_DROP = true;

        if(hotbarHandler.hotbar.GetSlot(PlayerHotbarHandler.hotbarSlot) == null)
            return;

        ushort id = hotbarHandler.hotbar.GetSlot(PlayerHotbarHandler.hotbarSlot).GetID();
        ItemStack its;
        byte amount;

        if(!MainControllerManager.ctrl){
            if(hotbarHandler.hotbar.GetSlot(PlayerHotbarHandler.hotbarSlot).Decrement()){
                hotbarHandler.hotbar.SetNull(PlayerHotbarHandler.hotbarSlot);
            }

            amount = 1;
            its = new ItemStack(id, amount);
        }
        else{
            amount = hotbarHandler.hotbar.GetSlot(PlayerHotbarHandler.hotbarSlot).GetAmount();
            hotbarHandler.hotbar.SetNull(PlayerHotbarHandler.hotbarSlot);

            its = new ItemStack(id, amount);
        }  


        Vector3 force = this.playerCamera.forward / 5f;

        NetMessage message = new NetMessage(NetCode.DROPITEM);
        message.DropItem(this.playerCamera.position.x, this.playerCamera.position.y, this.playerCamera.position.z, force.x, force.y, force.z, (ushort)id, amount, (byte)PlayerHotbarHandler.hotbarSlot);       
        this.cl.client.Send(message);

        hotbarHandler.DrawHotbarSlot(PlayerHotbarHandler.hotbarSlot);

        hotbarHandler.playerInventoryManager.DrawSlot(1, PlayerHotbarHandler.hotbarSlot);
    }
}
