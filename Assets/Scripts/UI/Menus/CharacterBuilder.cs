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

		Mesh mesh = CopyMesh(current.sharedMesh);

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

	private Mesh CopyMesh(Mesh mesh){
        Mesh newMesh = new Mesh();
        List<int> tris = new List<int>();

        newMesh.subMeshCount = mesh.subMeshCount;
        newMesh.vertices = mesh.vertices;
        newMesh.uv = mesh.uv;
        newMesh.normals = mesh.normals;
        newMesh.colors = mesh.colors;
        newMesh.tangents = mesh.tangents;

        newMesh.boneWeights = mesh.boneWeights;
        newMesh.bindposes = mesh.bindposes;

        // Triangles to Submeshes
        for(int i=0; i < mesh.subMeshCount; i++){
        	mesh.GetTriangles(tris, i);
        	newMesh.SetTriangles(tris, i);
        	tris.Clear();
        }

        return newMesh;
	}
}