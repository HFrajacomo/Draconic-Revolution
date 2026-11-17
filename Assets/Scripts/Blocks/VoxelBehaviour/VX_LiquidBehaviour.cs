using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

/*
LIQUID STATES:

0: Still 3
1: Still 2
2: Still 1
3: North2
4: NorthEast2
5: East2
6: SouthEast2
7: South2
8: SouthWest2
9: West2
10: NorthWest2
11: North1
12: NorthEast1
13: East1
14: SouthEast1
15: South1
16: SouthWest1
17: West1
18: NorthWest1
19: Falling3
20: Falling2
21: Falling1
22: Landed2
23: Landed1

Falling states are the column of a liquid that falls from a ledge. Falling3 keeps being the bottom of its column.
Falling2 and Falling1 become Landed2 and Landed1 once they reach solid ground. Landed states are full blocks that keep the spread of the level they had before falling.
Landed states only exist while the column above them feeds them. Landed2 spreads Level 1 water and Landed1 doesn't spread.
*/

[Serializable]
public class VX_LiquidBehaviour : VoxelBehaviour{
	public string liquidName;
	public int viscosityDelay;
	public float onBodyMovementMultiplierDrag;
	public float onBodyMovementMultiplierMaxSpeed;
	public float onBodyMovementMultiplierGravityAcceleration;
	public float onBodyMovementMultiplierMaxGravityTime;
	public float onBodyMovementMultiplierJumpHeight;
	public string vignetteName;
	public Color vignetteColor;

	private ushort liquidCode; // Post Serialize Set
	private VignetteData vignetteData;
	private MathOperation penaltyDrag;
	private MathOperation penaltyMaxSpeed;
	private MathOperation penaltyGravityAcceleration;
	private MathOperation penaltyMaxGravityAccelerationTime;
	private MathOperation penaltyJumpHeight;
	private MathOperation penaltyPOV;

	private static ushort[] aroundCodes = new ushort[8];
	private static ushort[] mainAroundCodes = new ushort[8];
	private static ushort[] aroundStates = new ushort[8];
	private static ushort[] mainAroundStates = new ushort[8];
	private CastCoord cachedPos;
	private CastCoord cachedCoord;
	private bool breakFLAG = false;
	private ChunkLoader_Server cl;

	private BUDSignal cachedBUD;

	private static Dictionary<ushort?, List<int>> spawnDirections = new Dictionary<ushort?, List<int>>();
	private static Dictionary<ushort, List<ushort>> spawnStateAdjascents = new Dictionary<ushort, List<ushort>>();
	private static Dictionary<ushort, int> stateDirection = new Dictionary<ushort, int>();
	private static Dictionary<ushort, byte> statePriority = new Dictionary<ushort, byte>();
	private static Dictionary<ushort, int3> cameFromDir = new Dictionary<ushort, int3>();
	private static Dictionary<ushort, HashSet<ushort>> cameFromState = new Dictionary<ushort, HashSet<ushort>>();

	public override void PostDeserializationSetup(bool isClient){
		this.liquidCode = VoxelLoader.GetBlockID(liquidName);

		// Water Spawn Directions
		if(spawnDirections.Count == 0){
			spawnDirections.Add(3, new List<int>(new int[]{6,0,2}));
			spawnDirections.Add(5, new List<int>(new int[]{0,2,4}));
			spawnDirections.Add(7, new List<int>(new int[]{2,4,6}));
			spawnDirections.Add(9, new List<int>(new int[]{4,6,0}));
			spawnDirections.Add(4, new List<int>(new int[]{0,2}));
			spawnDirections.Add(6, new List<int>(new int[]{2,4}));
			spawnDirections.Add(8, new List<int>(new int[]{4,6}));
			spawnDirections.Add(10, new List<int>(new int[]{6,0}));
			spawnDirections.Add(11, new List<int>(new int[]{2,6}));
			spawnDirections.Add(13, new List<int>(new int[]{4,0}));
			spawnDirections.Add(15, new List<int>(new int[]{6,2}));
			spawnDirections.Add(17, new List<int>(new int[]{0,4}));
		}

		// Water states priority
		if(statePriority.Count == 0){
			statePriority.Add(0, 9);
			statePriority.Add(1, 3);
			statePriority.Add(2, 0);
			statePriority.Add(3, 5);
			statePriority.Add(4, 4);
			statePriority.Add(5, 5);
			statePriority.Add(6, 4);
			statePriority.Add(7, 5);
			statePriority.Add(8, 4);
			statePriority.Add(9, 5);
			statePriority.Add(10, 4);
			statePriority.Add(11, 2);
			statePriority.Add(12, 1);
			statePriority.Add(13, 2);
			statePriority.Add(14, 1);
			statePriority.Add(15, 2);
			statePriority.Add(16, 1);
			statePriority.Add(17, 2);
			statePriority.Add(18, 1);
			statePriority.Add(19, 8);
			statePriority.Add(20, 7);
			statePriority.Add(21, 6);
			statePriority.Add(22, 7);
			statePriority.Add(23, 6);
		}

		// Adds to CameFrom dictionary
		if(cameFromDir.Count == 0){
			cameFromDir.Add(3, new int3(4,5,3));
			cameFromDir.Add(4, new int3(4,6,-1));
			cameFromDir.Add(5, new int3(6,5,7));
			cameFromDir.Add(6, new int3(0,6,-1));
			cameFromDir.Add(7, new int3(0,7,1));
			cameFromDir.Add(8, new int3(0,2,-1));
			cameFromDir.Add(9, new int3(2,1,3));
			cameFromDir.Add(10, new int3(2,4,-1));
			cameFromDir.Add(11, new int3(4,5,3));
			cameFromDir.Add(12, new int3(4,6,-1));
			cameFromDir.Add(13, new int3(6,5,7));
			cameFromDir.Add(14, new int3(0,6,-1));
			cameFromDir.Add(15, new int3(0,7,1));
			cameFromDir.Add(16, new int3(0,2,-1));
			cameFromDir.Add(17, new int3(2,1,3));
			cameFromDir.Add(18, new int3(2,4,-1));
		}

		// Adds to CameFromState dict
		if(cameFromState.Count == 0){
			cameFromState.Add(3, new HashSet<ushort>(){0,19});
			cameFromState.Add(4, new HashSet<ushort>(){0,19,3,5});
			cameFromState.Add(5, new HashSet<ushort>(){0,19});
			cameFromState.Add(6, new HashSet<ushort>(){0,19,5,7});
			cameFromState.Add(7, new HashSet<ushort>(){0,19});
			cameFromState.Add(8, new HashSet<ushort>(){0,19,7,9});
			cameFromState.Add(9, new HashSet<ushort>(){0,19});
			cameFromState.Add(10, new HashSet<ushort>(){0,19,9,3});
			cameFromState.Add(11, new HashSet<ushort>(){10,3,4,22,1});
			cameFromState.Add(12, new HashSet<ushort>(){22,1,11,13});
			cameFromState.Add(13, new HashSet<ushort>(){4,5,6,22,1});
			cameFromState.Add(14, new HashSet<ushort>(){22,1,13,15});
			cameFromState.Add(15, new HashSet<ushort>(){6,7,8,22,1});
			cameFromState.Add(16, new HashSet<ushort>(){22,1,15,17});
			cameFromState.Add(17, new HashSet<ushort>(){8,9,10,22,1});
			cameFromState.Add(18, new HashSet<ushort>(){22,1,11,17});
		}

		this.penaltyDrag = new MathOperation{code = (ushort)MovementModifierCode.LIQUID, operation = '*', number = onBodyMovementMultiplierDrag};
		this.penaltyMaxSpeed = new MathOperation{code = (ushort)MovementModifierCode.LIQUID, operation = '*', number = onBodyMovementMultiplierMaxSpeed};
		this.penaltyGravityAcceleration = new MathOperation{code = (ushort)MovementModifierCode.LIQUID, operation = '*', number = onBodyMovementMultiplierGravityAcceleration};
		this.penaltyMaxGravityAccelerationTime = new MathOperation{code = (ushort)MovementModifierCode.LIQUID, operation = '*', number = onBodyMovementMultiplierMaxGravityTime};
		this.penaltyJumpHeight = new MathOperation{code = (ushort)MovementModifierCode.LIQUID, operation = '*', number = onBodyMovementMultiplierJumpHeight};
		this.penaltyPOV = new MathOperation{code = (ushort)MovementModifierCode.LIQUID, operation = '*', number = 0f};
		
		this.vignetteData = new VignetteData{
			vignetteEffectName = this.vignetteName,
			color = this.vignetteColor,
			center = new Vector2(-2f, -2f),
			intensity = 1f,
			smoothness = 1f,
			roundness = 1f,
			effectTime = 0.12f
		};
	}

