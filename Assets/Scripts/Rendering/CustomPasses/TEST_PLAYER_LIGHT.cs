using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TEST_PLAYER_LIGHT: MonoBehaviour{
	public PlayerPositionHandler player;
	private Vector4[] positions = new Vector4[32];


	void Update(){
		FillVoid(player.GetPlayerWorldPosition());
	}

	private void FillVoid(Vector3 pos){
		this.positions[0] = new Vector4(pos.x, pos.y, pos.z, 1);
		this.positions[1] = new Vector4(0,-1000,0,0);

		for(int i=2; i < 32; i++){
			this.positions[i] = new Vector4(0,0,0,0);
		}

		Shader.SetGlobalVectorArray("_AAAAA", this.positions);
	}
}