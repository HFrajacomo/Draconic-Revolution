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

	private static Dictionary<string, int> BONE_MAP;

	// Settings
	private static readonly int ROOT_BONE_INDEX = 0;
	private static readonly string ARMATURE_NAME_MALE = "Armature-Man";
	private static readonly string ARMATURE_NAME_FEMALE = "Armature-Woman";
	private static readonly Vector3 POS_1 = Vector3.zero;
	private static readonly Vector3 ROT_1 = new Vector3(270, 180, 20);
	private static readonly Vector3 SCL_1 = new Vector3(25,25,25);
	private static readonly Vector3 SCL_2 = new Vector3(25,25,25);
	private static readonly int CHARACTER_CREATION_CHARACTER_SCALING = 150;

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

		if(isMale){
			this.armature.name = ARMATURE_NAME_MALE;
			this.armature.transform.localScale = ElementWiseMult(SCL_1, this.raceSettings.scaling);
		}
		else{
			this.armature.name = ARMATURE_NAME_FEMALE;
			this.armature.transform.localScale = ElementWiseMult(SCL_2, this.raceSettings.scaling);
		}

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
		if(type != ModelType.FACE)
			return this.bodyParts[type].GetComponent<SkinnedMeshRenderer>().materials.Length;
		else
			return this.bodyParts[type].GetComponent<SkinnedMeshRenderer>().materials.Length + 1;
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
			return;
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

		if(type != ModelType.ADDON)
			FixMaterialOrder(current);
		else{
			if(this.raceSettings.GetRace() == Race.DRAGONLING)
				current.SetMaterials(new List<Material>(){this.addonMats[1]});
			else
				current.SetMaterials(new List<Material>(){this.addonMats[0]});
		}


		mesh.name = current.sharedMesh.name;
		current.sharedMesh = mesh;
		current.rootBone = newBones[ROOT_BONE_INDEX];
		current.bones = newBones;

		this.bodyParts[type] = obj;
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

		this.parent.transform.localScale = this.raceSettings.scaling * CharacterBuilderMenu.CHARACTER_CREATION_CHARACTER_SCALING;

		if(isMale){
			this.armature.name = ARMATURE_NAME_MALE;
			this.armature.transform.localScale = ElementWiseMult(SCL_1, this.raceSettings.scaling);
		}
		else{
			this.armature.name = ARMATURE_NAME_FEMALE;
			this.armature.transform.localScale = ElementWiseMult(SCL_2, this.raceSettings.scaling);
		}

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
		FixMaterialOrder(current);

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
		foreach(Transform t in this.armature.GetComponentsInChildren<Transform>()){
			t.position = new Vector3(t.position.x * this.raceSettings.scaling.x, t.position.y * this.raceSettings.scaling.y, t.position.z * this.raceSettings.scaling.z);
		}

		if(isMale)
			this.armature.transform.localScale = ElementWiseMult(SCL_1, this.raceSettings.scaling);
		else
			this.armature.transform.localScale = ElementWiseMult(SCL_2, this.raceSettings.scaling);


		this.armature.transform.eulerAngles = ROT_1;
		this.armature.transform.localPosition = POS_1;
		this.boneRenderer = this.armature.AddComponent<BoneRenderer>();

		LoadRootBone();
	}

	private void LoadRootBone(){
		this.rootBone = this.armature.transform.Find("Pelvis").transform;
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
			case 1:
				if(rend.materials[0].name == "Skin (Instance)"){
					ConvertSubMesh(prefab, newMesh, 0, 0);
				}
				else{
					newMesh.subMeshCount = 2;
					rend.materials = new Material[2];
					ConvertSubMesh(prefab, newMesh, 0, 1);
				}
				return;
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

	private Vector3 ElementWiseMult(Vector3 a, Vector3 b){
		return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
	}
}