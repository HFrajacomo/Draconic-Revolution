using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestStruct : Structure
{
	public ushort[] blocks = new ushort[2]{3,24};
	public ushort[] hps = new ushort[2]{0,24};
	public ushort[] states = new ushort[2]{0,24};

	public TestStruct(){
		this.code = (ushort)StructureCode.TestStruct; 

		this.sizeX = 12;
		this.sizeY = 1;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new HashSet<ushort>();

		Prepare(blocks, hps, states);
	}
}

public class TreeSmallA : Structure
{

	public ushort[] blocks = new ushort[65]{0,12,4,0,24,4,0,24,4,0,13,7,3,0,1,7,7,4,7,7,0,1,7,3,0,2,7,3,0,1,7,7,4,7,7,0,1,7,3,0,2,7,3,0,1,7,15,0,1,7,3,0,7,7,3,0,2,7,3,0,2,7,3,0,6};
	public ushort[] hps = new ushort[2]{0,175};
	public ushort[] states = new ushort[2]{0,175};

	public TreeSmallA(){
		this.code = (ushort)StructureCode.TreeSmallA; 

		this.sizeX = 5;
		this.sizeY = 7;
		this.sizeZ = 5;

		this.offsetX = 2;
		this.offsetZ = 2;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.type = FillType.FreeSpace;
		this.overwriteBlocks = new HashSet<ushort>();
		this.needsBase = true;
		this.acceptableBaseBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Grass"), VoxelLoader.GetBlockID("BASE_Dirt"), VoxelLoader.GetBlockID("BASE_Snow")};

		Prepare(blocks, hps, states);
	}
}

public class TreeMediumA : Structure
{

	public ushort[] blocks = new ushort[214]{0,14,4,0,7,4,4,4,0,6,4,4,4,0,7,4,0,35,4,0,7,4,4,0,7,4,4,0,52,4,4,0,7,4,4,0,43,7,1,0,7,7,1,4,4,0,6,7,1,4,4,7,1,0,6,7,3,0,6,7,2,0,7,7,2,0,7,7,4,0,1,7,7,0,4,7,2,4,4,7,2,0,1,7,4,4,4,7,1,0,4,7,3,4,7,2,0,3,7,3,4,7,1,0,7,7,1,0,7,7,1,4,7,1,0,2,7,5,4,7,2,0,1,7,2,4,4,4,4,7,2,0,1,7,4,4,4,7,2,0,2,7,3,4,7,3,0,2,7,7,0,6,7,1,0,8,7,1,0,7,7,3,0,3,7,3,4,4,7,1,0,3,7,3,4,7,3,0,2,7,6,0,6,7,2,0,1,7,1,0,22,7,2,0,7,7,4,0,5,7,1,4,7,1,0,6,7,3,0,52,7,1,0,31};
	public ushort[] hps = new ushort[2]{0,567};
	public ushort[] states = new ushort[2]{0,567};

	public TreeMediumA(){
		this.code = (ushort)StructureCode.TreeMediumA; 

		this.sizeX = 7;
		this.sizeY = 9;
		this.sizeZ = 9;

		this.offsetX = 3;
		this.offsetZ = 3;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.type = FillType.FreeSpace;
		this.overwriteBlocks = new HashSet<ushort>();
		this.needsBase = true;
		this.acceptableBaseBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Grass"), VoxelLoader.GetBlockID("BASE_Dirt"), VoxelLoader.GetBlockID("BASE_Snow")};

		Prepare(blocks, hps, states);
	}
}

public class DirtPileA : Structure
{
	public ushort[] blocks = new ushort[66]{0,2,2,3,0,1,2,29,0,2,2,3,0,4,2,1,0,1,2,1,0,3,2,3,0,1,2,5,0,2,2,4,0,2,2,4,0,2,2,4,0,4,2,1,0,10,2,2,0,3,2,3,0,3,2,4,0,2,2,3,0,3,2,2,0,9};
	public ushort[] hps = new ushort[2]{0,126};
	public ushort[] states = new ushort[2]{0,126};

	public DirtPileA(){
		this.code = (ushort)StructureCode.DirtPileA; 

		this.sizeX = 7;
		this.sizeY = 3;
		this.sizeZ = 6;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone"), VoxelLoader.GetBlockID("BASE_Grass")};

		Prepare(blocks, hps, states);
	}
}

public class DirtPileB : Structure
{
	public ushort[] blocks = new ushort[66]{0,3,2,2,0,3,2,5,0,1,2,27,0,3,2,3,0,5,2,1,0,6,2,3,0,1,2,6,0,1,2,6,0,1,2,6,0,2,2,5,0,4,2,2,0,12,2,2,0,3,2,5,0,1,2,6,0,1,2,5,0,4,2,1,0,11};
	public ushort[] hps = new ushort[2]{0,147};
	public ushort[] states = new ushort[2]{0,147};

	public DirtPileB(){
		this.code = (ushort)StructureCode.DirtPileB; 

		this.sizeX = 7;
		this.sizeY = 3;
		this.sizeZ = 7;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone"), VoxelLoader.GetBlockID("BASE_Grass")};

		Prepare(blocks, hps, states);
	}
}

public class BoulderNormalA : Structure
{
	public ushort[] blocks = new ushort[54]{0,3,3,3,0,2,3,6,0,1,3,6,0,5,3,1,0,3,3,4,0,1,3,14,0,2,3,4,0,4,3,2,0,3,3,5,0,2,3,5,0,4,3,2,0,11,3,3,0,4,3,3,0,9};
	public ushort[] hps = new ushort[2]{0,112};
	public ushort[] states = new ushort[2]{0,112};

	public BoulderNormalA(){
		this.code = (ushort)StructureCode.BoulderNormalA; 

		this.sizeX = 4;
		this.sizeY = 4;
		this.sizeZ = 7;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new HashSet<ushort>();

		Prepare(blocks, hps, states);
	} 
}

