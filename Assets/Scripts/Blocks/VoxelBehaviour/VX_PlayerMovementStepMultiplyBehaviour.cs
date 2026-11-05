using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;


[Serializable]
public class VX_PlayerMovementStepMultiplyBehaviour : VoxelBehaviour{
	public float maxSpeed = 1f;
	public float drag = 1f;
	public float jumpHeight = 1f;
	public float momentumGrowth = 1f;
	public float minimumMomentumToStop = 1f;
	public float gravityAcceleration = 1f;
	public float gravityMaxAccelerationTime = 1f;
	public float maxRunningMomentum = 1f;
	public float runMomentumGrowth = 1f;
	public float runMomentumDecrease = 1f;
	public float povAdjustment = 1f;
	public float maximumImpactAngleTolerance = 1f;
	public float maximumAllowedMomentumAfterImpact = 1f;

	private MathOperation maxSpeedOperation;
	private MathOperation dragOperation;
	private MathOperation jumpHeightOperation;
	private MathOperation momentumGrowthOperation;
	private MathOperation minimumMomentumToStopOperation;
	private MathOperation gravityAccelerationOperation;
	private MathOperation gravityMaxAccelerationTimeOperation;
	private MathOperation maxRunningMomentumOperation;
	private MathOperation runMomentumGrowthOperation;
	private MathOperation runMomentumDecreaseOperation;
	private MathOperation povAdjustmentOperation;
	private MathOperation maximumImpactAngleToleranceOperation;
	private MathOperation maximumAllowedMomentumAfterImpactOperation;

	public override void PostDeserializationSetup(bool isClient){
		this.maxSpeedOperation = new MathOperation{code = (ushort)MovementModifierCode.BASIC_MULTIPLIER, operation = '*', number = maxSpeed};
		this.dragOperation = new MathOperation{code = (ushort)MovementModifierCode.BASIC_MULTIPLIER, operation = '*', number = drag};
		this.jumpHeightOperation = new MathOperation{code = (ushort)MovementModifierCode.BASIC_MULTIPLIER, operation = '*', number = jumpHeight};
		this.momentumGrowthOperation = new MathOperation{code = (ushort)MovementModifierCode.BASIC_MULTIPLIER, operation = '*', number = momentumGrowth};
		this.minimumMomentumToStopOperation = new MathOperation{code = (ushort)MovementModifierCode.BASIC_MULTIPLIER, operation = '*', number = minimumMomentumToStop};
		this.gravityAccelerationOperation = new MathOperation{code = (ushort)MovementModifierCode.BASIC_MULTIPLIER, operation = '*', number = gravityAcceleration};
		this.gravityMaxAccelerationTimeOperation = new MathOperation{code = (ushort)MovementModifierCode.BASIC_MULTIPLIER, operation = '*', number = gravityMaxAccelerationTime};
		this.maxRunningMomentumOperation = new MathOperation{code = (ushort)MovementModifierCode.BASIC_MULTIPLIER, operation = '*', number = maxRunningMomentum};
		this.runMomentumGrowthOperation = new MathOperation{code = (ushort)MovementModifierCode.BASIC_MULTIPLIER, operation = '*', number = runMomentumGrowth};
		this.runMomentumDecreaseOperation = new MathOperation{code = (ushort)MovementModifierCode.BASIC_MULTIPLIER, operation = '*', number = runMomentumDecrease};
		this.povAdjustmentOperation = new MathOperation{code = (ushort)MovementModifierCode.BASIC_MULTIPLIER, operation = '*', number = povAdjustment};
		this.maximumImpactAngleToleranceOperation = new MathOperation{code = (ushort)MovementModifierCode.BASIC_MULTIPLIER, operation = '*', number = maximumImpactAngleTolerance};
		this.maximumAllowedMomentumAfterImpactOperation = new MathOperation{code = (ushort)MovementModifierCode.BASIC_MULTIPLIER, operation = '*', number = maximumAllowedMomentumAfterImpact};
	}

	public override void OnPlayerStepEnter(PlayerVoxelLocation location, CharacterSheet sheet, ChunkLoader cl){AddMods(cl);}

