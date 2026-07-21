using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // Unity Reference
    public ChunkLoader cl;
    public CharacterController controller;
    public MainControllerManager controls;
    private PlayerSheetController playerSheetController;
    private PlayerActionController playerActionController;

    // Movement Preset
    private BaseMovePreset movementOrchestrator;

    // Movement variables
    private float momentum;
    private Vector3 velocity = Vector3.zero;
    private Vector3 direction;
    private Vector3 finalMovement;
    private float movementAlignment;
    private float runMomentumBoost;

    // Knockback
    public float knockbackAlignment;
    public Vector3 knockbackForce = Vector3.zero;
    public float knockbackMomentum;

    // Gravity
    public float gravityMomentum;

    // Flags
    public MovementFlags? flags;


    void OnDestroy(){
        this.controller = null;
        this.movementOrchestrator.Reset();
    }

    void FixedUpdate(){
        if(this.movementOrchestrator == null || this.flags == null)
            return;

        this.gravityMomentum = this.movementOrchestrator.CalculateGravityAcceleration((MovementFlags)this.flags, this.gravityMomentum);
        this.gravityMomentum = this.movementOrchestrator.CalculateJump((MovementFlags)this.flags, this.gravityMomentum);
    }

    // Update is called once per frame
    void Update(){
        if(this.movementOrchestrator == null)
            return;

        this.flags = new MovementFlags{
            isGrounded = this.controller.isGrounded,
            isJumping = this.controls.jumping && !this.playerActionController.HasRestriction(PlayerActionRestriction.MOVEMENT),
            isShifting = MainControllerManager.shifting,
            isControlling = MainControllerManager.ctrl,
            collision = this.controller.collisionFlags
        };

        this.direction = this.movementOrchestrator.CalculateDirection(this.transform, this.controls.movementX, this.controls.movementZ);
        this.movementAlignment = this.movementOrchestrator.CalculateMovementAlignment(this.velocity, this.direction, this.velocity);

        // Update movement values only if no restriction is applied to player
        if(!(this.playerActionController.HasRestriction(PlayerActionRestriction.MOVEMENT) ||
           this.playerActionController.HasRestriction(PlayerActionRestriction.STUNNED) ||
           this.playerActionController.HasRestriction(PlayerActionRestriction.SYSTEM))){

            this.momentum = this.movementOrchestrator.CalculateMomentum(this.direction, this.momentum, this.knockbackMomentum, this.movementAlignment);
            this.runMomentumBoost = this.movementOrchestrator.CalculateRunMomentumBoost(this.transform, this.direction, this.runMomentumBoost, this.momentum, this.movementAlignment);
            this.velocity = this.movementOrchestrator.CalculateFinalVelocity(this.direction, this.velocity, this.momentum, this.runMomentumBoost, this.movementAlignment);
            this.finalMovement = this.movementOrchestrator.CalculateFinalMovement(this.velocity, this.knockbackForce, this.knockbackMomentum, this.gravityMomentum);

            this.controller.Move(this.finalMovement * Time.deltaTime);

            this.knockbackMomentum = this.movementOrchestrator.CalculateKnockbackMomentumDecay(this.knockbackMomentum);

            this.movementOrchestrator.UpdateFOV(this.cl.playerRaycast.playerCamera, this.runMomentumBoost);

            this.playerActionController.VerifyMovement(this.transform.forward, this.direction, this.runMomentumBoost/this.movementOrchestrator.GetMaxRunningMomentum(), Mathf.Clamp(this.gravityMomentum/15f, -5f, 5f), (MovementFlags)this.flags);
        }
        else{
            this.direction = Vector3.zero;
            this.runMomentumBoost = 0f;
            this.momentum = this.movementOrchestrator.CalculateMomentum(this.direction, this.momentum, this.knockbackMomentum, this.movementAlignment);
            this.movementOrchestrator.UpdateFOV(this.cl.playerRaycast.playerCamera, this.runMomentumBoost);
            this.velocity = this.movementOrchestrator.CalculateFinalVelocity(this.direction, this.velocity, this.momentum, this.runMomentumBoost, this.movementAlignment);
            this.finalMovement = this.movementOrchestrator.CalculateFinalMovement(this.velocity, this.knockbackForce, this.knockbackMomentum, this.gravityMomentum);

            this.controller.Move(this.finalMovement * Time.deltaTime);

            this.knockbackMomentum = this.movementOrchestrator.CalculateKnockbackMomentumDecay(this.knockbackMomentum);
        }


        //Debug.Log($"Dir: {this.direction} -- Velocity: {this.velocity} -- Alignment: {this.movementAlignment} -- Momentum: {this.momentum} -- RunBoost: {this.runMomentumBoost}");
        //Debug.Log(this.movementOrchestrator.Length());
    }

    public void Init(){
        this.playerSheetController = this.cl.playerSheetController;
        this.playerActionController = this.cl.playerActionController;
        this.movementOrchestrator = new NormalMovePreset(this.cl.playerSheetController.GetSheet());
    }

    public bool IsGrounded(){return this.controller.isGrounded;}

    public void ChangeMoveset(Moveset moveSet){
        CharacterSheet sheet = this.playerSheetController.GetSheet();

        switch(moveSet){
            case Moveset.NORMAL:
                this.movementOrchestrator = new NormalMovePreset(sheet);
                break;
            case Moveset.FREECAM:
                this.movementOrchestrator = new FreecamMovePreset(sheet);
                break;
            case Moveset.SWIM:
                this.movementOrchestrator = new SwimmingMovePreset(sheet);
                break;
            default:
                this.movementOrchestrator = new NormalMovePreset(sheet);
                break;
        }
    }

    public Vector3 GetLookDirection(){return this.transform.forward;}
    public Vector3 GetForwardDirection(){return this.movementOrchestrator.CalculateDirection(this.transform, this.controls.movementX, this.controls.movementZ);}

    public void AddModifier(MovePresetProperty prop, MathOperation op){
        this.movementOrchestrator.AddModifier(prop, op);
    }
    public bool CheckModifier(MovePresetProperty prop, MathOperation op){return this.movementOrchestrator.CheckModifierExists(prop, op);}
    public void RemoveModifier(MovePresetProperty prop, MathOperation op){this.movementOrchestrator.RemoveModifier(prop, op);}

    public void AddKnockback(Vector3 dir, float momentum){
        // If has no other knockback happening
        if(this.knockbackMomentum == 0f){
            this.knockbackAlignment = this.movementOrchestrator.CalculateMovementAlignment(this.velocity, dir, this.velocity);
            this.knockbackMomentum = this.movementOrchestrator.ProcessKnockbackMomentum(momentum);
            this.knockbackForce = dir.normalized;
        }
        // Handles multiple knockback at the same time
        else{
            this.knockbackForce = (this.knockbackForce * this.knockbackMomentum) + (dir * momentum);
            this.knockbackMomentum = this.movementOrchestrator.ProcessKnockbackMomentum(this.knockbackForce.magnitude);
            this.knockbackForce = this.knockbackForce.normalized;
            this.knockbackAlignment = this.movementOrchestrator.CalculateMovementAlignment(this.velocity, this.knockbackForce, this.velocity);
        }

        // Adjust gravity momentum
        if(this.gravityMomentum < 0f){
            this.gravityMomentum += Mathf.Max(this.knockbackForce.y * this.knockbackMomentum, 0f);
        }

        // Fast Stop
        if(this.knockbackAlignment <= 0){
            this.momentum = Mathf.Max(this.momentum * (this.knockbackAlignment * momentum), 0f);
        }
    }

    // Headbumping Mechanics
    private void OnControllerColliderHit(ControllerColliderHit hit){
        if((this.controller.collisionFlags & CollisionFlags.Sides) != 0){
            this.momentum = this.movementOrchestrator.CalculateImpact(this.velocity, this.momentum, hit.normal);
            this.knockbackMomentum = this.movementOrchestrator.CalculateImpact(this.knockbackForce, this.knockbackMomentum, hit.normal);
            this.runMomentumBoost = this.movementOrchestrator.CalculateImpact(this.velocity, this.runMomentumBoost, hit.normal);
        }

        if((this.controller.collisionFlags & CollisionFlags.Above) != 0){
            this.gravityMomentum = Mathf.Min(this.gravityMomentum, 0f);
        }
    }
}
