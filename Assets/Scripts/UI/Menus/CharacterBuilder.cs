using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class CharacterBuilder{
	private GameObject parent;
	private Dictionary<ModelType, GameObject> bodyParts;
	private GameObject armature;
	private Transform rootBone;
	private BoneRenderer boneRenderer;

	private static Dictionary<string, int> BONE_MAP;

	// Settings
	private static readonly int ROOT_BONE_INDEX = 0;
	private static readonly string ARMATURE_NAME = "Armature";
	private static readonly Vector3 POS_1 = new Vector3(15, 0, 100);
	private static readonly Vector3 ROT_1 = new Vector3(270, 180, 20);
	private static readonly Vector3 SCL_1 = new Vector3(25,25,25);

	private List<int> cachedTris = new List<int>();

	public CharacterBuilder(GameObject par, bool isMale=true){
		this.parent = par;
		this.bodyParts = new Dictionary<ModelType, GameObject>();
		this.armature = ModelHandler.GetArmature(isMale:isMale);
		this.armature.name = ARMATURE_NAME;

		this.armature.transform.SetParent(this.parent.transform);
		FixArmature();
		LoadRootBone();
	}

	public void Add(ModelType type, GameObject obj){
		if(this.bodyParts.ContainsKey(type)){
			GameObject.DestroyImmediate(this.bodyParts[type]);
		}

		obj.transform.SetParent(this.parent.transform);
		obj.transform.localScale = Vector3.one;
		obj.transform.eulerAngles = ROT_1;
		obj.transform.localPosition = POS_1;

		SkinnedMeshRenderer current = obj.GetComponent<SkinnedMeshRenderer>();

		if(BONE_MAP == null){
			SetBoneMap(current.bones);
		}

		Transform[] newBones = ModelHandler.GetArmatureBones(this.armature.transform, BONE_MAP);

		if(boneRenderer.transforms == null)
			boneRenderer.transforms = newBones;

		Mesh mesh = CopyMesh(current.sharedMesh, current);
		FixMaterialOrder(current);

		mesh.name = current.sharedMesh.name;
		current.sharedMesh = mesh;
		current.rootBone = newBones[ROOT_BONE_INDEX];
		current.bones = newBones;

		this.bodyParts[type] = obj;
	}

	private void SetBoneMap(Transform[] prefabBones){
		BONE_MAP = new Dictionary<string, int>();

		for(int i=0; i < prefabBones.Length; i++){
			BONE_MAP.Add(prefabBones[i].name, i);
		}

	}

	private void FixArmature(){
		this.armature.transform.localScale = SCL_1;
		this.armature.transform.eulerAngles = ROT_1;
		this.armature.transform.localPosition = POS_1;
		this.boneRenderer = this.armature.AddComponent<BoneRenderer>();
	}

	private void LoadRootBone(){
		this.rootBone = this.armature.transform.Find("Hips").transform;
	}

	private Mesh CopyMesh(Mesh mesh, SkinnedMeshRenderer rend){
        Mesh newMesh = new Mesh();

        newMesh.subMeshCount = mesh.subMeshCount;
        newMesh.vertices = mesh.vertices;
        newMesh.uv = mesh.uv;
        newMesh.normals = mesh.normals;
        newMesh.colors = mesh.colors;
        newMesh.tangents = mesh.tangents;

        newMesh.boneWeights = mesh.boneWeights;
        newMesh.bindposes = mesh.bindposes;

        FixMeshVertexGroups(mesh, newMesh, rend);
        /*
        for(int i=0; i < mesh.subMeshCount; i++){
	    	mesh.GetTriangles(this.cachedTris, i);
	    	newMesh.SetTriangles(this.cachedTris, i);
	    	this.cachedTris.Clear();
        }
        */

        return newMesh;
	}

	private void FixMaterialOrder(SkinnedMeshRenderer rend){
		Material[] materials = new Material[rend.materials.Length];

		for(int i=0; i < materials.Length; i++){
			materials[i] = FindMaterial(rend.materials, i);
		}

		rend.materials = materials;
	}

	private Material FindMaterial(Material[] mats, int index){
		if(index == 0){
			for(int i=0; i < mats.Length; i++){
				if(mats[i].name == "Skin (Instance)"){
					return mats[i];
				}
			}
		}
		else if(index == 1){
			for(int i=0; i < mats.Length; i++){
				if(mats[i].name == "Pcolor (Instance)"){
					return mats[i];
				}
			}
		}
		else if(index == 2){
			for(int i=0; i < mats.Length; i++){
				if(mats[i].name == "Scolor (Instance)"){
					return mats[i];
				}
			}
		}
		else if(index == 3){
			for(int i=0; i < mats.Length; i++){
				if(mats[i].name == "Tcolor (Instance)"){
					return mats[i];
				}
			}
		}
		return mats[0];
	}

	private void FixMeshVertexGroups(Mesh prefab, Mesh newMesh, SkinnedMeshRenderer rend){
		switch(prefab.subMeshCount){
			case 2:
				ConvertSubMesh(prefab, newMesh, GetPrefabMeshSubMesh(0, rend), 0);
				ConvertSubMesh(prefab, newMesh, GetPrefabMeshSubMesh(1, rend), 1);
				return;
			case 3:
				ConvertSubMesh(prefab, newMesh, GetPrefabMeshSubMesh(0, rend), 0);
				ConvertSubMesh(prefab, newMesh, GetPrefabMeshSubMesh(1, rend), 1);
				ConvertSubMesh(prefab, newMesh, GetPrefabMeshSubMesh(2, rend), 2);
				return;
			case 4:
				ConvertSubMesh(prefab, newMesh, GetPrefabMeshSubMesh(0, rend), 0);
				ConvertSubMesh(prefab, newMesh, GetPrefabMeshSubMesh(1, rend), 1);
				ConvertSubMesh(prefab, newMesh, GetPrefabMeshSubMesh(2, rend), 2);
				ConvertSubMesh(prefab, newMesh, GetPrefabMeshSubMesh(3, rend), 3);
				return;				
			default:
				return;		
		}
	}

	private int GetPrefabMeshSubMesh(int index, SkinnedMeshRenderer rend){
		Material[] mats = rend.materials;

		if(index == 0){
			return FindMaterialIndex(mats, "Skin (Instance)");
		}
		else if(index == 1){
			return FindMaterialIndex(mats, "Pcolor (Instance)");
		}
		else if(index == 2){
			return FindMaterialIndex(mats, "Scolor (Instance)");
		}
		else if(index == 3){
			return FindMaterialIndex(mats, "Tcolor (Instance)");
		}
		else{
			return 0;
		}
	}

	private int FindMaterialIndex(Material[] mats, string name){
		for(int i=0; i < mats.Length; i++){
			if(mats[i].name == name){
				return i;
			}
		}
		return -1;
	}

	private void ConvertSubMesh(Mesh p, Mesh n, int indexP, int indexN){
    	p.GetTriangles(this.cachedTris, indexP);
    	n.SetTriangles(this.cachedTris, indexN);
    	this.cachedTris.Clear();
	}
}