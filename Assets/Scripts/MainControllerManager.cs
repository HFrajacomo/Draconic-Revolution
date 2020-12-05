using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainControllerManager : MonoBehaviour
{
	// Public passed variables
	public float movementX = 0f;
	public float movementZ = 0f;
	public bool jumping = false;
	public float mouseX = 0f;
	public float mouseY = 0f;

	public bool primaryAction = false;
	public bool secondaryAction = false;
	public bool interact = false;
    public bool gravityHack = false;
    public bool prefabRead = false;
    public bool prefabReadAir = false;
    public bool freecam = false;
    public bool shifting = false;
    public static bool reload = false;
    public static bool debugKey = false;


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
    	primaryAction = true;
    }

    public void OnSecondaryAction(){
    	secondaryAction = true;
    }

    public void OnInteract(){
    	interact = true;
    }

    public void OnToggleGravity(){
        gravityHack = !gravityHack;
    }

    public void OnPrefabRead(){
        prefabRead = true;
    }

    public void OnPrefabReadAir(){
        prefabReadAir = true;
    }

    public void OnToggleFreeCam(){
        freecam = !freecam;
    }

    public void OnShifting(){
        if(!shifting){
            shifting = true;
        }
        // If it's release
        else
            shifting = false;       
    }

    public void OnReload(){
        reload = true;
    }

    public void OnDebugKey(){
        MainControllerManager.debugKey = true;
    }
}
