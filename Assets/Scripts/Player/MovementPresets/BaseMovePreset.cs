using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public abstract class BaseMovePreset {
	private CharacterSheet playerSheet;
    protected bool ignoreModifiers = false;

    // Movement variables
    protected float maxNaturalSpeed = 0f;
    protected float drag = 3.5f;
    protected float jumpHeight = 5.4f;

    // Growth
    protected float momentumGrowth = 2.4f;
    protected float minimumMomentumToStop = 0.3f;

    // Gravity
    protected float gravityAcceleration = -25f;
    protected float gravityMaxAccelerationTime = 1.6f;

    // Running
    protected float maxRunningMomentum = 0.5f;
    protected float runMomentumGrowth = 0.6f;
    protected float runMomentumDecrease = 2f;
    protected float povAdjustment = 15f;

    // Impact
    protected float maximumImpactAngleTolerance = -0.7f;
    protected float maximumAllowedMomentumAfterImpact = 0.3f;

    // Fov
    protected float maximumFovDeacceleration = 0.4f;
    protected float maximumFovAcceleration = 0.8f;

    // Airborne: share of the acceleration and steering kept in the air. Drag stays the one of the surface jumped from
    protected float airControl = 0.7f;

    protected static List<MathOperation>[] modifier;

    public BaseMovePreset(CharacterSheet sheet){
    	this.playerSheet = sheet;
    	this.maxNaturalSpeed = 2 + (sheet.GetSpeed().GetFinal())/5f;
		this.drag = 3.5f;
		this.jumpHeight = 5.4f;
		this.momentumGrowth = 2.4f;
		this.minimumMomentumToStop = 0.3f;
		this.gravityAcceleration = -25f;
		this.gravityMaxAccelerationTime = 1.6f;
		this.maxRunningMomentum = 0.5f;
		this.runMomentumGrowth = 0.6f;
		this.runMomentumDecrease = 2f;
		this.povAdjustment = 15f;
        this.maximumImpactAngleTolerance = -0.7f;
        this.maximumAllowedMomentumAfterImpact = 0.3f;
        this.airControl = 0.7f;

        if(BaseMovePreset.modifier == null){
            BaseMovePreset.modifier = new List<MathOperation>[Enum.GetNames(typeof(MovePresetProperty)).Length];
            for(int i=0; i < Enum.GetNames(typeof(MovePresetProperty)).Length; i++){BaseMovePreset.modifier[i] = new List<MathOperation>();}
        }
    }

    public void Reset(){
        for(int i=0; i < Enum.GetNames(typeof(MovePresetProperty)).Length; i++){BaseMovePreset.modifier[i].Clear();}
    }

    public int Length(){return BaseMovePreset.modifier[(int)MovePresetProperty.DRAG].Count;}

    public bool CheckModifierExists(MovePresetProperty property, MathOperation op){
        for(int i=0; i < BaseMovePreset.modifier[(int)property].Count; i++){
            if(op.Equals(BaseMovePreset.modifier[(int)property][i])){
                return true;
            }
        }

        return false;
    }

    public bool AddModifier(MovePresetProperty property, MathOperation op){
        if(!CheckModifierExists(property, op)){
            if(op.operation == '+')
                BaseMovePreset.modifier[(int)property].Insert(0, op);
            else
                BaseMovePreset.modifier[(int)property].Add(op);
            return true;
        }

        return false;
    }

    public bool RemoveModifier(MovePresetProperty property, MathOperation op){
        for(int i=0; i < BaseMovePreset.modifier[(int)property].Count; i++){
            if(op.Equals(BaseMovePreset.modifier[(int)property][i])){
                BaseMovePreset.modifier[(int)property].RemoveAt(i);
                return true;
            }
        }

        return false;
    }
	
	public virtual Vector3 CalculateDirection(Transform player, float controllerX, float controllerZ){return (player.right * controllerX + player.forward * controllerZ).normalized;}

    // Presets that are never grounded (like swimming) don't count as airborne
    public virtual bool IsAirborne(MovementFlags flags){return !flags.isGrounded;}

	public virtual float CalculateMovementAlignment(Vector3 a, Vector3 b, Vector3 playerVelocity){
        float align = 0f;
        float absolute = 0f;

        if(a.magnitude == 0 || b.magnitude == 0 || playerVelocity.magnitude == 0)
            return 1f;

        align = Vector3.Dot(a.normalized, b.normalized);
        absolute = Mathf.Abs(align);

        if(align >= 0.9995f){
            align = 1f;
        }

        else if(absolute > 0 && absolute <= 0.01f){
            align = 0f;
        }

        return align;
	}

	public virtual float CalculateMomentum(Vector3 playerDirection, float currentMomentum, float currentKnockbackMomentum, float movementAlignment, MovementFlags flags){
        if(playerDirection != Vector3.zero){
            if(movementAlignment > 0)
                return Mathf.Clamp(currentMomentum + (movementAlignment * GetMomentumGrowth() * GetControl(flags) * Time.deltaTime), 0f, 1f);
            else{
                return Mathf.Clamp(currentMomentum - (GetDrag() * Time.deltaTime), 0f, 1f);
            }
        }
        else{
            if(currentMomentum + currentKnockbackMomentum <= GetMinimumMomentumToStop()){
                return 0f;
            }

            return Mathf.Clamp(currentMomentum - (GetDrag() * Time.deltaTime), 0f, 1f);
        }
	}

	public virtual float CalculateRunMomentumBoost(Transform transf, Vector3 playerDirection, float currentRunMomentum, float momentum, float movementAlignment){
        if(momentum == 1 && MainControllerManager.shifting && CheckValidRunDirection(transf, playerDirection)){
            return Mathf.Clamp(currentRunMomentum + (movementAlignment * GetRunMomentumGrowth() * Time.deltaTime), 0, GetMaxRunningMomentum());
        }
        
        return Mathf.Clamp(currentRunMomentum - (Mathf.Abs(movementAlignment) * GetRunMomentumDecrease() * Time.deltaTime), 0, GetMaxRunningMomentum());
	}

	public virtual Vector3 CalculateFinalVelocity(Vector3 playerDirection, Vector3 playerVelocity, float currentMomentum, float currentRunMomentum, float alignment, MovementFlags flags){
        Vector3 sum;

        if(playerDirection == Vector3.zero){
            return playerVelocity.normalized * currentMomentum * GetMaxNaturalSpeed();
        }
        else{
            // Steering is weaker in the air, so the direction of a jump is mostly kept
            if(alignment >= 0){
                sum = playerVelocity + (playerDirection * (currentMomentum + currentRunMomentum) * GetMaxNaturalSpeed() * GetControl(flags));
            }
            else
                sum = playerVelocity.normalized * (currentMomentum + currentRunMomentum) * GetMaxNaturalSpeed();


            return sum.normalized * GetMaxNaturalSpeed() * (currentMomentum + currentRunMomentum);
        }
	}

	public virtual Vector3 CalculateFinalMovement(Vector3 velocity, Vector3 knockbackForce, float knockbackMomentum, float gravityMomentum){
		return velocity + (knockbackForce * knockbackMomentum) + (gravityMomentum * Vector3.up);
	}

	public virtual float CalculateGravityAcceleration(MovementFlags flags, float currentGravityMomentum){
        if(flags.isGrounded){
            return -0.01f;
        }

        return Mathf.Max(currentGravityMomentum + (GetGravityAcceleration() * Time.fixedDeltaTime) / GetGravityMaxAccelerationTime(), GetGravityAcceleration());
	}

	public virtual float CalculateJump(MovementFlags flags, float gravityMomentum){
        if(flags.isGrounded && flags.isJumping && gravityMomentum < GetJumpHeight())
            return GetJumpHeight();

        return gravityMomentum;
	}

    public virtual float ProcessKnockbackMomentum(float currentKnockbackMomentum){return currentKnockbackMomentum;}

	public virtual float CalculateKnockbackMomentumDecay(float currentKnockbackMomentum){
        float newMomentum = 0f;

        if(currentKnockbackMomentum == 0f)
            return 0f;

        newMomentum = currentKnockbackMomentum - (drag * 2 * Time.deltaTime);

        if(newMomentum <= GetMinimumMomentumToStop())
            return 0f;

        return newMomentum;
	}

    public virtual void UpdateFOV(Camera cam, float currentRunMomentum){
        if(currentRunMomentum == 0 && cam.fieldOfView == Configurations.fieldOfView)
            return;

        float newFov = Configurations.fieldOfView + Mathf.Lerp(0, GetPovAdjustment(), currentRunMomentum/GetMaxRunningMomentum());

        if(newFov < cam.fieldOfView && cam.fieldOfView - newFov > this.maximumFovDeacceleration){
            cam.fieldOfView -= this.maximumFovDeacceleration;
        }
        else if(newFov > cam.fieldOfView && newFov - cam.fieldOfView > this.maximumFovAcceleration){
            cam.fieldOfView += this.maximumFovAcceleration;
        }
        else{
            cam.fieldOfView = newFov;
        }
    }

    public virtual float CalculateImpact(Vector3 movement, float momentum, Vector3 impactNormal){
        float dot = Vector3.Dot(movement.normalized, impactNormal.normalized);

        if(dot >= GetMaximumImpactAngleTolerance())
            return momentum;

        if(momentum >= GetMaximumAllowedMomentumAfterImpact())
            return GetMaximumAllowedMomentumAfterImpact();

        return momentum;
    }

    protected bool CheckValidRunDirection(Transform t, Vector3 playerDirection){
        float alignment = Vector3.Dot(t.forward, playerDirection);

        if(alignment >= 0.7f)
            return true;
        return false;
    }

    public float GetMaxRunningMomentum(){return ProcessModifiers(MovePresetProperty.MAX_RUNNING_MOMENTUM, this.maxRunningMomentum);}
    protected float GetMaxNaturalSpeed(){return ProcessModifiers(MovePresetProperty.MAX_NATURAL_SPEED, this.maxNaturalSpeed);}
    protected float GetDrag(){return ProcessModifiers(MovePresetProperty.DRAG, this.drag);}
    protected float GetJumpHeight(){return ProcessModifiers(MovePresetProperty.JUMP_HEIGHT, this.jumpHeight);}
    protected float GetMomentumGrowth(){return ProcessModifiers(MovePresetProperty.MOMENTUM_GROWTH, this.momentumGrowth);}
    protected float GetMinimumMomentumToStop(){return ProcessModifiers(MovePresetProperty.MINIMUM_MOMENTUM_TO_STOP, this.minimumMomentumToStop);}
    protected float GetGravityAcceleration(){return ProcessModifiers(MovePresetProperty.GRAVITY_ACCELERATION, this.gravityAcceleration);}
    protected float GetGravityMaxAccelerationTime(){return ProcessModifiers(MovePresetProperty.GRAVITY_MAX_ACCELERATION_TIME, this.gravityMaxAccelerationTime);}
    protected float GetRunMomentumGrowth(){return ProcessModifiers(MovePresetProperty.RUN_MOMENTUM_GROWTH, this.runMomentumGrowth);}
    protected float GetRunMomentumDecrease(){return ProcessModifiers(MovePresetProperty.RUN_MOMENTUM_DECREASE, this.runMomentumDecrease);}
    protected float GetPovAdjustment(){return ProcessModifiers(MovePresetProperty.POV_ADJUSTMENT, this.povAdjustment);}
    protected float GetMaximumImpactAngleTolerance(){return ProcessModifiers(MovePresetProperty.MAXIMUM_IMPACT_ANGLE_TOLERANCE, this.maximumImpactAngleTolerance);}
    protected float GetMaximumAllowedMomentumAfterImpact(){return ProcessModifiers(MovePresetProperty.MAXIMUM_ALLOWED_MOMENTUM_AFTER_IMPACT, this.maximumAllowedMomentumAfterImpact);}
    protected float GetAirControl(){return ProcessModifiers(MovePresetProperty.AIR_CONTROL, this.airControl);}

    // Share of acceleration and steering available: full on the ground, airControl in the air
    protected float GetControl(MovementFlags flags){return IsAirborne(flags) ? GetAirControl() : 1f;}

    protected float ProcessModifiers(MovePresetProperty prop, float number){
        if(this.ignoreModifiers)
            return number;

        for(int i=0; i < BaseMovePreset.modifier[(int)prop].Count; i++){
            number = BaseMovePreset.modifier[(int)prop][i].Apply(number);
        }

        return number;
    }
}