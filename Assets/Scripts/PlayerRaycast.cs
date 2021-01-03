using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerRaycast : MonoBehaviour
{
  public ChunkLoader loader;
	public Transform cam;
	public float reach = 4.0f;
	public float step = 0.025f;
	private Vector3 position;
	private Vector3 direction;
	private Vector3 cachePos;
	public CastCoord current;
	private CastCoord lastCoord;


	// Selected Block
	public ushort blockToBePlaced = 1;
	public TextMeshProUGUI blockNameUI;

	// Prefab System
	private bool prefabSetFlag = false;
	private CastCoord prefabPos = new CastCoord(false);

	// Current player block position
	private CastCoord playerHead;
	private CastCoord playerBody;
	public MainControllerManager control;

	// Cached
	private BUDSignal cachedBUD;

	/*
	0 = X+ => side
	1 = Z- => side
	2 = X- => side
	3 = Z+ => side
	4 = Y- => ceiling
	5 = Y+ => ground
	*/
	public int facing;


		public void Start(){
			this.blockToBePlaced = 1;
			blockNameUI.text = "Grass";
		}

		// Update is called once per frame
		void Update()
		{
			if(!loader.WORLD_GENERATED)
				return;

			// Updates player block position
			playerHead = new CastCoord(cam.position);
			playerBody = new CastCoord(cam.position);
			playerBody.blockY -= 1;

			float traveledDistance = 0f;
			lastCoord = new CastCoord(false);
			bool FOUND = false;

			// Raycast Detection
			position = cam.position;
			direction = Vector3.Normalize(cam.forward);
			cachePos = position;

			// Shoots Raycast
			while(traveledDistance <= reach){
				cachePos = cachePos + (direction*step);
				current = new CastCoord(cachePos);

				// Out of bounds control
				if(current.blockY >= Chunk.chunkDepth || current.blockY < 0f){
					return;
				}

				// Checks for solid block hit
				// Checks for hit
				if(HitAll(current)){ // HitSolid
					FOUND = true;
					break;
				}

				traveledDistance += step;
			}

			if(!FOUND){
				current = new CastCoord(false);
			}
			else
				lastCoord = new CastCoord(cachePos - direction*step);
			
			facing = current - lastCoord;

			// Click to break block
			if(control.primaryAction){
				BreakBlock();
				control.primaryAction = false;
			}

			// Click to place block
			if(control.secondaryAction){
				PlaceBlock(blockToBePlaced);
				control.secondaryAction = false;
			}

			// Click for interact
			if(control.interact){
				Interact();
				control.interact = false;
			}

			if(control.prefabRead || control.prefabReadAir){
				PrefabRead(control.prefabRead);

				this.prefabSetFlag = !this.prefabSetFlag;
				control.prefabRead = false;
				control.prefabReadAir = false;
			}


		}

		// Detects hit of solid block
		public bool HitSolid(CastCoord coords){
			ChunkPos ck = new ChunkPos(coords.chunkX, coords.chunkZ);
			ushort blockID = loader.chunks[ck].data.GetCell(coords.blockX, coords.blockY, coords.blockZ);

			// If hits a full block
			if(blockID <= ushort.MaxValue/2){
				if(loader.chunks.ContainsKey(ck)){
					if(loader.blockBook.blocks[blockID].solid){
						return true;
					}
				}
			}
				// If hits an Asset
			else{
				if(loader.chunks.ContainsKey(ck)){
					blockID = (ushort)(ushort.MaxValue - blockID);
					if(loader.blockBook.objects[blockID].solid){
						return true;
					}
				}
			}
			return false;
		}

		// Detects hit in any block except air
		public bool HitAll(CastCoord coords){
			ChunkPos ck = new ChunkPos(coords.chunkX, coords.chunkZ);

			// Exception
			if(!loader.chunks.ContainsKey(ck)){
				return false;
			}

			ushort blockID = loader.chunks[ck].data.GetCell(coords.blockX, coords.blockY, coords.blockZ);

			// If hits something
			if(blockID != 0)
				if(loader.chunks.ContainsKey(ck)){
					// DEBUG 
					//print(blockID + " : " + loader.chunks[ck].metadata.GetState(coords.blockX, coords.blockY, coords.blockZ));
					return true;
				}
			return false;
		}

		// Block Breaking mechanic
		private void BreakBlock(){
			if(!current.active){
				return;
			}

			ChunkPos toUpdate = new ChunkPos(current.chunkX, current.chunkZ);
			ushort blockCode = loader.chunks[toUpdate].data.GetCell(current.blockX, current.blockY, current.blockZ);

			// If doesn't has special break handling
			if(!loader.blockBook.CheckCustomBreak(blockCode)){

				// Actually breaks new block and updates chunk
				loader.chunks[toUpdate].data.SetCell(current.blockX, current.blockY, current.blockZ, 0);
				loader.chunks[toUpdate].metadata.Reset(current.blockX, current.blockY, current.blockZ);

				// Triggers OnBreak
				if(blockCode <= ushort.MaxValue/2)
					loader.blockBook.blocks[blockCode].OnBreak(toUpdate, current.blockX, current.blockY, current.blockZ, loader);
				else
					loader.blockBook.objects[ushort.MaxValue - blockCode].OnBreak(toUpdate, current.blockX, current.blockY, current.blockZ, loader);

				EmitBlockUpdate("break", current.GetWorldX(), current.GetWorldY(), current.GetWorldZ(), 0, loader);
				
				loader.budscheduler.ScheduleReload(toUpdate, 0, x:current.blockX, y:current.blockY, z:current.blockZ);

			}
			// If has special break handlings
			else{

				if(blockCode <= ushort.MaxValue/2){
					loader.blockBook.blocks[blockCode].OnBreak(toUpdate, current.blockX, current.blockY, current.blockZ, loader);
				}
				else{
					loader.blockBook.objects[ushort.MaxValue - blockCode].OnBreak(toUpdate, current.blockX, current.blockY, current.blockZ, loader);
					loader.regionHandler.SaveChunk(loader.chunks[toUpdate]);
				}
			}

		}

		// Block Placing mechanic
		private void PlaceBlock(ushort blockCode){
			int translatedBlockCode;

			// Encodes for Block Mode
			if(blockCode <= ushort.MaxValue/2){
				translatedBlockCode = blockCode;

				// Won't happen if not raycasting something or if block is in player's body or head
				if(!current.active || (CastCoord.Eq(lastCoord, playerHead) && loader.blockBook.CheckSolid(blockCode)) || (CastCoord.Eq(lastCoord, playerBody) && loader.blockBook.CheckSolid(blockCode))){
					return;
				}
			}
			// Encodes for Asset Mode
			else{
				translatedBlockCode = ushort.MaxValue - blockCode;

				// Won't happen if not raycasting something or if block is in player's body or head
				if(!current.active || (CastCoord.Eq(lastCoord, playerHead) && loader.blockBook.CheckSolid(blockCode)) || (CastCoord.Eq(lastCoord, playerBody) && loader.blockBook.CheckSolid(blockCode))){
					return;
				}
			}

			ChunkPos toUpdate = new ChunkPos(lastCoord.chunkX, lastCoord.chunkZ);

			// Checks if specific block has specific placement rules that may hinder the placement
			if(blockCode <= ushort.MaxValue/2){
				if(!loader.blockBook.blocks[translatedBlockCode].PlacementRule(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, loader)){
					return;
				}
			}
			else{
				if(!loader.blockBook.objects[translatedBlockCode].PlacementRule(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, loader)){
					return;
				}
			}

			// If doesn't have special place handling
			if(!loader.blockBook.CheckCustomPlace(blockCode)){
				// Actually places block/asset into terrain
				loader.chunks[toUpdate].data.SetCell(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode);
				loader.budscheduler.ScheduleReload(toUpdate, 0);

				// Applies OnPlace Event
				if(blockCode <= ushort.MaxValue/2)
					loader.blockBook.blocks[translatedBlockCode].OnPlace(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, loader);
				else{
					loader.blockBook.objects[translatedBlockCode].OnPlace(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, loader);
				}
			}

			// If has special handling
			else{
				// Actually places block/asset into terrain
				loader.chunks[toUpdate].data.SetCell(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, blockCode);

				if(blockCode <= ushort.MaxValue/2){
					loader.blockBook.blocks[blockCode].OnPlace(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, loader);
				}
				else{
					loader.blockBook.objects[translatedBlockCode].OnPlace(toUpdate, lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, loader);
				}

			}
		

		}

		// Triggers Blocktype.OnInteract()
		private void Interact(){
			if(!current.active)
				return;

			ChunkPos toUpdate = new ChunkPos(current.chunkX, current.chunkZ);
			int callback;
			int blockCode = loader.chunks[toUpdate].data.GetCell(current.blockX, current.blockY, current.blockZ);
			
			
			if(blockCode <= ushort.MaxValue/2)
				callback = loader.blockBook.blocks[blockCode].OnInteract(toUpdate, current.blockX, current.blockY, current.blockZ, loader);
			else
				callback = loader.blockBook.objects[ushort.MaxValue - blockCode].OnInteract(toUpdate, current.blockX, current.blockY, current.blockZ, loader);

			// Actual handling of message
			CallbackHandler(callback, toUpdate, current, facing);
		}

		public void Scroll1(){
			this.blockToBePlaced = 1;
			blockNameUI.text = loader.blockBook.CheckName(1);
		}
		public void Scroll2(){
			this.blockToBePlaced = 2;
			blockNameUI.text = loader.blockBook.CheckName(2);
		}
		public void Scroll3(){
			this.blockToBePlaced = 3;
			blockNameUI.text = loader.blockBook.CheckName(3);
		}
		public void Scroll4(){
			this.blockToBePlaced = 4;
			blockNameUI.text = loader.blockBook.CheckName(4);
		}
		public void Scroll5(){
			this.blockToBePlaced = 5;
			blockNameUI.text = loader.blockBook.CheckName(5);
		}
		public void Scroll6(){
			this.blockToBePlaced = 6;
			blockNameUI.text = loader.blockBook.CheckName(6);
		}
		public void Scroll7(){
			this.blockToBePlaced = ushort.MaxValue;
			blockNameUI.text = loader.blockBook.CheckName(ushort.MaxValue);
		}
		public void Scroll8(){
			this.blockToBePlaced = ushort.MaxValue-1;
			blockNameUI.text = loader.blockBook.CheckName(ushort.MaxValue-1);
		}



		// Runs Prefab read and returns the arrays needed to create the prefab
		private void PrefabRead(bool blockBased){
			if(!current.active)
				return;

				Debug.Log(current.RealPos());

			// If first position is not set
			if(!this.prefabSetFlag){
				Debug.Log("Maked First Position");

				if(blockBased){
					this.prefabPos = current.Copy();
				}
				else{
					this.prefabPos = lastCoord.Copy();
				}
			}

			// If this is last position to be set
			else{

				CastCoord finalPos;
				ChunkPos newPos;
				string outBlock = "{";
				string outHp = "{";
				string outState = "{";

				int xCount = 0;
				int zCount = 0;

				if(blockBased){
					finalPos = current.Copy();
					finalPos = finalPos.Add(1,1,1);
				}
				else{
					finalPos = lastCoord.Copy();
					finalPos = finalPos.Add(1,1,1);
				}

				xCount = finalPos.chunkX - prefabPos.chunkX;
				zCount = finalPos.chunkZ - prefabPos.chunkZ;


				int x,y,z;
				int xEnd, zEnd;

				for(y=prefabPos.blockY; y < finalPos.blockY; y++){
					for(int xChunk=0; xChunk <= xCount; xChunk++){

						// X Spec
						if(xChunk == 0 && xChunk == xCount){
							x = prefabPos.blockX;
							xEnd = finalPos.blockX;
						}
						else if(xChunk == 0 && xChunk != xCount){
							x = prefabPos.blockX;
							xEnd = Chunk.chunkWidth;
						}
						else if(xChunk != xCount){
							x = 0;
							xEnd = Chunk.chunkWidth;
						}
						else{
							x = 0;
							xEnd = finalPos.blockX;
						}

						for(; x < xEnd; x++){
							for(int zChunk=0; zChunk <= zCount; zChunk++){
								newPos = new ChunkPos(prefabPos.chunkX + xChunk, prefabPos.chunkZ + zChunk);

								// Z Spec
								if(zChunk == 0 && zChunk == zCount){
									z = prefabPos.blockZ;
									zEnd = finalPos.blockZ;
								}
								else if(zChunk == 0 && zChunk != zCount){
									z = prefabPos.blockZ;
									zEnd = Chunk.chunkWidth;
								}
								else if(zChunk != zCount){
									z = 0;
									zEnd = Chunk.chunkWidth;
								}
								else{
									z = 0;
									zEnd = finalPos.blockZ;
								}

								for(; z < zEnd; z++){
									outBlock += loader.chunks[newPos].data.GetCell(x,y,z).ToString();
									outBlock += ",";

									if(loader.chunks[newPos].metadata.IsUnassigned(x,y,z)){
										outHp += "null,";
										outState += "null,";
									}
									else{
										if(loader.chunks[newPos].metadata.IsHPNull(x,y,z)){
											outHp += "null,";
										}
										else{
											outHp += loader.chunks[newPos].metadata.GetHP(x,y,z);
											outHp += ",";
										}

										if(loader.chunks[newPos].metadata.IsStateNull(x,y,z)){
											outState += "null,";
										}
										else{
											outState += loader.chunks[newPos].metadata.GetState(x,y,z);
											outState += ",";
										}
									}
								}
							}
						}
					}
				}

			int sizeX, sizeZ, sizeY;

			// Calculates Struct Size
			if(xCount >= 1)
				sizeX = (Chunk.chunkWidth - prefabPos.blockX) + (xCount-1)*Chunk.chunkWidth + finalPos.blockX;
			else
				sizeX = finalPos.blockX - prefabPos.blockX;

			if(zCount >= 1)
				sizeZ = (Chunk.chunkWidth - prefabPos.blockZ) + (zCount-1)*Chunk.chunkWidth + finalPos.blockZ;
			else
				sizeZ = finalPos.blockZ - prefabPos.blockZ;

			sizeY = finalPos.blockY - prefabPos.blockY;

			StreamWriter file = new StreamWriter("SavedStruct.txt", append:true);

			file.WriteLine("Blocks:\n");
			file.WriteLine(outBlock + "}");
			file.WriteLine("\n\nHP:\n");
			file.WriteLine(outHp + "}");
			file.WriteLine("\n\nState:\n");
			file.WriteLine(outState + "}");
			file.WriteLine("\nSizes: " + sizeX + " | " + sizeY + " | " + sizeZ + "		[" + sizeX*sizeY*sizeZ + "]\n\n\n\n");
			file.Close();

			}
		}

		/*
		Main Callback function for block interactions
		(REFER TO THESE CODES WHENEVER ADDING NEW BLOCK INTERACTIONS)
		(MAY BE NEEDED IN ORDER TO IMPLEMENT NEW POST HANDLERS FOR NEW BLOCKS)
		*/
		private void CallbackHandler(int code, ChunkPos targetChunk, CastCoord thisPos, int facing){
			// 0: No further actions necessary
			if(code == 0)
				return;
			// 1: Interaction forces the target chunk to reload/rebuild
			else if(code == 1){
				loader.chunks[targetChunk].BuildChunk();
				loader.chunks[targetChunk].BuildSideBorder(reload:true);
				loader.regionHandler.SaveChunk(loader.chunks[targetChunk]);
			}
			// 2: Emits BUD instantly and forces chunk reload
			else if(code == 2){
				EmitBlockUpdate("change", current.GetWorldX(), current.GetWorldY(), current.GetWorldZ(), 0, loader);
				loader.budscheduler.ScheduleReload(targetChunk, 0);	
			}
			// 3: Emits BUD in next tick and forces chunk reload
			else if(code == 3){
				EmitBlockUpdate("change", current.GetWorldX(), current.GetWorldY(), current.GetWorldZ(), 1, loader);
				loader.budscheduler.ScheduleReload(targetChunk, 1);
			}
			// 4: Saves chunk to RDF file silently
			else if(code == 4){
				loader.regionHandler.SaveChunk(loader.chunks[targetChunk]);
			}

		}

		// Handles the emittion of BUD to neighboring blocks
		public void EmitBlockUpdate(string type, int x, int y, int z, int tickOffset, ChunkLoader cl){
			CastCoord thisPos = GetCoordinates(x, y, z);

			CastCoord[] neighbors = {
			thisPos.Add(1,0,0),
			thisPos.Add(-1,0,0),
			thisPos.Add(0,1,0),
			thisPos.Add(0,-1,0),
			thisPos.Add(0,0,1),
			thisPos.Add(0,0,-1)
			};

			int[] facings = {2,0,4,5,1,3};


			int blockCode;
			int faceCounter=0;

			foreach(CastCoord c in neighbors){
				// Ignores void updates
				if(c.blockY < 0 || c.blockY > Chunk.chunkDepth-1){
					continue;
				}

				blockCode = cl.chunks[c.GetChunkPos()].data.GetCell(c.blockX, c.blockY, c.blockZ);

				cachedBUD = new BUDSignal(type, c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), facings[faceCounter]);
				cl.budscheduler.ScheduleBUD(cachedBUD, tickOffset);			 
			
				faceCounter++;
			}
		}

		private CastCoord GetCoordinates(int x, int y, int z){
			return new CastCoord(new Vector3(x ,y ,z));
		}

		// Checks if neighbor chunk from chunkpos needs to update it's sides
		private void UpdateNeighborChunk(ChunkPos pos, int x, int y, int z){
			if(x == 0)
				loader.chunks[new ChunkPos(pos.x-1, pos.z)].BuildSideBorder(reloadXM:true);
			else if(x == Chunk.chunkWidth-1)
				loader.chunks[new ChunkPos(pos.x+1, pos.z)].BuildSideBorder(reloadXP:true);
			else if(z == 0)
				loader.chunks[new ChunkPos(pos.x, pos.z-1)].BuildSideBorder(reloadZM:true);
			else if(z == Chunk.chunkWidth-1)
				loader.chunks[new ChunkPos(pos.x, pos.z+1)].BuildSideBorder(reloadZP:true);
		}

}