using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using TMPro;

public class PlayerRaycast : MonoBehaviour
{
	public Camera playerCamera;
	private static readonly string WORLD_SCREENSHOT_NAME = "world_screenshot.png";

	public ChunkLoader loader;
	private Vector3 cachePos;
	public CastCoord current;
	private CastCoord previousCoord;
	private CastCoord lastCoord;
	private const int objectLayerMask = 1 << 11;

	// Raycast Settings
	public Transform cam;
	private Vector3 position;
	private Vector3 direction;
	public float reach = 4.0f;
	public float step = 0.025f;

	// Optimization Set
	private HashSet<CastCoord> alreadyVisited = new HashSet<CastCoord>();

	// Decal Specifics Test
	private ushort blockDamage = 65534;

	// Prefab System
	private bool prefabSetFlag = false;
	private CastCoord prefabPos = new CastCoord(false);

	// Current player block position
	private CastCoord playerHead;
	private CastCoord playerBody;
	public MainControllerManager control;

	public ushort lastBlockPlaced = 0;
	public PlayerEvents playerEvents;

	// Cached
	private BUDSignal cachedBUD;
	private RaycastHit cachedHit;

	/*
	0 = X+ => side
	1 = Z- => side
	2 = X- => side
	3 = Z+ => side
	4 = Y- => ceiling
	5 = Y+ => ground
	*/
	public int facing;


	public bool Raycast(){
		if(!loader.WORLD_GENERATED)
			return false;

		// Updates player block position
		this.playerHead = new CastCoord(cam.position);
		this.playerBody = new CastCoord(cam.position);
		this.playerBody.blockY -= 1;

		float traveledDistance = 0f;
		ushort blockCode = 0;
		lastCoord = new CastCoord(false);
		previousCoord = new CastCoord(false);
		bool FOUND = false;

		// Raycast Detection
		position = cam.position;
		direction = Vector3.Normalize(cam.forward);
		cachePos = position;

		// Shoots Raycast
		while(traveledDistance <= reach){
			cachePos = cachePos + (direction*step);
			traveledDistance += step;
			current = new CastCoord(cachePos);
			blockCode = loader.GetBlock(current);

			if(alreadyVisited.Contains(current) && blockCode <= ushort.MaxValue/2){
				continue;
			}

			// Out of bounds control
			if(current.blockY >= Chunk.chunkDepth || current.blockY < 0f){
				return false;
			}

			// Checks for solid block hit
			// Checks for hit
			if(HitNonLiquid(current, traveledDistance)){
				FOUND = true;
				break;
			}

			if(previousCoord.active){
				if(!CastCoord.Eq(previousCoord, current)){
					lastCoord = previousCoord;
				}
			}

			previousCoord = current;

			alreadyVisited.Add(current);
		}

		alreadyVisited.Clear();

		if(!FOUND){
			current = new CastCoord(false);
			return false;
		}
		else{
			if(blockCode <= ushort.MaxValue/2)
				lastCoord = previousCoord;
		}
		
		facing = current - lastCoord;

		return true;
	}

	// Detects hit of solid block
	public bool HitSolid(CastCoord coords){
		ChunkPos ck = new ChunkPos(coords.chunkX, coords.chunkZ, coords.chunkY);

		if(!loader.Contains(ck))
			return false;

		ushort blockID = loader.Get(ck).data.GetCell(coords.blockX, coords.blockY, coords.blockZ);

		// If hits a full block
		if(loader.Contains(ck)){
			if(VoxelLoader.CheckSolid(blockID)){
				return true;
			}
		}
		
		return false;
	}

	// Detects hit of solid block
	public bool HitNonLiquid(CastCoord coords, float traveledDistance){
		ChunkPos ck = new ChunkPos(coords.chunkX, coords.chunkZ, coords.chunkY);

		if(!loader.Contains(ck))
			return false;

		ushort blockID = loader.Get(ck).data.GetCell(coords.blockX, coords.blockY, coords.blockZ);

		// If hits a full block
		if(loader.Contains(ck)){
			if(!VoxelLoader.CheckLiquid(blockID) && blockID != 0 && blockID <= ushort.MaxValue/2){
				return true;
			}
			else if(blockID > ushort.MaxValue/2){
				CastCoord castedCoord;
				if(Physics.Raycast(position, direction, out cachedHit, maxDistance:traveledDistance, layerMask:objectLayerMask)){
					castedCoord = new CastCoord(cachedHit.point);
					if(CastCoord.Eq(coords, castedCoord)){
						return true;
					}
				}
			}
		}
		
		return false;
	}

