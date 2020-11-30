using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class VoxelMetadata
{
	ushort[] hp;
	ushort[] state;

	int xSize = Chunk.chunkWidth;
	int ySize = Chunk.chunkDepth;
	int zSize = Chunk.chunkWidth;
	
	// Default Constructor
	public VoxelMetadata(){
		this.hp = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
		this.state = new ushort[Chunk.chunkWidth*Chunk.chunkDepth*Chunk.chunkWidth];
	}


	// Sized Constructor
	public VoxelMetadata(int x, int y, int z){
		xSize = x;
		ySize = y;
		zSize = z;

		this.hp = new ushort[xSize*ySize*zSize];
		this.state = new ushort[xSize*ySize*zSize];
	}

	public VoxelMetadata(VoxelMetadata vd){
		this.hp = (ushort[])vd.GetHPData().Clone();
		this.state = (ushort[])vd.GetStateData().Clone();
	}

	// Metadata Constructor
	public VoxelMetadata(ushort[] hp, ushort[] state){
		this.hp = (ushort[])hp.Clone();
		this.state = (ushort[])hp.Clone();
	}

	// Returns hp of a given voxel coordinate
	public ushort GetHP(int x, int y, int z){
		return this.hp[x*zSize*ySize+y*zSize+z];
	}

	public void SetHP(int x, int y, int z, ushort val){
		this.hp[x*zSize*ySize+y*zSize+z] = val;
	}

	// Returns state of a given voxel coordinate
	public ushort GetState(int x, int y, int z){
		return this.state[x*zSize*ySize+y*zSize+z];
	}	

	// Returns state of a given voxel coordinate
	public ushort GetState(int3 coord){
		return this.state[coord.x*zSize*ySize+coord.y*zSize+coord.z];
	}	

	public void SetState(int x, int y, int z, ushort val){
		this.state[x*zSize*ySize+y*zSize+z] = val;
	}

	// Applies a sum to current state
	public void AddToState(int x, int y, int z, ushort a){
		this.state[x*zSize*ySize+y*zSize+z] += a;
	}

	// Applies a sum to current state
	public void AddToState(int x, int y, int z, int a){
		this.state[x*zSize*ySize+y*zSize+z] += (ushort)a;
	}

	// Applies a sum to current state
	public void AddToHp(int x, int y, int z, ushort a){
		this.hp[x*zSize*ySize+y*zSize+z] += (ushort)a;
	}

	// Applies a sum to current state
	public void AddToHp(int x, int y, int z, int a){
		this.hp[x*zSize*ySize+y*zSize+z] += (ushort)a;
	}

	public ushort[] GetHPData(){
		return this.hp;
	}

	public ushort[] GetStateData(){
		return this.state;
	}

	// Clears all Metadata
	public void Clear(){
		this.hp = new ushort[xSize*ySize*zSize];
		this.state = new ushort[xSize*ySize*zSize];
	}

	// Resets a single cell in Metadata
	public void Reset(int x, int y, int z){
		this.hp[x*zSize*ySize+y*zSize+z] = ushort.MaxValue;
		this.state[x*zSize*ySize+y*zSize+z] = ushort.MaxValue;
	}

	// Returns true if HP and State is ushort.MaxValue
	public bool IsUnassigned(int x, int y, int z){
		if(this.GetHP(x,y,z) == ushort.MaxValue && this.GetState(x,y,z) == ushort.MaxValue)
			return true;
		return false;
	}

	// Returns true if HP is ushort.MaxValue
	public bool IsHPNull(int x, int y, int z){
		if(this.GetHP(x,y,z) == ushort.MaxValue)
			return true;
		return false;
	}

	// Returns true if State is ushort.MaxValue
	public bool IsStateNull(int x, int y, int z){
		if(this.GetState(x,y,z) == ushort.MaxValue)
			return true;
		return false;
	}

}