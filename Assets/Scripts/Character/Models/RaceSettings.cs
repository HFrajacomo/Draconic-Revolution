using UnityEngine;

public struct RaceSettings{
	public Vector3 scaling;
	public Color initialColor;
	public Color endColor;

	public RaceSettings(Vector3 scaling, Color initialColor, Color endColor){
		this.scaling = scaling;
		this.initialColor = initialColor;
		this.endColor = endColor;
	}

	public override string ToString(){return scaling.ToString();}
}