	public override void OnPlayerBodyEnter(PlayerVoxelLocation location, CharacterSheet sheet, ChunkLoader cl){
		if(location.head != this.liquidCode){
			AddMods(cl);
			cl.playerMovement.ChangeMoveset(Moveset.NORMAL);
		}
		else{
			RemoveMods(cl);
			cl.playerMovement.ChangeMoveset(Moveset.SWIM);
		}
	}

	public override void OnPlayerBodyExit(PlayerVoxelLocation location, CharacterSheet sheet, ChunkLoader cl){
		if(location.head != this.liquidCode){
			RemoveMods(cl);
			cl.playerMovement.ChangeMoveset(Moveset.NORMAL);
		}
		else{
			AddMods(cl);
			cl.playerMovement.ChangeMoveset(Moveset.NORMAL);
		}
	}

	public override void OnPlayerHeadEnter(PlayerVoxelLocation location, CharacterSheet sheet, ChunkLoader cl){
		if(location.body != this.liquidCode){
			AddMods(cl);
			cl.playerMovement.ChangeMoveset(Moveset.NORMAL);
		}
		else{
			RemoveMods(cl);
			cl.playerMovement.ChangeMoveset(Moveset.SWIM);
		}

		if(!cl.playerMovement.IsJumping())
			cl.vignetteController.Add(this.vignetteData);
	}

	public override void OnPlayerHeadExit(PlayerVoxelLocation location, CharacterSheet sheet, ChunkLoader cl){
		if(location.body != this.liquidCode){
			RemoveMods(cl);
			cl.playerMovement.ChangeMoveset(Moveset.NORMAL);
		}
		else{
			AddMods(cl);
			cl.playerMovement.ChangeMoveset(Moveset.NORMAL);
		}

		cl.vignetteController.Remove(this.vignetteData);
	}

	private void AddMods(ChunkLoader cl){
		cl.playerMovement.AddModifier(MovePresetProperty.DRAG, this.penaltyDrag);
		cl.playerMovement.AddModifier(MovePresetProperty.MAX_NATURAL_SPEED, this.penaltyMaxSpeed);
		cl.playerMovement.AddModifier(MovePresetProperty.GRAVITY_ACCELERATION, this.penaltyGravityAcceleration);
		cl.playerMovement.AddModifier(MovePresetProperty.GRAVITY_MAX_ACCELERATION_TIME, this.penaltyMaxGravityAccelerationTime);
		cl.playerMovement.AddModifier(MovePresetProperty.JUMP_HEIGHT, this.penaltyJumpHeight);
		cl.playerMovement.AddModifier(MovePresetProperty.POV_ADJUSTMENT, this.penaltyPOV);
	}

	private void RemoveMods(ChunkLoader cl){
		cl.playerMovement.RemoveModifier(MovePresetProperty.DRAG, this.penaltyDrag);
		cl.playerMovement.RemoveModifier(MovePresetProperty.MAX_NATURAL_SPEED, this.penaltyMaxSpeed);
		cl.playerMovement.RemoveModifier(MovePresetProperty.GRAVITY_ACCELERATION, this.penaltyGravityAcceleration);
		cl.playerMovement.RemoveModifier(MovePresetProperty.GRAVITY_MAX_ACCELERATION_TIME, this.penaltyMaxGravityAccelerationTime);
		cl.playerMovement.RemoveModifier(MovePresetProperty.JUMP_HEIGHT, this.penaltyJumpHeight);
		cl.playerMovement.RemoveModifier(MovePresetProperty.POV_ADJUSTMENT, this.penaltyPOV);
	}

	// Custom Place operation with Raycasting class overwrite
	// The new voxel is sent to clients at the end of the frame, merged with the rest of the flow
	public override int OnPlace(ChunkPos pos, int x, int y, int z, int facing, ChunkLoader_Server cl){
		CastCoord thisPos = new CastCoord(pos, x, y, z);
		cl.GetChunk(pos).data.SetCell(x, y, z, this.liquidCode);

		cl.budscheduler.ScheduleBUD(new BUDSignal(BUDCode.CHANGE, thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), facing), this.viscosityDelay);

		// If has been placed by player
		if(facing >= 0){
			cl.GetChunk(thisPos.GetChunkPos()).metadata.Reset(x,y,z);
			cl.server.QueueChunkChanges(thisPos.GetChunkPos());
			return 0;
		}

