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

    // Static lock to not send too many messages to server
    public static bool SENTFRAMEDATA = false;



	float xRotation = 0f;

    void Start(){
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    // Update is called once per frame
    void Update()
    {
        MouseLook.SENTFRAMEDATA = false;

        if(MainControllerManager.InUI)
            return;

        float mouseX = controls.mouseX * xSensitivity;
        float mouseY = controls.mouseY * ySensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        playerBody.Rotate(Vector3.up * mouseX);

        // Movement detection
        if(mouseX != 0 || mouseY != 0){
            this.position = this.playerBody.transform.position;
            this.rotation = this.playerBody.transform.eulerAngles;

            this.mouseMessage = new NetMessage(NetCode.CLIENTPLAYERPOSITION);
            this.mouseMessage.ClientPlayerPosition(this.position.x, this.position.y, this.position.z, this.rotation.x, this.rotation.y, this.rotation.z);
            this.cl.client.Send(this.mouseMessage.GetMessage(), this.mouseMessage.size);

            MouseLook.SENTFRAMEDATA = true;
        }
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
