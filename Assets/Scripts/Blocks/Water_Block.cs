using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
WATER STATES:

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
*/

public class Water_Block : Blocks
{
	// Unique
	public ushort waterCode;
	public ushort[] aroundCodes;
	public ushort[] aroundStates;
	public CastCoord cachedPos;
	private CastCoord cachedCoord;
	private BUDSignal cachedBUD;
	private bool breakFLAG = false;
	private NetMessage reloadMessage;

	private int viscosityDelay;

	private Dictionary<ushort?, List<int>> spawnDirections = new Dictionary<ushort?, List<int>>();
	private Dictionary<ushort, List<ushort>> spawnStateAdjascents = new Dictionary<ushort, List<ushort>>();
	private Dictionary<ushort, int> stateDirection = new Dictionary<ushort, int>();
	private Dictionary<ushort, byte> statePriority = new Dictionary<ushort, byte>();
	private Dictionary<ushort, int> cameFrom = new Dictionary<ushort, int>();

	// Just loaded block
	public Water_Block(){
		this.shaderIndex = ShaderIndex.WATER; // Liquid
		this.name = "Water";
		this.solid = false;
		this.transparent = 1;
		this.invisible = false;
		this.liquid = true;
		this.waterCode = (ushort)BlockID.WATER;
		this.customBreak = true;
		this.customPlace = true;
		this.hasLoadEvent = false;
		this.affectLight = true;
		this.seamless = true;
		this.drawRegardless = true;
		this.viscosityDelay = 12;
		this.maxHP = 1;
		this.flags = new HashSet<BlockFlags>(){BlockFlags.IMMUNE};

		this.aroundCodes = new ushort[8];
		this.aroundStates = new ushort[8];

		// Water Spawn Directions
		this.spawnDirections.Add(3, new List<int>(new int[]{6,0,2}));
		this.spawnDirections.Add(5, new List<int>(new int[]{0,2,4}));
		this.spawnDirections.Add(7, new List<int>(new int[]{2,4,6}));
		this.spawnDirections.Add(9, new List<int>(new int[]{4,6,0}));
		this.spawnDirections.Add(4, new List<int>(new int[]{0,2}));
		this.spawnDirections.Add(6, new List<int>(new int[]{2,4}));
		this.spawnDirections.Add(8, new List<int>(new int[]{4,6}));
		this.spawnDirections.Add(10, new List<int>(new int[]{6,0}));

		// Water states priority
		this.statePriority.Add(0, 6);
		this.statePriority.Add(1, 3);
		this.statePriority.Add(2, 0);
		this.statePriority.Add(3, 5);
		this.statePriority.Add(4, 4);
		this.statePriority.Add(5, 5);
		this.statePriority.Add(6, 4);
		this.statePriority.Add(7, 5);
		this.statePriority.Add(8, 4);
		this.statePriority.Add(9, 5);
		this.statePriority.Add(10, 4);
		this.statePriority.Add(11, 2);
		this.statePriority.Add(12, 1);
		this.statePriority.Add(13, 2);
		this.statePriority.Add(14, 1);
		this.statePriority.Add(15, 2);
		this.statePriority.Add(16, 1);
		this.statePriority.Add(17, 2);
		this.statePriority.Add(18, 1);
		this.statePriority.Add(19, 6);
		this.statePriority.Add(20, 6);
		this.statePriority.Add(21, 6);

		// Adds to CameFrom dictionary
		this.cameFrom.Add(3, 4);
		this.cameFrom.Add(4, 5);
		this.cameFrom.Add(5, 6);
		this.cameFrom.Add(6, 7);
		this.cameFrom.Add(7, 0);
		this.cameFrom.Add(8, 1);
		this.cameFrom.Add(9, 2);
		this.cameFrom.Add(10, 3);
		this.cameFrom.Add(11, 4);
		this.cameFrom.Add(12, 5);
		this.cameFrom.Add(13, 6);
		this.cameFrom.Add(14, 7);
		this.cameFrom.Add(15, 0);
		this.cameFrom.Add(16, 1);
		this.cameFrom.Add(17, 2);
		this.cameFrom.Add(18, 3);
	}

