using Unity.Mathematics;

/*
A visible face, decal or Object found by BuildChunkJob, turned into mesh data by MergeChunkMeshJob
*/
public struct MeshRecord{
	public const byte FACE = 0;
	public const byte DECAL = 1;
	public const byte OBJECT = 2;

	public byte kind;
	public byte dir;
	public byte material; // ShaderIndex of faces
	public byte dampness;
	public byte decal; // Decal stage of decals
	public bool isObject; // Records of the same Object are only used once
	public ushort block;
	public ushort natural; // Natural light of the 4 face corners, 4 bits each
	public ushort extra; // Extra light of the 4 face corners, 4 bits each
	public short x;
	public short y;
	public short z;

	// Packs 4 light levels (0 to 15)
	public static ushort Pack(int4 light){
		return (ushort)(light.x | (light.y << 4) | (light.z << 8) | (light.w << 12));
	}

	public static int Unpack(ushort packed, int corner){
		return (packed >> (corner*4)) & 0x0F;
	}
}
