using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{


    public ChunkLoader cl;
	public float xSensitivity = 0.07f;
	public float ySensitivity = 0.1f;
	public Transform playerBody;
    public MainControllerManager controls;

    // Networking
    private NetMessage mouseMessage;
    private Vector3 position;
    private Vector3 rotation;

	private float xRotation = 0f;


    void Start(){
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    // Update is called once per frame
    void Update()
    {
        if(MainControllerManager.InUI)
            return;

        float mouseX = controls.mouseX * xSensitivity;
        float mouseY = controls.mouseY * ySensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    public static void ToggleMouseCursor(bool b){
        if(b){
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;       
        }
        else{
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;     
        }
    }
}
