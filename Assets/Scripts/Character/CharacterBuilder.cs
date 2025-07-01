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
	private HairlinePlane hairline = new HairlinePlane(Vector3.zero, Vector3.zero, false);

	// Mesh Cache
	private List<Material> meshMat = new List<Material>();

	private List<Vector3> meshVert = new List<Vector3>();
	private List<Vector3> meshNormal = new List<Vector3>();
	private List<Vector4> meshTangent = new List<Vector4>();
	private List<Vector2> meshUV = new List<Vector2>();
	private List<List<int>> meshTris = new List<List<int>>();
	private List<BoneWeight> meshBoneWeights = new List<BoneWeight>();
	private List<ShapeKeyDeltaData> meshSKData = new List<ShapeKeyDeltaData>();


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
	private static Material eyeMaterial;

	// Settings
	private static readonly int ROOT_BONE_INDEX = 0;
	private static readonly string ARMATURE_NAME_MALE = "Armature-Man";
	private static readonly string ARMATURE_NAME_FEMALE = "Armature-Woman";
	private static readonly Vector3 POS_1 = new Vector3(0f, -4.15f, 0f);
	private static readonly Quaternion ROT_1 = Quaternion.Euler(new Vector3(270, 0, 0));
	private static readonly Vector3 SCL_1 = new Vector3(100f, 100f, 100f);
	private static readonly string EMPTY_OBJECT_PATHNAME = "----- PrefabModels -----/EmptyObject";


	public CharacterBuilder(GameObject par, RuntimeAnimatorController animations, CharacterAppearance app, Material clothing, Material dragonskin, Material eye, bool isMale, bool isPlayerCharacter){
		if(EMPTY_OBJECT_PREFAB == null)
			EMPTY_OBJECT_PREFAB = GameObject.Find(EMPTY_OBJECT_PATHNAME);

		this.parent = par;
		this.isMale = isMale;
		this.appearance = app;
		this.animator = par.GetComponent<Animator>();
		this.animator.runtimeAnimatorController = animations;
		this.armature = ModelHandler.GetArmature(isMale:isMale, rotated:true);
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
		eyeMaterial = eye;

		FixArmature();
	}

	// Whenever clothes are being changed
	public void ChangeAppearanceAndBuild(CharacterAppearance app){
		this.appearance = app;
		Build();
	}

	/**
	 * Builds the character model from CharacterAppearance by doing Combined Skinned Meshing (Mesh Stitching)
	 */
	public void Build(){
		SkinnedMeshRenderer modelRenderer;
		Mesh combinedMesh = new Mesh();
		combinedMesh.name = "PlayerMesh";
		char hatCover = ModelHandler.GetHatCover(ModelType.HEADGEAR, this.appearance.hat.GetFullName(ModelType.HEADGEAR));

		// Hat
		modelRenderer = ModelHandler.GetModelByCode(ModelType.HEADGEAR, this.appearance.hat.code).GetComponent<SkinnedMeshRenderer>();
        SetBoneMap(modelRenderer.bones);
        combinedMesh.bindposes = modelRenderer.sharedMesh.bindposes;
		SetHairline(modelRenderer.sharedMesh);
		SaveShapeKeys(modelRenderer.sharedMesh);
        AddGeometryToMesh(modelRenderer.sharedMesh, modelRenderer, this.appearance, ModelType.HEADGEAR);

        GameObject.Destroy(modelRenderer.gameObject);

        // Hair
        if(hatCover == 'N'){
	        modelRenderer = ModelHandler.GetModelByCode(ModelType.HAIR, this.appearance.hair.code).GetComponent<SkinnedMeshRenderer>();
	        SaveShapeKeys(modelRenderer.sharedMesh);
	        AddGeometryToMesh(modelRenderer.sharedMesh, modelRenderer, this.appearance, ModelType.HAIR);
	        GameObject.Destroy(modelRenderer.gameObject);
	    }

		// Torso
		modelRenderer = ModelHandler.GetModelByCode(ModelType.CLOTHES, this.appearance.torso.code).GetComponent<SkinnedMeshRenderer>();
        SaveShapeKeys(modelRenderer.sharedMesh);
        AddGeometryToMesh(modelRenderer.sharedMesh, modelRenderer, this.appearance, ModelType.CLOTHES);
        GameObject.Destroy(modelRenderer.gameObject);

		// Legs
		modelRenderer = ModelHandler.GetModelByCode(ModelType.LEGS, this.appearance.legs.code).GetComponent<SkinnedMeshRenderer>();
        SaveShapeKeys(modelRenderer.sharedMesh);
        AddGeometryToMesh(modelRenderer.sharedMesh, modelRenderer, this.appearance, ModelType.LEGS);
        GameObject.Destroy(modelRenderer.gameObject);

		// Boots
		modelRenderer = ModelHandler.GetModelByCode(ModelType.FOOTGEAR, this.appearance.boots.code).GetComponent<SkinnedMeshRenderer>();
        SaveShapeKeys(modelRenderer.sharedMesh);
        AddGeometryToMesh(modelRenderer.sharedMesh, modelRenderer, this.appearance, ModelType.FOOTGEAR);
        GameObject.Destroy(modelRenderer.gameObject);

        // Addon
		modelRenderer = ModelHandler.GetModelObject(ModelType.ADDON, GetAddonName(this.appearance.race)).GetComponent<SkinnedMeshRenderer>();
        SaveShapeKeys(modelRenderer.sharedMesh);
        AddGeometryToMesh(modelRenderer.sharedMesh, modelRenderer, this.appearance, ModelType.FOOTGEAR);
        GameObject.Destroy(modelRenderer.gameObject);

        // Face
		modelRenderer = ModelHandler.GetModelByCode(ModelType.FACE, this.appearance.boots.code).GetComponent<SkinnedMeshRenderer>();
        SaveShapeKeys(modelRenderer.sharedMesh);
        AddGeometryToMesh(modelRenderer.sharedMesh, modelRenderer, this.appearance, ModelType.FACE);
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
		this.renderer.gameObject.AddComponent<ShapeKeyAnimator>();

		this.meshMat.Clear();
		this.animator.Rebind();
	}

	// Copy ShapeKeys data from a given mesh and saves to cache
	private void SaveShapeKeys(Mesh mesh){
        for (int i = 0; i < mesh.blendShapeCount; i++){
            string shapeName = mesh.GetBlendShapeName(i);
            float weight = mesh.GetBlendShapeFrameWeight(i, 0);

            this.meshSKData.Add(new ShapeKeyDeltaData(shapeName, i, weight, mesh, this.meshVert.Count));
        }
	}

	// Loads the ShapeKeys into the combined mesh
	private void ApplyShapeKeys(Mesh mesh){
		ShapeKeyDeltaData.CopyBlendShapes(this.meshSKData, mesh);
	}

	private void AddGeometryToMesh(Mesh newMesh, SkinnedMeshRenderer rend, CharacterAppearance app, ModelType type){
		int vertsCount = this.meshVert.Count;
		int submeshCount = newMesh.subMeshCount;

		newMesh.GetVertices(this.cachedVerts);

		// Hat-hair hiding
		if(type == ModelType.HAIR)
			ProcessHairMesh(this.cachedVerts);

		this.meshVert.AddRange(this.cachedVerts);
		newMesh.GetUVs(0, this.cachedUV);
		this.meshUV.AddRange(this.cachedUV);
		newMesh.GetNormals(this.cachedNormal);
		this.meshNormal.AddRange(this.cachedNormal);
		newMesh.GetTangents(this.cachedTangent);
		this.meshTangent.AddRange(this.cachedTangent);
		newMesh.GetBoneWeights(this.cachedBW);
		this.meshBoneWeights.AddRange(this.cachedBW);

		// Removes hairline submesh
		if(type == ModelType.HEADGEAR){
			RemoveLastFour(this.meshVert);
			RemoveLastFour(this.meshUV);
			RemoveLastFour(this.meshNormal);
			RemoveLastFour(this.meshTangent);
			RemoveTill(this.meshBoneWeights, this.meshVert.Count);
			submeshCount--;
		}

		for(int i=0; i < submeshCount; i++){
			this.meshTris.Add(new List<int>());
			newMesh.GetTriangles(this.meshTris[this.meshTris.Count-1], i);
			ReassignTriangles(this.meshTris[this.meshTris.Count-1], vertsCount);

			this.meshMat.Add(SetMaterial(type, app.GetInfo(type), app.skinColor, app.race, i));
		}
	}

	private void ProcessHairMesh(List<Vector3> hairVerts){
		if(!this.hairline.valid)
			return;

		for(int i=0; i < hairVerts.Count; i++){
			if(!this.hairline.GetSide(hairVerts[i])){
				continue;
			}

			hairVerts[i] = this.hairline.GetClosestPoint(hairVerts[i]);

		}
	}

	// Removes the last 4 elements from a list. Used for removing data from hairplane submesh
	private void RemoveLastFour<T>(List<T> lista){
		if(lista.Count == 0)
			return;

		for(int i=0; i < 4; i++){
			lista.RemoveAt(lista.Count-1);
		}
	}

	// Removes the last elements until List is of size x
	private void RemoveTill<T>(List<T> lista, int x){
		if(lista.Count == 0 || x < 0)
			return;

		if(lista.Count <= x)
			return;

		for(;lista.Count > x;){
			lista.RemoveAt(x);
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

		ApplyShapeKeys(mesh);

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
		this.meshSKData.Clear();
	}

	private void SetHairline(Mesh hatMesh){
		List<Vector3> planeVerts = GetVerticesForSubmesh(hatMesh, hatMesh.subMeshCount-1);
		Vector3 normal = GetFirstNormalInSubmesh(hatMesh, hatMesh.subMeshCount-1);

		if(planeVerts.Count < 4)
			return;
		
		this.hairline = new HairlinePlane(planeVerts[0], planeVerts[1], planeVerts[2], planeVerts[3], normal);
	}

    private List<Vector3> GetVerticesForSubmesh(Mesh mesh, int submeshIndex){
        // Get all vertex indices for the specified submesh
        int[] submeshIndices = mesh.GetTriangles(submeshIndex);

        // Get all vertices in the mesh
        Vector3[] allVertices = mesh.vertices;

        // Use a hash set to collect unique vertices
        HashSet<Vector3> submeshVertices = new HashSet<Vector3>();

        // Add vertices used by the submesh
        foreach (int index in submeshIndices)
        {
            submeshVertices.Add(allVertices[index]);
        }

        // Return as a list
        return new List<Vector3>(submeshVertices);
    }

    private Vector3 GetFirstNormalInSubmesh(Mesh mesh, int submeshIndex){
    	int normalIndice = mesh.GetTriangles(submeshIndex)[0];
    	return mesh.normals[normalIndice];
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

	private void FixArmature(){
		this.armature.transform.localRotation = ROT_1;
		this.armature.transform.localPosition = POS_1;
		this.armature.transform.localScale = SCL_1;
		this.boneRenderer = this.armature.AddComponent<BoneRenderer>();

		LoadRootBone();
	}

	private void LoadRootBone(){
		this.rootBone = this.armature.transform.Find("Hips").transform;
	}

	private Material SetMaterial(ModelType type, ClothingInfo info, Color skin, Race r, int index){
		Material newMaterial;

		// Skin
		if(index == 0){
			if(r == Race.DRAGONLING)
				newMaterial = Material.Instantiate(dragonSkinMaterial);
			else
				newMaterial = Material.Instantiate(plainClothingMaterial);

			newMaterial.SetColor("_Color", skin);
			return newMaterial;
		}
		else if(index == 1){
			if(type == ModelType.FACE){
				newMaterial = Material.Instantiate(eyeMaterial);
				newMaterial.SetColor("_Color", info.primary);
				newMaterial.SetColor("_IrisColor", info.secondary);
			}
			else{
				newMaterial = Material.Instantiate(plainClothingMaterial);
				newMaterial.SetColor("_Color", info.primary);
			}

			return newMaterial;
		}
		else if(index == 2){
			newMaterial = Material.Instantiate(plainClothingMaterial);

			if(type != ModelType.FACE)
				newMaterial.SetColor("_Color", info.secondary);
			else
				newMaterial.SetColor("_Color", info.terciary);

			return newMaterial;
		}
		else if(index == 3){
			newMaterial = Material.Instantiate(plainClothingMaterial);

			if(type != ModelType.FACE)
				newMaterial.SetColor("_Color", info.terciary);
			else
				newMaterial.SetColor("_Color", Color.white);

			return newMaterial;
		}

		return Material.Instantiate(plainClothingMaterial);
	}

	private string GetAddonName(Race r){
		switch(r){
			case Race.HUMAN:
				if(this.isMale)
					return "Base_Ears/M";
				else
					return "Base_Ears/W";
			case Race.ELF:
				if(this.isMale)
					return "Elven_Ears/M";
				else
					return "Elven_Ears/W";
			case Race.DWARF:
				if(this.isMale)
					return "Base_Ears/M";
				else
					return "Base_Ears/W";
			case Race.ORC:
				if(this.isMale)
					return "Orcish_Ears/M";
				else
					return "Orcish_Ears/W";
			case Race.DRAGONLING:
				if(this.isMale)
					return "Dragonling_Horns/M";
				else
					return "Dragonling_Horns/W";
			case Race.HALFLING:
				if(this.isMale)
					return "Base_Ears/M";
				else
					return "Base_Ears/W";
			default:
				if(this.isMale)
					return "Base_Ears/M";
				else
					return "Base_Ears/W";
		}
	}
}
