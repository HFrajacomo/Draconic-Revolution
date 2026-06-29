using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class CharacterBuilder{
	private GameObject parent;
	private GameObject cam;
	private GameObject thirdPersonRig;
	private GameObject firstPersonRig;
	private GameObject tpAnimGO;
	private GameObject fpAnimGO;
	private SkinnedMeshRenderer tpRenderer;
	private SkinnedMeshRenderer fpRenderer;
	private Animator tpAnimator;
	private Animator fpAnimator;
	private GameObject tpArmature;
	private GameObject fpArmature;

	private GameObject tpModelRoot;
	private GameObject fpModelRoot;

	private BoneRenderer boneRenderer;
	private CharacterAppearance appearance;
	private FaceExpressionAnimator faceAnimator;
	private bool isMale;
	private bool isPlayer;
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
	private static Material faceMaterial;
	private static Material dragonHornMaterial;

	// Settings
	private static readonly int ROOT_BONE_INDEX = 0;
	private static readonly Vector3 POS_1 = new Vector3(0f, 0f, 0f);
	private static readonly Quaternion ROT_1 = Quaternion.Euler(new Vector3(270, 0, 0));
	private static readonly Vector3 SCL_1 = new Vector3(100f, 100f, 100f);
	private static readonly string EMPTY_OBJECT_PATHNAME = "----- PrefabModels -----/EmptyObject";


	public CharacterBuilder(GameObject par, RuntimeAnimatorController tpAnimations, RuntimeAnimatorController fpAnimations, CharacterAppearance app, Material clothing, Material dragonhorn, Material dragonskin, Material face, bool isMale, bool isPlayerCharacter){
		if(EMPTY_OBJECT_PREFAB == null)
			EMPTY_OBJECT_PREFAB = GameObject.Find(EMPTY_OBJECT_PATHNAME);

		this.parent = par;
		this.cam = this.parent.transform.Find("Camera").gameObject;
		this.isMale = isMale;
		this.isPlayer = isPlayerCharacter;
		this.appearance = app;

		this.thirdPersonRig = SetupNewGO("TP-Rig", this.parent.transform);
		this.tpAnimGO = SetupNewGO("Animator", this.thirdPersonRig.transform);

		this.tpArmature = ModelHandler.GetArmature(rotated:true);
		this.tpArmature.transform.SetParent(this.tpAnimGO.transform);

		this.tpModelRoot = GameObject.Instantiate(EMPTY_OBJECT_PREFAB);
		this.tpModelRoot.transform.SetParent(this.tpAnimGO.transform);
		this.tpModelRoot.name = "Model";

		if(this.isPlayer)
			this.tpModelRoot.layer = 9;

		this.tpRenderer = this.tpModelRoot.AddComponent<SkinnedMeshRenderer>();

		plainClothingMaterial = clothing;
		dragonSkinMaterial = dragonskin;
		faceMaterial = face;
		dragonHornMaterial = dragonhorn;

		if(this.isPlayer){
			this.firstPersonRig = SetupNewGO("FP-Rig", this.cam.transform);
			this.firstPersonRig.transform.localPosition = new Vector3(0, -3.8f, 0);
			this.fpAnimGO = SetupNewGO("Animator", this.firstPersonRig.transform);
			this.fpModelRoot = SetupNewGO("Model", this.fpAnimGO.transform);

			this.fpRenderer = this.fpModelRoot.AddComponent<SkinnedMeshRenderer>();
			
			this.fpArmature = ModelHandler.GetArmature(rotated:true);
			this.fpArmature.transform.SetParent(this.fpAnimGO.transform);

			this.firstPersonRig.layer = 12;
			this.fpModelRoot.layer = 12;
			this.fpArmature.layer = 12;

			this.fpAnimator = this.fpAnimGO.AddComponent<Animator>();
			this.fpAnimator.runtimeAnimatorController = fpAnimations;
		}

		this.tpAnimator = this.tpAnimGO.AddComponent<Animator>();
		this.tpAnimator.runtimeAnimatorController = tpAnimations;

		FixArmature();
		this.faceAnimator = this.tpModelRoot.AddComponent<FaceExpressionAnimator>();
	}

	// Whenever clothes are being changed
	public void ChangeAppearanceAndBuild(CharacterAppearance app){
		this.appearance = app;
		Build();
	}

	public GameObject GetThirdPersonAnimatorObject(){return this.tpAnimGO;}
	public GameObject GetThirdPersonModelObject(){return this.tpModelRoot;}

	public void StartAnimation(){
		this.faceAnimator.Play(0);
	}

	/**
	 * Builds the character model from CharacterAppearance by doing Combined Skinned Meshing (Mesh Stitching)
	 */
	public void Build(){
        List<Material> mats = new List<Material>();

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
        AddGeometryToMesh(modelRenderer.sharedMesh, modelRenderer, this.appearance, ModelType.ADDON);
        GameObject.Destroy(modelRenderer.gameObject);

        // Face
		modelRenderer = ModelHandler.GetModelByCode(ModelType.FACE, this.appearance.face.code).GetComponent<SkinnedMeshRenderer>();
        SaveShapeKeys(modelRenderer.sharedMesh);
        AddGeometryToMesh(modelRenderer.sharedMesh, modelRenderer, this.appearance, ModelType.FACE);
        GameObject.Destroy(modelRenderer.gameObject);

        // Head
        modelRenderer = ModelHandler.GetModelObject(ModelType.ESSENTIAL, GetEssentialName(this.isMale)).GetComponent<SkinnedMeshRenderer>();
        SaveShapeKeys(modelRenderer.sharedMesh);
        AddGeometryToMesh(modelRenderer.sharedMesh, modelRenderer, this.appearance, ModelType.ESSENTIAL);
        GameObject.Destroy(modelRenderer.gameObject);

		Transform[] newBonesTP = ModelHandler.GetArmatureBones(this.tpArmature.transform, BONE_MAP);
		#if UNITY_EDITOR
			if(boneRenderer.transforms == null)
				boneRenderer.transforms = newBonesTP;
		#endif

        // First Person Model
        if(this.isPlayer){
    		Transform[] newBonesFP = ModelHandler.GetArmatureBones(this.fpArmature.transform, BONE_MAP);
    		Bounds bounds = this.fpRenderer.localBounds;
    		bounds.Expand(Vector3.one);

        	modelRenderer = ModelHandler.GetModelByCode(ModelType.CLOTHES, this.appearance.torso.code).GetComponent<SkinnedMeshRenderer>();
        	this.fpRenderer.sharedMesh = modelRenderer.sharedMesh;
        	GameObject.Destroy(modelRenderer.gameObject);
        	this.fpRenderer.rootBone = newBonesFP[ROOT_BONE_INDEX];
        	this.fpRenderer.bones = newBonesFP;

			for(int i=0; i < this.fpRenderer.sharedMesh.subMeshCount; i++){
				mats.Add(SetMaterial(ModelType.CLOTHES, this.appearance.GetInfo(ModelType.CLOTHES), this.appearance.skinColor, this.appearance.race, i));
			}

        	this.fpRenderer.materials = mats.ToArray();
        	this.fpRenderer.shadowCastingMode = ShadowCastingMode.Off;
        	this.fpRenderer.localBounds = bounds;
    	}

		BuildMesh(combinedMesh);

		this.tpRenderer.sharedMesh = combinedMesh;
		this.tpRenderer.rootBone = newBonesTP[ROOT_BONE_INDEX];
		this.tpRenderer.bones = newBonesTP;
		this.tpRenderer.materials = this.meshMat.ToArray();
		this.tpRenderer.gameObject.AddComponent<ShapeKeyAnimator>();

		this.meshMat.Clear();
	}

	private GameObject SetupNewGO(string name, Transform parent){
		GameObject go = new GameObject();
		go.transform.SetParent(parent);
		go.name = name;
		go.transform.localPosition = Vector3.zero;
		go.transform.localRotation = Quaternion.identity;
		go.transform.localScale = Vector3.one;

		return go;
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
		this.tpArmature.transform.localRotation = ROT_1;
		this.tpArmature.transform.localPosition = POS_1;
		this.tpArmature.transform.localScale = SCL_1;
		this.boneRenderer = this.tpArmature.AddComponent<BoneRenderer>();

		if(this.isPlayer){
			this.fpArmature.transform.localRotation = ROT_1;
			this.fpArmature.transform.localPosition = POS_1;
			this.fpArmature.transform.localScale = SCL_1;
		}
	}

	private Material SetMaterial(ModelType type, ClothingInfo info, Color skin, Race r, int index){
		Material newMaterial;

		// Skin
		if(index == 0){
			if(r == Race.DRAGONLING && type == ModelType.ADDON){
				newMaterial = Material.Instantiate(dragonHornMaterial);
				return newMaterial;
			}
			else if(type == ModelType.FACE){
				newMaterial = Material.Instantiate(faceMaterial);
		        newMaterial.SetTexture("_FaceTexture", ModelHandler.GetFaceTextureArray(info.code));
				newMaterial.SetColor("_SkinColor", skin);
				newMaterial.SetColor("_Color", info.primary);

		        if(r == Race.DRAGONLING)
		        	newMaterial.SetFloat("_Dragonling", 1f);
		        else
		        	newMaterial.SetFloat("_Dragonling", 0f);

		        this.faceAnimator.SetMaterial(newMaterial);
		        return newMaterial;
			}
			else if(r == Race.DRAGONLING){
				newMaterial = Material.Instantiate(dragonSkinMaterial);
			}
			else{
				newMaterial = Material.Instantiate(plainClothingMaterial);
			}

			newMaterial.SetColor("_Color", skin);
			return newMaterial;
		}
		else if(index == 1){
			newMaterial = Material.Instantiate(plainClothingMaterial);
			newMaterial.SetColor("_Color", info.primary);

			return newMaterial;
		}
		else if(index == 2){
			newMaterial = Material.Instantiate(plainClothingMaterial);
			newMaterial.SetColor("_Color", info.secondary);

			return newMaterial;
		}
		else if(index == 3){
			newMaterial = Material.Instantiate(plainClothingMaterial);
			newMaterial.SetColor("_Color", info.terciary);

			return newMaterial;
		}

		return Material.Instantiate(plainClothingMaterial);
	}

	private string GetEssentialName(bool isMale){
		if(isMale)
			return "Base_Head/M";
		return "Base_Head/F";
	}

	private string GetAddonName(Race r){
		switch(r){
			case Race.HUMAN:
				if(this.isMale)
					return "Base_Ears/M";
				else
					return "Base_Ears/F";
			case Race.ELF:
				if(this.isMale)
					return "Elven_Ears/M";
				else
					return "Elven_Ears/F";
			case Race.DWARF:
				if(this.isMale)
					return "Base_Ears/M";
				else
					return "Base_Ears/F";
			case Race.ORC:
				if(this.isMale)
					return "Orcish_Ears/M";
				else
					return "Orcish_Ears/F";
			case Race.DRAGONLING:
				if(this.isMale)
					return "Dragonling_Horns/M";
				else
					return "Dragonling_Horns/F";
			case Race.HALFLING:
				if(this.isMale)
					return "Base_Ears/M";
				else
					return "Base_Ears/F";
			default:
				if(this.isMale)
					return "Base_Ears/M";
				else
					return "Base_Ears/F";
		}
	}
}
