﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using Unity.Mathematics;

/*
STATES:
	0 = Lit facing X+
	1 = Lit facing Z-
	2 = Lit facing X-
	3 = Lit facing Z+
	4 = Unlit facing X+
	5 = Unlit facing Z-
	6 = Unlit facing X-
	7 = Unlit facing Z+
*/
public class Torch_Object : BlocklikeObject
{
	public GameObject fireVFX;
	private AudioName audio = AudioName.TORCHFIRE;

	public Torch_Object(bool isClient){
		this.name = "Torch";
		this.solid = false;
		this.transparent = 1;
		this.invisible = false;
		this.liquid = false;
		this.washable = true;
		this.hasLoadEvent = true;
		this.affectLight = false;
		this.luminosity = 8 + (3 << 4);

		this.maxHP = 1;

		if(isClient){
			this.go = GameObject.Find("----- PrefabObjects -----/Torch_Object");
			this.hitboxObject = GameObject.Find("----- PrefabObjects -----/Torch_Object/Hitbox");
			this.mesh = this.go.GetComponent<MeshFilter>().sharedMesh;
			this.scaling = new Vector3(10, 20, 10);
			this.hitboxScaling = new Vector3(0.22f, 0.76f, 0.25f);
			this.fireVFX = GameObject.Find("----- PrefabVFX -----/FireVFX");
			this.hitboxMesh = hitboxObject.GetComponent<MeshFilter>().sharedMesh;
		}
		else{
			this.fireVFX = null;
		}

		this.needsRotation = true;
		this.stateNumber = 8;

        this.droppedItem = Item.GenerateItem(ItemID.TORCH);
        this.minDropQuantity = 1;
        this.maxDropQuantity = 1;
    }