public class TreeBigA : Structure
{
	public ushort[] blocks = new ushort[2805]{0,128,4,4,4,4,0,15,4,4,4,4,4,4,0,14,4,4,4,4,4,4,0,14,4,4,4,4,4,4,0,14,4,4,4,4,4,4,0,15,4,4,4,4,0,257,4,4,0,17,4,4,4,4,0,15,4,4,4,4,4,4,0,14,4,4,4,4,4,4,0,15,4,4,4,4,0,17,4,4,0,258,4,4,0,17,4,4,4,4,0,15,4,4,4,4,4,4,0,14,4,4,4,4,4,4,0,15,4,4,4,4,0,17,4,4,0,277,4,4,4,4,0,16,4,4,4,4,0,16,4,4,4,4,0,16,4,4,4,4,0,194,7,1,0,5,7,2,0,13,7,1,0,15,7,1,0,4,7,1,0,7,7,1,0,7,7,1,0,10,7,1,0,6,7,1,0,20,7,1,0,4,4,4,4,4,0,4,7,1,0,11,4,4,4,4,0,10,7,1,0,5,4,4,4,4,0,11,7,2,0,3,4,4,4,4,0,10,7,1,0,12,7,2,0,7,7,1,0,11,7,1,0,7,7,1,0,7,7,1,0,10,7,1,0,4,7,1,0,2,7,1,0,1,7,1,0,12,7,2,0,2,7,1,0,2,7,1,0,72,7,1,0,5,7,2,0,11,7,3,0,1,7,1,0,2,7,3,0,8,7,1,0,2,7,3,0,1,7,1,0,1,7,2,0,2,7,1,0,5,7,3,0,3,7,2,0,1,7,1,0,3,7,3,0,5,7,3,0,2,7,1,0,2,7,1,0,2,7,3,0,4,7,5,0,8,7,3,0,6,7,4,0,1,4,4,4,4,0,2,7,4,0,4,7,2,0,4,4,4,4,4,0,4,7,1,0,4,7,4,0,3,4,4,4,4,0,10,7,5,0,1,4,4,4,4,0,1,7,1,0,1,7,2,0,4,7,5,0,8,7,3,0,5,7,4,0,5,7,3,0,1,7,2,0,6,7,2,0,1,7,2,0,3,7,2,0,3,7,1,0,5,7,3,0,1,7,9,0,8,7,1,0,1,7,4,0,1,7,2,0,1,7,2,0,11,7,2,0,2,7,1,0,2,7,1,0,32,7,1,0,5,7,2,0,11,7,3,0,3,7,4,0,8,7,3,4,7,5,4,4,7,3,0,5,7,5,4,7,10,0,3,7,2,4,7,4,4,7,7,4,7,2,0,2,7,3,4,7,2,0,1,7,2,0,1,7,2,0,1,7,1,4,7,2,0,2,7,2,4,7,4,0,7,7,1,4,7,2,0,3,7,2,4,7,3,0,1,4,4,4,4,0,2,7,2,4,7,2,0,2,7,6,0,1,4,4,4,4,0,2,7,4,0,2,7,2,4,7,4,0,1,4,4,4,4,0,3,7,2,0,4,7,2,4,4,7,2,0,1,4,4,4,4,0,1,7,5,0,2,7,2,4,7,4,0,6,7,2,4,4,7,2,0,2,7,3,4,7,3,0,2,7,6,4,7,2,0,3,7,2,4,7,3,0,2,7,2,4,7,5,0,3,7,2,4,7,1,0,1,7,2,4,7,2,4,7,1,4,7,3,0,5,7,4,4,4,7,2,4,7,2,4,7,2,0,9,7,10,0,11,7,2,0,2,7,1,0,2,7,1,0,15,7,1,0,2,7,1,0,13,7,1,0,1,7,6,0,11,7,4,4,7,2,4,7,2,0,8,7,2,0,1,7,4,4,7,1,4,7,2,0,1,7,1,0,5,7,6,4,7,2,4,7,6,0,4,7,2,4,7,3,4,7,2,4,7,5,0,4,7,4,4,7,1,0,1,4,0,1,4,4,0,1,7,1,4,4,7,2,0,3,7,2,4,4,4,4,4,4,4,4,4,4,4,4,7,3,0,4,7,5,0,1,4,4,4,4,4,7,4,0,4,7,6,4,4,4,4,4,0,1,7,3,0,6,7,2,4,4,4,4,4,4,4,4,4,4,7,3,0,4,7,4,4,7,1,0,2,4,4,4,0,1,7,1,4,4,7,2,0,4,7,6,4,4,7,1,4,7,5,0,6,7,3,4,4,4,7,2,4,4,7,4,0,5,7,6,4,7,1,0,1,7,4,0,8,7,1,0,2,7,9,0,11,7,3,0,1,7,1,0,2,7,1,0,55,7,1,0,2,7,1,0,13,7,8,0,13,7,7,0,9,7,2,0,2,7,6,0,3,7,1,0,6,7,3,0,1,7,6,0,1,7,2,0,6,7,5,0,7,7,3,0,6,7,4,0,1,4,4,4,4,0,1,7,3,0,7,7,4,0,1,4,4,4,4,0,2,7,1,0,7,7,1,0,1,7,3,0,1,4,4,4,4,0,1,7,1,0,9,7,4,0,1,4,4,4,4,0,1,7,3,0,6,7,1,0,1,7,3,0,7,7,3,0,7,7,1,0,1,7,6,0,2,7,3,0,7,7,5,0,1,7,4,0,9,7,1,0,3,7,3,0,1,7,3,0,12,7,3,0,1,7,1,0,2,7,1,0,73,7,1,0,5,7,1,0,12,7,4,0,2,7,3,0,8,7,1,0,3,7,7,0,2,7,1,0,5,7,3,0,2,7,6,0,2,7,3,0,5,7,3,0,2,7,1,0,2,7,1,0,2,7,3,0,6,7,3,0,7,7,3,0,6,7,5,0,1,4,4,4,4,0,1,7,4,0,6,7,4,0,1,4,4,4,4,0,1,7,1,0,1,7,1,0,6,7,5,0,1,4,4,4,4,0,1,7,1,0,9,7,4,0,1,4,4,4,4,0,1,7,2,0,9,7,3,0,6,7,4,0,6,7,3,0,1,7,5,0,1,7,5,0,5,7,10,0,1,7,3,0,7,7,1,0,1,7,3,0,2,7,3,0,11,7,3,0,4,7,3,0,11,7,1,0,6,7,1,0,33,7,1,0,5,7,1,0,12,7,3,0,3,7,3,0,8,7,1,0,1,7,2,4,7,2,0,1,7,2,4,7,3,0,5,7,3,0,1,7,2,4,7,3,4,7,5,0,3,7,2,4,7,4,4,7,2,4,7,4,4,7,2,0,3,7,2,4,7,4,4,4,7,4,4,7,2,0,4,7,3,4,4,0,3,4,0,2,7,1,4,7,2,0,4,7,2,4,4,7,1,4,4,4,4,4,0,1,4,4,7,1,4,7,2,0,4,7,5,0,1,4,4,4,4,0,1,7,4,0,4,7,2,4,7,3,4,4,4,4,4,4,4,7,2,0,6,7,2,4,4,4,0,2,4,4,0,2,4,7,3,0,6,7,2,4,7,1,0,2,4,0,3,7,1,4,7,3,0,4,7,2,4,7,1,0,1,7,2,4,4,7,3,4,4,4,7,2,0,3,7,2,4,7,2,4,4,7,2,4,7,6,0,5,7,4,4,7,4,4,7,4,0,7,7,2,4,7,2,0,2,7,2,4,7,2,0,9,7,3,0,4,7,3,0,11,7,1,0,6,7,1,0,33,7,1,0,5,7,1,0,12,7,3,0,3,7,3,0,8,7,1,0,3,7,3,0,1,7,3,0,2,7,1,0,5,7,3,0,2,7,6,0,2,7,3,0,5,7,4,0,1,7,4,0,2,7,3,0,6,7,4,0,6,7,3,0,6,7,5,0,2,4,4,0,2,7,4,0,6,7,2,0,1,7,1,0,1,4,4,4,4,0,1,7,1,0,1,7,1,0,6,7,5,0,1,4,4,4,4,0,1,7,1,0,9,7,4,0,2,4,4,4,0,1,7,2,0,9,7,4,0,5,7,4,0,6,7,3,0,1,7,11,0,5,7,10,0,1,7,3,0,7,7,1,0,1,7,3,0,2,7,3,0,11,7,3,0,4,7,3,0,11,7,1,0,6,7,1,0,73,7,1,0,5,7,1,0,14,7,1,0,1,7,1,0,1,7,1,0,10,7,1,0,2,7,6,0,2,7,1,0,1,7,1,0,7,7,1,0,2,7,1,0,1,7,2,0,2,7,3,0,10,7,1,0,6,7,2,0,8,7,4,0,2,4,4,0,2,7,1,0,1,7,1,0,9,7,2,0,1,4,4,4,4,0,1,7,2,0,8,7,1,0,4,4,4,4,4,0,1,7,1,0,10,7,3,0,2,4,4,0,2,7,2,0,10,7,3,0,6,7,2,0,8,7,4,0,1,7,2,0,1,7,5,0,7,7,2,0,1,7,3,0,1,7,3,0,13,7,2,0,3,7,1,0,13,7,1,0,6,7,1,0,92,7,2,0,2,7,2,0,13,7,8,0,1,7,1,0,10,7,2,4,7,2,4,7,5,0,8,7,3,4,7,1,4,7,4,4,7,2,0,6,7,5,4,0,4,4,7,2,0,7,7,2,4,4,4,4,0,2,4,4,7,3,0,8,7,3,0,2,4,4,0,2,4,4,7,2,0,7,7,3,0,2,4,4,0,2,7,4,0,6,7,2,4,7,1,0,1,4,0,2,4,0,1,7,3,0,8,7,2,4,4,0,3,4,0,1,7,1,4,7,2,0,6,7,2,4,7,1,4,7,4,4,4,7,3,0,7,7,4,4,7,3,4,7,3,0,9,7,10,0,12,7,3,0,1,7,3,0,72,7,1,0,4,7,1,0,13,7,3,0,2,7,3,0,11,7,2,4,7,4,4,7,4,0,9,7,2,4,7,2,4,7,2,0,1,7,3,0,6,7,9,0,1,7,1,4,4,7,2,0,4,7,2,4,7,2,0,6,7,4,0,6,7,2,4,7,1,0,6,7,3,0,8,7,3,0,2,4,4,0,2,7,1,4,7,2,0,6,7,4,0,2,4,4,0,2,7,2,4,7,2,0,4,7,2,4,4,7,1,0,7,7,3,0,6,7,4,0,4,7,1,0,1,7,1,4,7,2,0,6,7,2,4,7,4,0,1,7,4,4,7,2,0,5,7,2,4,7,2,4,7,3,4,7,4,0,7,7,3,4,7,1,4,7,1,4,7,1,4,7,2,0,10,7,9,0,12,7,1,0,1,7,1,0,1,7,1,0,1,7,1,0,72,7,1,0,4,7,1,0,13,7,3,0,2,7,3,0,13,7,7,0,1,7,2,0,8,7,1,0,2,7,1,0,1,7,8,0,6,7,3,0,2,7,1,4,7,1,4,0,1,7,1,0,1,7,1,0,8,7,4,0,2,4,0,3,7,1,0,10,7,2,4,4,4,4,0,1,4,7,3,0,8,7,4,0,1,4,4,4,0,2,7,3,0,6,7,5,0,1,4,0,2,4,0,1,7,2,0,8,7,3,0,1,7,1,4,7,2,0,1,7,3,0,8,7,5,0,3,7,5,0,7,7,6,0,1,7,3,0,1,7,1,0,9,7,10,0,12,7,1,0,3,7,1,0,1,7,1,0,112,7,1,0,1,7,1,0,2,7,2,0,12,7,1,0,1,7,7,0,10,7,4,4,7,3,4,7,3,0,7,7,2,4,7,3,4,7,1,4,7,2,0,10,7,2,4,7,1,0,4,7,3,0,10,7,2,4,0,2,4,0,1,4,4,7,2,0,8,7,2,4,7,1,0,1,4,0,2,7,4,0,7,7,2,4,7,1,0,5,4,7,2,0,9,7,5,4,7,1,0,1,7,1,4,7,2,0,9,7,3,4,7,1,4,7,5,0,9,7,2,4,7,2,4,7,3,0,12,7,2,0,1,7,3,0,1,7,1,0,13,7,1,0,2,7,1,0,137,7,1,0,3,7,1,0,12,7,1,0,1,7,7,0,10,7,3,0,2,7,4,0,12,7,2,0,5,7,1,0,1,7,1,0,11,7,2,0,2,4,0,1,7,3,0,10,7,3,0,1,4,0,2,7,1,0,11,7,3,0,5,7,2,0,11,7,1,0,2,7,2,0,2,7,3,0,12,7,5,0,1,7,1,0,12,7,6,0,15,7,1,0,2,7,1,0,159,7,1,0,16,7,6,0,12,7,5,4,7,2,0,12,7,2,4,7,2,4,7,3,0,12,7,2,4,7,1,4,4,4,7,2,0,11,7,3,4,7,4,0,11,7,2,4,4,7,1,4,7,2,0,13,7,5,4,7,2,0,13,7,2,0,1,7,3,0,14,7,1,0,2,7,2,0,198,7,1,0,16,7,1,0,1,7,3,0,14,7,7,0,14,7,3,4,7,3,0,13,7,2,4,7,3,0,13,7,6,0,15,7,2,0,1,7,3,0,18,7,1,0,238,7,1,0,16,7,1,0,2,7,1,0,17,7,5,0,15,7,3,0,17,7,3,0,20,7,1,0,278,7,1,0,19,7,2,0,17,7,1,0,170};
	public ushort[] hps = new ushort[2]{0,7920};
	public ushort[] states = new ushort[2]{0,7920};

