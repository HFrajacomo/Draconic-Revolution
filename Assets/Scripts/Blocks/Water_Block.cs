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
*/

public class Water_Block : Blocks
{
	// Unique
	public int waterCode;
	public int[] aroundCodes;
	public ushort?[] aroundStates;
	public CastCoord cachedPos;

	public Dictionary<ushort?, List<int>> spawnDirection = new Dictionary<ushort?, List<int>>();

	// Just loaded block
	public Water_Block(){
		this.name = "Water";
		this.solid = false;
		this.transparent = true;
		this.invisible = false;
		this.liquid = true;
		this.waterCode = 6;
		this.customBreak = true;
		this.customPlace = true;

		
		this.aroundCodes = new int[8];
		this.aroundStates = new ushort?[8];

		// Water Spawn Directions
		this.spawnDirection.Add(3, new List<int>(new int[]{7,0,1}));
		this.spawnDirection.Add(4, new List<int>(new int[]{0,1,2}));
		this.spawnDirection.Add(5, new List<int>(new int[]{1,2,3}));
		this.spawnDirection.Add(6, new List<int>(new int[]{2,3,4}));
		this.spawnDirection.Add(7, new List<int>(new int[]{3,4,5}));
		this.spawnDirection.Add(8, new List<int>(new int[]{4,5,6}));
		this.spawnDirection.Add(9, new List<int>(new int[]{5,6,7}));
		this.spawnDirection.Add(10, new List<int>(new int[]{6,7,0}));

	}

	public override int OnPlace(ChunkPos pos, int x, int y, int z, int facing, ChunkLoader cl){
		CastCoord thisPos = new CastCoord(pos, x, y, z);
		cl.budscheduler.ScheduleBUD(new BUDSignal("change", thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), 0), 1);
		
