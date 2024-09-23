using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterBuilder{
	private GameObject parent;
	private SkinnedMeshRenderer renderer;
	private Animator animator;
	private GameObject armature;
	private GameObject modelRoot;
	private Transform rootBone;
	private BoneRenderer boneRenderer;
	private CharacterAppearance appearance;
	private bool isMale;

	// Mesh Cache
	private List<Material> meshMat = new List<Material>();

	private List<Vector3> meshVert = new List<Vector3>();
	private List<Vector3> meshNormal = new List<Vector3>();
	private List<Vector4> meshTangent = new List<Vector4>();
	private List<Vector2> meshUV = new List<Vector2>();
	private List<List<int>> meshTris = new List<List<int>>();
	private List<BoneWeight> meshBoneWeights = new List<BoneWeight>();


	private List<Vector3> cachedVerts = new List<Vector3>();
	private List<Vector2> cachedUV = new List<Vector2>();
	private List<Vector3> cachedNormal = new List<Vector3>();
	private List<Vector4> cachedTangent = new List<Vector4>();
	private List<BoneWeight> cachedBW = new List<BoneWeight>();

	// Statics
	private static Dictionary<string, int> BONE_MAP;
	private static GameObject EMPTY_OBJECT_PREFAB;

	// Materials
	private static Material plainClothingMaterial;
	private static Material dragonSkinMaterial;

	// Settings
	private static readonly int ROOT_BONE_INDEX = 0;
	private static readonly string ARMATURE_NAME_MALE = "Armature";
	private static readonly string ARMATURE_NAME_FEMALE = "Armature-Woman";
	private static readonly Vector3 POS_1 = Vector3.zero;
	private static readonly Quaternion ROT_1 = Quaternion.Euler(new Vector3(270, 180, 180));
	private static readonly string EMPTY_OBJECT_PATHNAME = "----- PrefabModels -----/EmptyObject";


	public CharacterBuilder(GameObject par, RuntimeAnimatorController animations, CharacterAppearance app, Material clothing, Material dragonskin, bool isMale, bool isPlayerCharacter){
		if(EMPTY_OBJECT_PREFAB == null)
			EMPTY_OBJECT_PREFAB = GameObject.Find(EMPTY_OBJECT_PATHNAME);

		this.parent = par;
		this.isMale = isMale;
		this.appearance = app;
		this.animator = par.GetComponent<Animator>();
		this.animator.runtimeAnimatorController = animations;
		this.armature = ModelHandler.GetArmature(isMale:isMale);
		this.armature.transform.SetParent(this.parent.transform);

		this.modelRoot = GameObject.Instantiate(EMPTY_OBJECT_PREFAB);
		this.modelRoot.transform.parent = this.parent.transform;
		this.modelRoot.name = "CharacterModel";

		if(isPlayerCharacter){
			this.modelRoot.layer = 9;
		}

		this.renderer = this.modelRoot.AddComponent<SkinnedMeshRenderer>();

		if(isMale)
			this.armature.name = ARMATURE_NAME_MALE;
		else
			this.armature.name = ARMATURE_NAME_FEMALE;

		plainClothingMaterial = clothing;
		dragonSkinMaterial = dragonskin;

		FixArmature(isMale);
	}

	// Whenever clothes are being changed
	public void ChangeAppearaceAndBuild(CharacterAppearance app){
		this.appearance = app;
		Build();
	}

	/**
	 * Builds the character model from CharacterAppearance by doing Combined Skinned Meshing (Mesh Stitching)
	 */
	public void Build(){
		SkinnedMeshRenderer modelRenderer;
		Mesh combinedMesh = new Mesh();
		combinedMesh.name = "CombinedMesh";

		// Hat
		modelRenderer = ModelHandler.GetModelByCode(ModelType.HEADGEAR, this.appearance.hat.code).GetComponent<SkinnedMeshRenderer>();
        SetBoneMap(modelRenderer.bones);
        combinedMesh.bindposes = modelRenderer.sharedMesh.bindposes;
        AddGeometryToMesh(combinedMesh, modelRenderer.sharedMesh, modelRenderer, this.appearance, ModelType.HEADGEAR);

        GameObject.Destroy(modelRenderer.gameObject);

		// Torso
		modelRenderer = ModelHandler.GetModelByCode(ModelType.CLOTHES, this.appearance.torso.code).GetComponent<SkinnedMeshRenderer>();
        AddGeometryToMesh(combinedMesh, modelRenderer.sharedMesh, modelRenderer, this.appearance, ModelType.CLOTHES);
        GameObject.Destroy(modelRenderer.gameObject);


		// Legs
		modelRenderer = ModelHandler.GetModelByCode(ModelType.LEGS, this.appearance.legs.code).GetComponent<SkinnedMeshRenderer>();
        AddGeometryToMesh(combinedMesh, modelRenderer.sharedMesh, modelRenderer, this.appearance, ModelType.LEGS);
        GameObject.Destroy(modelRenderer.gameObject);

		// Boots
		modelRenderer = ModelHandler.GetModelByCode(ModelType.FOOTGEAR, this.appearance.boots.code).GetComponent<SkinnedMeshRenderer>();
        AddGeometryToMesh(combinedMesh, modelRenderer.sharedMesh, modelRenderer, this.appearance, ModelType.FOOTGEAR);
        GameObject.Destroy(modelRenderer.gameObject);

        // Face
		modelRenderer = ModelHandler.GetModelByCode(ModelType.FACE, this.appearance.face.code).GetComponent<SkinnedMeshRenderer>();
        AddGeometryToMesh(combinedMesh, modelRenderer.sharedMesh, modelRenderer, this.appearance, ModelType.FACE);
        GameObject.Destroy(modelRenderer.gameObject);

		Transform[] newBones = ModelHandler.GetArmatureBones(this.armature.transform, BONE_MAP);
		#if UNITY_EDITOR
			if(boneRenderer.transforms == null)
				boneRenderer.transforms = newBones;
		#endif

		BuildMesh(combinedMesh);

		this.renderer.sharedMesh = combinedMesh;
		this.renderer.rootBone = newBones[ROOT_BONE_INDEX];
		this.renderer.bones = newBones;
		this.renderer.materials = this.meshMat.ToArray();

		this.meshMat.Clear();
		this.animator.Rebind();
	}

	private void AddGeometryToMesh(Mesh main, Mesh newMesh, SkinnedMeshRenderer rend, CharacterAppearance app, ModelType type){
		int vertsCount = this.meshVert.Count;

		newMesh.GetVertices(this.cachedVerts);
		this.meshVert.AddRange(this.cachedVerts);
		newMesh.GetUVs(0, this.cachedUV);
		this.meshUV.AddRange(this.cachedUV);
		newMesh.GetNormals(this.cachedNormal);
		this.meshNormal.AddRange(this.cachedNormal);
		newMesh.GetTangents(this.cachedTangent);
		this.meshTangent.AddRange(this.cachedTangent);
		newMesh.GetBoneWeights(this.cachedBW);
		this.meshBoneWeights.AddRange(this.cachedBW);

		for(int i=0; i < newMesh.subMeshCount; i++){
			this.meshTris.Add(new List<int>());
			newMesh.GetTriangles(this.meshTris[this.meshTris.Count-1], i);
			ReassignTriangles(this.meshTris[this.meshTris.Count-1], vertsCount);

			this.meshMat.Add(FixMaterial(rend.materials[i], app.GetInfo(type), app.skinColor, app.race));
		}
	}

	private void BuildMesh(Mesh mesh){
		mesh.SetVertices(this.meshVert);
		mesh.SetUVs(0, this.meshUV);
		mesh.SetNormals(this.meshNormal);
		mesh.SetTangents(this.meshTangent);

		mesh.boneWeights = this.meshBoneWeights.ToArray();

		mesh.subMeshCount = this.meshTris.Count;

		for(int i=0; i < this.meshTris.Count; i++){
			mesh.SetTriangles(this.meshTris[i], i);
		}


		this.meshVert.Clear();
		this.meshUV.Clear();
		this.meshNormal.Clear();
		this.meshTangent.Clear();
		this.cachedVerts.Clear();
		this.cachedUV.Clear();
		this.cachedNormal.Clear();
		this.cachedTangent.Clear();
		this.meshTris.Clear();
		this.meshBoneWeights.Clear();
		this.cachedBW.Clear();
	}

	private void ReassignTriangles(List<int> tris, int vertsCount){
		for(int i=0; i < tris.Count; i++){
			tris[i] += vertsCount;
		}
	}

	private void SetBoneMap(Transform[] prefabBones){
		if(BONE_MAP == null){
			BONE_MAP = new Dictionary<string, int>();

			for(int i=0; i < prefabBones.Length; i++){
				BONE_MAP.Add(prefabBones[i].name, i);
			}
		}
	}

	private void FixArmature(bool isMale){
		/* DEBUG
		if(isMale)
			this.armature.transform.localScale = Multiply(this.armature.transform.localScale, SCL_1);
		else
			this.armature.transform.localScale = Multiply(this.armature.transform.localScale, SCL_2);
		*/


		this.armature.transform.eulerAngles = ROT_1.eulerAngles;
		this.armature.transform.localPosition = POS_1;
		this.boneRenderer = this.armature.AddComponent<BoneRenderer>();

		LoadRootBone();
	}

	private void LoadRootBone(){
		this.rootBone = this.armature.transform.Find("Pelvis").transform;
	}

	private Material FixMaterial(Material mat, ClothingInfo info, Color skin, Race r){
		Material newMaterial;

		if(mat.name == "Skin (Instance)"){
			if(r == Race.DRAGONLING)
				newMaterial = Material.Instantiate(dragonSkinMaterial);
			else
				newMaterial = Material.Instantiate(plainClothingMaterial);

			newMaterial.SetColor("_Color", skin);
			return newMaterial;
		}
		else if(mat.name == "Pcolor (Instance)"){
			newMaterial = Material.Instantiate(plainClothingMaterial);
			newMaterial.SetColor("_Color", info.primary);
			return newMaterial;
		}
		else if(mat.name == "Scolor (Instance)"){
			newMaterial = Material.Instantiate(plainClothingMaterial);
			newMaterial.SetColor("_Color", info.secondary);
			return newMaterial;
		}
		else if(mat.name == "Tcolor (Instance)"){
			newMaterial = Material.Instantiate(plainClothingMaterial);
			newMaterial.SetColor("_Color", info.terciary);
			return newMaterial;
		}

		return Material.Instantiate(plainClothingMaterial);
	}

    private Vector3 Multiply(Vector3 vec1, Vector3 vec2){
        Vector3 result = new Vector3();
        result.x = vec1.x * vec2.x;
        result.y = vec1.y * vec2.y;
        result.z = vec1.z * vec2.z;
        return result;
    }
}