		cl.budscheduler.ScheduleSave(thisPos.GetChunkPos());
		cl.server.QueueChunkChanges(pos);
		EmitWaterBUD(thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), cl);
		return 0;
	}

	// Custom Break operation with Raycasting class overwrite
	public override int OnBreak(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){
		cl.GetChunk(pos).data.SetCell(x, y, z, 0);
		cl.GetChunk(pos).metadata.Reset(x,y,z);

		cachedCoord = new CastCoord(pos, x, y, z);

		// Reloads surrounding data when was manually broken by player
		if(!this.breakFLAG)
			GetCodeAround(cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), cl);
		
		this.Update(cachedCoord, BUDCode.BREAK, -1, cl);
		cl.budscheduler.ScheduleSave(cachedCoord.GetChunkPos());
		EmitWaterBUD(cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), cl);
		this.breakFLAG = false;

		return 0;
	}

	// Emits BUD Signal to Water Blocks around this one
	// Diagonals are included, since Level 1 states may depend on diagonal neighbors
	private void EmitWaterBUD(int myX, int myY, int myZ, ChunkLoader_Server cl){
		GetCodeAround(myX, myY, myZ, cl);

		// Horizontal neighbors
		for(int i=0; i<8; i++){
			if(aroundCodes[i] == this.liquidCode){
				cachedCoord = new CastCoord(this.GetNeighborBlock(i, myX, myY, myZ));
				cachedBUD = new BUDSignal(BUDCode.CHANGE, cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), -1);
				cl.budscheduler.ScheduleBUD(cachedBUD, this.viscosityDelay);
			}
		}

		// Below
		if(GetCodeBelow(myX, myY, myZ, cl) == this.liquidCode){
			cachedBUD = new BUDSignal(BUDCode.CHANGE, myX, myY-1, myZ, myX, myY-1, myZ, -1);
			cl.budscheduler.ScheduleBUD(cachedBUD, this.viscosityDelay);
		}

		// Above
		if(GetCodeAbove(myX, myY, myZ, cl) == this.liquidCode){
			cachedBUD = new BUDSignal(BUDCode.CHANGE, myX, myY+1, myZ, myX, myY+1, myZ, -1);
			cl.budscheduler.ScheduleBUD(cachedBUD, this.viscosityDelay);
		}
	}

	// Translates around dir to block coordinates in World Space
	private Vector3 GetNeighborBlock(int dir, int myX, int myY, int myZ){
		if(dir == 0)
			return new Vector3(myX, myY, myZ+1);
		else if(dir == 1)
			return new Vector3(myX+1, myY, myZ+1);
		else if(dir == 2)
			return new Vector3(myX+1, myY, myZ);
		else if(dir == 3)
			return new Vector3(myX+1, myY, myZ-1);
		else if(dir == 4)
			return new Vector3(myX, myY, myZ-1);
		else if(dir == 5)
			return new Vector3(myX-1, myY, myZ-1);
		else if(dir == 6)
			return new Vector3(myX-1, myY, myZ);
		else if(dir == 7)
			return new Vector3(myX-1, myY, myZ+1);
		else if(dir == 8)
			return new Vector3(myX, myY-1, myZ);
		else if(dir == 9)
			return new Vector3(myX, myY+1, myZ);
		else{
			return new Vector3(myX, myY, myZ);
		}
	}

	// Applies Water Movement
	public override void OnBlockUpdate(BUDCode type, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing, ChunkLoader_Server cl){
		if(type == BUDCode.BREAK || type == BUDCode.CHANGE){
			CastCoord thisPos = new CastCoord(new Vector3(myX, myY, myZ));
			ushort state = cl.GetChunk(thisPos.GetChunkPos()).metadata.GetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ);

			/*
			Still Level 3
			*/
			if(state == 0){
				ushort below = GetCodeBelow(thisPos, cl);
				ushort belowState = GetStateBelow(thisPos, cl);
				GetCodeAround(myX, myY, myZ, cl, main:true);
				GetStateAround(myX, myY, myZ, cl, main:true);

				if(IsOutOfBounds())
					return;

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should expand downwards
				if(below == 0 || (below == this.liquidCode && ShouldStateOverpower(state, belowState)) || IsWashable(below, cl)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// If there are at least one adjascent Still Water 3 -> Expand falling blocks
					if(GetSameLevelAroundCount(myX, myY, myZ, 3, cl) > 0){

						// Should break washable block below
						if(IsWashable(below, cl)){
							if(below <= ushort.MaxValue/2)
								VoxelLoader.GetBlock(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
							else
								VoxelLoader.GetObject(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						}

						cl.GetChunk(newPos.GetChunkPos()).data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
						cl.GetChunk(newPos.GetChunkPos()).metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 19);
						this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1, cl);
						return;
					}
					// If this block is a lone water one -> Fall by itself
					else{
						// Should break washable block below
						if(IsWashable(below, cl)){
							if(below <= ushort.MaxValue/2)
								VoxelLoader.GetBlock(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
							else
								VoxelLoader.GetObject(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						}

						this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
						cl.GetChunk(newPos.GetChunkPos()).data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
						cl.GetChunk(newPos.GetChunkPos()).metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 0);
						this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1, cl);
						return;
					}
				}
				// Normal Behaviour
				else{
					bool found;
					ushort targetState = 0;

					for(int i=0; i < 8; i+=2){
						found = false;
						GetDirectionPos(myX, myY, myZ, i);
						targetState = GetNewState(state, i);

						// If is air
						if(mainAroundCodes[i] == 0){
							found = true;
						}
						// If is washable
						else if(IsWashable(mainAroundCodes[i], cl)){
							found = true;

							if(mainAroundCodes[i] <= ushort.MaxValue/2)
								VoxelLoader.GetBlock(mainAroundCodes[i]).OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
							else
								VoxelLoader.GetObject(mainAroundCodes[i]).OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						}
						// If is water
						else if(mainAroundCodes[i] == liquidCode && ShouldStateOverpower(targetState, mainAroundStates[i])){
							if(targetState != ushort.MaxValue)
								found = true;
						}

						// Found cases
						if(found){
							cl.GetChunk(cachedPos.GetChunkPos()).data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.liquidCode);
							cl.GetChunk(cachedPos.GetChunkPos()).metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
							this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
						}

					}

				}
			}

			/*
			Still Level 2
			*/
			else if(state == 1){
				ushort below = GetCodeBelow(thisPos, cl);
				ushort belowState = GetStateBelow(thisPos, cl);
				GetCodeAround(myX, myY, myZ, cl, main:true);
				GetStateAround(myX, myY, myZ, cl, main:true);

				if(IsOutOfBounds())
					return;

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should upgrade to Still Level 3
				if(below == this.liquidCode && belowState == 1){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					cl.GetChunk(newPos.GetChunkPos()).data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
					cl.GetChunk(newPos.GetChunkPos()).metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 0);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1, cl);					
					return;
				}

				// If should expand downwards
				if(below == 0 || (below == this.liquidCode && ShouldStateOverpower(state, belowState)) || IsWashable(below, cl)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// If there are at least one adjascent Still Water 2 -> Expand falling blocks
					if(GetSameLevelAroundCount(myX, myY, myZ, 2, cl) > 0){
						// Should break washable block below
						if(IsWashable(below, cl)){
							if(below <= ushort.MaxValue/2)
								VoxelLoader.GetBlock(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
							else
								VoxelLoader.GetObject(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						}

						PlaceFalling(newPos, 2, cl);
						return;
					}
					// If this block is a lone water one -> Fall by itself
					else{
						// Should break washable block below
						if(IsWashable(below, cl)){
							if(below <= ushort.MaxValue/2)
								VoxelLoader.GetBlock(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
							else
								VoxelLoader.GetObject(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						}

						this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
						cl.GetChunk(newPos.GetChunkPos()).data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
						cl.GetChunk(newPos.GetChunkPos()).metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 1);
						this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1, cl);
						return;
					}
				}
				// Normal Behaviour
				else{
					bool found;
					ushort targetState = 0;

					for(int i=0; i < 8; i+=2){
						found = false;
						GetDirectionPos(myX, myY, myZ, i);
						targetState = GetNewState(state, i);

						// If is air
						if(mainAroundCodes[i] == 0){
							found = true;
						}
						// If is washable
						else if(IsWashable(mainAroundCodes[i], cl)){
							found = true;

							if(mainAroundCodes[i] <= ushort.MaxValue/2)
								VoxelLoader.GetBlock(mainAroundCodes[i]).OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
							else
								VoxelLoader.GetObject(mainAroundCodes[i]).OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						}
						// If is water
						else if(mainAroundCodes[i] == liquidCode && ShouldStateOverpower(targetState, mainAroundStates[i])){
							if(targetState != ushort.MaxValue)
								found = true;
						}

						// Found cases
						if(found){
							cl.GetChunk(cachedPos.GetChunkPos()).data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.liquidCode);
							cl.GetChunk(cachedPos.GetChunkPos()).metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
							this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
						}

					}

				}
			}

			/*
			Still Level 1
			*/
			else if(state == 2){
				ushort below = GetCodeBelow(thisPos, cl);
				ushort belowState = GetStateBelow(thisPos, cl);
				GetCodeAround(myX, myY, myZ, cl, main:true);
				GetStateAround(myX, myY, myZ, cl, main:true);

				if(IsOutOfBounds())
					return;

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should upgrade to Still Level 2
				if(below == this.liquidCode && belowState == 2){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					cl.GetChunk(newPos.GetChunkPos()).data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
					cl.GetChunk(newPos.GetChunkPos()).metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 1);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1, cl);					
					return;
				}

				// If should fall
				if(below == 0 || (below == this.liquidCode && ShouldStateOverpower(state, belowState)) || IsWashable(below, cl)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below, cl)){
						if(below <= ushort.MaxValue/2)
							VoxelLoader.GetBlock(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							VoxelLoader.GetObject(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					cl.GetChunk(newPos.GetChunkPos()).data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
					cl.GetChunk(newPos.GetChunkPos()).metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 2);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1, cl);
					return;
				}

				// If should die
				for(int i=0; i < 8; i+=2){
					if(mainAroundCodes[i] == 0){
						this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
						return;
					}
				}	
			}

			/*
			Directional Adjascent Level 2
			*/
			else if(state >= 3 && state <= 9 && state%2 == 1){
				ushort below = GetCodeBelow(thisPos, cl);
				ushort belowState = GetStateBelow(thisPos, cl);
				int3 cameFrom = cameFromDir[state];
				GetCodeAround(myX, myY, myZ, cl, main:true);
				GetStateAround(myX, myY, myZ, cl, main:true);

				if(IsOutOfBounds())
					return;

				// Dies if no Still Level 3 around
				if(GetSameLevelAroundCountSpec(myX, myY, myZ, 3, cl) <= 0){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					return;
				}

				// If should upgrade to Still Level 3
				if(GetSameLevelAroundCount(myX, myY, myZ, 3, cl) >= 2){
					cl.GetChunk(thisPos.GetChunkPos()).data.SetCell(thisPos.blockX, thisPos.blockY, thisPos.blockZ, this.liquidCode);
					cl.GetChunk(thisPos.GetChunkPos()).metadata.SetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ, 0);
					this.OnPlace(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, -1, cl);
					return;
				}

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should create falling blocks
				if(below == 0 || below == this.liquidCode && ShouldStateOverpower(20, belowState) || IsWashable(below, cl)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below, cl)){
						if(below <= ushort.MaxValue/2)
							VoxelLoader.GetBlock(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							VoxelLoader.GetObject(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					PlaceFalling(newPos, 2, cl);
					return;
				}

				// If is falling block pivot
				else if(below == this.liquidCode && (belowState == 20 || belowState == 22)){return;}

				// Normal Behaviour
				else{
					int i;
					ushort targetState;
					bool found;

					for(int j=0; j < 3; j++){
						i = spawnDirections[state][j];
						targetState = GetNewState(state, i);

						found = false;
						GetDirectionPos(myX, myY, myZ, i);

						// If is air
						if(mainAroundCodes[i] == 0){
							found = true;
						}
						// If is washable
						else if(IsWashable(mainAroundCodes[i], cl)){
							found = true;

							if(mainAroundCodes[i] <= ushort.MaxValue/2)
								VoxelLoader.GetBlock(mainAroundCodes[i]).OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
							else
								VoxelLoader.GetObject(mainAroundCodes[i]).OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						}
						// If is water
						else if(mainAroundCodes[i] == liquidCode && ShouldStateOverpower(targetState, mainAroundStates[i])){
							found = true;
						}

						// Found cases
						if(found){
							cl.GetChunk(cachedPos.GetChunkPos()).data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.liquidCode);
							cl.GetChunk(cachedPos.GetChunkPos()).metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
							this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
						}
					}
				}
			}

			/*
			Directional Diagonal Level 2
			*/
			else if(state >= 4 && state <= 10 && state%2 == 0){
				ushort below = GetCodeBelow(thisPos, cl);
				ushort belowState = GetStateBelow(thisPos, cl);
				int3 cameFrom = cameFromDir[state];
				GetCodeAround(myX, myY, myZ, cl, main:true);
				GetStateAround(myX, myY, myZ, cl, main:true);

				if(IsOutOfBounds())
					return;

				// Dies if no Still Level 3 around
				if((mainAroundCodes[cameFrom.x] != liquidCode || !cameFromState[state].Contains(mainAroundStates[cameFrom.x])) &&
					(mainAroundCodes[cameFrom.y] != liquidCode || !cameFromState[state].Contains(mainAroundStates[cameFrom.y]))){

					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					return;
				}

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should create falling blocks
				if(below == 0 || below == this.liquidCode && ShouldStateOverpower(20, belowState) || IsWashable(below, cl)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below, cl)){
						if(below <= ushort.MaxValue/2)
							VoxelLoader.GetBlock(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							VoxelLoader.GetObject(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					PlaceFalling(newPos, 2, cl);
					return;
				}

				// If is falling block pivot
				else if(below == this.liquidCode && (belowState == 20 || belowState == 22)){return;}

				// Normal Behaviour
				else{
					int i;
					ushort targetState;
					bool found;

					for(int j=0; j < 2; j++){
						i = spawnDirections[state][j];
						targetState = GetNewState(state, i);

						GetDirectionPos(myX, myY, myZ, i);
						found = false;

						// If is air
						if(mainAroundCodes[i] == 0){
							found = true;
						}
						// If is washable
						else if(IsWashable(mainAroundCodes[i], cl)){
							found = true;

							if(mainAroundCodes[i] <= ushort.MaxValue/2)
								VoxelLoader.GetBlock(mainAroundCodes[i]).OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
							else
								VoxelLoader.GetObject(mainAroundCodes[i]).OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						}
						// If is water
						else if(mainAroundCodes[i] == liquidCode && ShouldStateOverpower(targetState, mainAroundStates[i])){
							found = true;
						}

						// Found cases
						if(found){
							cl.GetChunk(cachedPos.GetChunkPos()).data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.liquidCode);
							cl.GetChunk(cachedPos.GetChunkPos()).metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
							this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
						}
					}			
				}
			}

			/*
			Directional Adjascent Level 1
			*/
			else if(state >= 11 && state <= 17 && state%2 == 1){
				ushort below = GetCodeBelow(thisPos, cl);
				ushort belowState = GetStateBelow(thisPos, cl);
				int3 cameFrom = cameFromDir[state];
				GetCodeAround(myX, myY, myZ, cl, main:true);
				GetStateAround(myX, myY, myZ, cl, main:true);

				if(IsOutOfBounds())
					return;

				// Dies if no Level 2 around
				if((mainAroundCodes[cameFrom.x] != liquidCode || !cameFromState[state].Contains(mainAroundStates[cameFrom.x])) &&
					(mainAroundCodes[cameFrom.y] != liquidCode || !cameFromState[state].Contains(mainAroundStates[cameFrom.y])) &&
					(mainAroundCodes[cameFrom.z] != liquidCode || !cameFromState[state].Contains(mainAroundStates[cameFrom.z]))){

					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					return;
				}

				// If should upgrade to Still Level 2
				if(GetSameLevelAroundCount(myX, myY, myZ, 2, cl) >= 2){
					cl.GetChunk(thisPos.GetChunkPos()).data.SetCell(thisPos.blockX, thisPos.blockY, thisPos.blockZ, this.liquidCode);
					cl.GetChunk(thisPos.GetChunkPos()).metadata.SetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ, 1);
					this.OnPlace(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, -1, cl);
					return;
				}

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should create falling blocks
				if(below == 0 || below == this.liquidCode && ShouldStateOverpower(21, belowState) || IsWashable(below, cl)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below, cl)){
						if(below <= ushort.MaxValue/2)
							VoxelLoader.GetBlock(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							VoxelLoader.GetObject(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					PlaceFalling(newPos, 1, cl);
					return;
				}

				// If is falling block pivot
				else if(below == this.liquidCode && (belowState == 21 || belowState == 23)){return;}

				// Normal Behaviour
				else{
					int i;
					ushort targetState;
					bool found;

					for(int j=0; j < 2; j++){
						i = spawnDirections[state][j];
						targetState = GetNewState(state, i);

						GetDirectionPos(myX, myY, myZ, i);
						found = false;

						// If is air
						if(mainAroundCodes[i] == 0){
							found = true;
						}
						// If is washable
						else if(IsWashable(mainAroundCodes[i], cl)){
							found = true;

							if(mainAroundCodes[i] <= ushort.MaxValue/2)
								VoxelLoader.GetBlock(mainAroundCodes[i]).OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
							else
								VoxelLoader.GetObject(mainAroundCodes[i]).OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						}
						// If is water
						else if(mainAroundCodes[i] == liquidCode && ShouldStateOverpower(targetState, mainAroundStates[i])){
							found = true;
						}

						// Found cases
						if(found){
							cl.GetChunk(cachedPos.GetChunkPos()).data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.liquidCode);
							cl.GetChunk(cachedPos.GetChunkPos()).metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
							this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
						}
					}					
				}
			}

			/*
			Directional Diagonal Level 1
			*/
			else if(state >= 12 && state <= 18 && state%2 == 0){
				ushort below = GetCodeBelow(thisPos, cl);
				ushort belowState = GetStateBelow(thisPos, cl);
				int3 cameFrom = cameFromDir[state];
				GetCodeAround(myX, myY, myZ, cl, main:true);
				GetStateAround(myX, myY, myZ, cl, main:true);

				if(IsOutOfBounds())
					return;

				// Dies if no Level 2 around
				if((mainAroundCodes[cameFrom.x] != liquidCode || !cameFromState[state].Contains(mainAroundStates[cameFrom.x])) &&
					(mainAroundCodes[cameFrom.y] != liquidCode || !cameFromState[state].Contains(mainAroundStates[cameFrom.y]))){

					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					return;
				}

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should create falling blocks
				if(below == 0 || below == this.liquidCode && ShouldStateOverpower(21, belowState) || IsWashable(below, cl)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below, cl)){
						if(below <= ushort.MaxValue/2)
							VoxelLoader.GetBlock(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							VoxelLoader.GetObject(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					PlaceFalling(newPos, 1, cl);
					return;
				}

				// If is falling block pivot
				else if(below == this.liquidCode && (belowState == 21 || belowState == 23)){return;}
			}

			/*
			Falling 3
			*/
			else if(state == 19){
				ushort below = GetCodeBelow(thisPos, cl);
				ushort belowState = GetStateBelow(thisPos, cl);
				ushort above = GetCodeAbove(thisPos, cl);
				ushort aboveState = GetStateAbove(thisPos, cl);

				// If should die
				if(above != this.liquidCode || (above == this.liquidCode && (aboveState != 0 && aboveState != 19))){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					return;					
				}

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should create new falling 3s
				if(below == 0 || below == this.liquidCode && ShouldStateOverpower(19, belowState) || IsWashable(below, cl)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below, cl)){
						if(below <= ushort.MaxValue/2)
							VoxelLoader.GetBlock(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							VoxelLoader.GetObject(below).OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					cl.GetChunk(newPos.GetChunkPos()).data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
					cl.GetChunk(newPos.GetChunkPos()).metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 19);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1, cl);
					return;							
				}

				// If should upgrade
				if(GetHighestDoubleLevelAround(myX, myY, myZ, cl) == 3){
					int highestLevel = GetHighestDoubleLevelAround(myX, myY, myZ, cl);
					CastCoord newPos = new CastCoord(new Vector3(myX, myY, myZ));

					cl.GetChunk(newPos.GetChunkPos()).metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 0);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1, cl);

					return;
				}

				// If is pivot to falling block
				if(below == this.liquidCode && above == this.liquidCode) {return;}

				// Normal Behaviour	
				bool found;
				ushort targetState = 0;

				GetCodeAround(myX, myY, myZ, cl, main:true);
				GetStateAround(myX, myY, myZ, cl, main:true);

				if(IsOutOfBounds())
					return;

				for(int i=0; i < 8; i+=2){
					found = false;
					GetDirectionPos(myX, myY, myZ, i);
					targetState = GetNewState(state, i);

					// If is air
					if(mainAroundCodes[i] == 0){
						found = true;
					}
					// If is washable
					else if(IsWashable(mainAroundCodes[i], cl)){
						found = true;

						if(mainAroundCodes[i] <= ushort.MaxValue/2)
							VoxelLoader.GetBlock(mainAroundCodes[i]).OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						else
							VoxelLoader.GetObject(mainAroundCodes[i]).OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
					}
					// If is water
					else if(mainAroundCodes[i] == liquidCode && ShouldStateOverpower(targetState, mainAroundStates[i])){
						if(targetState != ushort.MaxValue)
							found = true;
					}


					// Found cases
					if(found){
						cl.GetChunk(cachedPos.GetChunkPos()).data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.liquidCode);
						cl.GetChunk(cachedPos.GetChunkPos()).metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
						this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
					}
				}
			}

			/*
			Falling 2
			*/
			else if(state == 20){
				ushort below = GetCodeBelow(thisPos, cl);
				ushort belowState = GetStateBelow(thisPos, cl);
				ushort above = GetCodeAbove(thisPos, cl);
				ushort aboveState = GetStateAbove(thisPos, cl);

				// If should die
				if(!IsLevelTwoFeeder(above, aboveState)){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					return;
				}

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should create new falling 2s
				if(IsFallable(below, belowState, 20, cl)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					BreakWashable(below, newPos, cl);
					PlaceFalling(newPos, 2, cl);
					return;
				}

				// If should merge into Still Level 3
				if(GetHighestDoubleLevelAround(myX, myY, myZ, cl) == 3){
					cl.GetChunk(thisPos.GetChunkPos()).metadata.SetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ, 0);
					this.OnPlace(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, -1, cl);
					return;
				}

				// If is pivot to falling block or fell into liquid
				if(below == this.liquidCode)
					return;

				// If reached solid ground -> Keeps its level as Landed 2
				cl.GetChunk(thisPos.GetChunkPos()).metadata.SetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ, 22);
				this.OnPlace(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, -1, cl);
			}

			/*
			Falling 1
			*/
			else if(state == 21){
				ushort below = GetCodeBelow(thisPos, cl);
				ushort belowState = GetStateBelow(thisPos, cl);
				ushort above = GetCodeAbove(thisPos, cl);
				ushort aboveState = GetStateAbove(thisPos, cl);

				// If should die
				if(!IsLevelOneFeeder(above, aboveState)){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					return;
				}

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should create new falling 1s
				if(IsFallable(below, belowState, 21, cl)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					BreakWashable(below, newPos, cl);
					PlaceFalling(newPos, 1, cl);
					return;
				}

				// If should merge into Still Level 3
				if(GetHighestDoubleLevelAround(myX, myY, myZ, cl) == 3){
					cl.GetChunk(thisPos.GetChunkPos()).metadata.SetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ, 0);
					this.OnPlace(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, -1, cl);
					return;
				}

				// If is pivot to falling block or fell into liquid
				if(below == this.liquidCode)
					return;

				// If reached solid ground -> Keeps its level as Landed 1
				cl.GetChunk(thisPos.GetChunkPos()).metadata.SetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ, 23);
				this.OnPlace(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, -1, cl);
			}

			/*
			Landed 2
			*/
			else if(state == 22){
				ushort below = GetCodeBelow(thisPos, cl);
				ushort belowState = GetStateBelow(thisPos, cl);
				ushort above = GetCodeAbove(thisPos, cl);
				ushort aboveState = GetStateAbove(thisPos, cl);

				// If should die
				if(!IsLevelTwoFeeder(above, aboveState)){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					return;
				}

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If the ground below is gone -> Falls again
				if(IsFallable(below, belowState, 20, cl)){
					cl.GetChunk(thisPos.GetChunkPos()).metadata.SetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ, 20);
					this.OnPlace(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, -1, cl);
					return;
				}

				// Normal Behaviour
				GetCodeAround(myX, myY, myZ, cl, main:true);

				if(IsOutOfBounds())
					return;

				GetStateAround(myX, myY, myZ, cl, main:true);

				for(int i=0; i < 8; i+=2){
					SpreadTo(i, GetNewState(state, i), myX, myY, myZ, cl);
				}
			}

			/*
			Landed 1
			*/
			else if(state == 23){
				ushort below = GetCodeBelow(thisPos, cl);
				ushort belowState = GetStateBelow(thisPos, cl);
				ushort above = GetCodeAbove(thisPos, cl);
				ushort aboveState = GetStateAbove(thisPos, cl);

				// If should die
				if(!IsLevelOneFeeder(above, aboveState)){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					return;
				}

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If the ground below is gone -> Falls again
				if(IsFallable(below, belowState, 21, cl)){
					cl.GetChunk(thisPos.GetChunkPos()).metadata.SetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ, 21);
					this.OnPlace(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, -1, cl);
					return;
				}
			}
		}
	}

	// Get state of new propagation of water
	// Returns ushort.MaxValue if failed
	private ushort GetNewState(ushort state, int dir){
		if(state == 0 || state == 19){
			return (ushort)(3 + dir);
		}
		if(state == 1 || state == 22){
			return (ushort)(11 + dir);
		}
		else if(state == 3){
			switch(dir){
				case 6:
					return 10;
				case 0:
					return 11;
				case 2:
					return 4;
			}
		}
		else if(state == 5){
			switch(dir){
				case 0:
					return 4;
				case 2:
					return 13;
				case 4:
					return 6;
			}
		}
		else if(state == 7){
			switch(dir){
				case 2:
					return 6;
				case 4:
					return 15;
				case 6:
					return 8;
			}
		}
		else if(state == 9){
			switch(dir){
				case 4:
					return 8;
				case 6:
					return 17;
				case 0:
					return 10;
			}
		}
		else if(state == 4){
			switch(dir){
				case 0:
					return 11;
				case 2:
					return 13;
			}
		}
		else if(state == 6){
			switch(dir){
				case 2:
					return 13;
				case 4:
					return 15;
			}
		}
		else if(state == 8){
			switch(dir){
				case 4:
					return 15;
				case 6:
					return 17;
			}
		}
		else if(state == 10){
			switch(dir){
				case 6:
					return 17;
				case 0:
					return 11;
			}
		}
		else if(state == 11){
			switch(dir){
				case 2:
					return 12;
				case 6:
					return 18;
			}
		}
		else if(state == 13){
			switch(dir){
				case 4:
					return 14;
				case 0:
					return 12;
			}
		}
		else if(state == 15){
			switch(dir){
				case 2:
					return 14;
				case 6:
					return 16;
			}
		}
		else if(state == 17){
			switch(dir){
				case 4:
					return 16;
				case 0:
					return 18;
			}
		}

		return ushort.MaxValue;
	}

	// Sets the CachedPos to the coord of the direction given
	private void GetDirectionPos(int x, int y, int z, int dir){
		switch(dir){
			case 0:
				cachedPos = new CastCoord(new Vector3(x, y, z+1));
				break;
			case 1:
				cachedPos = new CastCoord(new Vector3(x+1, y, z+1));
				break;
			case 2:
				cachedPos = new CastCoord(new Vector3(x+1, y, z));
				break;
			case 3:
				cachedPos = new CastCoord(new Vector3(x+1, y, z-1));
				break;
			case 4:
				cachedPos = new CastCoord(new Vector3(x, y, z-1));
				break;
			case 5:
				cachedPos = new CastCoord(new Vector3(x-1, y, z-1));
				break;
			case 6:
				cachedPos = new CastCoord(new Vector3(x-1, y, z));
				break;
			case 7:
				cachedPos = new CastCoord(new Vector3(x-1, y, z+1));
				break;
			default:
				break;
		}

	}

	// Gets Code of block below
	private ushort GetCodeBelow(int myX, int myY, int myZ, ChunkLoader_Server cl){
		if(IsOutOfWorldY(myY-1))
			return (ushort)(ushort.MaxValue/2);

		CastCoord cord = new CastCoord(new Vector3(myX, myY-1, myZ));
		if(!cl.Contains(cord.GetChunkPos()))
			return (ushort)(ushort.MaxValue/2);

		return cl.GetChunk(cord.GetChunkPos()).data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
	}
	private ushort GetCodeBelow(CastCoord c, ChunkLoader_Server cl){
		return GetCodeBelow(c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), cl);
	}

	// Gets State of block below
	private ushort GetStateBelow(int myX, int myY, int myZ, ChunkLoader_Server cl){
		if(IsOutOfWorldY(myY-1))
			return (ushort)(ushort.MaxValue/2);

		CastCoord cord = new CastCoord(new Vector3(myX, myY-1, myZ));
		if(!cl.Contains(cord.GetChunkPos()))
			return (ushort)(ushort.MaxValue/2);

		return cl.GetChunk(cord.GetChunkPos()).metadata.GetState(cord.blockX, cord.blockY, cord.blockZ);
	}
	private ushort GetStateBelow(CastCoord c, ChunkLoader_Server cl){
		return GetStateBelow(c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), cl);
	}

	// Gets State of block above
	private ushort GetStateAbove(int myX, int myY, int myZ, ChunkLoader_Server cl){
		if(IsOutOfWorldY(myY+1))
			return (ushort)(ushort.MaxValue/2);

		CastCoord cord = new CastCoord(new Vector3(myX, myY+1, myZ));
		if(!cl.Contains(cord.GetChunkPos()))
			return (ushort)(ushort.MaxValue/2);

		return cl.GetChunk(cord.GetChunkPos()).metadata.GetState(cord.blockX, cord.blockY, cord.blockZ);
	}
	private ushort GetStateAbove(CastCoord c, ChunkLoader_Server cl){
		return GetStateAbove(c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), cl);
	}

	// Gets Code of block above
	private ushort GetCodeAbove(int myX, int myY, int myZ, ChunkLoader_Server cl){
		if(IsOutOfWorldY(myY+1))
			return (ushort)(ushort.MaxValue/2);

		CastCoord cord = new CastCoord(new Vector3(myX, myY+1, myZ));
		if(!cl.Contains(cord.GetChunkPos()))
			return (ushort)(ushort.MaxValue/2);

		return cl.GetChunk(cord.GetChunkPos()).data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
	}
	private ushort GetCodeAbove(CastCoord c, ChunkLoader_Server cl){
		return GetCodeAbove(c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), cl);
	}


	// Gets a list of block codes of around blocks
	// Order is N Clockwise
	// The value (ushort)(ushort.MaxValue/2) is considered the error code
	private void GetCodeAround(int myX, int myY, int myZ, ChunkLoader_Server cl, bool main=false){
		CastCoord cord;
		ushort[] selectedArray;

		if(main)
			selectedArray = mainAroundCodes;
		else
			selectedArray = aroundCodes;

		cord = new CastCoord(new Vector3(myX, myY, myZ+1)); // North
		if(cl.Contains(cord.GetChunkPos()))
			selectedArray[0] = cl.GetChunk(cord.GetChunkPos()).data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[0] = (ushort)(ushort.MaxValue/2);
		

		cord = new CastCoord(new Vector3(myX+1, myY, myZ+1)); // NE
		if(cl.Contains(cord.GetChunkPos()))
			selectedArray[1] = cl.GetChunk(cord.GetChunkPos()).data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[1] = (ushort)(ushort.MaxValue/2);
		

		cord = new CastCoord(new Vector3(myX+1, myY, myZ)); // East
		if(cl.Contains(cord.GetChunkPos()))
			selectedArray[2] = cl.GetChunk(cord.GetChunkPos()).data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[2] = (ushort)(ushort.MaxValue/2);
		
		
		cord = new CastCoord(new Vector3(myX+1, myY, myZ-1)); // SE
		if(cl.Contains(cord.GetChunkPos()))
			selectedArray[3] = cl.GetChunk(cord.GetChunkPos()).data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[3] = (ushort)(ushort.MaxValue/2);
		
		
		cord = new CastCoord(new Vector3(myX, myY, myZ-1)); // South
		if(cl.Contains(cord.GetChunkPos()))
			selectedArray[4] = cl.GetChunk(cord.GetChunkPos()).data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[4] = (ushort)(ushort.MaxValue/2);
		

		cord = new CastCoord(new Vector3(myX-1, myY, myZ-1)); // SW
		if(cl.Contains(cord.GetChunkPos()))
			selectedArray[5] = cl.GetChunk(cord.GetChunkPos()).data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[5] = (ushort)(ushort.MaxValue/2);
		

		cord = new CastCoord(new Vector3(myX-1, myY, myZ)); // West
		if(cl.Contains(cord.GetChunkPos()))
			selectedArray[6] = cl.GetChunk(cord.GetChunkPos()).data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[6] = (ushort)(ushort.MaxValue/2);
		

		cord = new CastCoord(new Vector3(myX-1, myY, myZ+1)); // NW
		if(cl.Contains(cord.GetChunkPos()))
			selectedArray[7] = cl.GetChunk(cord.GetChunkPos()).data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[7] = (ushort)(ushort.MaxValue/2);

	}

	// Gets a list of states of around blocks if they are water
	// Run this after GetCodeAround() on the same position, since it only reads neighbors that are loaded liquids
	private void GetStateAround(int myX, int myY, int myZ, ChunkLoader_Server cl, bool main=false){
		ushort[] codes;
		ushort[] selectedArray;
		CastCoord cast;

		if(main){
			codes = mainAroundCodes;
			selectedArray = mainAroundStates;
		}
		else{
			codes = aroundCodes;
			selectedArray = aroundStates;
		}

		for(int i=0; i < 8; i++){
			if(codes[i] != this.liquidCode){
				selectedArray[i] = ushort.MaxValue;
				continue;
			}

			cast = new CastCoord(GetNeighborBlock(i, myX, myY, myZ));
			selectedArray[i] = cl.GetChunk(cast.GetChunkPos()).metadata.GetState(cast.blockX, cast.blockY, cast.blockZ);
		}
	}

	// Gets Water Level based on state table
	private int TranslateWaterLevel(ushort state){
		if(state == ushort.MaxValue)
			return 0;
		else if(state == 2 || (state >= 11 && state <= 18) || state == 23)
			return 1;
		else if(state == 1 || (state >= 3 && state <= 10) || state == 22)
			return 2;
		else if(state == 0 || state == 19 || state == 20 || state == 21)
			return 3;
		else
			return 0;
	}

	// Gets Water Level based on state table without considering falling blocks a level 3
	private int TranslateWaterLevelSpec(ushort state){
		if(state == ushort.MaxValue)
			return 0;
		else if(state == 2 || (state >= 11 && state <= 18) || state == 23)
			return 1;
		else if(state == 1 || (state >= 3 && state <= 10) || state == 22)
			return 2;
		else if(state == 0)
			return 3;
		else
			return 0;		
	}

	// Checks the amount of same level water ONLY IN ADJASCENT blocks considering Falling Blocks
	private int GetSameLevelAroundCountSpec(int x, int y, int z, int currentWaterLevel, ChunkLoader_Server cl){
		int count=0;
		GetCodeAround(x,y,z,cl);
		GetStateAround(x,y,z,cl);

		if(currentWaterLevel == 1){
			for(int i=0; i<8; i+=2){
				if(aroundCodes[i] == this.liquidCode && (aroundStates[i] == 2 || aroundStates[i] == 21)){
					count++;
				}
			}
		}
		if(currentWaterLevel == 2){
			for(int i=0; i<8; i+=2){
				if(aroundCodes[i] == this.liquidCode && (aroundStates[i] == 1 || aroundStates[i] == 20)){
					count++;
				}
			}
		}
		if(currentWaterLevel == 3){
			for(int i=0; i<8; i+=2){
				if(aroundCodes[i] == this.liquidCode && (aroundStates[i] == 0 || aroundStates[i] == 19)){
					count++;
				}
			}
		}

		return count;
	}

	// Checks the amount of same level water ONLY IN ADJASCENT blocks disconsidering Falling Blocks
	private int GetSameLevelAroundCount(int x, int y, int z, int currentWaterLevel, ChunkLoader_Server cl){
		int count=0;
		GetCodeAround(x,y,z,cl);
		GetStateAround(x,y,z,cl);

		if(currentWaterLevel == 1){
			for(int i=0; i<8; i+=2){
				if(aroundCodes[i] == this.liquidCode && aroundStates[i] == 2){
					count++;
				}
			}
		}
		if(currentWaterLevel == 2){
			for(int i=0; i<8; i+=2){
				if(aroundCodes[i] == this.liquidCode && aroundStates[i] == 1){
					count++;
				}
			}
		}
		if(currentWaterLevel == 3){
			for(int i=0; i<8; i+=2){
				if(aroundCodes[i] == this.liquidCode && aroundStates[i] == 0){
					count++;
				}
			}
		}

		return count;
	}

	// Gets the highest state level around only if there are at least two blocks of that level
	private int GetHighestDoubleLevelAround(int x, int y, int z, ChunkLoader_Server cl){
		int countOne=0;
		int countTwo=0;
		int countThree=0;

		GetCodeAround(x,y,z,cl);
		GetStateAround(x,y,z,cl);

		for(int i=0; i<8; i+=2){
			if(aroundCodes[i] == this.liquidCode){
				switch(TranslateWaterLevelSpec(aroundStates[i])){
					case 0:
						continue;
					case 1:
						countOne++;
						continue;
					case 2:
						countTwo++;
						continue;
					case 3:
						countThree++;
						continue;
					default:
						continue;
				}
			}
		}

		if(countThree >= 2)
			return 3;
		if(countTwo >= 2)
			return 2;
		if(countOne >= 1)
			return 1;

		return 0;
	}

	// Check if given blockCode is washable
	private bool IsWashable(ushort blockCode, ChunkLoader_Server cl){
		return VoxelLoader.CheckWashable(blockCode);
	}	

    // Sends the changed voxels of the chunk to users at the end of the frame
	public void Update(CastCoord c, BUDCode type, int facing, ChunkLoader_Server cl){
		cl.server.QueueChunkChanges(c.GetChunkPos());
	}

	// Checks if current state overpowers the priority of target state
	private bool ShouldStateOverpower(ushort current, ushort target){
		if(!statePriority.ContainsKey(current) || !statePriority.ContainsKey(target))
			return false;

		return statePriority[current] > statePriority[target];
	}

	// Checks if Water Update is out of bounds
	// Run this after running GetCodeAround() with main:true
	private bool IsOutOfBounds(){
		for(int i=0; i < 8; i++){
			if(mainAroundCodes[i] == ushort.MaxValue/2)
				return true;
		}

		return false;
	}

	// Checks if a world Y coordinate is outside of the world
	private bool IsOutOfWorldY(int y){
		return y < 0 || y >= (Chunk.chunkMaxY+1)*Chunk.chunkDepth;
	}

	// Checks if a falling liquid with the given state can move into a voxel
	private bool IsFallable(ushort code, ushort state, ushort fallingState, ChunkLoader_Server cl){
		if(code == 0)
			return true;
		if(code == this.liquidCode)
			return ShouldStateOverpower(fallingState, state);
		if(code == (ushort)(ushort.MaxValue/2))
			return false;

		return IsWashable(code, cl);
	}

	// Checks if the voxel above a Falling 2 or Landed 2 keeps feeding it
	private bool IsLevelTwoFeeder(ushort code, ushort state){
		return code == this.liquidCode && (state == 1 || state == 20 || (state >= 3 && state <= 10));
	}

	// Checks if the voxel above a Falling 1 or Landed 1 keeps feeding it
	private bool IsLevelOneFeeder(ushort code, ushort state){
		return code == this.liquidCode && (state == 2 || state == 21 || (state >= 11 && state <= 18));
	}

	// Breaks the washable block or object a liquid is about to replace
	private void BreakWashable(ushort code, CastCoord pos, ChunkLoader_Server cl){
		if(code == 0 || code == this.liquidCode || code == (ushort)(ushort.MaxValue/2) || !IsWashable(code, cl))
			return;

		if(code <= ushort.MaxValue/2)
			VoxelLoader.GetBlock(code).OnBreak(pos.GetChunkPos(), pos.blockX, pos.blockY, pos.blockZ, cl);
		else
			VoxelLoader.GetObject(code).OnBreak(pos.GetChunkPos(), pos.blockX, pos.blockY, pos.blockZ, cl);
	}

	// Creates a Falling 2 or Falling 1 (level) in a voxel that this liquid can fall into
	// It becomes a Landed state if the voxel below it is solid ground, so it keeps the level it had before falling
	private void PlaceFalling(CastCoord pos, int level, ChunkLoader_Server cl){
		ushort fallingState = (ushort)(level == 2 ? 20 : 21);
		ushort landedState = (ushort)(level == 2 ? 22 : 23);
		ushort below = GetCodeBelow(pos, cl);
		ushort newState;

		// Keeps falling, meets a liquid or reaches unloaded chunks
		if(below == this.liquidCode || below == (ushort)(ushort.MaxValue/2) || IsFallable(below, GetStateBelow(pos, cl), fallingState, cl))
			newState = fallingState;
		else
			newState = landedState;

		cl.GetChunk(pos.GetChunkPos()).data.SetCell(pos.blockX, pos.blockY, pos.blockZ, this.liquidCode);
		cl.GetChunk(pos.GetChunkPos()).metadata.SetState(pos.blockX, pos.blockY, pos.blockZ, newState);
		this.OnPlace(pos.GetChunkPos(), pos.blockX, pos.blockY, pos.blockZ, -1, cl);
	}

	// Spreads this liquid with the given state to the horizontal neighbor in a direction
	// Run this after GetCodeAround() and GetStateAround() with main:true
	private void SpreadTo(int dir, ushort targetState, int myX, int myY, int myZ, ChunkLoader_Server cl){
		ushort code = mainAroundCodes[dir];
		bool found = false;

		if(targetState == ushort.MaxValue)
			return;

		GetDirectionPos(myX, myY, myZ, dir);

		// If is air
		if(code == 0){
			found = true;
		}
		// If is water
		else if(code == this.liquidCode){
			found = ShouldStateOverpower(targetState, mainAroundStates[dir]);
		}
		// If is washable
		else if(code != (ushort)(ushort.MaxValue/2) && IsWashable(code, cl)){
			found = true;
			BreakWashable(code, cachedPos, cl);
		}

		if(!found)
			return;

		cl.GetChunk(cachedPos.GetChunkPos()).data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.liquidCode);
		cl.GetChunk(cachedPos.GetChunkPos()).metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
		this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
	}
}