using UnityEngine;

public struct EntityChunkTransaction{
	public EntityID old;
	public EntityID novel;
	public AbstractAI ai;

	public EntityChunkTransaction(EntityID o, EntityID n, AbstractAI a){
		this.old = o;
		this.novel = n;
		this.ai = a;
	}
}