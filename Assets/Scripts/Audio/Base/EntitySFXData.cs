using UnityEngine;

/*
Network-transferable description of an SFX attached to an Entity
*/
public struct EntitySFXData{
	public string name;
	public bool isLoop;
	public bool hasAnchor;
	public BoneAnchorType anchor;
	public float offsetX, offsetY, offsetZ;

	public EntitySFXData(string name, bool isLoop, bool hasAnchor, BoneAnchorType anchor, float offsetX, float offsetY, float offsetZ){
		this.name = name;
		this.isLoop = isLoop;
		this.hasAnchor = hasAnchor;
		this.anchor = anchor;
		this.offsetX = offsetX;
		this.offsetY = offsetY;
		this.offsetZ = offsetZ;
	}

	public EntitySFXData(string name){
		this.name = name;
		this.isLoop = false;
		this.hasAnchor = false;
		this.anchor = (BoneAnchorType)0;
		this.offsetX = 0f;
		this.offsetY = 0f;
		this.offsetZ = 0f;
	}

	public Vector3 GetOffset(){return new Vector3(this.offsetX, this.offsetY, this.offsetZ);}
}
