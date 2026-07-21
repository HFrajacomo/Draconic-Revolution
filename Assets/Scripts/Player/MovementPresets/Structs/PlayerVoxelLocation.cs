using System;

public struct PlayerVoxelLocation{
	public static PlayerVoxelLocation zero = new PlayerVoxelLocation{feet = 0, body = 0, head = 0};
	public ushort feet;
	public ushort body;
	public ushort head;

	public override string ToString(){return $"Feet: {VoxelLoader.GetName(this.feet)} -- Body: {VoxelLoader.GetName(this.body)} -- Head: {VoxelLoader.GetName(this.head)}";}
}