	// Detects hit in any block except air
	public bool HitAll(CastCoord coords, float traveledDistance){
		ChunkPos ck = new ChunkPos(coords.chunkX, coords.chunkZ, coords.chunkY);

		// Exception
		if(!loader.Contains(ck)){
			return false;
		}

		ushort blockID = loader.Get(ck).data.GetCell(coords.blockX, coords.blockY, coords.blockZ);

		// If hits something
		if(blockID != VoxelLoader.GetBlockID("BASE_Air") && (ushort)blockID <= ushort.MaxValue/2){
			if(loader.Contains(ck)){ 
				return true;
			}
		}
		// If is an objects, raycast against its hitbox
		else if((ushort)blockID > ushort.MaxValue/2){
			CastCoord castedCoord;
			if(Physics.Raycast(position, direction, out cachedHit, maxDistance:traveledDistance, layerMask:objectLayerMask)){
				castedCoord = new CastCoord(cachedHit.point);
				if(CastCoord.Eq(coords, castedCoord)){
					return true;
				}
			}
		}

		return false;
	}

	// Block Breaking mechanic
	public void BreakBlock(){
		if(!Raycast()){
			return;
		}

		ChunkPos toUpdate = new ChunkPos(current.chunkX, current.chunkZ, current.chunkY);
		ushort blockCode = loader.Get(toUpdate).data.GetCell(current.blockX, current.blockY, current.blockZ);
		ushort state = loader.Get(toUpdate).metadata.GetState(current.blockX, current.blockY, current.blockZ);
		ushort hp = loader.Get(toUpdate).metadata.GetHP(current.blockX, current.blockY, current.blockZ);

		NetMessage message = new NetMessage(NetCode.BLOCKDAMAGE);
		message.BlockDamage(current.GetChunkPos(), current.blockX, current.blockY, current.blockZ, this.blockDamage, false);
		this.loader.client.Send(message);
	}

	// Uses item in hand
	public void UseItem(){
		// If ain't aiming at anything
		if(!Raycast())
			return;

		ItemStack its = playerEvents.GetSlotStack();

		// If is holding no items
		if(its == null){
			return;
		}

		Item it = its.GetItem();

		it.OnUseClient(this.loader, its, this.position, lastCoord, playerBody, playerHead, current);
	}

	// Triggers Blocktype.OnInteract()
	public void Interact(){		
		if(!Raycast())
			return;

		/*
		Debug.Log("Name: " + VoxelLoader.CheckName(loader.Get(lastCoord.GetChunkPos()).data.GetCell(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ)) + " | State: " + loader.Get(lastCoord.GetChunkPos()).metadata.GetState(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ) +  "\nShadowMap: " + loader.Get(lastCoord.GetChunkPos()).data.GetShadow(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ) + "    " + loader.Get(lastCoord.GetChunkPos()).data.GetShadow(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, isNatural:false) + " -> (" + lastCoord.blockX + ", " + lastCoord.blockY + ", " + lastCoord.blockZ + ")\n" +
		"LightMap: " + loader.Get(lastCoord.GetChunkPos()).data.GetLight(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ) + "   " + loader.Get(lastCoord.GetChunkPos()).data.GetLight(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ, isNatural:false) + " -> (" + lastCoord.blockX + ", " + lastCoord.blockY + ", " + lastCoord.blockZ + ")\n" + 
		"\t\tState: " + loader.Get(lastCoord.GetChunkPos()).metadata.GetState(lastCoord.blockX, lastCoord.blockY, lastCoord.blockZ)  + "\n" +
		"HeightMap: " + loader.Get(lastCoord.GetChunkPos()).data.GetHeight((byte)lastCoord.blockX, (byte)lastCoord.blockZ) + "\n" +
		"RenderMap: " + loader.Get(lastCoord.GetChunkPos()).data.GetRender((byte)lastCoord.blockX, (byte)lastCoord.blockZ));
		*/
		
		ChunkPos toUpdate = new ChunkPos(current.chunkX, current.chunkZ, current.chunkY);
		int blockCode = loader.Get(toUpdate).data.GetCell(current.blockX, current.blockY, current.blockZ);
		
		NetMessage message = new NetMessage(NetCode.INTERACT);
		message.Interact(toUpdate, current.blockX, current.blockY, current.blockZ, facing);
		this.loader.client.Send(message);
	}

