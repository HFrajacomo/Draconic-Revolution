using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BlocklikeObject
{
	public string name;
	public bool solid; // Is collidable
	public bool transparent; // Should render the back side?
	public bool invisible; // Should not render at all
	public bool liquid;
	public static int objectCount = 1;
	public VFXLoader vfx = GameObject.Find("/VFXLoader").GetComponent<VFXLoader>();
	public bool washable = false; // Can be destroyed by flowing water
	public bool needsRotation = false;
	public bool customBreak = false;
	public bool customPlace = false;

	// Texture
	public string prefabName;
	public Mesh mesh;
	public Vector3 centeringOffset;
	public Vector3 scaling;

	public static BlocklikeObject Create(int blockID){
		if(blockID == -1){
			Torch_Object aux = new Torch_Object();
			return aux;
		}
		else{
			return new Torch_Object();
		}
	}

	// Moves all vertices from local to world space
	public Vector3[] ToWorldSpace(Vector3 pos){
		if(this.mesh == null){
			return null;
		}

		Vector3[] newVert = new Vector3[this.mesh.vertices.Length];


		for(int i=0;i<this.mesh.vertices.Length;i++){
			newVert[i] = this.mesh.vertices[i] + pos;
		}

		return newVert;
	}

	// Moves all vertices from local to world space
	public Vector3[] ToWorldSpace(Vector3 pos, Vector3[] v){
		if(this.mesh == null){
			return null;
		}

		Vector3[] newVert = new Vector3[v.Length];


		for(int i=0;i<v.Length;i++){
			newVert[i] = v[i] + pos;
		}

		return newVert;
	}


	public void LoadMesh(){
		GameObject go;
		Vector3[] newVerts;
		Vector3 cachedVector;

		go = GameObject.Find(this.prefabName);
		this.mesh = go.GetComponent<MeshFilter>().mesh;
		newVerts = new Vector3[this.mesh.vertices.Length];

		for(int i=0; i<this.mesh.vertices.Length; i++){
			cachedVector = new Vector3(this.mesh.vertices[i].x, this.mesh.vertices[i].y, this.mesh.vertices[i].z);
			
			// Rotation
			cachedVector = Quaternion.AngleAxis(-90, Vector3.right) * cachedVector;
			
			// Whole Scaling
			cachedVector *= 10f;

			// Scaling
			cachedVector.x *= this.scaling.x;
			cachedVector.y *= this.scaling.y;
			cachedVector.z *= this.scaling.z;

			// Shifting
			newVerts[i] = cachedVector + this.centeringOffset;
		}
		this.mesh.vertices = newVerts;
	}

	// Used to Rotate a set of Vector3 that represent mesh.vertices
	public Vector3[] Rotate(float x=0, float y=0, float z=0){
		Vector3[] newV = new Vector3[this.mesh.vertices.Length];

		if(x != 0){
			for(int i=0;i<this.mesh.vertices.Length;i++){
				newV[i] = Quaternion.AngleAxis(x, Vector3.right) * this.mesh.vertices[i];
			}
		}
		else if(y != 0){
			for(int i=0;i<this.mesh.vertices.Length;i++){
				newV[i] = Quaternion.AngleAxis(y, Vector3.up) * this.mesh.vertices[i];
			}			
		}
		else if(z != 0){
			for(int i=0;i<this.mesh.vertices.Length;i++){
				newV[i] = Quaternion.AngleAxis(z, Vector3.forward) * this.mesh.vertices[i];
			}			
		}
		else
			return this.mesh.vertices;

		return newV;
	}

    // Handles the emittion of BUD to neighboring blocks
    public void EmitBlockUpdate(string type, int x, int y, int z, int tickOffset, ChunkLoader cl){
      CastCoord thisPos = new CastCoord(new Vector3(x, y, z));

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
        blockCode = cl.chunks[c.GetChunkPos()].data.GetCell(c.blockX, c.blockY, c.blockZ);

        cl.budscheduler.ScheduleBUD(new BUDSignal(type, c.GetWorldX(), c.GetWorldY(), c.GetWorldZ(), thisPos.GetWorldX(), thisPos.GetWorldY(), thisPos.GetWorldZ(), facings[faceCounter]), tickOffset);     
      
        faceCounter++;
      }
    }

	// Unassigns metadata from block (use after OnBreak events)
	public void EraseMetadata(ChunkPos pos, int x, int y, int z, ChunkLoader cl){cl.chunks[pos].metadata.GetMetadata(x,y,z).Reset();}
	
	/*
	VIRTUAL METHODS
	*/

	/* BUD Types
		"break": When emitting block is broken
		"change": When emitting block has been turned into another block or changed properties
		"trigger": When emitting block has been electrically triggered
	*/
	public virtual void OnBlockUpdate(string budType, int myX, int myY, int myZ, int budX, int budY, int budZ, int facing, ChunkLoader cl){}

	public virtual int OnInteract(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){return 0;}
	public virtual int OnPlace(ChunkPos pos, int blockX, int blockY, int blockZ, int facing, ChunkLoader cl){return 0;}
	public virtual int OnBreak(ChunkPos pos, int blockX, int blockY, int blockZ, ChunkLoader cl){return 0;}
	public virtual bool PlacementRule(ChunkPos pos, int blockX, int blockY, int blockZ, int direction, ChunkLoader cl){return true;}
	public virtual Vector3[] ApplyRotation(Chunk c, int blockX, int blockY, int blockZ){return this.mesh.vertices;}
}