	// Custom Place operation with Raycasting class overwrite
	public override int OnPlace(ChunkPos pos, int x, int y, int z, int facing, ChunkLoader_Server cl){
		CastCoord thisPos = new CastCoord(pos, x, y, z);
		NetMessage message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
		message.DirectBlockUpdate(BUDCode.PLACE, pos, thisPos.blockX, thisPos.blockY, thisPos.blockZ, facing, this.waterCode, cl.chunks[thisPos.GetChunkPos()].metadata.GetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ), cl.chunks[thisPos.GetChunkPos()].metadata.GetHP(thisPos.blockX, thisPos.blockY, thisPos.blockZ));
		
		cl.budscheduler.ScheduleBUD(new BUDSignal(BUDCode.CHANGE, thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), facing), this.viscosityDelay);

		// If has been placed by player
		if(facing >= 0){
			cl.chunks[thisPos.GetChunkPos()].metadata.Reset(x,y,z);
			cl.server.SendToClients(thisPos.GetChunkPos(), message);
			return 0;
		}

		this.Update(thisPos, BUDCode.CHANGE, -1, cl);
		cl.budscheduler.ScheduleSave(thisPos.GetChunkPos());
		return 0;
	}

	// Custom Break operation with Raycasting class overwrite
	public override int OnBreak(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){
		cl.chunks[pos].data.SetCell(x, y, z, 0);
		cl.chunks[pos].metadata.Reset(x,y,z);

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

	// Applies Water Movement
	public override void OnBlockUpdate(BUDCode type, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing, ChunkLoader_Server cl){
		if(type == BUDCode.BREAK || type == BUDCode.CHANGE){
			CastCoord thisPos = new CastCoord(new Vector3(myX, myY, myZ));
			ushort state = cl.chunks[thisPos.GetChunkPos()].metadata.GetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ);

			/*
			Still Level 3
			*/
			if(state == 0){
				ushort below = GetCodeBelow(thisPos, cl);
				ushort belowState = GetStateBelow(thisPos, cl);

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should expand downwards
				if(below == 0 || (below == this.waterCode && ShouldStateOverpower(state, belowState)) || IsWashable(below, cl)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// If there are at least one adjascent Still Water 3 -> Expand falling blocks
					if(GetSameLevelAroundCount(myX, myY, myZ, 3, cl) > 0){
						// Should break washable block below
						if(IsWashable(below, cl)){
							if(below <= ushort.MaxValue/2)
								cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
							else
								cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						}

						cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.waterCode);
						cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 19);
						this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1, cl);
						return;
					}
					// If this block is a lone water one -> Fall by itself
					else{
						// Should break washable block below
						if(IsWashable(below, cl)){
							if(below <= ushort.MaxValue/2)
								cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
							else
								cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						}

						this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
						cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.waterCode);
						cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 0);
						this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1, cl);
						return;
					}
				}
				// Normal Behaviour
				else{
					GetCodeAround(myX, myY, myZ, cl);
					GetStateAround(myX, myY, myZ, cl);

					bool found;
					ushort targetState = 0;

					for(int i=0; i < 8; i+=2){
						found = false;
						GetDirectionPos(myX, myY, myZ, i);
						targetState = GetNewState(state, i);

						// If is air
						if(this.aroundCodes[i] == 0){
							found = true;
						}
						// If is washable
						else if(IsWashable(this.aroundCodes[i], cl)){
							found = true;

							if(this.aroundCodes[i] <= ushort.MaxValue/2)
								cl.blockBook.blocks[this.aroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
							else
								cl.blockBook.objects[ushort.MaxValue - this.aroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						}
						// If is water
						else if(this.aroundCodes[i] == waterCode && ShouldStateOverpower(state, this.aroundStates[i])){
							if(targetState != ushort.MaxValue)
								found = true;
						}


						// Found cases
						if(found){
							GetDirectionPos(myX, myY, myZ, i);
							cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.waterCode);
							cl.chunks[cachedPos.GetChunkPos()].metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
							this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
						}

					}

				}
			}

			/*
			Directional Adjascent Level 2
			*/
			else if(state >= 3 && state <= 9 && state%2 == 1){
				ushort below = GetCodeBelow(thisPos, cl);
				ushort belowState = GetStateBelow(thisPos, cl);
				GetCodeAround(myX, myY, myZ, cl);
				GetStateAround(myX, myY, myZ, cl);

				// Dies if no Still Level 3 around
				if(this.aroundCodes[cameFrom[state]] != waterCode || this.aroundStates[cameFrom[state]] != 0){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					return;
				}

				// If should upgrade to Still Level 3
				if(GetSameLevelAroundCount(myX, myY, myZ, 3, cl) >= 2){
					cl.chunks[thisPos.GetChunkPos()].data.SetCell(thisPos.blockX, thisPos.blockY, thisPos.blockZ, this.waterCode);
					cl.chunks[thisPos.GetChunkPos()].metadata.SetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ, 0);
					this.OnPlace(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, -1, cl);
					return;
				}

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should do nothing because already created a falling block
				if(below == this.waterCode && !ShouldStateOverpower(state, belowState)){}

				// If should create falling blocks
				else if(below == 0 || below == this.waterCode && ShouldStateOverpower(state, belowState) || IsWashable(below, cl)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below, cl)){
						if(below <= ushort.MaxValue/2)
							cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.waterCode);
					cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 20);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1, cl);
					return;					
				}

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
						if(this.aroundCodes[i] == 0){
							found = true;
						}
						// If is washable
						else if(IsWashable(this.aroundCodes[i], cl)){
							found = true;

							if(this.aroundCodes[i] <= ushort.MaxValue/2)
								cl.blockBook.blocks[this.aroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
							else
								cl.blockBook.objects[ushort.MaxValue - this.aroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						}
						// If is water
						else if(this.aroundCodes[i] == waterCode && ShouldStateOverpower(state, this.aroundStates[i])){
							found = true;
						}


						// Found cases
						if(found){
							GetDirectionPos(myX, myY, myZ, i);
							cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.waterCode);
							cl.chunks[cachedPos.GetChunkPos()].metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
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
				GetCodeAround(myX, myY, myZ, cl);
				GetStateAround(myX, myY, myZ, cl);

				// Dies if no Still Level 3 around
				if(this.aroundCodes[cameFrom[state]] != waterCode || this.aroundStates[cameFrom[state]] != 0){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					return;
				}

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should do nothing because already created a falling block
				if(below == this.waterCode && !ShouldStateOverpower(state, belowState)){}

				// If should create falling blocks
				else if(below == 0 || below == this.waterCode && ShouldStateOverpower(state, belowState) || IsWashable(below, cl)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below, cl)){
						if(below <= ushort.MaxValue/2)
							cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.waterCode);
					cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 20);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1, cl);
					return;					
				}

				// Normal Behaviour
				else{
					int i;
					ushort targetState;
					bool found;

					for(int j=0; j < 2; j++){
						i = spawnDirections[state][j];
						targetState = GetNewState(state, i);
						found = false;

						// If is air
						if(this.aroundCodes[i] == 0){
							found = true;
						}
						// If is washable
						else if(IsWashable(this.aroundCodes[i], cl)){
							found = true;
							GetDirectionPos(myX, myY, myZ, i);

							if(this.aroundCodes[i] <= ushort.MaxValue/2)
								cl.blockBook.blocks[this.aroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
							else
								cl.blockBook.objects[ushort.MaxValue - this.aroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						}
						// If is water
						else if(this.aroundCodes[i] == waterCode && ShouldStateOverpower(state, this.aroundStates[i])){
							found = true;
						}


						// Found cases
						if(found){
							GetDirectionPos(myX, myY, myZ, i);
							cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.waterCode);
							cl.chunks[cachedPos.GetChunkPos()].metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
							this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
						}
					}			
				}
			}

			/*
			Directional Adjascent Level 1
			*/
			else if(state >= 11 && state <= 17 && state%2 == 1){

			}
		}
	}

	// Get state of new propagation of water
	// Returns ushort.MaxValue if failed
	private ushort GetNewState(ushort state, int dir){
		if(state == 0){
			return (ushort)(3 + dir);
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


		return ushort.MaxValue;
	}

	// Check if is upgradable
	private bool IsUpgradeable(ushort currentWaterLevel, ushort targetState){
		if(currentWaterLevel == 3 && targetState >= 3 && targetState <= 10 && targetState%2 == 1)
			return true;
		else if(currentWaterLevel == 2 && targetState >= 11 && targetState <= 18 && targetState%2 == 1)
			return true;
		return false;
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
		CastCoord cord = new CastCoord(new Vector3(myX, myY-1, myZ));
		if(myY-1 < 0)
			return (ushort)(ushort.MaxValue/2);

		return cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
	}
	private ushort GetCodeBelow(CastCoord c, ChunkLoader_Server cl){
		return GetCodeBelow(c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), cl);
	}

	// Gets State of block below
	private ushort GetStateBelow(int myX, int myY, int myZ, ChunkLoader_Server cl){
		CastCoord cord = new CastCoord(new Vector3(myX, myY-1, myZ));
		if(myY-1 < 0)
			return (ushort)(ushort.MaxValue/2);

		return cl.chunks[cord.GetChunkPos()].metadata.GetState(cord.blockX, cord.blockY, cord.blockZ);
	}
	private ushort GetStateBelow(CastCoord c, ChunkLoader_Server cl){
		return GetStateBelow(c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), cl);
	}

	// Gets State of block above
	private ushort GetStateAbove(int myX, int myY, int myZ, ChunkLoader_Server cl){
		CastCoord cord = new CastCoord(new Vector3(myX, myY+1, myZ));
		if(myY+1 < Chunk.chunkDepth)
			return (ushort)(ushort.MaxValue/2);

		return cl.chunks[cord.GetChunkPos()].metadata.GetState(cord.blockX, cord.blockY, cord.blockZ);
	}

	// Gets Code of block above
	private ushort GetCodeAbove(int myX, int myY, int myZ, ChunkLoader_Server cl){
		CastCoord cord = new CastCoord(new Vector3(myX, myY+1, myZ));
		if(myY+1 >= Chunk.chunkDepth)
			return (ushort)(ushort.MaxValue/2);

		return cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
	}


	// Gets a list of block codes of around blocks
	// Order is N Clockwise
	// The value (ushort)(ushort.MaxValue/2) is considered the error code
	private void GetCodeAround(int myX, int myY, int myZ, ChunkLoader_Server cl){
		CastCoord cord;

		cord = new CastCoord(new Vector3(myX, myY, myZ+1)); // North
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			this.aroundCodes[0] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			this.aroundCodes[0] = (ushort)(ushort.MaxValue/2);
		

		cord = new CastCoord(new Vector3(myX+1, myY, myZ+1)); // NE
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			this.aroundCodes[1] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			this.aroundCodes[1] = (ushort)(ushort.MaxValue/2);
		

		cord = new CastCoord(new Vector3(myX+1, myY, myZ)); // East
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			this.aroundCodes[2] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			this.aroundCodes[2] = (ushort)(ushort.MaxValue/2);
		
		
		cord = new CastCoord(new Vector3(myX+1, myY, myZ-1)); // SE
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			this.aroundCodes[3] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			this.aroundCodes[3] = (ushort)(ushort.MaxValue/2);
		
		
		cord = new CastCoord(new Vector3(myX, myY, myZ-1)); // South
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			this.aroundCodes[4] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			this.aroundCodes[4] = (ushort)(ushort.MaxValue/2);
		

		cord = new CastCoord(new Vector3(myX-1, myY, myZ-1)); // SW
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			this.aroundCodes[5] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			this.aroundCodes[5] = (ushort)(ushort.MaxValue/2);
		

		cord = new CastCoord(new Vector3(myX-1, myY, myZ)); // West
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			this.aroundCodes[6] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			this.aroundCodes[6] = (ushort)(ushort.MaxValue/2);
		

		cord = new CastCoord(new Vector3(myX-1, myY, myZ+1)); // NW
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			this.aroundCodes[7] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			this.aroundCodes[7] = (ushort)(ushort.MaxValue/2);

	}

	// Gets a list of states of around blocks if they are water
	private void GetStateAround(int myX, int myY, int myZ, ChunkLoader_Server cl){
		cachedCoord = new CastCoord(new Vector3(myX, myY, myZ+1)); // North
		if(cl.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ) == this.waterCode)
			this.aroundStates[0] = cl.chunks[cachedCoord.GetChunkPos()].metadata.GetState(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ);
		else
			this.aroundStates[0] = ushort.MaxValue;

		cachedCoord = new CastCoord(new Vector3(myX+1, myY, myZ+1)); // NE
		if(cl.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ) == this.waterCode)
			this.aroundStates[1] = cl.chunks[cachedCoord.GetChunkPos()].metadata.GetState(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ);
		else
			this.aroundStates[1] = ushort.MaxValue;

		cachedCoord = new CastCoord(new Vector3(myX+1, myY, myZ)); // East
		if(cl.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ) == this.waterCode)
			this.aroundStates[2] = cl.chunks[cachedCoord.GetChunkPos()].metadata.GetState(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ);
		else
			this.aroundStates[2] = ushort.MaxValue;

		cachedCoord = new CastCoord(new Vector3(myX+1, myY, myZ-1)); // SE
		if(cl.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ) == this.waterCode)
			this.aroundStates[3] = cl.chunks[cachedCoord.GetChunkPos()].metadata.GetState(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ);
		else
			this.aroundStates[3] = ushort.MaxValue;

		cachedCoord = new CastCoord(new Vector3(myX, myY, myZ-1)); // South
		if(cl.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ) == this.waterCode)
			this.aroundStates[4] = cl.chunks[cachedCoord.GetChunkPos()].metadata.GetState(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ);
		else
			this.aroundStates[4] = ushort.MaxValue;

		cachedCoord = new CastCoord(new Vector3(myX-1, myY, myZ-1)); // SW
		if(cl.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ) == this.waterCode)
			this.aroundStates[5] = cl.chunks[cachedCoord.GetChunkPos()].metadata.GetState(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ);
		else
			this.aroundStates[5] = ushort.MaxValue;

		cachedCoord = new CastCoord(new Vector3(myX-1, myY, myZ)); // West
		if(cl.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ) == this.waterCode)
			this.aroundStates[6] = cl.chunks[cachedCoord.GetChunkPos()].metadata.GetState(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ);
		else
			this.aroundStates[6] = ushort.MaxValue;

		cachedCoord = new CastCoord(new Vector3(myX-1, myY, myZ+1)); // NW
		if(cl.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ) == this.waterCode)
			this.aroundStates[7] = cl.chunks[cachedCoord.GetChunkPos()].metadata.GetState(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ);
		else
			this.aroundStates[7] = ushort.MaxValue;	
	}

	// Gets Water Level based on state table
	private int TranslateWaterLevel(ushort state){
		if(state == ushort.MaxValue)
			return 0;
		else if(state == 2 || (state >= 11 && state <= 18))
			return 1;
		else if(state == 1 || (state >= 3 && state <= 10))
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
		else if(state == 2 || (state >= 11 && state <= 18))
			return 1;
		else if(state == 1 || (state >= 3 && state <= 10))
			return 2;
		else if(state == 0)
			return 3;
		else
			return 0;		
	}

	// Checks if there is any high level water to this block
	private bool CheckHigherLevelWaterAround(int myX, int myY, int myZ, int currentWaterLevel, ChunkLoader_Server cl){
		for(int i=0; i<8; i++){
			if(this.aroundCodes[i] == this.waterCode && TranslateWaterLevel(this.aroundStates[i]) > currentWaterLevel){
				return true;
			}				
		}
		return false;
	}

	// Checks the amount of high level water ONLY IN ADJASCENT blocks
	private int GetHighLevelAroundCount(int x, int y, int z, int currentWaterLevel, ChunkLoader_Server cl, bool nofalling=false){
		int count=0;

		if(!nofalling){
			for(int i=0; i<8; i+=2){
				if(this.aroundCodes[i] == this.waterCode && TranslateWaterLevel(this.aroundStates[i]) > currentWaterLevel){
					count++;
				}
			}
		}
		else{
			for(int i=0; i<8; i+=2){
				if(this.aroundCodes[i] == this.waterCode && TranslateWaterLevelSpec(this.aroundStates[i]) > currentWaterLevel){
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
				if(this.aroundCodes[i] == this.waterCode && this.aroundStates[i] == 2){
					count++;
				}
			}
		}
		if(currentWaterLevel == 2){
			for(int i=0; i<8; i+=2){
				if(this.aroundCodes[i] == this.waterCode && this.aroundStates[i] == 1){
					count++;
				}
			}
		}
		if(currentWaterLevel == 3){
			for(int i=0; i<8; i+=2){
				if(this.aroundCodes[i] == this.waterCode && this.aroundStates[i] == 0){
					count++;
				}
			}
		}

		return count;
	}

	// Gets the ground blockCode of a direction
	private int GetGroundCode(int dir, int myX, int myY, int myZ, ChunkLoader_Server cl){
		cachedCoord = new CastCoord(GetNeighborBlock(dir, myX, myY, myZ));
		cachedCoord = cachedCoord.Add(0, -1, 0);

		return cl.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ);
	}

	// Emits BUD Signal to Water Blocks around this one
	private void EmitWaterBUD(int myX, int myY, int myZ, ChunkLoader_Server cl){
		// Diagonals
		for(int i=1; i<8; i+=2){
			if(this.aroundCodes[i] == this.waterCode){
				cachedCoord = new CastCoord(this.GetNeighborBlock(i, myX, myY, myZ));
				cachedBUD = new BUDSignal(BUDCode.CHANGE, cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), -1);
				cl.budscheduler.ScheduleBUD(cachedBUD, this.viscosityDelay);
			}
		}
		// Adjascents
		for(int i=0; i<8; i+=2){
			if(this.aroundCodes[i] == this.waterCode){
				cachedCoord = new CastCoord(this.GetNeighborBlock(i, myX, myY, myZ));
				cachedBUD = new BUDSignal(BUDCode.CHANGE, cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), -1);
				cl.budscheduler.ScheduleBUD(cachedBUD, this.viscosityDelay);
			}
		}

		// Below
		cachedCoord = new CastCoord(this.GetNeighborBlock(8, myX, myY, myZ));
		if(cachedCoord.blockY >= 0){
			if(cl.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ) == this.waterCode){
				cachedBUD = new BUDSignal(BUDCode.CHANGE, cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), -1);
				cl.budscheduler.ScheduleBUD(cachedBUD, this.viscosityDelay);			
			}
		}

		// Above
		cachedCoord = new CastCoord(this.GetNeighborBlock(9, myX, myY, myZ));
		if(cachedCoord.blockY < Chunk.chunkDepth){
			if(cl.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ) == this.waterCode){
				cachedBUD = new BUDSignal(BUDCode.CHANGE, cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), -1);
				cl.budscheduler.ScheduleBUD(cachedBUD, this.viscosityDelay);			
			}
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

	// Checks if a state is a corner block
	private bool IsCorner(ushort state){
		if(state >= 4 && state <= 18 && state%2 == 0)
			return true;
		return false;
	}

	// Check if given blockCode is washable
	private bool IsWashable(ushort blockCode, ChunkLoader_Server cl){
		return cl.blockBook.CheckWashable(blockCode);
	}	

    // Sends a DirectBlockUpdate call to users
	public void Update(CastCoord c, BUDCode type, int facing, ChunkLoader_Server cl){
		this.reloadMessage = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
		this.reloadMessage.DirectBlockUpdate(type, c.GetChunkPos(), c.blockX, c.blockY, c.blockZ, facing, this.waterCode, cl.GetState(c), ushort.MaxValue);
		cl.server.SendToClients(c.GetChunkPos(), this.reloadMessage);
	}

	// Checks if current state overpowers the priority of target state
	private bool ShouldStateOverpower(ushort current, ushort target){
		if(!statePriority.ContainsKey(current) || !statePriority.ContainsKey(target))
			return false;

		return statePriority[current] > statePriority[target];
	}
}
