using UnityEngine;

public class FreecamMovePreset : BaseMovePreset {
	
	public FreecamMovePreset(CharacterSheet sheet) : base(sheet){
		this.ignoreModifiers = true;
    	this.maxNaturalSpeed = 15;
		this.drag = 100f;
		this.jumpHeight = 5.4f;
		this.momentumGrowth = 999f;
		this.minimumMomentumToStop = 0f;
		this.gravityAcceleration = 0f;
		this.gravityMaxAccelerationTime = 0f;
		this.maxRunningMomentum = 3f;
		this.runMomentumGrowth = 6f;
		this.runMomentumDecrease = 10f;
		this.povAdjustment = 25f;
        this.maximumImpactAngleTolerance = -999f;
        this.maximumAllowedMomentumAfterImpact = 1f;
	}

	public override bool IsAirborne(MovementFlags flags){return false;}

	public override float CalculateRunMomentumBoost(Transform transf, Vector3 playerDirection, float currentRunMomentum, float momentum, float movementAlignment){
        if(MainControllerManager.shifting){
            return Mathf.Clamp(currentRunMomentum + (movementAlignment * this.runMomentumGrowth * Time.deltaTime), 0, this.maxRunningMomentum);
        }
        
        return Mathf.Clamp(currentRunMomentum - (Mathf.Abs(movementAlignment) * this.runMomentumDecrease * Time.deltaTime), 0, this.maxRunningMomentum);
	}

	public override float CalculateGravityAcceleration(MovementFlags flags, float currentGravityMomentum){
		float multiplier = 1f;

		if(flags.isShifting)
			multiplier = 2f;

        if(flags.isJumping)
            return this.maxNaturalSpeed * multiplier;
        else if(flags.isControlling)
        	return -this.maxNaturalSpeed * multiplier;

        return 0f;
	}

	public override float CalculateKnockbackMomentumDecay(float currentKnockbackMomentum){return 0f;}

    public override float CalculateImpact(Vector3 movement, float momentum, Vector3 impactNormal){return momentum;}

    public override float ProcessKnockbackMomentum(float currentKnockbackMomentum){return 0f;}
}