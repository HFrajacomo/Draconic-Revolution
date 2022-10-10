using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestStruct : Structure
{
	public ushort[] blocks = new ushort[2]{3,24};
	public ushort[] hps = new ushort[2]{0,24};
	public ushort[] states = new ushort[2]{0,24};

	public TestStruct(){
		this.code = 0; 

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
		this.code = 1; 

		this.sizeX = 5;
		this.sizeY = 7;
		this.sizeZ = 5;

		this.offsetX = 2;
		this.offsetZ = 2;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

        this.isGrounded = true;

		this.considerAir = false;
		this.type = FillType.FreeSpace;
		this.overwriteBlocks = new HashSet<ushort>();
		this.needsBase = true;
		this.acceptableBaseBlocks = new HashSet<BlockID>(){BlockID.GRASS, BlockID.DIRT, BlockID.SNOW};

		Prepare(blocks, hps, states);
	}
}

public class TreeMediumA : Structure
{

	public ushort[] blocks = new ushort[214]{0,14,4,0,7,4,4,4,0,6,4,4,4,0,7,4,0,35,4,0,7,4,4,0,7,4,4,0,52,4,4,0,7,4,4,0,43,7,1,0,7,7,1,4,4,0,6,7,1,4,4,7,1,0,6,7,3,0,6,7,2,0,7,7,2,0,7,7,4,0,1,7,7,0,4,7,2,4,4,7,2,0,1,7,4,4,4,7,1,0,4,7,3,4,7,2,0,3,7,3,4,7,1,0,7,7,1,0,7,7,1,4,7,1,0,2,7,5,4,7,2,0,1,7,2,4,4,4,4,7,2,0,1,7,4,4,4,7,2,0,2,7,3,4,7,3,0,2,7,7,0,6,7,1,0,8,7,1,0,7,7,3,0,3,7,3,4,4,7,1,0,3,7,3,4,7,3,0,2,7,6,0,6,7,2,0,1,7,1,0,22,7,2,0,7,7,4,0,5,7,1,4,7,1,0,6,7,3,0,52,7,1,0,31};
	public ushort[] hps = new ushort[2]{0,567};
	public ushort[] states = new ushort[2]{0,567};

	public TreeMediumA(){
		this.code = 2; 

		this.sizeX = 7;
		this.sizeY = 9;
		this.sizeZ = 9;

		this.offsetX = 3;
		this.offsetZ = 5;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

        this.isGrounded = true;

		this.considerAir = false;
		this.type = FillType.FreeSpace;
		this.overwriteBlocks = new HashSet<ushort>();
		this.needsBase = true;
		this.acceptableBaseBlocks = new HashSet<BlockID>(){BlockID.GRASS, BlockID.DIRT, BlockID.SNOW};

		Prepare(blocks, hps, states);
	}
}

public class DirtPileA : Structure
{
	public ushort[] blocks = new ushort[66]{0,2,2,3,0,1,2,29,0,2,2,3,0,4,2,1,0,1,2,1,0,3,2,3,0,1,2,5,0,2,2,4,0,2,2,4,0,2,2,4,0,4,2,1,0,10,2,2,0,3,2,3,0,3,2,4,0,2,2,3,0,3,2,2,0,9};
	public ushort[] hps = new ushort[2]{0,126};
	public ushort[] states = new ushort[2]{0,126};

	public DirtPileA(){
		this.code = 3; 

		this.sizeX = 7;
		this.sizeY = 3;
		this.sizeZ = 6;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

        this.isGrounded = true;

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){1,3};

		Prepare(blocks, hps, states);
	}
}

public class DirtPileB : Structure
{
	public ushort[] blocks = new ushort[66]{0,3,2,2,0,3,2,5,0,1,2,27,0,3,2,3,0,5,2,1,0,6,2,3,0,1,2,6,0,1,2,6,0,1,2,6,0,2,2,5,0,4,2,2,0,12,2,2,0,3,2,5,0,1,2,6,0,1,2,5,0,4,2,1,0,11};
	public ushort[] hps = new ushort[2]{0,147};
	public ushort[] states = new ushort[2]{0,147};

