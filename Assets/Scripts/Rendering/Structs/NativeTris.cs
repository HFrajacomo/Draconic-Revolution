using UnityEngine;
using Unity.Collections;

public struct NativeTris {
	public NativeArray<int> tris;
	public NativeArray<int> specularTris;
	public NativeArray<int> liquidTris; 
	public NativeArray<int> leavesTris; 
	public NativeArray<int> iceTris; 
	public NativeArray<int> lavaTris;

	public NativeTris(NativeArray<int> t, NativeArray<int> spec, NativeArray<int> liquid, NativeArray<int> leav, NativeArray<int> ice, NativeArray<int> lava){
		this.tris = t;
		this.specularTris = spec;
		this.liquidTris = liquid;
		this.leavesTris = leav;
		this.iceTris = ice;
		this.lavaTris = lava;
	}

	public void Dispose(){
		this.tris.Dispose();
		this.specularTris.Dispose();
		this.liquidTris.Dispose();
		this.leavesTris.Dispose();
		this.iceTris.Dispose();
		this.lavaTris.Dispose();
	}
}