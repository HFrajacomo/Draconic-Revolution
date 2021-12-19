using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainControllerManager : MonoBehaviour
{
    private Vector3 cachedForce;

	// Public passed variables
	public float movementX = 0f;
	public float movementZ = 0f;
	public bool jumping = false;
	public float mouseX = 0f;
	public float mouseY = 0f;

    public bool gravityHack = false;
    public bool prefabRead = false;
    public bool prefabReadAir = false;
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
    public PlayerEvents playerEvents;
    public GameObject inventoryGUI;
    public InventoryUIPlayer invUIPlayer;
    public GameObject hotbar;
    public PlayerRaycast raycast;
    public Transform playerCamera;
    public ChunkLoader cl;

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

    // Mouse Camera Look
    public void OnMovement(InputValue val){
        movementX = val.Get<Vector2>().x;
        movementZ = val.Get<Vector2>().y;
    }

    public void OnPrimaryAction(){
    	if(!MainControllerManager.InUI)
            raycast.BreakBlock();
    }

    public void OnSecondaryAction(){
    	if(!MainControllerManager.InUI)
            raycast.UseItem();
    }

    public void OnInteract(){
        if(!MainControllerManager.InUI)
            raycast.Interact();
    }

    public void OnToggleGravity(){
        gravityHack = !gravityHack;
    }

    public void OnPrefabRead(){
        //prefabRead = true;
    }

    public void OnPrefabReadAir(){
        //prefabReadAir = true;
    }

    public void OnToggleFreeCam(){
        freecam = !freecam;
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
        SceneManager.LoadScene("Menu");
    }

    public void OnToggleHUD(){
        this.HUDActive = !this.HUDActive;
        this.mainHUD.SetActive(this.HUDActive);
    }

    public void OnScroll1(){
        playerEvents.Scroll1();
    }
    public void OnScroll2(){
        playerEvents.Scroll2();
    }
    public void OnScroll3(){
        playerEvents.Scroll3();
    }
    public void OnScroll4(){
        playerEvents.Scroll4();
    }
    public void OnScroll5(){
        playerEvents.Scroll5();
    }
    public void OnScroll6(){
        playerEvents.Scroll6();
    }
    public void OnScroll7(){
        playerEvents.Scroll7();
    }
    public void OnScroll8(){
        playerEvents.Scroll8();
    }
    public void OnScroll9(){
        playerEvents.Scroll9();
    }
    public void OnMouseScroll(InputValue val){
        playerEvents.MouseScroll((int)val.Get<Vector2>().y);
    }
    public void OnOpenInventory(){
        bool newState = !MainControllerManager.InUI;
        this.inventoryGUI.SetActive(newState);
        MainControllerManager.InUI = newState;

        if(newState)
            invUIPlayer.ReloadInventory();

        this.invUIPlayer.ResetSelection();
        hotbar.SetActive(!newState);
        MouseLook.ToggleMouseCursor(newState);

        // If closing, refresh the hotbar
        if(newState == false){
            playerEvents.DrawHotbar();
            playerEvents.DrawItemEntity(playerEvents.GetSlotStack());
        }
    }
    public void CloseInventory(){
        this.inventoryGUI.SetActive(false);
        MainControllerManager.InUI = false;
        this.invUIPlayer.ResetSelection();
        MouseLook.ToggleMouseCursor(false);
        hotbar.SetActive(true);
        playerEvents.DrawHotbar();
        playerEvents.DrawItemEntity(playerEvents.GetSlotStack());
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
        if(playerEvents.hotbar.GetSlot(PlayerEvents.hotbarSlot) == null)
            return;

        ItemID id = playerEvents.hotbar.GetSlot(PlayerEvents.hotbarSlot).GetID();
        ItemStack its;
        byte amount;

        if(!MainControllerManager.ctrl){
            if(playerEvents.hotbar.GetSlot(PlayerEvents.hotbarSlot).Decrement()){
                playerEvents.hotbar.SetNull(PlayerEvents.hotbarSlot);
            }

            amount = 1;
            its = new ItemStack(id, amount);
        }
        else{
            amount = playerEvents.hotbar.GetSlot(PlayerEvents.hotbarSlot).GetAmount();
            playerEvents.hotbar.SetNull(PlayerEvents.hotbarSlot);

            its = new ItemStack(id, amount);
        }  


        Vector3 force = this.playerCamera.forward / 5f;

        NetMessage message = new NetMessage(NetCode.DROPITEM);
        message.DropItem(this.playerCamera.position.x, this.playerCamera.position.y, this.playerCamera.position.z, this.playerCamera.rotation.x, this.playerCamera.rotation.y, this.playerCamera.rotation.z, force.x, force.y, force.z, (ushort)id, amount);       
        this.cl.client.Send(message.GetMessage(), message.size);

        playerEvents.DrawHotbarSlot(PlayerEvents.hotbarSlot);
        playerEvents.DrawItemEntity(playerEvents.hotbar.GetSlot(PlayerEvents.hotbarSlot));
        playerEvents.invUIPlayer.DrawSlot(1, PlayerEvents.hotbarSlot);
    }
}
