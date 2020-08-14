using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelData
{
	int[,,] data;

	public VoxelData(int[,,] data){
		this.data = (int[,,])data.Clone();
	}

	public static VoxelData CutUnderground(VoxelData a, VoxelData b, int upper=-1, int lower=-1){
		int[,,] vd = new int[a.GetWidth(), a.GetHeight(), a.GetDepth()];

		if(upper == -1)
			upper = a.GetHeight();
		if(lower == -1)
			lower = 0;

		for(int x=0;x<a.GetWidth();x++){
			for(int y=lower;y<upper;y++){
				for(int z=0;z<a.GetDepth();z++){
					if(a.GetCell(x,y,z) == 1 && b.GetCell(x,y,z) == 0)
						vd[x,y,z] = 1;
					else
						vd[x,y,z] = 0;
				}
			} 
		}
		return new VoxelData(vd);
	}


	public int GetWidth(){
		return data.GetLength(0);
	}

	public int GetHeight(){
		return data.GetLength(1);
	}

	public int GetDepth(){
		return data.GetLength(2);
	}

	public int GetCell(int x, int y, int z){
		return data[x,y,z];
	}

	public void SetCell(int x, int y, int z, int blockCode){
		data[x,y,z] = blockCode;
	}

	public override string ToString(){
		string str = "";
		foreach(var item in data){
			str += item.ToString();
		}

		return base.ToString() + " -> " + str;
	}

	public int GetNeighbor(int x, int y, int z, Direction dir){
		DataCoordinate offsetToCheck = offsets[(int)dir];
		DataCoordinate neighborCoord = new DataCoordinate(x + offsetToCheck.x, y + offsetToCheck.y, z + offsetToCheck.z);
		
		if(neighborCoord.x < 0 || neighborCoord.x >= GetWidth() || neighborCoord.z < 0 || neighborCoord.z >= GetDepth() || neighborCoord.y < 0 || neighborCoord.y >= GetHeight()){
			return 0;
		} 
		else{
			return GetCell(neighborCoord.x, neighborCoord.y, neighborCoord.z);
		}
		

	}

	struct DataCoordinate{
		public int x;
		public int y;
		public int z;

		public DataCoordinate(int x, int y, int z){
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}

	DataCoordinate[] offsets = {
		new DataCoordinate(0,0,1),
		new DataCoordinate(1,0,0),
		new DataCoordinate(0,0,-1),
		new DataCoordinate(-1,0,0),
		new DataCoordinate(0,1,0),
		new DataCoordinate(0,-1,0)
	};
}

public enum Direction{
	North,
	East,
	South,
	West,
	Up,
	Down
}