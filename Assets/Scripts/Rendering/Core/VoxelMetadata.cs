using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

/*
HP and State of every voxel of a chunk.
Each of them is stored as a single value while all voxels share it, and only becomes a full array once a voxel differs.
Full chunk arrays allocated here come from the MetadataPool and go back to it on Destroy()
*/
public class VoxelMetadata {
	private ushort[] hp; // null while uniform
	private ushort[] state; // null while uniform
	private ushort hpUniform = 0;
	private ushort stateUniform = 0;
	private bool hpPooled = false;
	private bool statePooled = false;
	private ChunkChangeSet changeSet;

	private int xSize = Chunk.chunkWidth;
	private int ySize = Chunk.chunkDepth;
	private int zSize = Chunk.chunkWidth;
	
	// Default Constructor
	public VoxelMetadata(){}


	// Sized Constructor
	public VoxelMetadata(int x, int y, int z){
		xSize = x;
		ySize = y;
		zSize = z;

		this.hp = new ushort[xSize*ySize*zSize];
		this.state = new ushort[xSize*ySize*zSize];
	}
	public VoxelMetadata(int x, int y, int z, ushort[] hp, ushort[] state){
		xSize = x;
		ySize = y;
		zSize = z;

		this.hp = hp;
		this.state = state;
	}

	public VoxelMetadata(VoxelMetadata vd){
		this.xSize = vd.xSize;
		this.ySize = vd.ySize;
		this.zSize = vd.zSize;

		if(vd.IsHPUniform())
			this.hpUniform = vd.GetHPUniform();
		else
			this.hp = (ushort[])vd.GetHPData().Clone();

		if(vd.IsStateUniform())
			this.stateUniform = vd.GetStateUniform();
		else
			this.state = (ushort[])vd.GetStateData().Clone();
	}

	// Metadata Constructor
	public VoxelMetadata(ushort[] hp, ushort[] state){
		this.hp = (ushort[])hp.Clone();
		this.state = (ushort[])state.Clone();
	}

	public void Destroy(){
		ReleaseHP();
		ReleaseState();
	}

	// Returns hp of a given voxel coordinate
	public ushort GetHP(int x, int y, int z){
		if(this.hp == null)
			return this.hpUniform;
		return this.hp[x*zSize*ySize+y*zSize+z];
	}

	// Returns hp of a given voxel coordinate
	public ushort GetHP(int i){
		if(this.hp == null)
			return this.hpUniform;
		return this.hp[i];
	}

	public void SetHP(int x, int y, int z, ushort val){
		if(this.hp == null){
			if(val == this.hpUniform)
				return;

			DensifyHP();
		}

		int index = x*zSize*ySize+y*zSize+z;
		this.hp[index] = val;

		if(this.changeSet != null)
			this.changeSet.Record(index);
	}

	// Server-side tracking of changed voxels. Null in Clients
	public void SetChangeSet(ChunkChangeSet changeSet){
		this.changeSet = changeSet;
	}

	// Uses the given array as HP data. The array is not returned to the MetadataPool
	public void SetHPData(ushort[] data){
		ReleaseHP();
		this.hp = data;
		MarkAllChanged();
	}

	// Makes every voxel have the same HP
	public void SetHPUniform(ushort value){
		ReleaseHP();
		this.hpUniform = value;
		MarkAllChanged();
	}

	// Copies HP data of a full chunk
	public void SetHPDense(NativeArray<ushort> data){
		if(this.hp == null || this.hp.Length != data.Length)
			DensifyHP();

		data.CopyTo(this.hp);
		MarkAllChanged();
	}

	public bool IsHPUniform(){return this.hp == null;}
	public ushort GetHPUniform(){return this.hpUniform;}

	// Writes the HP of every voxel to a native array of the same size, without making the HP dense
	public void CopyHPTo(NativeArray<ushort> output){
		if(this.hp == null)
			Fill(output, this.hpUniform);
		else
			output.CopyFrom(this.hp);
	}

	// Returns state of a given voxel coordinate
	public ushort GetState(int x, int y, int z){
		if(this.state == null)
			return this.stateUniform;
		return this.state[x*zSize*ySize+y*zSize+z];
	}