	public DirtPileB(){
		this.code = 4; 

		this.sizeX = 7;
		this.sizeY = 3;
		this.sizeZ = 7;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

        this.isGrounded = true;

		this.considerAir = false;
		this.needsBase = false;
		this.type = FillType.SpecificOverwrite;
		this.overwriteBlocks = new HashSet<ushort>(){1,3};

		Prepare(blocks, hps, states);
	}
}

public class BoulderNormalA : Structure
{
	public ushort[] blocks = new ushort[54]{0,3,3,3,0,2,3,6,0,1,3,6,0,5,3,1,0,3,3,4,0,1,3,14,0,2,3,4,0,4,3,2,0,3,3,5,0,2,3,5,0,4,3,2,0,11,3,3,0,4,3,3,0,9};
	public ushort[] hps = new ushort[2]{0,112};
	public ushort[] states = new ushort[2]{0,112};

	public BoulderNormalA(){
		this.code = 5; 

		this.sizeX = 4;
		this.sizeY = 4;
		this.sizeZ = 7;

		this.offsetX = 0;
		this.offsetZ = 0;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

        this.isGrounded = true;

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
		this.code = 6; 

		this.sizeX = 18;
		this.sizeY = 22;
		this.sizeZ = 20;

		this.offsetX = 9;
		this.offsetZ = 10;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

        this.isGrounded = true;

		this.considerAir = false;
		this.type = FillType.FreeSpace;
		this.overwriteBlocks = new HashSet<ushort>();
		this.needsBase = true;
		this.acceptableBaseBlocks = new HashSet<BlockID>(){BlockID.GRASS, BlockID.DIRT};

		Prepare(blocks, hps, states);
	}
}

public class TreeCrookedMediumA : Structure
{
	public ushort[] blocks = new ushort[361]{0,67,4,0,5,4,0,2,4,0,1,4,0,4,4,4,4,4,4,0,6,4,0,8,4,0,89,4,0,98,4,0,8,4,0,80,4,0,8,4,0,8,4,0,80,4,0,8,4,0,98,4,0,73,7,1,0,3,7,2,0,2,7,1,0,1,7,1,0,1,7,1,0,2,7,2,0,2,7,1,0,2,7,1,0,2,7,1,0,1,7,1,0,4,7,2,4,7,2,0,4,7,1,0,1,7,1,0,2,7,1,0,2,7,1,0,2,7,2,0,2,7,1,0,1,7,1,0,1,7,1,0,2,7,2,0,3,7,1,0,38,7,3,0,1,7,4,0,1,7,1,4,7,1,0,1,7,1,4,4,7,2,4,4,7,1,0,2,7,1,4,4,7,1,4,7,2,0,2,7,3,4,7,2,0,3,7,2,4,7,1,4,4,7,2,0,1,7,1,4,4,7,2,4,4,7,1,0,1,7,1,4,7,1,0,1,7,4,0,1,7,3,0,38,7,1,0,3,7,2,0,1,7,4,0,1,7,8,0,2,7,6,0,3,7,3,4,7,3,0,3,7,6,0,2,7,8,0,1,7,4,0,1,7,2,0,3,7,1,0,46,7,1,0,7,7,1,4,7,1,0,5,7,2,4,7,2,0,3,7,1,4,4,4,4,4,7,1,0,3,7,2,4,7,3,0,4,7,1,4,7,1,0,7,7,1,0,53,7,1,0,7,7,3,0,5,7,5,0,3,7,3,4,7,3,0,3,7,5,0,5,7,3,0,7,7,1,0,71,7,1,0,7,7,3,0,7,7,1,0,49};
	public ushort[] hps = new ushort[2]{0,1296};
	public ushort[] states = new ushort[2]{0,1296};

