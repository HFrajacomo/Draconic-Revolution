public struct EntityID{
	public EntityType type;
	public ChunkPos pos;
	public ulong code;

	public EntityID(EntityType t, ChunkPos p, ulong c){
		this.type = t;
		this.pos = p;
		this.code = c;
	}
}