	// Sets the Camera FOV
	public void SetFOV(){
		cam.GetComponent<Camera>().fieldOfView = Configurations.fieldOfView;
	}
	
	public void TakeWorldScreenshot(){
		loader.gameUI.SetActive(false);

        RenderTexture renderTexture = new RenderTexture(Screen.width, (int)(Screen.height/2), 24);
        this.playerCamera.targetTexture = renderTexture;

        Texture2D screenshotTexture = new Texture2D(Screen.width, (int)(Screen.height/2), TextureFormat.RGB24, false);
        this.playerCamera.Render();
        RenderTexture.active = renderTexture;

        screenshotTexture.ReadPixels(new Rect(0, 0, Screen.width, (int)(Screen.height/2)), 0, 0);
        screenshotTexture.Apply();

        this.playerCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);

        byte[] bytes = screenshotTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(EnvironmentVariablesCentral.saveDir + World.worldName + "/" + WORLD_SCREENSHOT_NAME, bytes);
	}

	// Runs Prefab read and returns the arrays needed to create the prefab
	public void PrefabRead(bool blockBased){
		if(!Raycast())
			return;

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
			StringBuilder sbBlock = new StringBuilder();
			StringBuilder sbHp = new StringBuilder();
			StringBuilder sbState = new StringBuilder();

			CastCoord finalPos;
			ChunkPos newPos;
			sbBlock.Append("{");
			sbHp.Append("{");
			sbState.Append("{");

			int xCount = 0;
			int zCount = 0;
			int yCount = 0;

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
			yCount = finalPos.chunkY - prefabPos.chunkY;


			int x,y,z;
			int xEnd, zEnd, yEnd;

			for(int yChunk=0; yChunk <= yCount; yChunk++){
				// Y Spec
				if(yChunk == 0 && yChunk == yCount){
					y = prefabPos.blockY;
					yEnd = finalPos.blockY;
				}
				else if(yChunk == 0 && yChunk != yCount){
					y = prefabPos.blockY;
					yEnd = Chunk.chunkDepth;
				}
				else if(yChunk != yCount){
					y = 0;
					yEnd = finalPos.blockY;
				}
				else{
					y = 0;
					yEnd = finalPos.blockY;
				}

				for(; y < yEnd; y++){
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
								newPos = new ChunkPos(prefabPos.chunkX + xChunk, prefabPos.chunkZ + zChunk, 3);

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
									sbBlock.Append(loader.Get(newPos).data.GetCell(x,y,z).ToString());
									sbBlock.Append(",");

									if(loader.Get(newPos).metadata.IsUnassigned(x,y,z)){
										sbHp.Append("0,");
										sbState.Append("0,");
									}
									else{
										if(loader.Get(newPos).metadata.IsHPNull(x,y,z)){
											sbHp.Append("0,");
										}
										else{
											sbHp.Append(loader.Get(newPos).metadata.GetHP(x,y,z));
											sbHp.Append(",");
										}

										if(loader.Get(newPos).metadata.IsStateNull(x,y,z)){
											sbState.Append("0,");
										}
										else{
											sbState.Append(loader.Get(newPos).metadata.GetState(x,y,z));
											sbState.Append(",");
										}
									}
								}
							}
						}
					}
				}
			}

			sbBlock.Append("}");
			sbHp.Append("}");
			sbState.Append("}");

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
			file.WriteLine(Compression.FormatPrereadInformation(sbBlock.ToString()));
			file.WriteLine("\n\nHP:\n");
			file.WriteLine(Compression.FormatPrereadInformation(sbHp.ToString()));
			file.WriteLine("\n\nState:\n");
			file.WriteLine(Compression.FormatPrereadInformation(sbState.ToString()));
			file.WriteLine("\nSizes: " + sizeX + " | " + sizeY + " | " + sizeZ + "		[" + sizeX*sizeY*sizeZ + "]\n\n\n\n");
			file.Close();
		}

		this.prefabSetFlag = !this.prefabSetFlag;
	}
}