	// Returns state of a given voxel coordinate
	public ushort GetState(int i){
		if(this.state == null)
			return this.stateUniform;
		return this.state[i];
	}	

	// Returns state of a given voxel coordinate
	public ushort GetState(int3 coord){
		return GetState(coord.x, coord.y, coord.z);
	}	

	// Uses the given array as State data. The array is not returned to the MetadataPool
	public void SetStateData(ushort[] data){
		ReleaseState();
		this.state = data;
		MarkAllChanged();
	}

	// Makes every voxel have the same State
	public void SetStateUniform(ushort value){
		ReleaseState();
		this.stateUniform = value;
		MarkAllChanged();
	}

	// Copies State data of a full chunk
	public void SetStateDense(NativeArray<ushort> data){
		if(this.state == null || this.state.Length != data.Length)
			DensifyState();

		data.CopyTo(this.state);
		MarkAllChanged();
	}

	public bool IsStateUniform(){return this.state == null;}
	public ushort GetStateUniform(){return this.stateUniform;}

	// Writes the State of every voxel to a native array of the same size, without making the State dense
	public void CopyStateTo(NativeArray<ushort> output){
		if(this.state == null)
			Fill(output, this.stateUniform);
		else
			output.CopyFrom(this.state);
	}

	public void SetState(int x, int y, int z, ushort val){
		if(this.state == null){
			if(val == this.stateUniform)
				return;

			DensifyState();
		}

		int index = x*zSize*ySize+y*zSize+z;
		this.state[index] = val;

		if(this.changeSet != null)
			this.changeSet.Record(index);
	}

	// Applies a sum to current state
	public void AddToState(int x, int y, int z, ushort a){
		SetState(x, y, z, (ushort)(GetState(x, y, z) + a));
	}

	// Applies a sum to current state
	public void AddToState(int x, int y, int z, int a){
		SetState(x, y, z, (ushort)(GetState(x, y, z) + (ushort)a));
	}

	// Applies a sum to current state
	public void AddToHp(int x, int y, int z, ushort a){
		SetHP(x, y, z, (ushort)(GetHP(x, y, z) + a));
	}

	// Applies a sum to current state
	public void AddToHp(int x, int y, int z, int a){
		SetHP(x, y, z, (ushort)(GetHP(x, y, z) + (ushort)a));
	}

	// Returns the HP array, making the HP dense if it's uniform
	public ushort[] GetHPData(){
		if(this.hp == null)
			DensifyHP();

		return this.hp;
	}

	// Returns the State array, making the State dense if it's uniform
	public ushort[] GetStateData(){
		if(this.state == null)
			DensifyState();

		return this.state;
	}

	// Clears all Metadata
	public void Clear(){
		ReleaseHP();
		ReleaseState();
		this.hpUniform = 0;
		this.stateUniform = 0;
		MarkAllChanged();
	}

	// Resets a single cell in Metadata
	public void Reset(int x, int y, int z){
		SetHP(x, y, z, 0);
		SetState(x, y, z, 0);
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

	private void MarkAllChanged(){
		if(this.changeSet != null)
			this.changeSet.MarkAll();
	}

	private void DensifyHP(){
		this.hp = NewArray(out this.hpPooled);
		Array.Fill(this.hp, this.hpUniform);
	}

	private void DensifyState(){
		this.state = NewArray(out this.statePooled);
		Array.Fill(this.state, this.stateUniform);
	}

	private void ReleaseHP(){
		if(this.hpPooled)
			MetadataPool.Return(this.hp);

		this.hp = null;
		this.hpPooled = false;
	}

	private void ReleaseState(){
		if(this.statePooled)
			MetadataPool.Return(this.state);

		this.state = null;
		this.statePooled = false;
	}

	// Arrays of the size of a chunk come from the MetadataPool
	private ushort[] NewArray(out bool pooled){
		int size = this.xSize*this.ySize*this.zSize;

		pooled = size == MetadataPool.ARRAY_SIZE;

		if(pooled)
			return MetadataPool.Rent();
		return new ushort[size];
	}

	private static unsafe void Fill(NativeArray<ushort> output, ushort value){
		UnsafeUtility.MemCpyReplicate(output.GetUnsafePtr(), &value, sizeof(ushort), output.Length);
	}
}
