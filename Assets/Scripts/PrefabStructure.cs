using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestStruct : Structure
{
	ushort[] blocks = new ushort[24]{3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3};
	ushort?[] hps = new ushort?[24]{null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null};
	ushort?[] states = new ushort?[24]{null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null};

	public TestStruct(){
		this.code = 0; 

		this.sizeX = 12;
		this.sizeY = 1;
		this.sizeZ = 2;

        this.blockdata = new ushort[sizeX, sizeY, sizeZ];
        this.hpdata = new ushort?[sizeX, sizeY, sizeZ];
        this.statedata = new ushort?[sizeX, sizeY, sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.type = FillType.OverwriteAll;
		this.overwriteBlocks = new List<ushort>();

		Prepare(blocks, hps, states);
	}
}

public class TreeSmallA : Structure
{

	ushort[] blocks = new ushort[175]{0,0,0,0,0,0,0,0,0,0,0,0,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,0,0,0,0,0,0,0,0,0,0,0,0,0,65534,65534,65534,0,65534,65534,65534,65534,65534,65534,65534,4,65534,65534,65534,65534,65534,65534,65534,0,65534,65534,65534,0,0,65534,65534,65534,0,65534,65534,65534,65534,65534,65534,65534,4,65534,65534,65534,65534,65534,65534,65534,0,65534,65534,65534,0,0,65534,65534,65534,0,65534,65534,65534,65534,65534,65534,65534,65534,65534,65534,65534,65534,65534,65534,65534,0,65534,65534,65534,0,0,0,0,0,0,0,65534,65534,65534,0,0,65534,65534,65534,0,0,65534,65534,65534,0,0,0,0,0,0};
	ushort?[] hps = new ushort?[175]{null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null};
	ushort?[] states = new ushort?[175]{null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null};

	public TreeSmallA(){
		this.code = 1; 

		this.sizeX = 5;
		this.sizeY = 7;
		this.sizeZ = 5;

        this.blockdata = new ushort[sizeX, sizeY, sizeZ];
        this.hpdata = new ushort?[sizeX, sizeY, sizeZ];
        this.statedata = new ushort?[sizeX, sizeY, sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.type = FillType.FreeSpace;
		this.overwriteBlocks = new List<ushort>();

		Prepare(blocks, hps, states);
	}
}

public class TreeMediumA : Structure
{

	ushort[] blocks = new ushort[567]{0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,0,0,0,0,0,0,0,4,4,0,0,0,0,0,0,0,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,0,0,0,0,0,0,0,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,65534,0,0,0,0,0,0,0,65534,4,4,0,0,0,0,0,0,65534,4,4,65534,0,0,0,0,0,0,65534,65534,65534,0,0,0,0,0,0,65534,65534,0,0,0,0,0,0,0,65534,65534,0,0,0,0,0,0,0,65534,65534,65534,65534,0,65534,65534,65534,65534,65534,65534,65534,0,0,0,0,65534,65534,4,4,65534,65534,0,65534,65534,65534,65534,4,4,65534,0,0,0,0,65534,65534,65534,4,65534,65534,0,0,0,65534,65534,65534,4,65534,0,0,0,0,0,0,0,65534,0,0,0,0,0,0,0,65534,4,65534,0,0,65534,65534,65534,65534,65534,4,65534,65534,0,65534,65534,4,4,4,4,65534,65534,0,65534,65534,65534,65534,4,4,65534,65534,0,0,65534,65534,65534,4,65534,65534,65534,0,0,65534,65534,65534,65534,65534,65534,65534,0,0,0,0,0,0,65534,0,0,0,0,0,0,0,0,65534,0,0,0,0,0,0,0,65534,65534,65534,0,0,0,65534,65534,65534,4,4,65534,0,0,0,65534,65534,65534,4,65534,65534,65534,0,0,65534,65534,65534,65534,65534,65534,0,0,0,0,0,0,65534,65534,0,65534,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,65534,65534,0,0,0,0,0,0,0,65534,65534,65534,65534,0,0,0,0,0,65534,4,65534,0,0,0,0,0,0,65534,65534,65534,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,65534,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};
	ushort?[] hps = new ushort?[567]{null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null};
	ushort?[] states = new ushort?[567]{null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null};

	public TreeMediumA(){
		this.code = 2; 

		this.sizeX = 7;
		this.sizeY = 9;
		this.sizeZ = 9;

        this.blockdata = new ushort[sizeX, sizeY, sizeZ];
        this.hpdata = new ushort?[sizeX, sizeY, sizeZ];
        this.statedata = new ushort?[sizeX, sizeY, sizeZ];
        this.meta = new VoxelMetadata(sizeX, sizeY, sizeZ);

		this.considerAir = false;
		this.type = FillType.FreeSpace;
		this.overwriteBlocks = new List<ushort>();

		Prepare(blocks, hps, states);
	}
}

/*
ADD TO THIS ENUM EVERY NEW STRUCTURE IMPLEMENTED
*/

public enum StructureCode{
	TestStruct,
	TreeSmallA,
	TreeMediumA
}