		// If has been placed by player
		if(facing >= 0)
			this.EmitBlockUpdate("change", thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), 0, cl);
		
		cl.budscheduler.ScheduleReload(pos, 0);
		return 0;
	}

	// Custom Break operation with Raycasting class overwrite
	public override int OnBreak(ChunkPos pos, int x, int y, int z, ChunkLoader cl){
		cl.chunks[pos].data.SetCell(x, y, z, 0);
		cl.chunks[pos].metadata.CreateNull(x, y, z);	

		cachedPos = new CastCoord(pos, x, y, z);

		this.EmitBlockUpdate("break", cachedPos.GetWorldX(), cachedPos.GetWorldY(), cachedPos.GetWorldZ(), 1, cl);
		cl.budscheduler.ScheduleReload(pos, 0);
		return 0;
	}

	// Applies Water Movement
	public override void OnBlockUpdate(string type, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing, ChunkLoader cl){
		if(type == "break" || type == "change"){
			CastCoord thisPos = new CastCoord(new Vector3(myX, myY, myZ));
			ushort? thisState = cl.chunks[thisPos.GetChunkPos()].metadata.GetMetadata(thisPos.blockX, thisPos.blockY, thisPos.blockZ).state;


			/* Directional Level 1 State 
			 Deletes if not any highlevel water around */
			if(thisState >= 11 && thisState <= 18){
				GetCodeAround(myX, myY, myZ, cl);
				GetStateAround(myX, myY, myZ, cl);
				// If lone directional level 1 water
				if(!CheckHigherLevelWaterAround(thisPos.blockX, thisPos.blockY, thisPos.blockZ, 1, cl)){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					return;
				}
			}

			/* Still Level 1 State
			 Deletes if not around solid blocks */
			else if(thisState == 2){
				bool destroy = false;

				GetCodeAround(myX, myY, myZ, cl);
				for(int i=0; i<4; i++){
					if(!cl.blockBook.Get(this.aroundCodes[i]).solid){
						destroy = true;
						break;
					}
				}
				if(!cl.blockBook.Get((int)GetCodeBelow(myX, myY, myZ, cl)).solid)
					destroy = true;

				if(destroy){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);					
					return;
				}
			}

			/* Still Level 2 State */
			else if(thisState == 1){
				int? below = GetCodeBelow(myX, myY, myZ, cl);
				ushort? newState;

				// If Y chunk end
				if(below == null)
					return;

				// If air below, falls as Still Block
				else if(below == 0){

					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);

					cachedPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.waterCode);
					cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state = 1;
					this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
					cl.budscheduler.ScheduleReload(thisPos.GetChunkPos(), 1);
					return;
				}

				// If should be upgraded to Still 3
				if(GetHighLevelAroundCount(myX, myY, myZ, 2, cl) >= 2){
					cl.chunks[thisPos.GetChunkPos()].metadata.GetMetadata(thisPos.blockX, thisPos.blockY, thisPos.blockZ).state = 0;
					//this.EmitBlockUpdate("change", myX, myY, myZ, 1, cl);
					cl.budscheduler.ScheduleReload(thisPos.GetChunkPos(), 1);
					return;
				}

				// Handles normal behaviour of Still 2
				GetCodeAround(myX, myY, myZ, cl);
				GetStateAround(myX, myY, myZ, cl);
				for(int i=0; i<8; i++){
					if(this.aroundCodes[i] == 0){
						if(i == 0){ // North
							cachedPos = new CastCoord(new Vector3(myX, myY, myZ+1));
							newState = 11;
						}
						else if(i == 1 && this.aroundCodes[0] == this.waterCode && this.aroundStates[0] <= 1 && this.aroundStates[2] <= 1 && this.aroundCodes[2] == this.waterCode){ // NE
							cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ+1));
							newState = 12;
						}
						else if(i == 2){ // East
							cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ));
							newState = 13;
						}
						else if(i == 3 && this.aroundCodes[2] == this.waterCode && this.aroundStates[2] <= 1 && this.aroundStates[4] <= 1 && this.aroundCodes[4] == this.waterCode){ // SE
							cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ-1));
							newState = 14;
						}
						else if(i == 4){ // South
							cachedPos = new CastCoord(new Vector3(myX, myY, myZ-1));
							newState = 15;
						}
						else if(i == 5 && this.aroundCodes[4] == this.waterCode && this.aroundStates[4] <= 1 && this.aroundStates[6] <= 1 &&  this.aroundCodes[6] == this.waterCode){ // SW
							cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ-1));
							newState = 16;
						}
						else if(i == 6){ // West
							cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ));
							newState = 17;
						}
						else if(i == 7 && this.aroundCodes[6] == this.waterCode && this.aroundStates[6] <= 1 && this.aroundStates[0] <= 1 &&  this.aroundCodes[0] == this.waterCode){ // NW
							cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ));
							newState = 18;
						}
					else
						continue;

					cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.waterCode);
					cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state = newState;
					this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
					//this.EmitBlockUpdate("change", myX, myY, myZ, 1, cl);
					cl.budscheduler.ScheduleReload(thisPos.GetChunkPos(), 1);
					}
				}
			}

			/*
			Directional Level 2
			*/
			else if(thisState >= 3 && thisState <= 10){
				// Fall handler
				int? below = GetCodeBelow(myX, myY, myZ, cl);
				GetCodeAround(myX, myY, myZ, cl);
				GetStateAround(myX, myY, myZ, cl);

				if(below == 0){
					cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.waterCode);
					cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state = 20;
					this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
					//this.EmitBlockUpdate("change", myX, myY, myZ, 1, cl);
					cl.budscheduler.ScheduleReload(thisPos.GetChunkPos(), 1);
					return;
				}
				// Dies if no Still 3 around
				else if(!CheckHigherLevelWaterAround(myX, myY, myZ, 2, cl)){
					this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);
					return;					
				}
				

				// Does nothing if it's already a pivot of falling block
				else if(below == this.waterCode && GetStateBelow(myX, myY, myZ, cl) == 20){
					return;
				}
				// Upgrade if has two Still 3 adjascent
				else if(GetHighLevelAroundCount(myX, myY, myZ, 2, cl) >= 2){
					cl.chunks[thisPos.GetChunkPos()].metadata.GetMetadata(thisPos.blockX, thisPos.blockY, thisPos.blockZ).state = 0;
					this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
					//this.EmitBlockUpdate("change", myX, myY, myZ, 1, cl);
					cl.budscheduler.ScheduleReload(thisPos.GetChunkPos(), 1);
				}
				// Upgrade if has two Still 2 adjascent
				else if(GetSameLevelAroundCount(myX, myY, myZ, 2, cl) >= 2){
					cl.chunks[thisPos.GetChunkPos()].metadata.GetMetadata(thisPos.blockX, thisPos.blockY, thisPos.blockZ).state = 1;
					this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
					//this.EmitBlockUpdate("change", myX, myY, myZ, 1, cl);
					cl.budscheduler.ScheduleReload(thisPos.GetChunkPos(), 1);
				}

				// Normal Behaviour
				else{
					bool found;
					ushort? newState;

					for(int i=0; i<8; i++){
						found = false;

						// Ignores spread if block direction is illegal
						if(!this.spawnDirection[thisState].Contains(i))
							continue;

						if(this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && TranslateWaterLevel(this.aroundStates[i]) == 1)){
							if(i == 0 && (!(this.aroundCodes[7] == this.waterCode && TranslateWaterLevel(this.aroundStates[7]) > 1) && !(this.aroundCodes[1] == this.waterCode && TranslateWaterLevel(this.aroundStates[1]) > 1))){ // North
								cachedPos = new CastCoord(new Vector3(myX, myY, myZ+1));
								newState = 11;
								found = true;
							}
							else if(i == 1 && (!(this.aroundCodes[0] == this.waterCode && TranslateWaterLevel(this.aroundStates[0]) > 1) && !(this.aroundCodes[2] == this.waterCode && TranslateWaterLevel(this.aroundStates[2]) > 1))){ // NE
								cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ+1));
								newState = 12;
								found = true;
							}
							else if(i == 2 && (!(this.aroundCodes[1] == this.waterCode && TranslateWaterLevel(this.aroundStates[1]) > 1) && !(this.aroundCodes[3] == this.waterCode && TranslateWaterLevel(this.aroundStates[3]) > 1))){ // East
								cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ));
								newState = 13;
								found = true;
							}
							else if(i == 3 && (!(this.aroundCodes[2] == this.waterCode && TranslateWaterLevel(this.aroundStates[2]) > 1) && !(this.aroundCodes[4] == this.waterCode && TranslateWaterLevel(this.aroundStates[4]) > 1))){ // SE
								cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ-1));
								newState = 14;
								found = true;
							}
							else if(i == 4 && (!(this.aroundCodes[3] == this.waterCode && TranslateWaterLevel(this.aroundStates[3]) > 1) && !(this.aroundCodes[5] == this.waterCode && TranslateWaterLevel(this.aroundStates[5]) > 1))){ // South
								cachedPos = new CastCoord(new Vector3(myX, myY, myZ-1));
								newState = 15;
								found = true;
							}
							else if(i == 5 && (!(this.aroundCodes[4] == this.waterCode && TranslateWaterLevel(this.aroundStates[4]) > 1) && !(this.aroundCodes[6] == this.waterCode && TranslateWaterLevel(this.aroundStates[6]) > 1))){ // SW
								cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ-1));
								newState = 16;
								found = true;
							}
							else if(i == 6 && (!(this.aroundCodes[5] == this.waterCode && TranslateWaterLevel(this.aroundStates[5]) > 1) && !(this.aroundCodes[7] == this.waterCode && TranslateWaterLevel(this.aroundStates[7]) > 1))){ // West
								cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ));
								newState = 17;
								found = true;
							}
							else if(i == 7 && (!(this.aroundCodes[6] == this.waterCode && TranslateWaterLevel(this.aroundStates[6]) > 1) && !(this.aroundCodes[0] == this.waterCode && TranslateWaterLevel(this.aroundStates[0]) > 1))){ // NW
								cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ+1));
								newState = 18;
								found = true;
							}
							else{
								continue;
							}

							if(found){
								cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.waterCode);
								cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state = newState;
								this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);	
								cl.budscheduler.ScheduleReload(cachedPos.GetChunkPos(), 1);
							}			
						}
					}
				}
			}

			/*
			Still Level 3
			*/
			else if(thisState == 0){
				// Falling
				int? below = GetCodeBelow(myX, myY, myZ, cl);
				ushort? newState;

				// If Y chunk end
				if(below == null)
					return;

				// If air below
				else if(below == 0 || (below == this.waterCode && GetCodeBelow(myX, myY, myZ, cl) != 0)){
					GetCodeAround(myX, myY, myZ, cl);

					// Checks if all adjascent blocks are water, so it should spawn F3 State below
					if(this.aroundCodes[0] == this.waterCode && this.aroundCodes[2] == this.waterCode && this.aroundCodes[4] == this.waterCode && this.aroundCodes[6] == this.waterCode){
						cachedPos = new CastCoord(new Vector3(myX, myY-1, myZ));

						cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.waterCode);
						cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state = 19;
						this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
						//this.EmitBlockUpdate("change", myX, myY, myZ, 1, cl);
						cl.budscheduler.ScheduleReload(cachedPos.GetChunkPos(), 1);
						return;
					}
					// General case of making the block fall
					else{
						this.OnBreak(thisPos.GetChunkPos(), thisPos.blockX, thisPos.blockY, thisPos.blockZ, cl);

						cachedPos = new CastCoord(new Vector3(myX, myY-1, myZ));

						cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.waterCode);
						cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state = 0;
						this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
						//this.EmitBlockUpdate("change", myX, myY, myZ, 1, cl);
						cl.budscheduler.ScheduleReload(thisPos.GetChunkPos(), 1);
						return;
					}
				}

				// Normal Behaviour
				GetCodeAround(myX, myY, myZ, cl);
				GetStateAround(myX, myY, myZ, cl);

				for(int i=0; i<8; i++){
					if(this.aroundCodes[i] == 0 || this.aroundCodes[i] == this.waterCode){
						if(i == 0 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18))){ // North
							cachedPos = new CastCoord(new Vector3(myX, myY, myZ+1));
							newState = 3;
						}
						else if(i == 1 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18))){ // NE
							cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ+1));
							newState = 4;
						}
						else if(i == 2 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18))){ // East
							cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ));
							newState = 5;
						}
						else if(i == 3 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18))){ // SE
							cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ-1));
							newState = 6;
						}
						else if(i == 4 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18))){ // South
							cachedPos = new CastCoord(new Vector3(myX, myY, myZ-1));
							newState = 7;
						}
						else if(i == 5 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18))){ // SW
							cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ-1));
							newState = 8;
						}
						else if(i == 6 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18))){ // West
							cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ));
							newState = 9;
						}
						else if(i == 7 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18))){ // NW
							cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ+1));
							newState = 10;
						}
					else
						continue;	

					cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.waterCode);
					cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state = newState;
					this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
					cl.budscheduler.ScheduleReload(cachedPos.GetChunkPos(), 1);
					}
				}			
			}

			/*
			Falling 3
			*/
			else if(thisState == 19){
				
				int? below = GetCodeBelow(myX, myY, myZ, cl);
				int? above = GetCodeAbove(myX, myY, myZ, cl);

				// If needs to spawn more falling blocks (no return to check if alive)
				if(below == 0 || (below == this.waterCode && GetStateBelow(myX, myY, myZ, cl) != 0)){
					cachedPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.waterCode);
					cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state = 19;
					this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
					cl.budscheduler.ScheduleReload(thisPos.GetChunkPos(), 1);
				}

				// If not alive
				if(above != this.waterCode){
					this.OnBreak(thisPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
					//this.EmitBlockUpdate("change", myX, myY, myZ, 1, cl);
					cl.budscheduler.ScheduleReload(thisPos.GetChunkPos(), 1);
					return;
				}

				// Normal Behaviour
				if(below != 0){
					GetCodeAround(myX, myY, myZ, cl);
					GetStateAround(myX, myY, myZ, cl);
					ushort? newState;

					for(int i=0; i<8; i++){
						if(this.aroundCodes[i] == 0 || this.aroundCodes[i] == this.waterCode){
							if(i == 0 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18))){ // North
								cachedPos = new CastCoord(new Vector3(myX, myY, myZ+1));
								newState = 3;
							}
							else if(i == 1 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18)) && this.aroundCodes[0] == this.waterCode && this.aroundStates[0] <= 1 && this.aroundStates[2] <= 1 && this.aroundCodes[2] == this.waterCode){ // NE
								cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ+1));
								newState = 4;
							}
							else if(i == 2 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18))){ // East
								cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ));
								newState = 5;
							}
							else if(i == 3 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18)) && this.aroundCodes[2] == this.waterCode && this.aroundStates[2] <= 1 && this.aroundStates[4] <= 1 && this.aroundCodes[4] == this.waterCode){ // SE
								cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ-1));
								newState = 6;
							}
							else if(i == 4 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18))){ // South
								cachedPos = new CastCoord(new Vector3(myX, myY, myZ-1));
								newState = 7;
							}
							else if(i == 5 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18)) && this.aroundCodes[4] == this.waterCode && this.aroundStates[4] <= 1 && this.aroundStates[6] <= 1 &&  this.aroundCodes[6] == this.waterCode){ // SW
								cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ-1));
								newState = 8;
							}
							else if(i == 6 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18))){ // West
								cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ));
								newState = 9;
							}
							else if(i == 7 && (this.aroundCodes[i] == 0 || (this.aroundCodes[i] == this.waterCode && this.aroundStates[i] >= 11 && this.aroundStates[i] <= 18)) && this.aroundCodes[6] == this.waterCode && this.aroundStates[6] <= 1 && this.aroundStates[0] <= 1 &&  this.aroundCodes[0] == this.waterCode){ // NW
								cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ));
								newState = 10;
							}
						else
							continue;	

						cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.waterCode);
						cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state = newState;
						this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
						//this.EmitBlockUpdate("change", myX, myY, myZ, 1, cl);
						cl.budscheduler.ScheduleReload(thisPos.GetChunkPos(), 1);
						}
					}					
				}
			}

			/*
			Falling 2
			*/
			else if(thisState == 20){
				
				int? below = GetCodeBelow(myX, myY, myZ, cl);
				int? above = GetCodeAbove(myX, myY, myZ, cl);

				// If needs to spawn more falling blocks (no return to check if alive)
				if(below == 0 || (below == this.waterCode && GetStateBelow(myX, myY, myZ, cl) > 1)){
					cachedPos = new CastCoord(new Vector3(myX, myY-1, myZ));

					cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.waterCode);
					cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state = 20;
					this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
					//this.EmitBlockUpdate("change", myX, myY, myZ, 1, cl);
					cl.budscheduler.ScheduleReload(thisPos.GetChunkPos(), 1);
				}

				// If not alive
				if(above != this.waterCode){
					this.OnBreak(thisPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, cl);
					//this.EmitBlockUpdate("change", myX, myY, myZ, 1, cl);
					cl.budscheduler.ScheduleReload(thisPos.GetChunkPos(), 1);
					return;
				}

				// Normal Behaviour
				if(below != 0){
					GetCodeAround(myX, myY, myZ, cl);
					GetStateAround(myX, myY, myZ, cl);
					ushort? newState;

					for(int i=0; i<8; i++){
						if(this.aroundCodes[i] == 0 || this.aroundCodes[i] == this.waterCode){
							if(i == 0){ // North
								cachedPos = new CastCoord(new Vector3(myX, myY, myZ+1));
								newState = 11;
							}
							else if(i == 1 && this.aroundCodes[0] == this.waterCode && this.aroundStates[0] <= 1 && this.aroundStates[2] <= 1 && this.aroundCodes[2] == this.waterCode){ // NE
								cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ+1));
								newState = 12;
							}
							else if(i == 2){ // East
								cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ));
								newState = 13;
							}
							else if(i == 3 && this.aroundCodes[2] == this.waterCode && this.aroundStates[2] <= 1 && this.aroundStates[4] <= 1 && this.aroundCodes[4] == this.waterCode){ // SE
								cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ-1));
								newState = 14;
							}
							else if(i == 4){ // South
								cachedPos = new CastCoord(new Vector3(myX, myY, myZ-1));
								newState = 15;
							}
							else if(i == 5 && this.aroundCodes[4] == this.waterCode && this.aroundStates[4] <= 1 && this.aroundStates[6] <= 1 &&  this.aroundCodes[6] == this.waterCode){ // SW
								cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ-1));
								newState = 16;
							}
							else if(i == 6){ // West
								cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ));
								newState = 17;
							}
							else if(i == 7 && this.aroundCodes[6] == this.waterCode && this.aroundStates[6] <= 1 && this.aroundStates[0] <= 1 &&  this.aroundCodes[0] == this.waterCode){ // NW
								cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ));
								newState = 18;
							}
						else
							continue;

						cl.chunks[cachedPos.GetChunkPos()].data.SetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, this.waterCode);
						cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state = newState;
						this.OnPlace(cachedPos.GetChunkPos(), cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ, -1, cl);
						//this.EmitBlockUpdate("change", myX, myY, myZ, 1, cl);
						cl.budscheduler.ScheduleReload(thisPos.GetChunkPos(), 1);
						}
					}					
				}				
			}
		}
	}


	// Gets Code of block below
	private int? GetCodeBelow(int myX, int myY, int myZ, ChunkLoader cl){
		CastCoord cord = new CastCoord(new Vector3(myX, myY-1, myZ));
		if(myY-1 < 0)
			return null;

		return cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
	}

	// Gets State of block below
	private ushort? GetStateBelow(int myX, int myY, int myZ, ChunkLoader cl){
		CastCoord cord = new CastCoord(new Vector3(myX, myY-1, myZ));
		if(myY-1 < 0)
			return null;

		return cl.chunks[cord.GetChunkPos()].metadata.GetMetadata(cord.blockX, cord.blockY, cord.blockZ).state;
	}

	// Gets Code of block above
	private int? GetCodeAbove(int myX, int myY, int myZ, ChunkLoader cl){
		CastCoord cord = new CastCoord(new Vector3(myX, myY+1, myZ));
		if(myY+1 >= Chunk.chunkDepth)
			return null;

		return cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
	}

	// Gets State of block above
	private ushort? GetStateAbove(int myX, int myY, int myZ, ChunkLoader cl){
		CastCoord cord = new CastCoord(new Vector3(myX, myY+1, myZ));
		if(myY+1 < 0)
			return null;

		return cl.chunks[cord.GetChunkPos()].metadata.GetMetadata(cord.blockX, cord.blockY, cord.blockZ).state;
	}

	// Gets a list of block codes of around blocks
	// Order is N Clockwise
	private void GetCodeAround(int myX, int myY, int myZ, ChunkLoader cl){
		CastCoord cord;
		cord = new CastCoord(new Vector3(myX, myY, myZ+1)); // North
		this.aroundCodes[0] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		cord = new CastCoord(new Vector3(myX+1, myY, myZ+1)); // NE
		this.aroundCodes[1] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		cord = new CastCoord(new Vector3(myX+1, myY, myZ)); // East
		this.aroundCodes[2] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		cord = new CastCoord(new Vector3(myX+1, myY, myZ-1)); // SE
		this.aroundCodes[3] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		cord = new CastCoord(new Vector3(myX, myY, myZ-1)); // South
		this.aroundCodes[4] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		cord = new CastCoord(new Vector3(myX-1, myY, myZ-1)); // SW
		this.aroundCodes[5] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		cord = new CastCoord(new Vector3(myX-1, myY, myZ)); // West
		this.aroundCodes[6] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
		cord = new CastCoord(new Vector3(myX-1, myY, myZ+1)); // NW
		this.aroundCodes[7] = cl.chunks[cord.GetChunkPos()].data.GetCell(cord.blockX, cord.blockY, cord.blockZ);
	}

	// Gets a list of states of around blocks if they are water
	private void GetStateAround(int myX, int myY, int myZ, ChunkLoader cl){
		cachedPos = new CastCoord(new Vector3(myX, myY, myZ+1)); // North
		if(cl.chunks[cachedPos.GetChunkPos()].data.GetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ) == this.waterCode)
			this.aroundStates[0] = cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state;
		else
			this.aroundStates[0] = null;

		cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ+1)); // NE
		if(cl.chunks[cachedPos.GetChunkPos()].data.GetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ) == this.waterCode)
			this.aroundStates[1] = cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state;
		else
			this.aroundStates[1] = null;

		cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ)); // East
		if(cl.chunks[cachedPos.GetChunkPos()].data.GetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ) == this.waterCode)
			this.aroundStates[2] = cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state;
		else
			this.aroundStates[2] = null;

		cachedPos = new CastCoord(new Vector3(myX+1, myY, myZ-1)); // SE
		if(cl.chunks[cachedPos.GetChunkPos()].data.GetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ) == this.waterCode)
			this.aroundStates[3] = cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state;
		else
			this.aroundStates[3] = null;

		cachedPos = new CastCoord(new Vector3(myX, myY, myZ-1)); // South
		if(cl.chunks[cachedPos.GetChunkPos()].data.GetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ) == this.waterCode)
			this.aroundStates[4] = cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state;
		else
			this.aroundStates[4] = null;

		cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ-1)); // SW
		if(cl.chunks[cachedPos.GetChunkPos()].data.GetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ) == this.waterCode)
			this.aroundStates[5] = cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state;
		else
			this.aroundStates[5] = null;

		cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ)); // West
		if(cl.chunks[cachedPos.GetChunkPos()].data.GetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ) == this.waterCode)
			this.aroundStates[6] = cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state;
		else
			this.aroundStates[6] = null;

		cachedPos = new CastCoord(new Vector3(myX-1, myY, myZ+1)); // NW
		if(cl.chunks[cachedPos.GetChunkPos()].data.GetCell(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ) == this.waterCode)
			this.aroundStates[7] = cl.chunks[cachedPos.GetChunkPos()].metadata.GetMetadata(cachedPos.blockX, cachedPos.blockY, cachedPos.blockZ).state;
		else
			this.aroundStates[7] = null;	
	}

	// Gets Water Level based on state table
	private int TranslateWaterLevel(ushort? state){
		if(state == null)
			return 0;
		else if(state == 2 || (state >= 11 && state <= 18))
			return 1;
		else if(state == 1 || (state >= 3 && state <= 10) || state == 20)
			return 2;
		else if(state == 0 || state == 19)
			return 3;
		else
			return 0;
	}

	// Checks if there is any high level water to this block
	private bool CheckHigherLevelWaterAround(int myX, int myY, int myZ, int currentWaterLevel, ChunkLoader cl){
		for(int i=0; i<8; i++){
			if(this.aroundCodes[i] == this.waterCode && TranslateWaterLevel(this.aroundStates[i]) > currentWaterLevel){
				return true;
			}				
		}
		return false;
	}

	// Checks the amount of high level water ONLY IN ADJASCENT blocks
	private int GetHighLevelAroundCount(int x, int y, int z, int currentWaterLevel, ChunkLoader cl){
		int count=0;

		for(int i=0; i<8; i+=2){
			if(this.aroundCodes[i] == this.waterCode && TranslateWaterLevel(this.aroundStates[i]) > currentWaterLevel){
				count++;
			}
		}

		return count;
	}

	// Checks the amount of same level water ONLY IN ADJASCENT blocks
	private int GetSameLevelAroundCount(int x, int y, int z, int currentWaterLevel, ChunkLoader cl){
		int count=0;

		for(int i=0; i<8; i+=2){
			if(this.aroundCodes[i] == this.waterCode && TranslateWaterLevel(this.aroundStates[i]) == 3){
				count++;
			}				
		}

		return count;
	}
}
