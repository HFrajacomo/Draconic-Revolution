using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct CastCoord{
	public int chunkX;
	public int chunkZ;
	public int chunkY;
	public int blockX;
	public int blockY;
	public int blockZ;
	public bool active;

	public CastCoord(bool a){
		chunkX = 0;
		chunkZ = 0;
		chunkY = 0;
		blockX = 0;
		blockZ = 0;
		blockY = 0;
		active = false;
	}

	public CastCoord Copy(){
		CastCoord c = new CastCoord(true);
		c.chunkX = this.chunkX;
		c.chunkZ = this.chunkZ;
		c.chunkY = this.chunkY;
		c.blockX = this.blockX;
		c.blockY = this.blockY;
		c.blockZ = this.blockZ;

		return c;
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


	    // Y Processing
	    if(nMark.y >= 0)
	      nMark.y = (int)Math.Round(nMark.y, MidpointRounding.AwayFromZero);
	    else
	      nMark.y = (int)(nMark.y - 0.5f);

	    chunkY = Mathf.FloorToInt(nMark.y/Chunk.chunkDepth);

	    if(chunkY >= 0)
	      blockY = Mathf.FloorToInt(nMark.y%Chunk.chunkDepth);
	    else
	      blockY = Mathf.CeilToInt(((Chunk.chunkDepth*-chunkY)+nMark.y)%Chunk.chunkDepth);

	 	active = true;
	}

	public CastCoord(float x, float y, float z){
	    // X Processing
	    if(x >= 0)
	      x = (int)Math.Round(x, MidpointRounding.AwayFromZero);
	    else
	      x = (int)(x - 0.5f);

	    chunkX = Mathf.FloorToInt(x/Chunk.chunkWidth);


	    if(chunkX >= 0)
	      blockX = Mathf.FloorToInt(x%Chunk.chunkWidth);
	    else
	      blockX = Mathf.CeilToInt(((Chunk.chunkWidth*-chunkX)+x)%Chunk.chunkWidth); 

	    // Z Processing
	    if(z >= 0)
	      z = (int)Math.Round(z, MidpointRounding.AwayFromZero);
	    else
	      z = (int)(z - 0.5f);

	    chunkZ = Mathf.FloorToInt(z/Chunk.chunkWidth);

	    if(chunkZ >= 0)
	      blockZ = Mathf.FloorToInt(z%Chunk.chunkWidth);
	    else
	      blockZ = Mathf.CeilToInt(((Chunk.chunkWidth*-chunkZ)+z)%Chunk.chunkWidth);

	    // Y Processing
	    if(y >= 0)
	      y = (int)Math.Round(y, MidpointRounding.AwayFromZero);
	    else
	      y = (int)(y - 0.5f);

	    chunkY = Mathf.FloorToInt(y/Chunk.chunkDepth);

	    if(chunkY >= 0)
	      blockY = Mathf.FloorToInt(y%Chunk.chunkDepth);
	    else
	      blockY = Mathf.CeilToInt(((Chunk.chunkDepth*-chunkY)+y)%Chunk.chunkDepth);

	 	active = true;
	}

	public CastCoord(ChunkPos pos, int x, int y, int z){
		chunkX = pos.x;
		chunkZ = pos.z;
		chunkY = pos.y;
		blockX = x;
		blockY = y;
		blockZ = z;
		active = true;
	}


	public int GetWorldX(){
	  return Chunk.chunkWidth*chunkX+blockX;
	}

	public int GetWorldY(){
	  return Chunk.chunkDepth*chunkY+blockY;
	}

	public int GetWorldZ(){
	  return Chunk.chunkWidth*chunkZ+blockZ;
	}

	public ChunkPos GetChunkPos(){
	  return new ChunkPos(chunkX, chunkZ, chunkY);
	}

	// Adds and returns a rebuilt CastCoord
	public CastCoord Add(int x, int y, int z){
	  return new CastCoord(new Vector3(Chunk.chunkWidth*chunkX+blockX+x, Chunk.chunkDepth*chunkY+blockY+y, Chunk.chunkWidth*chunkZ+blockZ+z));
	}

	public override string ToString(){
		return "ChunkX: " + chunkX + "\tChunkZ: " + chunkZ + "\tChunkY: " + (ChunkDepthID)chunkY + "\tX, Y, Z: " + GetWorldX() + ", " + GetWorldY() + ", " + GetWorldZ();
	}

	public string RealPos(){
		return "X: " + (chunkX*Chunk.chunkWidth + blockX).ToString() + "   Y: " + (chunkY*Chunk.chunkDepth + blockY).ToString() + "   Z: " + (chunkZ*Chunk.chunkWidth + blockZ);
	}

	/*
	Return a flag
	1: XP
	2: XM
	4: ZP
	8: ZM
	16: YP
	128: Block placed onto of item
	*/
	public static int TestEntityCollision(CastCoord current, CastCoord last){
		int cx, lx, cy, ly, cz, lz;
		int outFlag = 0;

		cx = current.GetWorldX();
		lx = last.GetWorldX();

		if(cx != lx){
			if(cx > lx)
				outFlag |= 1;
			else
				outFlag |= 2;
		}

		cz = current.GetWorldZ();
		lz = last.GetWorldZ();

		if(cz != lz){
			if(cz > lz)
				outFlag |= 4;
			else
				outFlag |= 8;
		}

		cy = current.GetWorldY();
		ly = last.GetWorldY();

		if(cy > ly)
			outFlag |= 16;

		if(outFlag == 0)
			outFlag |= 128;

		return outFlag;
	}

	public static int operator-(CastCoord a, CastCoord b){
		int x,y,z;

		if(!b.active)
			return -1;

		x = (a.chunkX*Chunk.chunkWidth+a.blockX) - (b.chunkX*Chunk.chunkWidth+b.blockX);
		z = (a.chunkZ*Chunk.chunkWidth+a.blockZ) - (b.chunkZ*Chunk.chunkWidth+b.blockZ);
		y = (a.chunkY*Chunk.chunkDepth+a.blockY) - (b.chunkY*Chunk.chunkDepth+b.blockY);

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
		if(a.chunkX != b.chunkX || a.chunkZ != b.chunkZ || a.blockX != b.blockX || a.blockY != b.blockY || a.blockZ != b.blockZ || a.chunkY != b.chunkY || a.blockY != b.blockY)
			return false;
		return true;
	}
}