	public TreeBigA(){
		this.code = (ushort)StructureCode.TreeBigA; 

		this.sizeX = 18;
		this.sizeY = 22;
		this.sizeZ = 20;

		this.offsetX = 9;
		this.offsetZ = 10;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.type = FillType.FreeSpace;
		this.overwriteBlocks = new HashSet<ushort>();
		this.needsBase = true;
		this.acceptableBaseBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Grass"), VoxelLoader.GetBlockID("BASE_Dirt")};

		Prepare(blocks, hps, states);
	}
}

public class TreeCrookedMediumA : Structure
{
	public ushort[] blocks = new ushort[361]{0,67,4,0,5,4,0,2,4,0,1,4,0,4,4,4,4,4,4,0,6,4,0,8,4,0,89,4,0,98,4,0,8,4,0,80,4,0,8,4,0,8,4,0,80,4,0,8,4,0,98,4,0,73,7,1,0,3,7,2,0,2,7,1,0,1,7,1,0,1,7,1,0,2,7,2,0,2,7,1,0,2,7,1,0,2,7,1,0,1,7,1,0,4,7,2,4,7,2,0,4,7,1,0,1,7,1,0,2,7,1,0,2,7,1,0,2,7,2,0,2,7,1,0,1,7,1,0,1,7,1,0,2,7,2,0,3,7,1,0,38,7,3,0,1,7,4,0,1,7,1,4,7,1,0,1,7,1,4,4,7,2,4,4,7,1,0,2,7,1,4,4,7,1,4,7,2,0,2,7,3,4,7,2,0,3,7,2,4,7,1,4,4,7,2,0,1,7,1,4,4,7,2,4,4,7,1,0,1,7,1,4,7,1,0,1,7,4,0,1,7,3,0,38,7,1,0,3,7,2,0,1,7,4,0,1,7,8,0,2,7,6,0,3,7,3,4,7,3,0,3,7,6,0,2,7,8,0,1,7,4,0,1,7,2,0,3,7,1,0,46,7,1,0,7,7,1,4,7,1,0,5,7,2,4,7,2,0,3,7,1,4,4,4,4,4,7,1,0,3,7,2,4,7,3,0,4,7,1,4,7,1,0,7,7,1,0,53,7,1,0,7,7,3,0,5,7,5,0,3,7,3,4,7,3,0,3,7,5,0,5,7,3,0,7,7,1,0,71,7,1,0,7,7,3,0,7,7,1,0,49};
	public ushort[] hps = new ushort[2]{0,1296};
	public ushort[] states = new ushort[2]{0,1296};

