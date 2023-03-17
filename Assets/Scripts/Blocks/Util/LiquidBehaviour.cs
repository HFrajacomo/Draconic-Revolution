using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
*/

public class LiquidBehaviour{
	public ushort[] aroundCodes;
	public ushort[] mainAroundCodes;
	public ushort[] aroundStates;
	public ushort[] mainAroundStates;
	private CastCoord cachedPos;
	private CastCoord cachedCoord;
	private bool breakFLAG = false;
	private ushort liquidCode;
	private int viscosityDelay;
	private ChunkLoader_Server cl;

	private BUDSignal cachedBUD;
	private NetMessage reloadMessage;

	private Dictionary<ushort?, List<int>> spawnDirections = new Dictionary<ushort?, List<int>>();
	private Dictionary<ushort, List<ushort>> spawnStateAdjascents = new Dictionary<ushort, List<ushort>>();
	private Dictionary<ushort, int> stateDirection = new Dictionary<ushort, int>();
	private Dictionary<ushort, byte> statePriority = new Dictionary<ushort, byte>();
	private Dictionary<ushort, int> cameFromDir = new Dictionary<ushort, int>();
	private Dictionary<ushort, HashSet<ushort>> cameFromState = new Dictionary<ushort, HashSet<ushort>>();

	public LiquidBehaviour(ushort liquidCode, int viscosityDelay){
		this.liquidCode = liquidCode;
		this.viscosityDelay = viscosityDelay;

		this.aroundCodes = new ushort[8];
		this.aroundStates = new ushort[8];
		this.mainAroundCodes = new ushort[8];
		this.mainAroundStates = new ushort[8];

		// Water Spawn Directions
		this.spawnDirections.Add(3, new List<int>(new int[]{6,0,2}));
		this.spawnDirections.Add(5, new List<int>(new int[]{0,2,4}));
		this.spawnDirections.Add(7, new List<int>(new int[]{2,4,6}));
		this.spawnDirections.Add(9, new List<int>(new int[]{4,6,0}));
		this.spawnDirections.Add(4, new List<int>(new int[]{0,2}));
		this.spawnDirections.Add(6, new List<int>(new int[]{2,4}));
		this.spawnDirections.Add(8, new List<int>(new int[]{4,6}));
		this.spawnDirections.Add(10, new List<int>(new int[]{6,0}));
		this.spawnDirections.Add(11, new List<int>(new int[]{2,6}));
		this.spawnDirections.Add(13, new List<int>(new int[]{4,0}));
		this.spawnDirections.Add(15, new List<int>(new int[]{6,2}));
		this.spawnDirections.Add(17, new List<int>(new int[]{0,4}));

		// Water states priority
		this.statePriority.Add(0, 9);
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
		this.statePriority.Add(19, 8);
		this.statePriority.Add(20, 7);
		this.statePriority.Add(21, 6);

		// Adds to CameFrom dictionary
		this.cameFromDir.Add(3, 4);
		this.cameFromDir.Add(4, 5);
		this.cameFromDir.Add(5, 6);
		this.cameFromDir.Add(6, 7);
		this.cameFromDir.Add(7, 0);
		this.cameFromDir.Add(8, 1);
		this.cameFromDir.Add(9, 2);
		this.cameFromDir.Add(10, 3);
		this.cameFromDir.Add(11, 4);
		this.cameFromDir.Add(12, 5);
		this.cameFromDir.Add(13, 6);
		this.cameFromDir.Add(14, 7);
		this.cameFromDir.Add(15, 0);
		this.cameFromDir.Add(16, 1);
		this.cameFromDir.Add(17, 2);
		this.cameFromDir.Add(18, 3);

		// Adds to CameFromState dict
		this.cameFromState.Add(3, new HashSet<ushort>(){0,19});
		this.cameFromState.Add(4, new HashSet<ushort>(){0,19});
		this.cameFromState.Add(5, new HashSet<ushort>(){0,19});
		this.cameFromState.Add(6, new HashSet<ushort>(){0,19});
		this.cameFromState.Add(7, new HashSet<ushort>(){0,19});
		this.cameFromState.Add(8, new HashSet<ushort>(){0,19});
		this.cameFromState.Add(9, new HashSet<ushort>(){0,19});
		this.cameFromState.Add(10, new HashSet<ushort>(){0,19});
		this.cameFromState.Add(11, new HashSet<ushort>(){10,3,4,20,1});
		this.cameFromState.Add(12, new HashSet<ushort>(){4,5,20,1});
		this.cameFromState.Add(13, new HashSet<ushort>(){4,5,6,20,1});
		this.cameFromState.Add(14, new HashSet<ushort>(){5,6,20,1});
		this.cameFromState.Add(15, new HashSet<ushort>(){6,7,8,20,1});
		this.cameFromState.Add(16, new HashSet<ushort>(){8,9,20,1});
		this.cameFromState.Add(17, new HashSet<ushort>(){8,9,10,20,1});
		this.cameFromState.Add(18, new HashSet<ushort>(){9,10,20,1});
	}

