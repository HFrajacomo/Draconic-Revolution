using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // Unity Reference
    public ChunkLoader cl;

    // Movement properties
	public CharacterController controller;
	public float speed = 5f;
	public float gravity = -19.62f;
	public float jumpHeight = 5f;
    public LayerMask layerMask;
    public bool isGrounded;
	public Vector3 velocity;
    public Vector3 move;
    private int jumpticks = 6; // Amount of ticks the skinWidth will stick to new blocks
    public MainControllerManager controls;

    // Position properties
    private ChunkPos currentPos;
    private ChunkPos lastPos;
    private CastCoord cacheCoord;

    // Cache
    private NetMessage movementMessage;
    private Vector3 position;
    private Vector3 rotation;


    // Update is called once per frame
    void Update()
    {
        isGrounded = controller.isGrounded;

        if(!controls.freecam){

            // If is Grounded
        	if(isGrounded){
        		velocity.y = -0.1f;
        	}
            // If not, gravity affects
            else{
                velocity.y += gravity * Time.deltaTime;
            }

            float x = controls.movementX;
            float z = controls.movementZ;

            // Only move if not in menu
            if(!MainControllerManager.InUI){
                move = transform.right * x + transform.forward * z;
                controller.Move(move * speed * Time.deltaTime);
            }


            if(controls.jumping && isGrounded){
                velocity.y = jumpHeight;
                jumpticks = 10;
                controller.skinWidth = 0.4f;
            }

            // Block Sticking
            if(jumpticks > 0){
                jumpticks--;
            }
            else{
                controller.skinWidth = 0f;
            }

            // If gravity hack is toggled
            if(controls.gravityHack){
                velocity.y = 10f;
            }

            // Gravity
            controller.Move(velocity * Time.deltaTime);
        }

        // If on Freecam
        else{

            float x = controls.movementX;
            float z = controls.movementZ;

            move = transform.right * x + transform.forward * z;
            controller.Move(move * speed * Time.deltaTime);

            if(controls.jumping){
                velocity.y = 5;
                controller.Move(velocity * Time.deltaTime);
                velocity.y = 0;
            }
            else if(MainControllerManager.shifting && !MainControllerManager.InUI){
                velocity.y = -5;
                controller.Move(velocity * Time.deltaTime);
                velocity.y = 0;
            }
        }

        // Movement detection
        // Sends location to server
        if((move.sqrMagnitude > 0.01f || velocity.sqrMagnitude > 0.012f) && !MouseLook.SENTFRAMEDATA){
            this.position = this.controller.transform.position;
            this.rotation = this.controller.transform.eulerAngles;

            this.movementMessage = new NetMessage(NetCode.CLIENTPLAYERPOSITION);
            this.movementMessage.ClientPlayerPosition(this.position.x, this.position.y, this.position.z, this.rotation.x, this.rotation.y, this.rotation.z);
            this.cl.client.Send(this.movementMessage.GetMessage(), this.movementMessage.size);
        
            // Sends ClientChunk Message
            this.cacheCoord = new CastCoord(this.position);
            if(this.currentPos == null){
                this.currentPos = this.cacheCoord.GetChunkPos();
                this.lastPos = this.cacheCoord.GetChunkPos();
            }
            else if(this.currentPos != this.cacheCoord.GetChunkPos()){
                this.currentPos = this.cacheCoord.GetChunkPos();
                SendChunkPosMessage();
                this.lastPos = this.currentPos;
            }
        }

    }

    public void SetCurrentChunkPos(ChunkPos pos){
        this.currentPos = pos;
        this.lastPos = pos;
    }

    public void SendChunkPosMessage(){
        NetMessage message = new NetMessage(NetCode.CLIENTCHUNK);
        message.ClientChunk(this.lastPos, this.currentPos);
        this.cl.client.Send(message.GetMessage(), message.size);
    }
}
