using System.Collections.Generic;
using UnityEngine;

public class CharacterBuilder{
	private GameObject parent;
	private Dictionary<ModelType, GameObject> bodyParts;
	private GameObject armature;

	// Settings
	private static readonly Vector3 POS_1 = new Vector3(60, 0, 300);
	private static readonly Vector3 ROT_1 = new Vector3(-95, 0, 15);

	public CharacterBuilder(GameObject par, bool isMale=true){
		this.parent = par;
		this.bodyParts = new Dictionary<ModelType, GameObject>();
		this.armature = ModelHandler.GetArmature(isMale:isMale);

		this.armature.transform.SetParent(this.parent.transform);
		FixArmature();
	}

	public void Add(ModelType type, GameObject obj){
		if(this.bodyParts.ContainsKey(type)){
			GameObject.DestroyImmediate(this.bodyParts[type]);
		}

		obj.transform.SetParent(this.parent.transform);
		obj.transform.localScale = Vector3.one;
		obj.transform.eulerAngles = ROT_1;
		obj.transform.localPosition = POS_1;

		this.bodyParts[type] = obj;
	}

	private void FixArmature(){
		this.armature.transform.localScale = Vector3.one;
		this.armature.transform.eulerAngles = ROT_1;
		this.armature.transform.localPosition = POS_1;		
	}
}