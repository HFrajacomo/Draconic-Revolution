using UnityEngine;

public struct RaceSettings{
	public Race race;
	public Vector3 scaling;
	public Gradient gradient1;
	public Gradient gradient2;
	public Gradient gradient3;

	public RaceSettings(Race r, Vector3 scaling, Gradient g1, Gradient g2, Gradient g3){
		this.race = r;
		this.scaling = scaling;
		this.gradient1 = g1;
		this.gradient2 = g2;
		this.gradient3 = g3;
	}

	public Race GetRace(){return this.race;}

	public override string ToString(){return scaling.ToString();}
}