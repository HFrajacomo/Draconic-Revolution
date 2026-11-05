using UnityEngine;

/*
Tracks a non-looping Entity SFX in SFXLoader so it can be destroyed once it finishes playing
*/
public struct OneShotSFX{
	public EntityID entity;
	public string name;
	public AudioSource source;
	public float creationTime;
	public bool started;

	public OneShotSFX(EntityID entity, string name, AudioSource source, float creationTime){
		this.entity = entity;
		this.name = name;
		this.source = source;
		this.creationTime = creationTime;
		this.started = false;
	}
}