	// Turns on and off Torch
	public override int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader_Server cl){
		ushort state = cl.chunks[pos].metadata.GetState(blockX,blockY,blockZ);
		ushort newState = 0;

		if(state == ushort.MaxValue)
			return 0;
		else if(state >= 4){
			cl.chunks[pos].metadata.AddToState(blockX,blockY,blockZ, -4);
			newState = (ushort)(state-4);
		}
		else if(state >= 0 && state < 4){
			cl.chunks[pos].metadata.AddToState(blockX,blockY,blockZ, 4);
			newState = (ushort)(state+4);
		}

		// Init VFX
		NetMessage message = new NetMessage(NetCode.VFXCHANGE);
		message.VFXChange(pos, blockX, blockY, blockZ, 0, ushort.MaxValue, newState);
		cl.server.SendToClients(pos, message);

		// Init SFX
		NetMessage SFXMessage = new NetMessage(NetCode.SFXPLAY);
		SFXMessage.SFXPlay(pos, blockX, blockY, blockZ, (ushort)BlockID.TORCH, newState);
		cl.server.SendToClients(pos, SFXMessage);

		return 1;
	}

	// Instantiates a FireVFX
	public override int OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ChunkLoader_Server cl){
		cl.chunks[pos].metadata.SetState(blockX,blockY,blockZ, (ushort)facing);

		// Init VFX
		NetMessage message = new NetMessage(NetCode.VFXDATA);
		message.VFXData(pos, blockX, blockY, blockZ, facing, ushort.MaxValue, (ushort)facing);
		cl.vfx.Add(pos, BuildVFXName(pos, blockX, blockY, blockZ), message);
		cl.server.SendToClients(pos, message);

		// Init SFX
		NetMessage SFXMessage = new NetMessage(NetCode.SFXPLAY);
		SFXMessage.SFXPlay(pos, blockX, blockY, blockZ, (ushort)BlockID.TORCH, cl.chunks[pos].metadata.GetState(blockX, blockY, blockZ));
		cl.server.SendToClients(pos, SFXMessage);
		return 0;
	}

	// Client handling the creation of the VFX
	public override int OnVFXBuild(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ushort state, ChunkLoader cl){
		Vector3 fireOffset;
		Quaternion shadowDirection;

		if(facing == 0){
			fireOffset = new Vector3(0.15f,0f,0f);
			shadowDirection = Quaternion.Euler(-20, 90, 90);
		}
		else if(facing == 1){
			fireOffset = new Vector3(0f,0f,-0.15f);
			shadowDirection = Quaternion.Euler(0, 90, 110);
		}
		else if(facing == 2){
			fireOffset = new Vector3(-0.15f, 0f, 0f);
			shadowDirection = Quaternion.Euler(20, 90, 90);
		}
		else if(facing == 3){
			fireOffset = new Vector3(0f, 0f, 0.15f);
			shadowDirection = Quaternion.Euler(0, 90, 70);
		}
		else{
			fireOffset = new Vector3(0f,0f,0f);
			shadowDirection = Quaternion.Euler(0, 0, 0);
		}

		GameObject fire = GameObject.Instantiate(this.fireVFX, new Vector3(pos.x*Chunk.chunkWidth + blockX, blockY + 0.35f + pos.y*Chunk.chunkDepth, pos.z*Chunk.chunkWidth + blockZ) + fireOffset, shadowDirection);
		fire.name = BuildVFXName(pos, blockX, blockY, blockZ);

		this.vfx.Add(pos, fire, active:true, isOnDemandLight:true);
		ControlFire(pos, blockX, blockY, blockZ, state);

		return 0;		
	}

	public override int OnVFXChange(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ushort state, ChunkLoader cl){
		ControlFire(pos, blockX, blockY, blockZ, state);
		return 0;
	}

	public override int OnVFXBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ushort state, ChunkLoader cl){
		this.vfx.Remove(pos, BuildVFXName(pos, blockX, blockY, blockZ), isOnDemandLight:true);
		return 0;
	}

	public override int OnSFXPlay(ChunkPos pos, int blockX, int blockY, int blockZ, ushort state, ChunkLoader cl){
		if(state < 4){
			cl.sfx.LoadBlockSFX(this.audio, pos, blockX, blockY, blockZ);
			return 1;
		}
		else{
			cl.sfx.RemoveBlockSFX(pos, blockX, blockY, blockZ);
			return 2;
		}
	}

	// Destroys FireVFX and SFX
	public override int OnBreak(ChunkPos pos, int x, int y, int z, ChunkLoader_Server cl){
		CastCoord coord = new CastCoord(pos, x, y, z);

        cl.server.entityHandler.AddItem(new float3(coord.GetWorldX(), coord.GetWorldY()+Constants.ITEM_ENTITY_SPAWN_HEIGHT_BONUS, coord.GetWorldZ()),
            Item.GenerateForceVector(), this.droppedItem, Item.RandomizeDropQuantity(minDropQuantity, maxDropQuantity), cl);

		NetMessage message = new NetMessage(NetCode.VFXBREAK);
		message.VFXBreak(pos, x, y, z, ushort.MaxValue, 0);
		cl.server.SendToClients(pos, message);

		// Init SFX
		NetMessage SFXMessage = new NetMessage(NetCode.SFXPLAY);
		SFXMessage.SFXPlay(pos, x, y, z, (ushort)BlockID.TORCH, ushort.MaxValue);
		cl.server.SendToClients(pos, SFXMessage);

		EraseMetadata(pos,x,y,z,cl);
		return 0;
	}

	// Creates FireVFX and SFX on Load
	public override int OnLoad(CastCoord coord, ChunkLoader_Server cl){
		ushort state = cl.chunks[coord.GetChunkPos()].metadata.GetState(coord.blockX, coord.blockY, coord.blockZ);
		int facing;

		if(state >= 4)
			facing = state-4;
		else
			facing = state;

		// Init VFX
		NetMessage message = new NetMessage(NetCode.VFXDATA);
		message.VFXData(coord.GetChunkPos(), coord.blockX, coord.blockY, coord.blockZ, facing, ushort.MaxValue, state);
		
		cl.vfx.Add(coord.GetChunkPos(), BuildVFXName(coord.GetChunkPos(), coord.blockX, coord.blockY, coord.blockZ), message);
		cl.server.SendToClients(coord.GetChunkPos(), message);

		// Init SFX
		NetMessage SFXMessage = new NetMessage(NetCode.SFXPLAY);
		SFXMessage.SFXPlay(coord.GetChunkPos(), coord.blockX, coord.blockY, coord.blockZ, (ushort)BlockID.TORCH, state);
		cl.server.SendToClients(coord.GetChunkPos(), SFXMessage);
		return 1;
	}

	// Handles Fire VFX turning on and off
	private void ControlFire(ChunkPos pos, int x, int y, int z, ushort? state){
		// Turns off fire when it's on
		if(state >= 4)
			this.vfx.data[pos][BuildVFXName(pos, x, y, z)].SetActive(false);
		// Turns on fire when it's off
		else if(state < 4)
			this.vfx.data[pos][BuildVFXName(pos, x, y, z)].SetActive(true);
	}	

	// Builds name for current Torch VFX
	private string BuildVFXName(ChunkPos pos, int x, int y, int z){
		return "Torch (" + pos.x + ", " + pos.z + ") [" + x + ", " + y + ", " + z + "]";

	}

	// Only able to place torches on walls and solid blocks
	public override bool PlacementRule(ChunkPos pos, int x, int y, int z, int direction, ChunkLoader_Server cl){	
		// If is stuck to walls
		if(direction <= 3 && direction >= 0){
			ushort blockCode;
			if(direction == 2){
				if(x > 0){
					blockCode = cl.chunks[pos].data.GetCell(x-1,y,z);
					if(cl.blockBook.CheckSolid(blockCode))
						return true;
					else
						return false;
				}
				else{
					ChunkPos newPos = new ChunkPos(pos.x-1, pos.z, pos.y);
					blockCode = cl.chunks[newPos].data.GetCell(Chunk.chunkWidth-1,y,z);
					if(cl.blockBook.CheckSolid(blockCode))
						return true;
					else
						return false;
				}
			}
			else if(direction == 0){
				if(x < Chunk.chunkWidth-1){
					blockCode = cl.chunks[pos].data.GetCell(x+1,y,z);
					if(cl.blockBook.CheckSolid(blockCode))
						return true;
					else
						return false;
				}
				else{
					ChunkPos newPos = new ChunkPos(pos.x+1, pos.z, pos.y);
					blockCode = cl.chunks[newPos].data.GetCell(0,y,z);
					if(cl.blockBook.CheckSolid(blockCode))
						return true;
					else
						return false;
				}
			}
			else if(direction == 3){
				if(z < Chunk.chunkWidth-1){
					blockCode = cl.chunks[pos].data.GetCell(x,y,z+1);
					if(cl.blockBook.CheckSolid(blockCode))
						return true;
					else
						return false;
				}
				else{
					ChunkPos newPos = new ChunkPos(pos.x, pos.z+1, pos.y);
					blockCode = cl.chunks[newPos].data.GetCell(x,y,0);
					if(cl.blockBook.CheckSolid(blockCode))
						return true;
					else
						return false;
				}
			}
			else if(direction == 1){
				if(z > 0){
					blockCode = cl.chunks[pos].data.GetCell(x,y,z-1);
					if(cl.blockBook.CheckSolid(blockCode))
						return true;
					else
						return false;
				}
				else{
					ChunkPos newPos = new ChunkPos(pos.x, pos.z-1, pos.y);
					blockCode = cl.chunks[newPos].data.GetCell(x,y,Chunk.chunkWidth-1);
					if(cl.blockBook.CheckSolid(blockCode))
						return true;
					else
						return false;
				}
			}
		}
		return false;
	}

	// Breaks Torch if broken
	public override void OnBlockUpdate(BUDCode type, int x, int y, int z, int budX, int budY, int budZ, int facing, ChunkLoader_Server cl){
		if(facing >= 4){
			return;
		}

		CastCoord aux = new CastCoord(new Vector3(x,y,z));

		if(type == BUDCode.LOAD){
			this.OnLoad(aux, cl);
		}

		ChunkPos thisPos = aux.GetChunkPos();
		int X = aux.blockX;
		int Y = aux.blockY;
		int Z = aux.blockZ;
		aux = new CastCoord(new Vector3(budX, budY, budZ));
		ChunkPos budPos = aux.GetChunkPos();
		int bX = aux.blockX;
		int bY = aux.blockY;
		int bZ = aux.blockZ;

		ushort state = cl.chunks[thisPos].metadata.GetState(X,Y,Z);

		// Breaks Torch if broken attached block
		if(type == BUDCode.BREAK && (facing == state || facing+4 == state)){
			cl.chunks[thisPos].data.SetCell(X, Y, Z, 0);
			this.OnBreak(thisPos, X, Y, Z, cl);

			NetMessage message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
			message.DirectBlockUpdate(BUDCode.BREAK, thisPos, X, Y, Z, facing, ushort.MaxValue, state, 0);
			cl.server.SendToClients(thisPos, message);

			EraseMetadata(thisPos, X, Y, Z, cl);		
		}
		// Breaks Torch if changed block is not solid
		else if(type == BUDCode.CHANGE && (facing == state || facing+4 == state)){
			int blockCode = cl.chunks[budPos].data.GetCell(bX,bY,bZ);

			if(blockCode <= ushort.MaxValue/2){
				// If changed block is not solid, break
				if(!cl.blockBook.blocks[blockCode].solid){
					cl.chunks[thisPos].data.SetCell(X, Y, Z, 0);
					this.OnBreak(thisPos, X, Y, Z, cl);

					NetMessage message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
					message.DirectBlockUpdate(BUDCode.BREAK, thisPos, X, Y, Z, facing, ushort.MaxValue, state, 0);
					cl.server.SendToClients(thisPos, message);

					EraseMetadata(thisPos, X, Y, Z, cl);					
				}
			}
			else{
				if(!cl.blockBook.objects[ushort.MaxValue-blockCode].solid){
					cl.chunks[thisPos].data.SetCell(X, Y, Z, 0);
					this.OnBreak(thisPos, X, Y, Z, cl);

					NetMessage message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
					message.DirectBlockUpdate(BUDCode.BREAK, thisPos, X, Y, Z, facing, ushort.MaxValue, state, 0);
					cl.server.SendToClients(thisPos, message);

					EraseMetadata(thisPos, X, Y, Z, cl);					
				}
			}
		}
	}

	// Applies Rotation to block in Chunk.BuildChunk()
	public override void ApplyRotation(GameObject go, ushort? stt, int blockX, int blockY, int blockZ){
		ushort? state = stt;
		Transform t = go.GetComponent<Transform>();

		if(state == 0 || state == 4){
			t.Rotate(0, -90,0);
			t.position += new Vector3(0.4f,-0.2f,0f);
		}
		else if(state == 3 || state == 7){
			t.Rotate(0, 180, 0);
			t.position += new Vector3(0f,-0.2f,0.4f);
		}
		else if(state == 2 || state == 6){
			t.Rotate(0, 90, 0);
			t.position += new Vector3(-0.4f,-0.2f,0f);
		}
		else{
			t.position += new Vector3(0f,-0.2f,-0.4f);
		}
	}

	// Functions for the new Bursting Core Rendering
	public override Vector3 GetOffsetVector(ushort state){
		if(state == 0 || state == 4)
			return new Vector3(0.4f, -0.2f, 0f);
		else if(state == 3 || state == 7)
			return new Vector3(0f, -0.2f, 0.4f);
		else if(state == 2 || state == 6)
			return new Vector3(-0.4f, -0.2f, 0f);
		else
			return new Vector3(0f, -0.2f, -0.4f);
	}

	// Get rotation in degrees
	public override int GetRotationValue(ushort state){
		if(state == 0 || state == 4)
			return 90;
		else if(state == 3 || state == 7)
			return 180;
		else if(state == 2 || state == 6)
			return -90;
		else
			return 0;
	}

}