	public override void OnPlayerStepExit(PlayerVoxelLocation location, CharacterSheet sheet, ChunkLoader cl){RemoveMods(cl);}

	private void AddMods(ChunkLoader cl){
		cl.playerMovement.AddModifier(MovePresetProperty.MAX_NATURAL_SPEED, this.maxSpeedOperation);
		cl.playerMovement.AddModifier(MovePresetProperty.DRAG, this.dragOperation);
		cl.playerMovement.AddModifier(MovePresetProperty.JUMP_HEIGHT, this.jumpHeightOperation);
		cl.playerMovement.AddModifier(MovePresetProperty.MOMENTUM_GROWTH, this.momentumGrowthOperation);
		cl.playerMovement.AddModifier(MovePresetProperty.MINIMUM_MOMENTUM_TO_STOP, this.minimumMomentumToStopOperation);
		cl.playerMovement.AddModifier(MovePresetProperty.GRAVITY_ACCELERATION, this.gravityMaxAccelerationTimeOperation);
		cl.playerMovement.AddModifier(MovePresetProperty.GRAVITY_MAX_ACCELERATION_TIME, this.gravityAccelerationOperation);
		cl.playerMovement.AddModifier(MovePresetProperty.MAX_RUNNING_MOMENTUM, this.maxRunningMomentumOperation);
		cl.playerMovement.AddModifier(MovePresetProperty.RUN_MOMENTUM_GROWTH, this.runMomentumGrowthOperation);
		cl.playerMovement.AddModifier(MovePresetProperty.RUN_MOMENTUM_DECREASE, this.runMomentumDecreaseOperation);
		cl.playerMovement.AddModifier(MovePresetProperty.POV_ADJUSTMENT, this.povAdjustmentOperation);
		cl.playerMovement.AddModifier(MovePresetProperty.MAXIMUM_IMPACT_ANGLE_TOLERANCE, this.maximumImpactAngleToleranceOperation);
		cl.playerMovement.AddModifier(MovePresetProperty.MAXIMUM_ALLOWED_MOMENTUM_AFTER_IMPACT, this.maximumAllowedMomentumAfterImpactOperation);
	}

	private void RemoveMods(ChunkLoader cl){
		cl.playerMovement.RemoveModifier(MovePresetProperty.MAX_NATURAL_SPEED, this.maxSpeedOperation);
		cl.playerMovement.RemoveModifier(MovePresetProperty.DRAG, this.dragOperation);
		cl.playerMovement.RemoveModifier(MovePresetProperty.JUMP_HEIGHT, this.jumpHeightOperation);
		cl.playerMovement.RemoveModifier(MovePresetProperty.MOMENTUM_GROWTH, this.momentumGrowthOperation);
		cl.playerMovement.RemoveModifier(MovePresetProperty.MINIMUM_MOMENTUM_TO_STOP, this.minimumMomentumToStopOperation);
		cl.playerMovement.RemoveModifier(MovePresetProperty.GRAVITY_ACCELERATION, this.gravityAccelerationOperation);
		cl.playerMovement.RemoveModifier(MovePresetProperty.GRAVITY_MAX_ACCELERATION_TIME, this.gravityMaxAccelerationTimeOperation);
		cl.playerMovement.RemoveModifier(MovePresetProperty.MAX_RUNNING_MOMENTUM, this.maxRunningMomentumOperation);
		cl.playerMovement.RemoveModifier(MovePresetProperty.RUN_MOMENTUM_GROWTH, this.runMomentumGrowthOperation);
		cl.playerMovement.RemoveModifier(MovePresetProperty.RUN_MOMENTUM_DECREASE, this.runMomentumDecreaseOperation);
		cl.playerMovement.RemoveModifier(MovePresetProperty.POV_ADJUSTMENT, this.povAdjustmentOperation);
		cl.playerMovement.RemoveModifier(MovePresetProperty.MAXIMUM_IMPACT_ANGLE_TOLERANCE, this.maximumImpactAngleToleranceOperation);
		cl.playerMovement.RemoveModifier(MovePresetProperty.MAXIMUM_ALLOWED_MOMENTUM_AFTER_IMPACT, this.maximumAllowedMomentumAfterImpactOperation);
	}
}