	public bool IsChunkLoaderSet(){
		return !(this.cl == null);
	}

	public void SetChunkLoader(ChunkLoader_Server cl){
		this.cl = cl;
	}

	// Custom Place operation with Raycasting class overwrite
	public int OnPlace(ChunkPos pos, int x, int y, int z, int facing){
		CastCoord thisPos = new CastCoord(pos, x, y, z);
		NetMessage message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
		message.DirectBlockUpdate(BUDCode.PLACE, pos, thisPos.blockX, thisPos.blockY, thisPos.blockZ, facing, this.liquidCode, cl.chunks[thisPos.GetChunkPos()].metadata.GetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ), cl.chunks[thisPos.GetChunkPos()].metadata.GetHP(thisPos.blockX, thisPos.blockY, thisPos.blockZ));
		
		cl.budscheduler.ScheduleBUD(new BUDSignal(BUDCode.CHANGE, thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), facing), this.viscosityDelay);

		// If has been placed by player
		if(facing >= 0){
			cl.chunks[thisPos.GetChunkPos()].metadata.Reset(x,y,z);
			cl.server.SendToClients(thisPos.GetChunkPos(), message);
			return 0;
		}

		this.Update(thisPos, BUDCode.CHANGE, -1);
		cl.budscheduler.ScheduleSave(thisPos.GetChunkPos());
		EmitWaterBUD(thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ());
		return 0;
	}

	// Custom Break operation with Raycasting class overwrite
	public int OnBreak(ChunkPos pos, int x, int y, int z){
		cl.chunks[pos].data.SetCell(x, y, z, 0);
		cl.chunks[pos].metadata.Reset(x,y,z);

		cachedCoord = new CastCoord(pos, x, y, z);

		// Reloads surrounding data when was manually broken by player
		if(!this.breakFLAG)
			GetCodeAround(cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ());
		
		this.Update(cachedCoord, BUDCode.BREAK, -1);
		cl.budscheduler.ScheduleSave(cachedCoord.GetChunkPos());
		EmitWaterBUD(cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ());
		this.breakFLAG = false;

		return 0;
	}

	// Emits BUD Signal to Water Blocks around this one
	private void EmitWaterBUD(int myX, int myY, int myZ){
		GetCodeAround(myX, myY, myZ);

		// Adjascents
		for(int i=0; i<8; i+=2){
			if(this.aroundCodes[i] == this.liquidCode){
				cachedCoord = new CastCoord(this.GetNeighborBlock(i, myX, myY, myZ));
				cachedBUD = new BUDSignal(BUDCode.CHANGE, cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), -1);
				cl.budscheduler.ScheduleBUD(cachedBUD, this.viscosityDelay);
			}
		}

		// Below
		cachedCoord = new CastCoord(this.GetNeighborBlock(8, myX, myY, myZ));
		if(cachedCoord.blockY >= 0 && cl.chunks.ContainsKey(cachedCoord.GetChunkPos())){
			if(cl.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ) == this.liquidCode){
				cachedBUD = new BUDSignal(BUDCode.CHANGE, cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), cachedCoord.GetWorldX(), cachedCoord.GetWorldY(), cachedCoord.GetWorldZ(), -1);
				cl.budscheduler.ScheduleBUD(cachedBUD, this.viscosityDelay);			
			}
		}

		// Above
		cachedCoord = new CastCoord(this.GetNeighborBlock(9, myX, myY, myZ));
		if(cachedCoord.blockY < Chunk.chunkDepth){
			if(cl.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ) == this.liquidCode){
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

	// Applies Water Movement
	public void OnBlockUpdate(BUDCode type, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing){
		if(type == BUDCode.BREAK || type == BUDCode.CHANGE){
			CastCoord thisPos = new CastCoord(new Vector3(myX, myY, myZ));
			ushort state = cl.chunks[thisPos.GetChunkPos()].metadata.GetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ);

			/*
			Still Level 3
			*/
			if(state == 0){
				ushort below = GetCodeBelow(thisPos);
				ushort belowState = GetStateBelow(thisPos);
				GetCodeAround(myX, myY, myZ, main:true);
				GetStateAround(myX, myY, myZ, main:true);

				if(IsOutOfBounds())
					return;

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should expand downwards
				if(below == 0 || (below == this.liquidCode && ShouldStateOverpower(state, belowState)) || IsWashable(below)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// If there are at least one adjascent Still Water 3 -> Expand falling blocks
					if(GetSameLevelAroundCount(myX, myY, myZ, 3) > 0){
						// Should break washable block below
						if(IsWashable(below)){
							if(below <= ushort.MaxValue/2)
								cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
							else
								cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						}

						cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
						cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 19);
						this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1);
						return;
					}
					// If this block is a lone water one -> Fall by itself
					else{
						// Should break washable block below
						if(IsWashable(below)){
							if(below <= ushort.MaxValue/2)
								cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
							else
								cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						}

						this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ);
						cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
						cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 0);
						this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1);
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
						if(this.mainAroundCodes[i] == 0){
							found = true;
						}
						// If is washable
						else if(IsWashable(this.mainAroundCodes[i])){
							found = true;

							if(this.mainAroundCodes[i] <= ushort.MaxValue/2)
								cl.blockBook.blocks[this.mainAroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
							else
								cl.blockBook.objects[ushort.MaxValue - this.mainAroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						}
						// If is water
						else if(this.mainAroundCodes[i] == liquidCode && ShouldStateOverpower(targetState, this.mainAroundStates[i])){
							if(targetState != ushort.MaxValue)
								found = true;
						}

						// Found cases
						if(found){
							cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.liquidCode);
							cl.chunks[cachedPos.GetChunkPos()].metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
							this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1);
						}

					}

				}
			}

			/*
			Still Level 2
			*/
			else if(state == 1){
				ushort below = GetCodeBelow(thisPos);
				ushort belowState = GetStateBelow(thisPos);
				GetCodeAround(myX, myY, myZ, main:true);
				GetStateAround(myX, myY, myZ, main:true);

				if(IsOutOfBounds())
					return;

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should upgrade to Still Level 3
				if(below == this.liquidCode && belowState == 1){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ);
					cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
					cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 0);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1);					
					return;
				}

				// If should expand downwards
				if(below == 0 || (below == this.liquidCode && ShouldStateOverpower(state, belowState)) || IsWashable(below)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// If there are at least one adjascent Still Water 2 -> Expand falling blocks
					if(GetSameLevelAroundCount(myX, myY, myZ, 2) > 0){
						// Should break washable block below
						if(IsWashable(below)){
							if(below <= ushort.MaxValue/2)
								cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
							else
								cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						}

						cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
						cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 20);
						this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1);
						return;
					}
					// If this block is a lone water one -> Fall by itself
					else{
						// Should break washable block below
						if(IsWashable(below)){
							if(below <= ushort.MaxValue/2)
								cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
							else
								cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						}

						this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ);
						cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
						cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 1);
						this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1);
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
						if(this.mainAroundCodes[i] == 0){
							found = true;
						}
						// If is washable
						else if(IsWashable(this.mainAroundCodes[i])){
							found = true;

							if(this.mainAroundCodes[i] <= ushort.MaxValue/2)
								cl.blockBook.blocks[this.mainAroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
							else
								cl.blockBook.objects[ushort.MaxValue - this.mainAroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						}
						// If is water
						else if(this.mainAroundCodes[i] == liquidCode && ShouldStateOverpower(targetState, this.mainAroundStates[i])){
							if(targetState != ushort.MaxValue)
								found = true;
						}

						// Found cases
						if(found){
							cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.liquidCode);
							cl.chunks[cachedPos.GetChunkPos()].metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
							this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1);
						}

					}

				}
			}

			/*
			Still Level 1
			*/
			else if(state == 2){
				ushort below = GetCodeBelow(thisPos);
				ushort belowState = GetStateBelow(thisPos);
				GetCodeAround(myX, myY, myZ, main:true);
				GetStateAround(myX, myY, myZ, main:true);

				if(IsOutOfBounds())
					return;

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should upgrade to Still Level 2
				if(below == this.liquidCode && belowState == 2){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ);
					cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
					cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 1);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1);					
					return;
				}

				// If should fall
				if(below == 0 || (below == this.liquidCode && ShouldStateOverpower(state, belowState)) || IsWashable(below)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below)){
						if(below <= ushort.MaxValue/2)
							cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ);
					cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
					cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 2);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1);
					return;
				}

				// If should die
				for(int i=0; i < 8; i+=2){
					if(this.mainAroundCodes[i] == 0){
						this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ);
						return;
					}
				}	
			}

			/*
			Directional Adjascent Level 2
			*/
			else if(state >= 3 && state <= 9 && state%2 == 1){
				ushort below = GetCodeBelow(thisPos);
				ushort belowState = GetStateBelow(thisPos);
				GetCodeAround(myX, myY, myZ, main:true);
				GetStateAround(myX, myY, myZ, main:true);

				if(IsOutOfBounds())
					return;

				// Dies if no Still Level 3 around
				if(this.mainAroundCodes[cameFromDir[state]] != liquidCode || !cameFromState[state].Contains(this.mainAroundStates[cameFromDir[state]])){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ);
					return;
				}

				// If should upgrade to Still Level 3
				if(GetSameLevelAroundCount(myX, myY, myZ, 3) >= 2){
					cl.chunks[thisPos.GetChunkPos()].data.SetCell(thisPos.blockX, thisPos.blockY, thisPos.blockZ, this.liquidCode);
					cl.chunks[thisPos.GetChunkPos()].metadata.SetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ, 0);
					this.OnPlace(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, -1);
					return;
				}

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should create falling blocks
				if(below == 0 || below == this.liquidCode && ShouldStateOverpower(20, belowState) || IsWashable(below)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below)){
						if(below <= ushort.MaxValue/2)
							cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
					cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 20);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1);
					return;					
				}

				// If is falling block pivot
				else if(below == this.liquidCode && belowState == 20){return;}

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
						if(this.mainAroundCodes[i] == 0){
							found = true;
						}
						// If is washable
						else if(IsWashable(this.mainAroundCodes[i])){
							found = true;

							if(this.mainAroundCodes[i] <= ushort.MaxValue/2)
								cl.blockBook.blocks[this.mainAroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
							else
								cl.blockBook.objects[ushort.MaxValue - this.mainAroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						}
						// If is water
						else if(this.mainAroundCodes[i] == liquidCode && ShouldStateOverpower(targetState, this.mainAroundStates[i])){
							found = true;
						}

						// Found cases
						if(found){
							cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.liquidCode);
							cl.chunks[cachedPos.GetChunkPos()].metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
							this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1);
						}
					}
				}
			}

			/*
			Directional Diagonal Level 2
			*/
			else if(state >= 4 && state <= 10 && state%2 == 0){
				ushort below = GetCodeBelow(thisPos);
				ushort belowState = GetStateBelow(thisPos);
				GetCodeAround(myX, myY, myZ, main:true);
				GetStateAround(myX, myY, myZ, main:true);

				if(IsOutOfBounds())
					return;

				// Dies if no Still Level 3 around
				if(this.mainAroundCodes[cameFromDir[state]] != liquidCode || !cameFromState[state].Contains(this.mainAroundStates[cameFromDir[state]])){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ);
					return;
				}

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should create falling blocks
				if(below == 0 || below == this.liquidCode && ShouldStateOverpower(20, belowState) || IsWashable(below)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below)){
						if(below <= ushort.MaxValue/2)
							cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
					cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 20);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1);
					return;					
				}

				// If is falling block pivot
				else if(below == this.liquidCode && belowState == 20){return;}

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
						if(this.mainAroundCodes[i] == 0){
							found = true;
						}
						// If is washable
						else if(IsWashable(this.mainAroundCodes[i])){
							found = true;

							if(this.mainAroundCodes[i] <= ushort.MaxValue/2)
								cl.blockBook.blocks[this.mainAroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
							else
								cl.blockBook.objects[ushort.MaxValue - this.mainAroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						}
						// If is water
						else if(this.mainAroundCodes[i] == liquidCode && ShouldStateOverpower(targetState, this.mainAroundStates[i])){
							found = true;
						}

						// Found cases
						if(found){
							cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.liquidCode);
							cl.chunks[cachedPos.GetChunkPos()].metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
							this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1);
						}
					}			
				}
			}

			/*
			Directional Adjascent Level 1
			*/
			else if(state >= 11 && state <= 17 && state%2 == 1){
				ushort below = GetCodeBelow(thisPos);
				ushort belowState = GetStateBelow(thisPos);
				GetCodeAround(myX, myY, myZ, main:true);
				GetStateAround(myX, myY, myZ, main:true);

				if(IsOutOfBounds())
					return;

				// Dies if no Level 2 around
				if(this.mainAroundCodes[cameFromDir[state]] != liquidCode || !cameFromState[state].Contains(this.mainAroundStates[cameFromDir[state]])){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ);
					return;
				}

				// If should upgrade to Still Level 2
				if(GetSameLevelAroundCount(myX, myY, myZ, 2) >= 2){
					cl.chunks[thisPos.GetChunkPos()].data.SetCell(thisPos.blockX, thisPos.blockY, thisPos.blockZ, this.liquidCode);
					cl.chunks[thisPos.GetChunkPos()].metadata.SetState(thisPos.blockX, thisPos.blockY, thisPos.blockZ, 1);
					this.OnPlace(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, -1);
					return;
				}

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should create falling blocks
				if(below == 0 || below == this.liquidCode && ShouldStateOverpower(21, belowState) || IsWashable(below)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below)){
						if(below <= ushort.MaxValue/2)
							cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
					cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 21);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1);
					return;					
				}

				// If is falling block pivot
				else if(below == this.liquidCode && belowState == 21){return;}

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
						if(this.mainAroundCodes[i] == 0){
							found = true;
						}
						// If is washable
						else if(IsWashable(this.mainAroundCodes[i])){
							found = true;

							if(this.mainAroundCodes[i] <= ushort.MaxValue/2)
								cl.blockBook.blocks[this.mainAroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
							else
								cl.blockBook.objects[ushort.MaxValue - this.mainAroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						}
						// If is water
						else if(this.mainAroundCodes[i] == liquidCode && ShouldStateOverpower(targetState, this.mainAroundStates[i])){
							found = true;
						}

						// Found cases
						if(found){
							cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.liquidCode);
							cl.chunks[cachedPos.GetChunkPos()].metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
							this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1);
						}
					}					
				}
			}

			/*
			Directional Diagonal Level 1
			*/
			else if(state >= 12 && state <= 18 && state%2 == 0){
				ushort below = GetCodeBelow(thisPos);
				ushort belowState = GetStateBelow(thisPos);
				GetCodeAround(myX, myY, myZ, main:true);
				GetStateAround(myX, myY, myZ, main:true);

				if(IsOutOfBounds())
					return;

				// Dies if no Level 2 around
				if(this.mainAroundCodes[cameFromDir[state]] != liquidCode || !cameFromState[state].Contains(this.mainAroundStates[cameFromDir[state]])){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ);
					return;
				}

				// If is out of Y bounds
				if(below == (ushort)(ushort.MaxValue/2))
					return;

				// If should create falling blocks
				if(below == 0 || below == this.liquidCode && ShouldStateOverpower(21, belowState) || IsWashable(below)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below)){
						if(below <= ushort.MaxValue/2)
							cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
					cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 21);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1);
					return;					
				}

				// If is falling block pivot
				else if(below == this.liquidCode && belowState == 21){return;}
			}

			/*
			Falling 3
			*/
			else if(state == 19){
				ushort below = GetCodeBelow(thisPos);
				ushort belowState = GetStateBelow(thisPos);
				ushort above = GetCodeAbove(thisPos);
				ushort aboveState = GetStateAbove(thisPos);

				// If should die
				if(above != this.liquidCode || (above == this.liquidCode && (aboveState != 0 && aboveState != 19))){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ);
					return;					
				}

				// If should create new falling 3s
				if(below == 0 || below == this.liquidCode && ShouldStateOverpower(19, belowState) || IsWashable(below)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below)){
						if(below <= ushort.MaxValue/2)
							cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
					cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 19);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1);
					return;							
				}

				// If is pivot to falling block
				if(below == this.liquidCode && above == this.liquidCode) {return;}

				// Normal Behaviour	
				bool found;
				ushort targetState = 0;

				GetCodeAround(myX, myY, myZ, main:true);
				GetStateAround(myX, myY, myZ, main:true);

				if(IsOutOfBounds())
					return;

				for(int i=0; i < 8; i+=2){
					found = false;
					GetDirectionPos(myX, myY, myZ, i);
					targetState = GetNewState(state, i);

					// If is air
					if(this.mainAroundCodes[i] == 0){
						found = true;
					}
					// If is washable
					else if(IsWashable(this.mainAroundCodes[i])){
						found = true;

						if(this.mainAroundCodes[i] <= ushort.MaxValue/2)
							cl.blockBook.blocks[this.mainAroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						else
							cl.blockBook.objects[ushort.MaxValue - this.mainAroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
					}
					// If is water
					else if(this.mainAroundCodes[i] == liquidCode && ShouldStateOverpower(targetState, this.mainAroundStates[i])){
						if(targetState != ushort.MaxValue)
							found = true;
					}


					// Found cases
					if(found){
						cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.liquidCode);
						cl.chunks[cachedPos.GetChunkPos()].metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
						this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1);
					}
				}
			}

			/*
			Falling 2
			*/
			else if(state == 20){
				ushort below = GetCodeBelow(thisPos);
				ushort belowState = GetStateBelow(thisPos);
				ushort above = GetCodeAbove(thisPos);
				ushort aboveState = GetStateAbove(thisPos);

				// If should die
				if(above != this.liquidCode || (above == this.liquidCode && (aboveState != 1 && aboveState != 20 && !(aboveState >= 3 && aboveState <= 10)))){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ);
					return;					
				}

				// If should create new falling 2s
				if(below == 0 || below == this.liquidCode && ShouldStateOverpower(20, belowState) || IsWashable(below)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below)){
						if(below <= ushort.MaxValue/2)
							cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
					cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 20);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1);
					return;							
				}

				// If is pivot to falling block
				if(below == this.liquidCode && above == this.liquidCode) {return;}

				// Normal Behaviour	
				bool found;
				ushort targetState = 0;

				GetCodeAround(myX, myY, myZ, main:true);
				GetStateAround(myX, myY, myZ, main:true);

				if(IsOutOfBounds())
					return;

				for(int i=0; i < 8; i+=2){
					found = false;
					GetDirectionPos(myX, myY, myZ, i);
					targetState = GetNewState(state, i);

					// If is air
					if(this.mainAroundCodes[i] == 0){
						found = true;
					}
					// If is washable
					else if(IsWashable(this.mainAroundCodes[i])){
						found = true;

						if(this.mainAroundCodes[i] <= ushort.MaxValue/2)
							cl.blockBook.blocks[this.mainAroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
						else
							cl.blockBook.objects[ushort.MaxValue - this.mainAroundCodes[i]].OnBreak(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
					}
					// If is water
					else if(this.mainAroundCodes[i] == liquidCode && ShouldStateOverpower(targetState, this.mainAroundStates[i])){
						if(targetState != ushort.MaxValue)
							found = true;
					}


					// Found cases
					if(found){
						cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.liquidCode);
						cl.chunks[cachedPos.GetChunkPos()].metadata.SetState(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, targetState);
						this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1);
					}
				}
			}

			/*
			Falling 1
			*/
			else if(state == 21){
				ushort below = GetCodeBelow(thisPos);
				ushort belowState = GetStateBelow(thisPos);
				ushort above = GetCodeAbove(thisPos);
				ushort aboveState = GetStateAbove(thisPos);

				// If should die
				if(above != this.liquidCode || (above == this.liquidCode && (aboveState != 2 && aboveState != 21 && !(aboveState >= 11 && aboveState <= 18)))){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ);
					return;					
				}

				// If should create new falling 1s
				if(below == 0 || below == this.liquidCode && ShouldStateOverpower(21, belowState) || IsWashable(below)){
					CastCoord newPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					// Should break washable block below
					if(IsWashable(below)){
						if(below <= ushort.MaxValue/2)
							cl.blockBook.blocks[below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
						else
							cl.blockBook.objects[ushort.MaxValue - below].OnBreak(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, cl);
					}

					cl.chunks[newPos.GetChunkPos()].data.SetCell(newPos.blockX, newPos.blockY, newPos.blockZ, this.liquidCode);
					cl.chunks[newPos.GetChunkPos()].metadata.SetState(newPos.blockX, newPos.blockY, newPos.blockZ, 21);
					this.OnPlace(newPos.GetChunkPos(), newPos.blockX, newPos.blockY, newPos.blockZ, -1);
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
		if(state == 1 || state == 20){
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
	private ushort GetCodeBelow(int myX, int myY, int myZ){
		CastCoord cord = new CastCoord(new Vector3(myX, myY-1, myZ));
		if(myY-1 < 0)
			return (ushort)(ushort.MaxValue/2);
		if(!cl.chunks.ContainsKey(cord.GetChunkPos()))
			return (ushort)(ushort.MaxValue/2);

		return cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
	}
	private ushort GetCodeBelow(CastCoord c){
		return GetCodeBelow(c.GetWorldX(), c.GetWorldY(), c.GetWorldZ());
	}

	// Gets State of block below
	private ushort GetStateBelow(int myX, int myY, int myZ){
		CastCoord cord = new CastCoord(new Vector3(myX, myY-1, myZ));
		if(myY-1 < 0)
			return (ushort)(ushort.MaxValue/2);
		if(!cl.chunks.ContainsKey(cord.GetChunkPos()))
			return (ushort)(ushort.MaxValue/2);

		return cl.chunks[cord.GetChunkPos()].metadata.GetState(cord.blockX, cord.blockY, cord.blockZ);
	}
	private ushort GetStateBelow(CastCoord c){
		return GetStateBelow(c.GetWorldX(), c.GetWorldY(), c.GetWorldZ());
	}

	// Gets State of block above
	private ushort GetStateAbove(int myX, int myY, int myZ){
		CastCoord cord = new CastCoord(new Vector3(myX, myY+1, myZ));
		if(myY+1 >= Chunk.chunkDepth)
			return (ushort)(ushort.MaxValue/2);

		return cl.chunks[cord.GetChunkPos()].metadata.GetState(cord.blockX, cord.blockY, cord.blockZ);
	}
	private ushort GetStateAbove(CastCoord c){
		return GetStateAbove(c.GetWorldX(), c.GetWorldY(), c.GetWorldZ());
	}

	// Gets Code of block above
	private ushort GetCodeAbove(int myX, int myY, int myZ){
		CastCoord cord = new CastCoord(new Vector3(myX, myY+1, myZ));
		if(myY+1 >= Chunk.chunkDepth)
			return (ushort)(ushort.MaxValue/2);

		return cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
	}
	private ushort GetCodeAbove(CastCoord c){
		return GetCodeAbove(c.GetWorldX(), c.GetWorldY(), c.GetWorldZ());
	}


	// Gets a list of block codes of around blocks
	// Order is N Clockwise
	// The value (ushort)(ushort.MaxValue/2) is considered the error code
	private void GetCodeAround(int myX, int myY, int myZ, bool main=false){
		CastCoord cord;
		ushort[] selectedArray;

		if(main)
			selectedArray = this.mainAroundCodes;
		else
			selectedArray = this.aroundCodes;

		cord = new CastCoord(new Vector3(myX, myY, myZ+1)); // North
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			selectedArray[0] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[0] = (ushort)(ushort.MaxValue/2);
		

		cord = new CastCoord(new Vector3(myX+1, myY, myZ+1)); // NE
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			selectedArray[1] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[1] = (ushort)(ushort.MaxValue/2);
		

		cord = new CastCoord(new Vector3(myX+1, myY, myZ)); // East
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			selectedArray[2] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[2] = (ushort)(ushort.MaxValue/2);
		
		
		cord = new CastCoord(new Vector3(myX+1, myY, myZ-1)); // SE
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			selectedArray[3] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[3] = (ushort)(ushort.MaxValue/2);
		
		
		cord = new CastCoord(new Vector3(myX, myY, myZ-1)); // South
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			selectedArray[4] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[4] = (ushort)(ushort.MaxValue/2);
		

		cord = new CastCoord(new Vector3(myX-1, myY, myZ-1)); // SW
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			selectedArray[5] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[5] = (ushort)(ushort.MaxValue/2);
		

		cord = new CastCoord(new Vector3(myX-1, myY, myZ)); // West
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			selectedArray[6] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[6] = (ushort)(ushort.MaxValue/2);
		

		cord = new CastCoord(new Vector3(myX-1, myY, myZ+1)); // NW
		if(cl.chunks.ContainsKey(cord.GetChunkPos()))
			selectedArray[7] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		else
			selectedArray[7] = (ushort)(ushort.MaxValue/2);

	}

	// Gets a list of states of around blocks if they are water
	private void GetStateAround(int myX, int myY, int myZ, bool main=false){
		ushort[] selectedArray;
		CastCoord cast;

		if(main)
			selectedArray = this.mainAroundStates;
		else
			selectedArray = this.aroundStates;

		cast = new CastCoord(new Vector3(myX, myY, myZ+1)); // North
		if(cl.chunks[cast.GetChunkPos()].data.GetCell(cast.blockX, cast.blockY, cast.blockZ) == this.liquidCode)
			selectedArray[0] = cl.chunks[cast.GetChunkPos()].metadata.GetState(cast.blockX, cast.blockY, cast.blockZ);
		else
			selectedArray[0] = ushort.MaxValue;

		cast = new CastCoord(new Vector3(myX+1, myY, myZ+1)); // NE
		if(cl.chunks[cast.GetChunkPos()].data.GetCell(cast.blockX, cast.blockY, cast.blockZ) == this.liquidCode)
			selectedArray[1] = cl.chunks[cast.GetChunkPos()].metadata.GetState(cast.blockX, cast.blockY, cast.blockZ);
		else
			selectedArray[1] = ushort.MaxValue;

		cast = new CastCoord(new Vector3(myX+1, myY, myZ)); // East
		if(cl.chunks[cast.GetChunkPos()].data.GetCell(cast.blockX, cast.blockY, cast.blockZ) == this.liquidCode)
			selectedArray[2] = cl.chunks[cast.GetChunkPos()].metadata.GetState(cast.blockX, cast.blockY, cast.blockZ);
		else
			selectedArray[2] = ushort.MaxValue;

		cast = new CastCoord(new Vector3(myX+1, myY, myZ-1)); // SE
		if(cl.chunks[cast.GetChunkPos()].data.GetCell(cast.blockX, cast.blockY, cast.blockZ) == this.liquidCode)
			selectedArray[3] = cl.chunks[cast.GetChunkPos()].metadata.GetState(cast.blockX, cast.blockY, cast.blockZ);
		else
			selectedArray[3] = ushort.MaxValue;

		cast = new CastCoord(new Vector3(myX, myY, myZ-1)); // South
		if(cl.chunks[cast.GetChunkPos()].data.GetCell(cast.blockX, cast.blockY, cast.blockZ) == this.liquidCode)
			selectedArray[4] = cl.chunks[cast.GetChunkPos()].metadata.GetState(cast.blockX, cast.blockY, cast.blockZ);
		else
			selectedArray[4] = ushort.MaxValue;

		cast = new CastCoord(new Vector3(myX-1, myY, myZ-1)); // SW
		if(cl.chunks[cast.GetChunkPos()].data.GetCell(cast.blockX, cast.blockY, cast.blockZ) == this.liquidCode)
			selectedArray[5] = cl.chunks[cast.GetChunkPos()].metadata.GetState(cast.blockX, cast.blockY, cast.blockZ);
		else
			selectedArray[5] = ushort.MaxValue;

		cast = new CastCoord(new Vector3(myX-1, myY, myZ)); // West
		if(cl.chunks[cast.GetChunkPos()].data.GetCell(cast.blockX, cast.blockY, cast.blockZ) == this.liquidCode)
			selectedArray[6] = cl.chunks[cast.GetChunkPos()].metadata.GetState(cast.blockX, cast.blockY, cast.blockZ);
		else
			selectedArray[6] = ushort.MaxValue;

		cast = new CastCoord(new Vector3(myX-1, myY, myZ+1)); // NW
		if(cl.chunks[cast.GetChunkPos()].data.GetCell(cast.blockX, cast.blockY, cast.blockZ) == this.liquidCode)
			selectedArray[7] = cl.chunks[cast.GetChunkPos()].metadata.GetState(cast.blockX, cast.blockY, cast.blockZ);
		else
			selectedArray[7] = ushort.MaxValue;	
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

	// Checks the amount of same level water ONLY IN ADJASCENT blocks disconsidering Falling Blocks
	private int GetSameLevelAroundCount(int x, int y, int z, int currentWaterLevel){
		int count=0;
		GetCodeAround(x,y,z);
		GetStateAround(x,y,z);

		if(currentWaterLevel == 1){
			for(int i=0; i<8; i+=2){
				if(this.aroundCodes[i] == this.liquidCode && this.aroundStates[i] == 2){
					count++;
				}
			}
		}
		if(currentWaterLevel == 2){
			for(int i=0; i<8; i+=2){
				if(this.aroundCodes[i] == this.liquidCode && this.aroundStates[i] == 1){
					count++;
				}
			}
		}
		if(currentWaterLevel == 3){
			for(int i=0; i<8; i+=2){
				if(this.aroundCodes[i] == this.liquidCode && this.aroundStates[i] == 0){
					count++;
				}
			}
		}

		return count;
	}

	// Gets the ground blockCode of a direction
	private int GetGroundCode(int dir, int myX, int myY, int myZ){
		cachedCoord = new CastCoord(GetNeighborBlock(dir, myX, myY, myZ));
		cachedCoord = cachedCoord.Add(0, -1, 0);

		return cl.chunks[cachedCoord.GetChunkPos()].data.GetCell(cachedCoord.blockX, cachedCoord.blockY, cachedCoord.blockZ);
	}

	// Checks if a state is a corner block
	private bool IsCorner(ushort state){
		if(state >= 4 && state <= 18 && state%2 == 0)
			return true;
		return false;
	}

	// Check if given blockCode is washable
	private bool IsWashable(ushort blockCode){
		return cl.blockBook.CheckWashable(blockCode);
	}	

    // Sends a DirectBlockUpdate call to users
	public void Update(CastCoord c, BUDCode type, int facing){
		this.reloadMessage = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
		this.reloadMessage.DirectBlockUpdate(type, c.GetChunkPos(), c.blockX, c.blockY, c.blockZ, facing, this.liquidCode, cl.GetState(c), ushort.MaxValue);
		cl.server.SendToClients(c.GetChunkPos(), this.reloadMessage);
	}

	// Checks if current state overpowers the priority of target state
	private bool ShouldStateOverpower(ushort current, ushort target){
		if(!statePriority.ContainsKey(current) || !statePriority.ContainsKey(target))
			return false;

		return statePriority[current] > statePriority[target];
	}

	// Checks if Water Update is out of bounds
	// Run this after running GetCodeAround()
	private bool IsOutOfBounds(){
		for(int i=0; i < 8; i++){
			if(this.aroundCodes[i] == ushort.MaxValue/2)
				return true;
		}

		return false;
	}
}