	public TreeCrookedMediumA(){
		this.code = 7; 

		this.sizeX = 12;
		this.sizeY = 12;
		this.sizeZ = 9;

		this.offsetX = 9;
		this.offsetZ = 4;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

        this.isGrounded = true;

		this.considerAir = false;
		this.type = FillType.FreeSpace;
		this.overwriteBlocks = new HashSet<ushort>();
		this.needsBase = true;
		this.acceptableBaseBlocks = new HashSet<BlockID>(){BlockID.GRASS, BlockID.DIRT, BlockID.SAND};

		Prepare(blocks, hps, states);
	}
}

public class TreeSmallB : Structure
{
	public ushort[] blocks = new ushort[399]{0,40,4,0,80,4,0,43,7,3,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,1,0,1,7,1,0,7,7,2,0,3,4,0,3,7,2,0,7,7,1,0,1,7,1,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,3,0,6,7,3,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,1,0,1,7,1,0,7,7,2,0,3,4,0,3,7,2,0,7,7,1,0,1,7,1,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,3,0,6,7,3,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,1,0,1,7,1,0,7,7,2,0,3,4,0,3,7,2,0,7,7,1,0,1,7,1,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,3,0,6,7,3,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,1,0,1,7,1,0,7,7,2,0,3,4,0,3,7,2,0,7,7,1,0,1,7,1,0,5,7,1,0,3,7,1,0,3,7,1,0,5,7,3,0,6,7,3,0,5,7,2,4,7,2,0,3,7,2,4,0,1,4,7,2,0,1,7,2,4,0,1,4,0,1,4,7,3,4,0,1,4,4,4,0,1,4,7,3,4,0,1,4,0,1,4,7,2,0,1,7,2,4,0,1,4,7,2,0,3,7,2,4,7,2,0,5,7,3,0,15,7,3,0,5,7,2,4,7,2,0,4,7,1,0,1,4,0,1,7,2,0,2,7,1,4,4,7,1,4,4,7,1,0,3,7,1,0,1,4,0,1,7,2,0,4,7,1,4,7,2,0,5,7,3,0,34,7,1,0,7,7,3,0,5,7,2,0,1,7,2,0,5,7,3,0,7,7,1,0,22};
	public ushort[] hps = new ushort[2]{0,729};
	public ushort[] states = new ushort[2]{0,729};

	public TreeSmallB(){
		this.code = 8; 

		this.sizeX = 9;
		this.sizeY = 9;
		this.sizeZ = 9;

		this.offsetX = 4;
		this.offsetZ = 4;

        this.blockdata = new ushort[sizeX*sizeY*sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

        this.isGrounded = true;

		this.considerAir = false;
		this.type = FillType.FreeSpace;
		this.overwriteBlocks = new HashSet<ushort>();
		this.needsBase = true;
		this.acceptableBaseBlocks = new HashSet<BlockID>(){BlockID.GRASS, BlockID.DIRT, BlockID.SNOW};

		Prepare(blocks, hps, states);
	}
}

public class MetalVeinA : Structure
{
	public ushort[] blocks = new ushort[8]{0,1,5,3,0,2,5,2};
	public ushort[] hps = new ushort[2]{0,8};
	public ushort[] states = new ushort[2]{0,8};

	public MetalVeinA(){
		this.code = 9; 

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
		this.overwriteBlocks = new HashSet<ushort>(){3};

		Prepare(blocks, hps, states);
	}
}

public class MetalVeinB : Structure
{
	public ushort[] blocks = new ushort[16]{5,2,0,2,5,3,0,2,5,1,0,2,5,2,0,4};
	public ushort[] hps = new ushort[2]{0,18};
	public ushort[] states = new ushort[2]{0,18};

	public MetalVeinB(){
		this.code = 10; 

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
		this.overwriteBlocks = new HashSet<ushort>(){3};

		Prepare(blocks, hps, states);
	}
}

public class MetalVeinC : Structure
{
	public ushort[] blocks = new ushort[2]{5,4};
	public ushort[] hps = new ushort[2]{0,4};
	public ushort[] states = new ushort[2]{0,4};

	public MetalVeinC(){
		this.code = 11; 

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
		this.overwriteBlocks = new HashSet<ushort>(){3};

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
	MetalVeinA,
	MetalVeinB,
	MetalVeinC
}