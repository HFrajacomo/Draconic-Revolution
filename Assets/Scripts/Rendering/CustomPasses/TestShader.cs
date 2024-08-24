using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TestShader: MonoBehaviour{
	private Vector4[] positions = new Vector4[32];

	void Start(){
		Debug.Log("SHADER SET");
		FillVoid();
		Shader.SetGlobalVectorArray("_AAAAA", this.positions);
	}

	private void FillVoid(){
		this.positions[0] = new Vector4(10,855,0,1);
		this.positions[1] = new Vector4(0,855,10,1);
		this.positions[2] = new Vector4(0,-1000,0,0);

		for(int i=3; i < 32; i++){
			this.positions[i] = new Vector4(0,0,0,0);
		}
	}
}