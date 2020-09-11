using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelMetadata
{
	public Metadata[,,] metadata = new Metadata[Chunk.chunkWidth, Chunk.chunkDepth, Chunk.chunkWidth];
	
	// Checks whether the metadata xyz in VoxelMetadata is still unassigned
	public bool IsUnassigned(int x, int y, int z){
		if(this.metadata[x,y,z] == null)
			return true;
		return false;
	}

	// Creates a null metadata xyz
	public void CreateNull(int x, int y, int z){
		this.metadata[x,y,z] = new Metadata();
	}

	// Gets metadata xyz if exists. Creates a null and gets it if doesn't exists.
	public Metadata GetMetadata(int x, int y, int z){
		if(IsUnassigned(x,y,z)){
			CreateNull(x,y,z);
		}

		return this.metadata[x,y,z];
	}

	// Serializes a single entry of metadata
	public string SerializeSingle(int x, int y, int z){
		return this.metadata[x,y,z].ToString();
	}

	// Clears all Metadata
	public void Clear(){
		this.metadata = new Metadata[Chunk.chunkWidth, Chunk.chunkDepth, Chunk.chunkWidth];
	}

	// Clones data in Metadata
	public Metadata[,,] Clone(){
		Metadata[,,] newData = new Metadata[Chunk.chunkWidth, Chunk.chunkDepth, Chunk.chunkWidth];

		for(int x=0; x<this.metadata.GetLength(0); x++){
			for(int y=0; y<this.metadata.GetLength(1); y++){
				for(int z=0; z<this.metadata.GetLength(2); z++){
					newData[x,y,z] = this.metadata[x,y,z];
				}
			}
		}

		return newData;
	}

	// Sets this objects Metadata
	public void Set(Metadata[,,] inputData){
		this.metadata = inputData;
	}
}


/*
Metadata class is a metadata entry for a single voxel

Serialized format = HP,state,storage
"," = field delimiter
":" = value delimiter in storage field
";" = item delimiter in storage field
"\n" = end of metadata

*/
public class Metadata{
	public ushort? hp = null;
	public ushort? state = null;
	public Dictionary<string, uint> storage;

	// Constructors
	public Metadata(){}
	public Metadata(bool initStorage){this.storage = new Dictionary<string, uint>();}
	public Metadata(ushort st){this.state = st;}
	public Metadata(ushort h, ushort st){this.hp = h; this.state = st;}
	public Metadata(string serialized){
		string buffer = "";
		bool hpField = true;

		foreach(char c in serialized){
			if(c == ','){
				if(hpField){
					SetHP(buffer);
					buffer = "";
					hpField = false;
					continue;
				}
				else if(!hpField){
					SetState(buffer);
					buffer = "";
					continue;
				}
			}
			else if(c == '\n'){
				SetStorage(buffer);
			}
			else
				buffer += c;
		}
	}

	// Serialize Metadata
	public override string ToString(){
		string outBuffer = "";

		if(this.hp != null)
			outBuffer = outBuffer + this.hp + ',';
		else
			outBuffer = ",";

		if(this.state != null)
			outBuffer = outBuffer + this.state + ',';
		else
			outBuffer += ",";

		if(this.storage != null){
			foreach(string key in this.storage.Keys){
				outBuffer = outBuffer + key + ':' + this.storage[key] + ';';
			}
		}

		return outBuffer + '\n';
	}

	// Instantiates the Dict<> forcefully if it's still null
	public void InitStorage(){
		if(this.storage == null)
			this.storage = new Dictionary<string, uint>();
	}

	// Internal Set Methods for serialized constructor -----------
	private void SetHP(string s){
		if(s != "")
			this.hp = Convert.ToUInt16(s);
	}

	private void SetState(string s){
		if(s != "")
			this.state = Convert.ToUInt16(s);
	}

	private void SetStorage(string s){
		if(s != ""){
			bool readingName = true;
			string bufferName = "";
			string bufferQuantity = "";
			this.storage = new Dictionary<string, uint>();

			foreach(char c in s){
				if(c == '\n')
					return;
				else if(c == ':'){
					readingName = false;
					continue;
				}
				else if(c == ';'){
					readingName = true;
					this.storage.Add(bufferName, Convert.ToUInt16(bufferQuantity));
					bufferName = "";
					bufferQuantity = "";
					continue;
				}

				if(readingName)
					bufferName += c;
				else
					bufferQuantity += c;
			}
		}
	}
	// --------------------------

	public void Reset(){
		this.hp = null;
		this.state = null;
		this.storage = null;
	}

	// Checks for Null Metadata entries
	public bool IsNull(){
		if(this.hp == null && this.state == null && this.storage == null)
			return true;
		return false;
	}
}