	public TreeCrookedMediumA(){
		this.code = (ushort)StructureCode.TreeCrookedMediumA; 

		this.sizeX = 12;
		this.sizeY = 12;
		this.sizeZ = 9;

		this.offsetX = 9;
		this.offsetZ = 4;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.type = FillType.FreeSpace;
		this.overwriteBlocks = new HashSet<ushort>();
		this.needsBase = true;
		this.acceptableBaseBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Grass"), VoxelLoader.GetBlockID("BASE_Dirt"), VoxelLoader.GetBlockID("BASE_Sand")};

		Prepare(blocks, hps, states);
	}
}

public class TreeSmallB : Structure
{
	public ushort[] blocks = new ushort[399]{0,40,4,0,80,4,0,43,7,3,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,1,0,1,7,1,0,7,7,2,0,3,4,0,3,7,2,0,7,7,1,0,1,7,1,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,3,0,6,7,3,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,1,0,1,7,1,0,7,7,2,0,3,4,0,3,7,2,0,7,7,1,0,1,7,1,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,3,0,6,7,3,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,1,0,1,7,1,0,7,7,2,0,3,4,0,3,7,2,0,7,7,1,0,1,7,1,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,3,0,6,7,3,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,1,0,1,7,1,0,7,7,2,0,3,4,0,3,7,2,0,7,7,1,0,1,7,1,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,3,0,6,7,3,0,5,7,2,4,7,2,0,3,7,2,4,0,1,4,7,2,0,1,7,2,4,0,1,4,0,1,4,7,3,4,0,1,4,4,4,0,1,4,7,3,4,0,1,4,0,1,4,7,2,0,1,7,2,4,0,1,4,7,2,0,3,7,2,4,7,2,0,5,7,3,0,15,7,3,0,5,7,2,4,7,2,0,4,7,1,0,1,4,0,1,7,2,0,2,7,1,4,4,7,1,4,4,7,1,0,3,7,1,0,1,4,0,1,7,2,0,4,7,1,4,7,2,0,5,7,3,0,34,7,1,0,7,7,3,0,5,7,2,0,1,7,2,0,5,7,3,0,7,7,1,0,22};
	public ushort[] hps = new ushort[2]{0,729};
	public ushort[] states = new ushort[2]{0,729};

	public TreeSmallB(){
		this.code = (ushort)StructureCode.TreeSmallB; 

		this.sizeX = 9;
		this.sizeY = 9;
		this.sizeZ = 9;

		this.offsetX = 4;
		this.offsetZ = 4;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.type = FillType.FreeSpace;
		this.overwriteBlocks = new HashSet<ushort>();
		this.needsBase = true;
		this.acceptableBaseBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Grass"), VoxelLoader.GetBlockID("BASE_Dirt"), VoxelLoader.GetBlockID("BASE_Snow")};

		Prepare(blocks, hps, states);
	}
}

public class IronVeinA : Structure
{
	public ushort[] blocks = new ushort[8]{0,1,5,3,0,2,5,2};
	public ushort[] hps = new ushort[2]{0,8};
	public ushort[] states = new ushort[2]{0,8};

