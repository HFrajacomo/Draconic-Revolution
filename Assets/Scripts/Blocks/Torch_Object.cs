using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;


/*
STATES:
	0 = Unlit facing X+
	1 = Unlit facing Z-
	2 = Unlit facing X-
	3 = Unlit facing Z+
	4 = Lit facing X+
	5 = Lit facing Z-
	6 = Lit facing X-
	7 = Lit facing Z+
*/
public class Torch_Object : BlocklikeObject
{
	public GameObject fireVFX;

	public Torch_Object(){
		this.name = "Torch";
		this.solid = false;
		this.transparent = true;
		this.invisible = false;
		this.liquid = false;
		this.washable = true;
		this.hasLoadEvent = true;
		this.go = GameObject.Find("----- PrefabObjects -----/Torch_Object");
		this.mesh = this.go.GetComponent<MeshFilter>().sharedMesh;
		this.scaling = new Vector3(10, 20, 10);

		this.needsRotation = true;
		this.stateNumber = 8;

		this.fireVFX = GameObject.Find("----- PrefabVFX -----/FireVFX");
	}

	// Turns on and off Torch
	public override int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){
		ushort state = cl.chunks[pos].metadata.GetState(blockX,blockY,blockZ);

		if(state == ushort.MaxValue)
			return 0;
		else if(state >= 4){
			cl.chunks[pos].metadata.AddToState(blockX,blockY,blockZ, -4);
			ControlFire(pos, blockX, blockY, blockZ, (ushort?)(state-4));
		}
		else if(state >= 0 && state < 4){
			cl.chunks[pos].metadata.AddToState(blockX,blockY,blockZ, 4);
			ControlFire(pos, blockX, blockY, blockZ, (ushort?)(state+4));
		}

		return 4;
	}

	// Instantiates a FireVFX
	public override int OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ChunkLoader cl){
		Vector3 fireOffset;

		if(facing == 0)
			fireOffset = new Vector3(0.15f,0f,0f);
		else if(facing == 1)
			fireOffset = new Vector3(0f,0f,-0.15f);
		else if(facing == 2)
			fireOffset = new Vector3(-0.15f, 0f, 0f);
		else if(facing == 3)
			fireOffset = new Vector3(0f, 0f, 0.15f);
		else
			fireOffset = new Vector3(0f,0f,0f);

		GameObject fire = GameObject.Instantiate(this.fireVFX, new Vector3(pos.x*Chunk.chunkWidth + blockX, blockY + 0.35f, pos.z*Chunk.chunkWidth + blockZ) + fireOffset, Quaternion.identity);
		fire.name = BuildVFXName(pos, blockX, blockY, blockZ);

		this.vfx.Add(pos, fire, active:true);
		
		cl.chunks[pos].metadata.SetState(blockX,blockY,blockZ, (ushort)facing);

		return 0;
	}

	// Destroys FireVFX
	public override int OnBreak(ChunkPos pos, int x, int y, int z, ChunkLoader cl){
		string name = BuildVFXName(pos,x,y,z);
		this.vfx.Remove(pos, name);
		EraseMetadata(pos,x,y,z,cl);
		return 0;
	}

	// Creates FireVFX on Load
	public override int OnLoad(ChunkPos pos, int x, int y, int z, ChunkLoader cl){
		ushort? state = cl.chunks[pos].metadata.GetState(x,y,z);
		Vector3 fireOffset;

		if(state == 0 || state == 4)
			fireOffset = new Vector3(0.15f,0f,0f);
		else if(state == 1 || state == 5)
			fireOffset = new Vector3(0f,0f,-0.15f);
		else if(state == 2 || state == 6)
			fireOffset = new Vector3(-0.15f, 0f, 0f);
		else if(state == 3 || state == 7)
			fireOffset = new Vector3(0f, 0f, 0.15f);
		else
			fireOffset = new Vector3(0f,0f,0f);	

		GameObject fire = GameObject.Instantiate(this.fireVFX, new Vector3(pos.x*Chunk.chunkWidth + x, y + 0.35f, pos.z*Chunk.chunkWidth + z) + fireOffset, Quaternion.identity);
		fire.name = BuildVFXName(pos, x, y, z);
		this.vfx.Add(pos, fire, active:true);

		ControlFire(pos, x, y, z, state);
	
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
	public override bool PlacementRule(ChunkPos pos, int x, int y, int z, int direction, ChunkLoader cl){	
		// If is stuck to walls
		if(direction <= 3 && direction >= 0){
			int blockCode;
			if(direction == 2){
				if(x > 0){
					blockCode = cl.chunks[pos].data.GetCell(x-1,y,z);
					if(blockCode >= 0){
						if(cl.blockBook.blocks[blockCode].solid){
							return true;
						}
					}
					else{
						if(cl.blockBook.objects[ushort.MaxValue-blockCode].solid){
							return true;
						}
					}
				}
				else{
					ChunkPos newPos = new ChunkPos(pos.x-1, pos.z);
					blockCode = cl.chunks[newPos].data.GetCell(Chunk.chunkWidth-1,y,z);
					if(blockCode >= 0){
						if(cl.blockBook.blocks[blockCode].solid){
							return true;
						}
					}
					else{
						if(cl.blockBook.objects[ushort.MaxValue-blockCode].solid){
							return true;
						}
					}
				}
			}
			else if(direction == 0){
				if(x < Chunk.chunkWidth-1){
					blockCode = cl.chunks[pos].data.GetCell(x+1,y,z);
					if(blockCode >= 0){
						if(cl.blockBook.blocks[blockCode].solid){
							return true;
						}
					}
					else{
						if(cl.blockBook.objects[ushort.MaxValue-blockCode].solid){
							return true;
						}
					}
				}
				else{
					ChunkPos newPos = new ChunkPos(pos.x+1, pos.z);
					blockCode = cl.chunks[newPos].data.GetCell(0,y,z);
					if(blockCode >= 0){
						if(cl.blockBook.blocks[blockCode].solid){
							return true;
						}
					}
					else{
						if(cl.blockBook.objects[ushort.MaxValue-blockCode].solid){
							return true;
						}
					}
				}
			}
			else if(direction == 3){
				if(z < Chunk.chunkWidth-1){
					blockCode = cl.chunks[pos].data.GetCell(x,y,z+1);
					if(blockCode >= 0){
						if(cl.blockBook.blocks[blockCode].solid){
							return true;
						}
					}
					else{
						if(cl.blockBook.objects[ushort.MaxValue-blockCode].solid){
							return true;
						}
					}
				}
				else{
					ChunkPos newPos = new ChunkPos(pos.x, pos.z+1);
					blockCode = cl.chunks[newPos].data.GetCell(x,y,0);
					if(blockCode >= 0){
						if(cl.blockBook.blocks[blockCode].solid){
							return true;
						}
					}
					else{
						if(cl.blockBook.objects[ushort.MaxValue-blockCode].solid){
							return true;
						}
					}
				}
			}
			else if(direction == 1){
				if(z > 0){
					blockCode = cl.chunks[pos].data.GetCell(x,y,z-1);
					if(blockCode >= 0){
						if(cl.blockBook.blocks[blockCode].solid){
							return true;
						}
					}
					else{
						if(cl.blockBook.objects[ushort.MaxValue-blockCode].solid){
							return true;
						}
					}
				}
				else{
					ChunkPos newPos = new ChunkPos(pos.x, pos.z-1);
					blockCode = cl.chunks[newPos].data.GetCell(x,y,Chunk.chunkWidth-1);
					if(blockCode >= 0){
						if(cl.blockBook.blocks[blockCode].solid){
							return true;
						}
					}
					else{
						if(cl.blockBook.objects[ushort.MaxValue-blockCode].solid){
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	// Breaks Torch if broken
	public override void OnBlockUpdate(string type, int x, int y, int z, int budX, int budY, int budZ, int facing, ChunkLoader cl){
		if(facing >= 4){
			return;
		}

		CastCoord aux = new CastCoord(new Vector3(x,y,z));
		ChunkPos thisPos = aux.GetChunkPos(); //new ChunkPos(Mathf.FloorToInt(x/Chunk.chunkWidth), Mathf.FloorToInt(z/Chunk.chunkWidth));
		int X = aux.blockX; //x%Chunk.chunkWidth;
		int Y = aux.blockY; //y%Chunk.chunkDepth;
		int Z = aux.blockZ; //z%Chunk.chunkWidth;
		aux = new CastCoord(new Vector3(budX, budY, budZ));
		ChunkPos budPos = aux.GetChunkPos(); //new ChunkPos(Mathf.FloorToInt(budX/Chunk.chunkWidth), Mathf.FloorToInt(budZ/Chunk.chunkWidth));
		int bX = aux.blockX; //budX%Chunk.chunkWidth;
		int bY = aux.blockY; //budY%Chunk.chunkDepth;
		int bZ = aux.blockZ; //budZ%Chunk.chunkWidth;

		ushort state = cl.chunks[thisPos].metadata.GetState(X,Y,Z);

		// Breaks Torch if broken attached block
		if(type == "break" && (facing == state || facing+4 == state)){
			cl.chunks[thisPos].data.SetCell(X, Y, Z, 0);
			this.OnBreak(thisPos, X, Y, Z, cl);
			EraseMetadata(thisPos, X, Y, Z, cl);		
		}
		// Breaks Torch if changed block is not solid
		else if(type == "change"){
			int blockCode = cl.chunks[budPos].data.GetCell(bX,bY,bZ);

			if(blockCode >= 0){
				// If changed block is not solid, break
				if(!cl.blockBook.blocks[blockCode].solid){
					cl.chunks[thisPos].data.SetCell(X, Y, Z, 0);
					this.OnBreak(thisPos, X, Y, Z, cl);
					EraseMetadata(thisPos, X, Y, Z, cl);					
				}
			}
			else{
				if(!cl.blockBook.objects[(blockCode*-1)-1].solid){
					cl.chunks[thisPos].data.SetCell(X, Y, Z, 0);
					this.OnBreak(thisPos, X, Y, Z, cl);
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
