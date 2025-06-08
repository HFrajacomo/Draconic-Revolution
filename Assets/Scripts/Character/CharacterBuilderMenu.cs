using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class CharacterBuilderMenu{
	private GameObject parent;
	private Animator animator;
	private Dictionary<ModelType, GameObject> bodyParts;
	private Dictionary<ModelType, string> bodyPartName;
	private GameObject armature;
	private Transform rootBone;
	private BoneRenderer boneRenderer;
	private RaceSettings raceSettings;
	private Material[] addonMats;

	// Hairline
	private HairlinePlane hairlinePlane = new HairlinePlane(Vector3.zero, Vector3.zero, false);

	private static Dictionary<string, int> BONE_MAP;

	// Settings
	private static readonly int ROOT_BONE_INDEX = 0;
	private static readonly string ARMATURE_NAME_MALE = "Armature-Man";
	private static readonly string ARMATURE_NAME_FEMALE = "Armature-Woman";
	private static readonly Vector3 POS_1 = Vector3.zero;
	private static readonly Vector3 ROT_1 = new Vector3(270, 180, 20);
	private static readonly Vector3 SCL_1 = new Vector3(25,25,25);
	private static readonly int CHARACTER_CREATION_CHARACTER_SCALING = 400;

	private List<int> cachedTris = new List<int>();

	public CharacterBuilderMenu(GameObject par, RuntimeAnimatorController animations, Race race, Material[] addonMats, bool isMale=true){
		this.raceSettings = RaceManager.GetHuman();

		this.parent = par;
		this.animator = par.GetComponent<Animator>();
		this.animator.runtimeAnimatorController = animations;
		this.bodyParts = new Dictionary<ModelType, GameObject>();
		this.bodyPartName = new Dictionary<ModelType, string>();
		this.armature = ModelHandler.GetArmature(isMale:isMale);
		this.armature.transform.SetParent(this.parent.transform);
		this.addonMats = addonMats;

		if(isMale)
			this.armature.name = ARMATURE_NAME_MALE;
		else
			this.armature.name = ARMATURE_NAME_FEMALE;

		this.parent.transform.localScale = this.raceSettings.scaling * CHARACTER_CREATION_CHARACTER_SCALING;

		FixArmature(isMale);
		PutAddon(race, isMale);
	}

	public void ChangeAnimationGender(RuntimeAnimatorController animation){
		this.animator.runtimeAnimatorController = animation;
	}

	public GameObject Get(ModelType type){
		return this.bodyParts[type];
	}

	public int GetMaterialLength(ModelType type){
		SkinnedMeshRenderer smr = this.bodyParts[type].GetComponent<SkinnedMeshRenderer>();

		if(smr == null)
			return 1;
		return smr.materials.Length;
	}

	public void ChangeRace(Race race, bool isMale){
		this.raceSettings = RaceManager.GetSettings(race);

		GameObject.DestroyImmediate(this.armature);

		this.armature = ModelHandler.GetArmature(isMale:isMale);

		if(isMale)
			this.armature.name = ARMATURE_NAME_MALE;
		else
			this.armature.name = ARMATURE_NAME_FEMALE;

		this.armature.transform.SetParent(this.parent.transform);

		PutAddon(race, isMale, isReload:true);
		FixArmature(isMale);
		ReloadModel(isMale);
		
		this.animator.Rebind();
	}

	public void ChangeGender(Race race, bool isMale){
		PutAddon(race, isMale);
	}

	public void Add(ModelType type, GameObject obj, string name, bool isReload=false){
		if(this.bodyParts.ContainsKey(type)){
			GameObject.DestroyImmediate(this.bodyParts[type]);
		}

		if(!isReload){
			this.bodyPartName[type] = name;
		}

		if(!ModelHandler.HasModel(type, name)){
			this.bodyParts[type] = obj;
			obj.transform.SetParent(this.parent.transform);
			obj.transform.localPosition = Vector3.zero;
			obj.transform.localScale = Vector3.one;
			obj.transform.localRotation = Quaternion.Euler(-90, 0, 0);
			return;
		}

		obj.transform.SetParent(this.parent.transform);
		obj.transform.localScale = this.raceSettings.scaling;
		obj.transform.eulerAngles = ROT_1;
		obj.transform.localPosition = POS_1;

		this.bodyParts[type] = obj;

		SkinnedMeshRenderer current = obj.GetComponent<SkinnedMeshRenderer>();

		Debug.Log(current.materials.Length);

		if(BONE_MAP == null){
			SetBoneMap(current.bones);
		}

		Transform[] newBones = ModelHandler.GetArmatureBones(this.armature.transform, BONE_MAP);

		#if UNITY_EDITOR
			if(boneRenderer.transforms == null)
				boneRenderer.transforms = newBones;
		#endif

		Mesh mesh = CopyMesh(current.sharedMesh, current);

		if(type == ModelType.ADDON){
			if(this.raceSettings.GetRace() == Race.DRAGONLING)
				current.SetMaterials(new List<Material>(){this.addonMats[1]});
			else
				current.SetMaterials(new List<Material>(){this.addonMats[0]});
		}

		mesh.name = current.sharedMesh.name;
		current.sharedMesh = mesh;
		current.rootBone = newBones[ROOT_BONE_INDEX];
		current.bones = newBones;

		// Handling hat mesh and saving of the hairline plane
		if(type == ModelType.HEADGEAR){
			List<Vector3> planeVerts = GetVerticesForSubmesh(current.sharedMesh, current.sharedMesh.subMeshCount-1);
			Vector3 normal = GetFirstNormalInSubmesh(current.sharedMesh, current.sharedMesh.subMeshCount-1);

			if(planeVerts.Count < 4){
				return;
			}

			this.hairlinePlane = new HairlinePlane(planeVerts[0], planeVerts[1], planeVerts[2], planeVerts[3], normal);

			current.sharedMesh.subMeshCount = current.sharedMesh.subMeshCount - 1;
			current.materials = RemoveLastElementFromArray(current.materials);

			RefreshHairlineApply();

			// Check if covers hair
			if(ModelHandler.GetHatCover(ModelType.HEADGEAR, this.bodyPartName[ModelType.HEADGEAR]) == 'Y' && this.bodyParts.ContainsKey(ModelType.HAIR)){
				this.bodyParts[ModelType.HAIR].SetActive(false);
			}
			else if(ModelHandler.GetHatCover(ModelType.HEADGEAR, this.bodyPartName[ModelType.HEADGEAR]) == 'N' && this.bodyParts.ContainsKey(ModelType.HAIR)){
				this.bodyParts[ModelType.HAIR].SetActive(true);
			}
		}

		// Hide hair options
		if(type == ModelType.HAIR){
			if(this.bodyParts.ContainsKey(ModelType.HAIR) && this.bodyParts.ContainsKey(ModelType.HEADGEAR)){
				if(ModelHandler.HasModel(ModelType.HAIR, this.bodyPartName[ModelType.HAIR]) && ModelHandler.HasModel(ModelType.HEADGEAR, this.bodyPartName[ModelType.HEADGEAR])){
					char hatSKCode = ModelHandler.GetHatCover(ModelType.HEADGEAR, this.bodyPartName[ModelType.HEADGEAR]);

					if(hatSKCode == 'N'){ // If covers hair
						this.bodyParts[ModelType.HAIR].SetActive(true);
						ProcessHairMesh(this.bodyParts[ModelType.HAIR].GetComponent<SkinnedMeshRenderer>());
					}
					else{
						this.bodyParts[ModelType.HAIR].SetActive(false);
					}
				}
			}
		}
	}

	private void RefreshHairlineApply(){
		if(this.bodyParts.ContainsKey(ModelType.HAIR)){
			if(ModelHandler.HasModel(ModelType.HAIR, this.bodyPartName[ModelType.HAIR])){
				if(ModelHandler.GetHatCover(ModelType.HEADGEAR, this.bodyPartName[ModelType.HEADGEAR]) == 'N'){
					SkinnedMeshRenderer hairRenderer = this.bodyParts[ModelType.HAIR].GetComponent<SkinnedMeshRenderer>();

					hairRenderer.sharedMesh.SetVertices(ModelHandler.GetVertices(ModelType.HAIR, this.bodyPartName[ModelType.HAIR]));
					ProcessHairMesh(hairRenderer);
				}
			}
		}
	}

	private T[] RemoveLastElementFromArray<T>(T[] input){
		T[] aux = new T[input.Length-1];

		for(int i=0; i < input.Length-1; i++){
			aux[i] = input[i];
		}

		return aux;
	}

	public void ChangeArmature(bool isMale){
		if(this.armature != null){
			GameObject.DestroyImmediate(this.armature);

			if(BONE_MAP != null)
				BONE_MAP.Clear();
			
			BONE_MAP = null;
		}

		this.armature = ModelHandler.GetArmature(isMale:isMale);
		this.armature.transform.SetParent(this.parent.transform);

		if(isMale)
			this.armature.name = ARMATURE_NAME_MALE;
		else
			this.armature.name = ARMATURE_NAME_FEMALE;

		FixArmature(isMale);
		this.animator.Rebind();
	}

	public void ChangeAddonColor(Color col, Race race){
		if(!this.bodyParts.ContainsKey(ModelType.ADDON))
			return;

		if(race == Race.DRAGONLING || race == Race.UNDEAD){
			return;	
		}

		Material[] materials = this.bodyParts[ModelType.ADDON].GetComponent<SkinnedMeshRenderer>().materials;

		materials[0].SetColor("_Color", col);

		this.bodyParts[ModelType.ADDON].GetComponent<SkinnedMeshRenderer>().materials = materials;	
	}

	private void ProcessHairMesh(SkinnedMeshRenderer hair){
		if(!this.hairlinePlane.valid)
			return;

		List<Vector3> hairVerts = new List<Vector3>();
		hair.sharedMesh.GetVertices(hairVerts);

		for(int i=0; i < hairVerts.Count; i++){
			if(!this.hairlinePlane.GetSide(hairVerts[i])){
				continue;
			}

			hairVerts[i] = this.hairlinePlane.GetClosestPoint(hairVerts[i]);

		}

		hair.sharedMesh.SetVertices(hairVerts);
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

    private Vector3 AddVector(Vector3 a, Vector3 b){
    	return new Vector3(a.x+b.x, a.y+b.y, a.z+b.z);
    }

    private Vector3 MultVector(Vector3 a, Vector3 b){
    	return new Vector3(a.x*b.x, a.y*b.y, a.z*b.z);
    }

	private void PutAddon(Race race, bool isMale, bool isReload=false){
		if(this.bodyParts.ContainsKey(ModelType.ADDON)){
			GameObject.DestroyImmediate(this.bodyParts[ModelType.ADDON]);
		}

		if(race == Race.UNDEAD){
			this.bodyPartName.Remove(ModelType.ADDON);
			return;
		}

		if(isMale)
			this.bodyPartName[ModelType.ADDON] = GetAddonName(race) + "/M";
		else
			this.bodyPartName[ModelType.ADDON] = GetAddonName(race) + "/F";


		if(isReload)
			return;

		GameObject obj;

		if(isMale){
			obj = ModelHandler.GetModelObject(ModelType.ADDON, GetAddonName(race) + "/M");
		}
		else{
			obj = ModelHandler.GetModelObject(ModelType.ADDON, GetAddonName(race) + "/F");
		}

		obj.transform.SetParent(this.parent.transform);
		obj.transform.localScale = this.raceSettings.scaling;
		obj.transform.eulerAngles = ROT_1;
		obj.transform.localPosition = POS_1;


		SkinnedMeshRenderer current = obj.GetComponent<SkinnedMeshRenderer>();

		if(BONE_MAP == null){
			SetBoneMap(current.bones);
		}

		Transform[] newBones = ModelHandler.GetArmatureBones(this.armature.transform, BONE_MAP);

		#if UNITY_EDITOR
			if(boneRenderer.transforms == null)
				boneRenderer.transforms = newBones;
		#endif

		Mesh mesh = CopyMesh(current.sharedMesh, current);

		obj.name = "ADDON";
		mesh.name = current.sharedMesh.name;
		current.sharedMesh = mesh;
		current.rootBone = newBones[ROOT_BONE_INDEX];
		current.bones = newBones;

		this.bodyParts[ModelType.ADDON] = obj;

		// Set Material
		if(race == Race.DRAGONLING)
			current.SetMaterials(new List<Material>(){this.addonMats[1]});
		else
			current.SetMaterials(new List<Material>(){this.addonMats[0]});
	}

	private string GetAddonName(Race r){
		switch(r){
			case Race.HUMAN:
				return "Base_Ears";
			case Race.ELF:
				return "Elven_Ears";
			case Race.DWARF:
				return "Base_Ears";
			case Race.ORC:
				return "Orcish_Ears";
			case Race.DRAGONLING:
				return "Dragonling_Horns";
			case Race.HALFLING:
				return "Base_Ears";
			default:
				return "Base_Ears";
		}
	}

	private void ReloadModel(bool isMale){
		ChangeArmature(isMale);

		foreach(ModelType type in this.bodyPartName.Keys){
			Add(type, ModelHandler.GetModelObject(type, this.bodyPartName[type]), this.bodyPartName[type], isReload:true);
		}
	}

	private void SetBoneMap(Transform[] prefabBones){
		BONE_MAP = new Dictionary<string, int>();

		for(int i=0; i < prefabBones.Length; i++){
			BONE_MAP.Add(prefabBones[i].name, i);
		}
	}

	private void FixArmature(bool isMale){
		this.parent.transform.localScale = this.raceSettings.scaling * CHARACTER_CREATION_CHARACTER_SCALING;
		this.armature.transform.localScale = SCL_1;
		this.armature.transform.eulerAngles = ROT_1;
		this.armature.transform.localPosition = POS_1;
		this.boneRenderer = this.armature.AddComponent<BoneRenderer>();

		LoadRootBone();
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

        CopyTriangles(mesh, newMesh);

        return newMesh;
	}

	private void CopyTriangles(Mesh prefab, Mesh newMesh){
		for(int i=0; i < prefab.subMeshCount; i++){
			newMesh.SetTriangles(prefab.GetTriangles(i), i);
		}
	}

	private void ConvertSubMesh(Mesh p, Mesh n, int indexP, int indexN){
    	p.GetTriangles(this.cachedTris, indexP);
    	n.SetTriangles(this.cachedTris, indexN);
    	this.cachedTris.Clear();
	}

	private Vector3 ElementWiseMult(Vector3 a, Vector3 b){
		return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
	}
}