	public IronVeinA(){
		this.code = (ushort)StructureCode.IronVeinA; 

		this.sizeX = 2;
		this.sizeY = 2;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class IronVeinB : Structure
{
	public ushort[] blocks = new ushort[16]{5,2,0,2,5,3,0,2,5,1,0,2,5,2,0,4};
	public ushort[] hps = new ushort[2]{0,18};
	public ushort[] states = new ushort[2]{0,18};

	public IronVeinB(){
		this.code = (ushort)StructureCode.IronVeinB; 

		this.sizeX = 3;
		this.sizeY = 2;
		this.sizeZ = 3;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class IronVeinC : Structure
{
	public ushort[] blocks = new ushort[2]{5,4};
	public ushort[] hps = new ushort[2]{0,4};
	public ushort[] states = new ushort[2]{0,4};

	public IronVeinC(){
		this.code = (ushort)StructureCode.IronVeinC; 

		this.sizeX = 2;
		this.sizeY = 1;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class CoalVeinA : Structure
{
	public ushort[] blocks = new ushort[]{19,8,0,1,19,4,0,3,19,2};
	public ushort[] hps = new ushort[]{0,18};
	public ushort[] states = new ushort[]{0,18};

	public CoalVeinA(){
		this.code = (ushort)StructureCode.CoalVeinA; 

		this.sizeX = 2;
		this.sizeY = 3;
		this.sizeZ = 3;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class CoalVeinB : Structure
{
	public ushort[] blocks = new ushort[]{19,6,0,1,19,5,0,1,19,1,0,2};
	public ushort[] hps = new ushort[]{0,16};
	public ushort[] states = new ushort[]{0,16};

	public CoalVeinB(){
		this.code = (ushort)StructureCode.CoalVeinB; 

		this.sizeX = 4;
		this.sizeY = 2;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class CoalVeinC : Structure
{
	public ushort[] blocks = new ushort[]{19,9,0,1,19,1,0,1,19,6,0,1,19,2,0,1,19,1,0,1};
	public ushort[] hps = new ushort[]{0,24};
	public ushort[] states = new ushort[]{0,24};

	public CoalVeinC(){
		this.code = (ushort)StructureCode.CoalVeinC; 

		this.sizeX = 4;
		this.sizeY = 2;
		this.sizeZ = 3;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class CopperVeinA : Structure
{
	public ushort[] blocks = new ushort[]{22,4,0,1,22,1,0,2};
	public ushort[] hps = new ushort[]{0,8};
	public ushort[] states = new ushort[]{0,8};

	public CopperVeinA(){
		this.code = (ushort)StructureCode.CopperVeinA; 

		this.sizeX = 2;
		this.sizeY = 2;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class CopperVeinB : Structure
{
	public ushort[] blocks = new ushort[]{22,4,0,1,22,1,0,1,22,3,0,2};
	public ushort[] hps = new ushort[]{0,12};
	public ushort[] states = new ushort[]{0,12};

	public CopperVeinB(){
		this.code = (ushort)StructureCode.CopperVeinB; 

		this.sizeX = 3;
		this.sizeY = 2;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class TinVeinA : Structure
{
	public ushort[] blocks = new ushort[]{23,3,0,1,23,1,0,2,23,2,0,3};
	public ushort[] hps = new ushort[]{0,12};
	public ushort[] states = new ushort[]{0,12};

	public TinVeinA(){
		this.code = (ushort)StructureCode.TinVeinA; 

		this.sizeX = 2;
		this.sizeY = 2;
		this.sizeZ = 3;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class TinVeinB : Structure
{
	public ushort[] blocks = new ushort[]{23,2,0,1,23,1,0,1,23,1,0,1,23,1,0,1,23,1,0,1,23,1};
	public ushort[] hps = new ushort[]{0,12};
	public ushort[] states = new ushort[]{0,12};

	public TinVeinB(){
		this.code = (ushort)StructureCode.TinVeinB; 

		this.sizeX = 2;
		this.sizeY = 3;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class GoldVeinA : Structure
{
	public ushort[] blocks = new ushort[]{24,4,0,1,24,1,0,3,24,1,0,2};
	public ushort[] hps = new ushort[]{0,12};
	public ushort[] states = new ushort[]{0,12};

	public GoldVeinA(){
		this.code = (ushort)StructureCode.GoldVeinA; 

		this.sizeX = 3;
		this.sizeY = 2;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class GoldVeinB : Structure
{
	public ushort[] blocks = new ushort[]{24,3,0,1,24,3,0,1};
	public ushort[] hps = new ushort[]{0,8};
	public ushort[] states = new ushort[]{0,8};

	public GoldVeinB(){
		this.code = (ushort)StructureCode.GoldVeinB; 

		this.sizeX = 2;
		this.sizeY = 2;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class AluminiumVeinA : Structure
{
	public ushort[] blocks = new ushort[]{21,3,0,1,21,1,0,1,21,2,0,4};
	public ushort[] hps = new ushort[]{0,12};
	public ushort[] states = new ushort[]{0,12};

	public AluminiumVeinA(){
		this.code = (ushort)StructureCode.AluminiumVeinA; 

		this.sizeX = 2;
		this.sizeY = 2;
		this.sizeZ = 3;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class AluminiumVeinB : Structure
{
	public ushort[] blocks = new ushort[]{21,4,0,3,21,1};
	public ushort[] hps = new ushort[]{0,8};
	public ushort[] states = new ushort[]{0,8};

	public AluminiumVeinB(){
		this.code = (ushort)StructureCode.AluminiumVeinB; 

		this.sizeX = 2;
		this.sizeY = 2;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class EmeriumVeinA : Structure
{
	public ushort[] blocks = new ushort[]{25,2,0,1,25,1,0,1,25,1,0,2};
	public ushort[] hps = new ushort[]{0,8};
	public ushort[] states = new ushort[]{0,8};

	public EmeriumVeinA(){
		this.code = (ushort)StructureCode.EmeriumVeinA; 

		this.sizeX = 2;
		this.sizeY = 2;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class EmeriumVeinB : Structure
{
	public ushort[] blocks = new ushort[]{25,4,0,1,25,1,0,1,25,1};
	public ushort[] hps = new ushort[]{0,8};
	public ushort[] states = new ushort[]{0,8};

	public EmeriumVeinB(){
		this.code = (ushort)StructureCode.EmeriumVeinB; 

		this.sizeX = 2;
		this.sizeY = 2;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class UraniumVeinA : Structure
{
	public ushort[] blocks = new ushort[]{26};
	public ushort[] hps = new ushort[]{0,1};
	public ushort[] states = new ushort[]{0,1};

	public UraniumVeinA(){
		this.code = (ushort)StructureCode.UraniumVeinA; 

		this.sizeX = 1;
		this.sizeY = 1;
		this.sizeZ = 1;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class UraniumVeinB : Structure
{
	public ushort[] blocks = new ushort[]{26, 26};
	public ushort[] hps = new ushort[]{0,2};
	public ushort[] states = new ushort[]{0,2};

	public UraniumVeinB(){
		this.code = (ushort)StructureCode.UraniumVeinB; 

		this.sizeX = 1;
		this.sizeY = 2;
		this.sizeZ = 1;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class MagnetiteVeinA : Structure
{
	public ushort[] blocks = new ushort[]{20,3,0,2,20,3,0,1,20,2,0,3,20,1,0,1};
	public ushort[] hps = new ushort[]{0,16};
	public ushort[] states = new ushort[]{0,16};

	public MagnetiteVeinA(){
		this.code = (ushort)StructureCode.MagnetiteVeinA; 

		this.sizeX = 2;
		this.sizeY = 2;
		this.sizeZ = 4;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class MagnetiteVeinB : Structure
{
	public ushort[] blocks = new ushort[]{20,9,0,1,20,2};
	public ushort[] hps = new ushort[]{0,12};
	public ushort[] states = new ushort[]{0,12};

	public MagnetiteVeinB(){
		this.code = (ushort)StructureCode.MagnetiteVeinB; 

		this.sizeX = 2;
		this.sizeY = 3;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class EmeraldVeinA : Structure
{
	public ushort[] blocks = new ushort[]{27,27,0,1,27};
	public ushort[] hps = new ushort[]{0,4};
	public ushort[] states = new ushort[]{0,4};

	public EmeraldVeinA(){
		this.code = (ushort)StructureCode.EmeraldVeinA; 

		this.sizeX = 2;
		this.sizeY = 1;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class EmeraldVeinB : Structure
{
	public ushort[] blocks = new ushort[]{27,27,0,1,27};
	public ushort[] hps = new ushort[]{0,4};
	public ushort[] states = new ushort[]{0,4};

	public EmeraldVeinB(){
		this.code = (ushort)StructureCode.EmeraldVeinB; 

		this.sizeX = 1;
		this.sizeY = 2;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class RubyVeinA : Structure
{
	public ushort[] blocks = new ushort[]{28,28,0,1,28};
	public ushort[] hps = new ushort[]{0,4};
	public ushort[] states = new ushort[]{0,4};

	public RubyVeinA(){
		this.code = (ushort)StructureCode.RubyVeinA; 

		this.sizeX = 2;
		this.sizeY = 1;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class RubyVeinB : Structure
{
	public ushort[] blocks = new ushort[]{28,28,0,1,28};
	public ushort[] hps = new ushort[]{0,4};
	public ushort[] states = new ushort[]{0,4};

	public RubyVeinB(){
		this.code = (ushort)StructureCode.RubyVeinB; 

		this.sizeX = 1;
		this.sizeY = 2;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone")};

		Prepare(blocks, hps, states);
	}
}

public class GravelPile : Structure
{
	public ushort[] blocks = new ushort[]{0,1,31,3,0,1,31,12,0,4,31,2,0,3,31,3,0,2,31,3,0,6};
	public ushort[] hps = new ushort[]{0,40};
	public ushort[] states = new ushort[]{0,40};

	public GravelPile(){
		this.code = (ushort)StructureCode.GravelPile; 

		this.sizeX = 4;
		this.sizeY = 2;
		this.sizeZ = 5;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Stone"), VoxelLoader.GetBlockID("BASE_Dirt")};

		Prepare(blocks, hps, states);
	}
}

public class BigFossil1 : Structure
{
	public ushort[] blocks = new ushort[]{0,3,16,3,0,6,16,3,0,7,16,1,0,8,16,1,0,8,16,1,0,8,16,1,0,8,16,1,0,5,16,7,0,5,16,1,0,8,16,1,0,8,16,1,0,5,16,7,0,5,16,1,0,8,16,1,0,8,16,1,0,5,16,7,0,5,16,1,0,8,16,1,0,8,16,1,0,4,16,9,0,64,16,1,0,5,16,1,0,29,16,1,0,5,16,1,0,29,16,1,0,5,16,1,0,28,16,1,0,7,16,1,0,64,16,1,0,5,16,1,0,29,16,1,0,5,16,1,0,29,16,1,0,5,16,1,0,28,16,1,0,7,16,1,0,64,16,1,0,5,16,1,0,29,16,1,0,5,16,1,0,29,16,1,0,5,16,1,0,28,16,1,0,7,16,1,0,65,16,2,0,1,16,2,0,31,16,2,0,1,16,2,0,31,16,2,0,1,16,2,0,30,16,2,0,3,16,2,0,1};
	public ushort[] hps = new ushort[]{0,900};
	public ushort[] states = new ushort[]{0,900};

	public BigFossil1(){
		this.code = (ushort)StructureCode.BigFossil1; 

		this.sizeX = 20;
		this.sizeY = 5;
		this.sizeZ = 9;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new HashSet<ushort>(){};

		Prepare(blocks, hps, states);
	}
}

public class BigFossil2 : Structure
{
	public ushort[] blocks = new ushort[]{16,2,0,3,16,2,0,7,16,2,0,3,16,2,0,7,16,2,0,3,16,2,0,7,16,2,0,3,16,2,0,7,16,2,0,3,16,2,0,7,16,2,0,3,16,2,0,7,16,2,0,3,16,2,0,7,16,2,0,3,16,2,0,14,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,14,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,14,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,7,16,1,0,5,16,1,0,14,16,7,0,3,16,1,0,3,16,7,0,3,16,1,0,3,16,7,0,3,16,1,0,3,16,7,0,3,16,1,0,3,16,7,0,3,16,1,0,3,16,7,0,3,16,1,0,3,16,7,0,3,16,1,0,3,16,7,0,3,16,1,0,6,16,1,0,3};
	public ushort[] hps = new ushort[]{0,595};
	public ushort[] states = new ushort[]{0,595};

	public BigFossil2(){
		this.code = (ushort)StructureCode.BigFossil2; 

		this.sizeX = 17;
		this.sizeY = 5;
		this.sizeZ = 7;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new HashSet<ushort>(){};

		Prepare(blocks, hps, states);
	}
}

public class LittleBone1 : Structure
{
	public ushort[] blocks = new ushort[]{16,1,0,1,16,1,0,1,16,1,0,2,16,1,0,2,16,1,0,2,16,1,0,1,16,1,0,1,16,1};
	public ushort[] hps = new ushort[]{0,18};
	public ushort[] states = new ushort[]{0,18};

	public LittleBone1(){
		this.code = (ushort)StructureCode.LittleBone1; 

		this.sizeX = 6;
		this.sizeY = 1;
		this.sizeZ = 3;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new HashSet<ushort>(){};

		Prepare(blocks, hps, states);
	}
}

public class LittleBone2 : Structure
{
	public ushort[] blocks = new ushort[]{16,2,0,3,16,1,0,3,16,1,0,3,16,2,0,1,16,4};
	public ushort[] hps = new ushort[]{0,20};
	public ushort[] states = new ushort[]{0,20};

	public LittleBone2(){
		this.code = (ushort)StructureCode.LittleBone2; 

		this.sizeX = 5;
		this.sizeY = 1;
		this.sizeZ = 4;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new HashSet<ushort>(){};

		Prepare(blocks, hps, states);
	}
}

public class BigUpBone : Structure
{
	public ushort[] blocks = new ushort[]{16,1,0,1,16,1,0,3,16,1,0,1,16,2,0,1,16,1,0,1,16,1,0,1,16,1,0,1,16,1,0,4,16,1,0,7,16,2,0,7,16,2,0,7,16,3,0,7,16,2,0,7,16,1,0,4,16,1,0,1,16,1,0,1,16,1,0,1,16,1,0,1,16,2,0,1,16,1,0,3,16,1,0,1,16,1};
	public ushort[] hps = new ushort[]{0,90};
	public ushort[] states = new ushort[]{0,90};

	public BigUpBone(){
		this.code = (ushort)StructureCode.BigUpBone; 

		this.sizeX = 3;
		this.sizeY = 10;
		this.sizeZ = 3;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new HashSet<ushort>(){};

		Prepare(blocks, hps, states);
	}
}

public class BigCrossBone : Structure
{
	public ushort[] blocks = new ushort[]{0,4,16,1,0,7,16,1,0,6,16,2,0,6,16,1,0,7,16,2,0,6,16,2,0,7,16,1,0,6,16,2,0,6,16,2,0,3,16,8,0,1,16,4,0,1,16,1,0,5,16,1,0,6,16,2,0,3};
	public ushort[] hps = new ushort[]{0,104};
	public ushort[] states = new ushort[]{0,104};

	public BigCrossBone(){
		this.code = (ushort)StructureCode.BigCrossBone; 

		this.sizeX = 1;
		this.sizeY = 13;
		this.sizeZ = 8;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new HashSet<ushort>(){};

		Prepare(blocks, hps, states);
	}
}

public class CobaltVeinA : Structure
{
	public ushort[] blocks = new ushort[]{36,3,0,1,36,1,0,3};
	public ushort[] hps = new ushort[]{0,8};
	public ushort[] states = new ushort[]{0,8};

	public CobaltVeinA(){
		this.code = (ushort)StructureCode.CobaltVeinA; 

		this.sizeX = 2;
		this.sizeY = 2;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Hell_Marble")};

		Prepare(blocks, hps, states);
	}
}

public class CobaltVeinB : Structure
{
	public ushort[] blocks = new ushort[]{36,1,0,1,36,4,0,3,36,1,0,2};
	public ushort[] hps = new ushort[]{0,12};
	public ushort[] states = new ushort[]{0,12};

	public CobaltVeinB(){
		this.code = (ushort)StructureCode.CobaltVeinB; 

		this.sizeX = 3;
		this.sizeY = 2;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Hell_Marble")};

		Prepare(blocks, hps, states);
	}
}

public class ArditeVeinA : Structure
{
	public ushort[] blocks = new ushort[]{37,4,0,1,37,1,0,1,37,1};
	public ushort[] hps = new ushort[]{0,8};
	public ushort[] states = new ushort[]{0,8};

	public ArditeVeinA(){
		this.code = (ushort)StructureCode.ArditeVeinA; 

		this.sizeX = 2;
		this.sizeY = 2;
		this.sizeZ = 2;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Hell_Marble")};

		Prepare(blocks, hps, states);
	}
}

public class ArditeVeinB : Structure
{
	public ushort[] blocks = new ushort[]{37,4,0,1,37,1,0,2};
	public ushort[] hps = new ushort[]{0,8};
	public ushort[] states = new ushort[]{0,8};

	public ArditeVeinB(){
		this.code = (ushort)StructureCode.ArditeVeinB; 

		this.sizeX = 4;
		this.sizeY = 2;
		this.sizeZ = 1;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Hell_Marble")};

		Prepare(blocks, hps, states);
	}
}

public class GrandiumVeinA : Structure
{
	public ushort[] blocks = new ushort[]{38,2,0,1,38,1};
	public ushort[] hps = new ushort[]{0,4};
	public ushort[] states = new ushort[]{0,4};

	public GrandiumVeinA(){
		this.code = (ushort)StructureCode.GrandiumVeinA; 

		this.sizeX = 2;
		this.sizeY = 2;
		this.sizeZ = 1;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Moonstone")};

		Prepare(blocks, hps, states);
	}
}

public class GrandiumVeinB : Structure
{
	public ushort[] blocks = new ushort[]{38,2,0,1,38,1,0,1,38,1};
	public ushort[] hps = new ushort[]{0,6};
	public ushort[] states = new ushort[]{0,6};

	public GrandiumVeinB(){
		this.code = (ushort)StructureCode.GrandiumVeinB; 

		this.sizeX = 3;
		this.sizeY = 2;
		this.sizeZ = 1;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Moonstone")};

		Prepare(blocks, hps, states);
	}
}

public class SteonyxVein : Structure
{
	public ushort[] blocks = new ushort[]{39,2};
	public ushort[] hps = new ushort[]{0,2};
	public ushort[] states = new ushort[]{0,2};

	public SteonyxVein(){
		this.code = (ushort)StructureCode.SteonyxVein; 

		this.sizeX = 2;
		this.sizeY = 1;
		this.sizeZ = 1;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){VoxelLoader.GetBlockID("BASE_Moonstone")};

		Prepare(blocks, hps, states);
	}
}
/*
    PRECANTIO_CRYSTAL = 65528,
    PERDITIO_CRYSTAL = 65529,
    ORDO_CRYSTAL = 65530,
    TERRA_CRYSTAL = 65531,
    AER_CRYSTAL = 65532,
    AQUA_CRYSTAL = 65533,
    IGNIS_CRYSTAL = 65534,
 */
public class SingleIgnisVisCrystal : Structure
{
	public ushort[] blocks = new ushort[]{65534};
	public ushort[] hps = new ushort[]{0,1};
	public ushort[] states = new ushort[]{0,1};

	public SingleIgnisVisCrystal(){
		this.code = (ushort)StructureCode.SingleIgnisVisCrystal;

		this.sizeX = 1;
		this.sizeY = 1;
		this.sizeZ = 1;

		this.offsetX = 0;
		this.offsetZ = 0;

		this.blockdata = new ushort[1];
		this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new HashSet<ushort>();

		Prepare(blocks, hps, states);
	}
}

public class SingleAquaVisCrystal : Structure
{
	public ushort[] blocks = new ushort[]{65533};
	public ushort[] hps = new ushort[]{0,1};
	public ushort[] states = new ushort[]{0,1};

	public SingleAquaVisCrystal(){
		this.code = (ushort)StructureCode.SingleAquaVisCrystal;

		this.sizeX = 1;
		this.sizeY = 1;
		this.sizeZ = 1;

		this.offsetX = 0;
		this.offsetZ = 0;

		this.blockdata = new ushort[1];
		this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new HashSet<ushort>();

		Prepare(blocks, hps, states);
	}
}

public class SingleAerVisCrystal : Structure
{
	public ushort[] blocks = new ushort[]{65532};
	public ushort[] hps = new ushort[]{0,1};
	public ushort[] states = new ushort[]{0,1};

	public SingleAerVisCrystal(){
		this.code = (ushort)StructureCode.SingleAerVisCrystal;

		this.sizeX = 1;
		this.sizeY = 1;
		this.sizeZ = 1;

		this.offsetX = 0;
		this.offsetZ = 0;

		this.blockdata = new ushort[1];
		this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new HashSet<ushort>();

		Prepare(blocks, hps, states);
	}
}


public class SingleTerraVisCrystal : Structure
{
	public ushort[] blocks = new ushort[]{65531};
	public ushort[] hps = new ushort[]{0,1};
	public ushort[] states = new ushort[]{0,1};

	public SingleTerraVisCrystal(){
		this.code = (ushort)StructureCode.SingleTerraVisCrystal;

		this.sizeX = 1;
		this.sizeY = 1;
		this.sizeZ = 1;

		this.offsetX = 0;
		this.offsetZ = 0;

		this.blockdata = new ushort[1];
		this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new HashSet<ushort>();

		Prepare(blocks, hps, states);
	}
}

public class SingleOrdoVisCrystal : Structure
{
	public ushort[] blocks = new ushort[]{65530};
	public ushort[] hps = new ushort[]{0,1};
	public ushort[] states = new ushort[]{0,1};

	public SingleOrdoVisCrystal(){
		this.code = (ushort)StructureCode.SingleOrdoVisCrystal;

		this.sizeX = 1;
		this.sizeY = 1;
		this.sizeZ = 1;

		this.offsetX = 0;
		this.offsetZ = 0;

		this.blockdata = new ushort[1];
		this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new HashSet<ushort>();

		Prepare(blocks, hps, states);
	}
}

public class SinglePerditioVisCrystal : Structure
{
	public ushort[] blocks = new ushort[]{65529};
	public ushort[] hps = new ushort[]{0,1};
	public ushort[] states = new ushort[]{0,1};

	public SinglePerditioVisCrystal(){
		this.code = (ushort)StructureCode.SinglePerditioVisCrystal;

		this.sizeX = 1;
		this.sizeY = 1;
		this.sizeZ = 1;

		this.offsetX = 0;
		this.offsetZ = 0;

		this.blockdata = new ushort[1];
		this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new HashSet<ushort>();

		Prepare(blocks, hps, states);
	}
}

public class SingleMagicVisCrystal : Structure
{
	public ushort[] blocks = new ushort[]{65528};
	public ushort[] hps = new ushort[]{0,1};
	public ushort[] states = new ushort[]{0,1};

	public SingleMagicVisCrystal(){
		this.code = (ushort)StructureCode.SingleMagicVisCrystal;

		this.sizeX = 1;
		this.sizeY = 1;
		this.sizeZ = 1;

		this.offsetX = 0;
		this.offsetZ = 0;

		this.blockdata = new ushort[1];
		this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new HashSet<ushort>();

		Prepare(blocks, hps, states);
	}
}

/*
ADD TO THIS ENUM EVERY NEW STRUCTURE IMPLEMENTED
*/

public enum StructureCode{
	TestStruct,
	TreeSmallA,
	TreeMediumA,
	DirtPileA,
	DirtPileB,
	BoulderNormalA,
	TreeBigA,
	TreeCrookedMediumA,
	TreeSmallB,
	IronVeinA,
	IronVeinB,
	IronVeinC,
	CoalVeinA,
	CoalVeinB,
	CoalVeinC,
	CopperVeinA,
	CopperVeinB,
	TinVeinA,
	TinVeinB,
	GoldVeinA,
	GoldVeinB,
	AluminiumVeinA,
	AluminiumVeinB,
	EmeriumVeinA,
	EmeriumVeinB,
	UraniumVeinA,
	UraniumVeinB,
	MagnetiteVeinA,
	MagnetiteVeinB,
	EmeraldVeinA,
	EmeraldVeinB,
	RubyVeinA,
	RubyVeinB,
	GravelPile,
	BigFossil1,
	LittleBone1,
	LittleBone2,
	BigFossil2,
	BigUpBone,
	BigCrossBone,
	CobaltVeinA,
	CobaltVeinB,
	ArditeVeinA,
	ArditeVeinB,
	GrandiumVeinA,
	GrandiumVeinB,
	SteonyxVein,
	SingleIgnisVisCrystal,
	SingleAquaVisCrystal,
	SingleTerraVisCrystal,
	SingleAerVisCrystal,
	SingleOrdoVisCrystal,
	SinglePerditioVisCrystal,
	SingleMagicVisCrystal
}