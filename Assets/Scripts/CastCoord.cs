using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct CastCoord{
	public int chunkX;
	public int chunkZ;
	public int blockX;
	public int blockY;
	public int blockZ;
	public bool active;

	public CastCoord(bool a){
		chunkX = 0;
		chunkZ = 0;
		blockX = 0;
		blockZ = 0;
		blockY = 0;
		active = false;
	}

	public CastCoord(Vector3 mark){
	    Vector3 nMark = new Vector3(mark.x, mark.y, mark.z);

	    // X Processing
	    if(nMark.x >= 0)
	      nMark.x = (int)Math.Round(nMark.x, MidpointRounding.AwayFromZero);
	    else
	      nMark.x = (int)(nMark.x - 0.5f);

	    chunkX = Mathf.FloorToInt(nMark.x/Chunk.chunkWidth);


	    if(chunkX >= 0)
	      blockX = Mathf.FloorToInt(nMark.x%Chunk.chunkWidth);
	    else
	      blockX = Mathf.CeilToInt(((Chunk.chunkWidth*-chunkX)+nMark.x)%Chunk.chunkWidth); 

	    // Z Processing
	    if(nMark.z >= 0)
	      nMark.z = (int)Math.Round(nMark.z, MidpointRounding.AwayFromZero);
	    else
	      nMark.z = (int)(nMark.z - 0.5f);

	    chunkZ = Mathf.FloorToInt(nMark.z/Chunk.chunkWidth);

	    if(chunkZ >= 0)
	      blockZ = Mathf.FloorToInt(nMark.z%Chunk.chunkWidth);
	    else
	      blockZ = Mathf.CeilToInt(((Chunk.chunkWidth*-chunkZ)+nMark.z)%Chunk.chunkWidth);



	    blockY = Mathf.RoundToInt(nMark.y);
	 	active = true;
	}

	public CastCoord(ChunkPos pos, int x, int y, int z){
		chunkX = pos.x;
		chunkZ = pos.z;
		blockX = x;
		blockY = y;
		blockZ = z;
		active = true;
	}


	public int GetWorldX(){
	  return Chunk.chunkWidth*chunkX+blockX;
	}

	public int GetWorldY(){
	  return blockY;
	}

	public int GetWorldZ(){
	  return Chunk.chunkWidth*chunkZ+blockZ;
	}

	public ChunkPos GetChunkPos(){
	  return new ChunkPos(chunkX, chunkZ);
	}

	// Adds and returns a rebuilt CastCoord
	public CastCoord Add(int x, int y, int z){
	  return new CastCoord(new Vector3(Chunk.chunkWidth*chunkX+blockX+x, blockY+y, Chunk.chunkWidth*chunkZ+blockZ+z));
	}

	public override string ToString(){
		return "ChunkX: " + chunkX + "\tChunkZ: " + chunkZ + "\tX, Y, Z: " + blockX + ", " + blockY + ", " + blockZ;
	}

	public static int operator-(CastCoord a, CastCoord b){
		int x,y,z;

		if(!b.active)
			return -1;

		x = (a.chunkX*Chunk.chunkWidth+a.blockX) - (b.chunkX*Chunk.chunkWidth+b.blockX);
		z = (a.chunkZ*Chunk.chunkWidth+a.blockZ) - (b.chunkZ*Chunk.chunkWidth+b.blockZ);
		y = a.blockY - b.blockY;

		/*
		0 = X+
		1 = Z-
		2 = X-
		3 = Z+
		4 = Y-
		5 = Y+
		*/

	    if(y == -1)
	      return 4;
	    else if(y == 1)
	      	return 5;
		else if(x == -1)
			return 2;
		else if(x == 1)
			return 0;
		else if(z == -1)
			return 1;
		else if(z == 1)
			return 3;
		else
			return -1;
	}

	public static bool Eq(CastCoord a, CastCoord b){
		if(a.chunkX != b.chunkX || a.chunkZ != b.chunkZ || a.blockX != b.blockX || a.blockY != b.blockY || a.blockZ != b.blockZ)
			return false;
		return true;
	}
}