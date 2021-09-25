using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
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

	// Prefab System
	private bool prefabSetFlag = false;
	private CastCoord prefabPos = new CastCoord(false);

	// Current player block position
	private CastCoord playerHead;
	private CastCoord playerBody;
	public MainControllerManager control;

	public static ItemID lastBlockPlaced = (ItemID)0;
	public PlayerEvents playerEvents;

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


	public void Start(){}

	
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
			if(HitSolid(current)){ // HitSolid
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

		if(!loader.chunks.ContainsKey(ck))
			return false;

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
	public void BreakBlock(){
		if(!current.active){
			return;
		}

		ChunkPos toUpdate = new ChunkPos(current.chunkX, current.chunkZ);
		ushort blockCode = loader.chunks[toUpdate].data.GetCell(current.blockX, current.blockY, current.blockZ);
		ushort state = loader.chunks[toUpdate].metadata.GetState(current.blockX, current.blockY, current.blockZ);
		ushort hp = loader.chunks[toUpdate].metadata.GetHP(current.blockX, current.blockY, current.blockZ);

		NetMessage message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
		message.DirectBlockUpdate(BUDCode.BREAK, current.GetChunkPos(), current.blockX, current.blockY, current.blockZ, facing, blockCode, state, hp);
		this.loader.client.Send(message.GetMessage(), message.size);
	}

	// Uses item in hand
	public void UseItem(){
		// If ain't aiming at anything
		if(!current.active)
			return;

		ItemStack its = playerEvents.GetSlotStack();

		// If is holding no items
		if(its == null){
			return;
		}

		Item it = its.GetItem();

		// If is a placeable item
		if(it is IPlaceable){
			IPlaceable itPlaceable = it as IPlaceable;
			// If block placement was successful in client
			if(this.PlaceBlock(itPlaceable.placeableBlockID)){
				PlayerRaycast.lastBlockPlaced = it.id;
				if(its.Decrement()){
					playerEvents.hotbar.SetNull(PlayerEvents.hotbarSlot);
				}
				playerEvents.DrawHotbarSlot(PlayerEvents.hotbarSlot);
				playerEvents.invUIPlayer.DrawSlot(1, PlayerEvents.hotbarSlot);
			}
		}
	}

	// Block Placing mechanic
	private bool PlaceBlock(ushort blockCode){
		// Won't happen if not raycasting something or if block is in player's body or head
		if(!current.active || (CastCoord.Eq(lastCoord, playerHead) && loader.blockBook.CheckSolid(blockCode)) || (CastCoord.Eq(lastCoord, playerBody) && loader.blockBook.CheckSolid(blockCode))){
			return false;
		}

		NetMessage message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
		message.DirectBlockUpdate(BUDCode.PLACE, lastCoord.GetChunkPos(), lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, facing, blockCode, ushort.MaxValue, ushort.MaxValue);
		this.loader.client.Send(message.GetMessage(), message.size);
		return true;
	}

	// Triggers Blocktype.OnInteract()
	public void Interact(){
		if(!current.active)
			return;

		ChunkPos toUpdate = new ChunkPos(current.chunkX, current.chunkZ);
		int blockCode = loader.chunks[toUpdate].data.GetCell(current.blockX, current.blockY, current.blockZ);
		
		NetMessage message = new NetMessage(NetCode.INTERACT);
		message.Interact(toUpdate, current.blockX, current.blockY, current.blockZ, facing);
		this.loader.client.Send(message.GetMessage(), message.size);
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