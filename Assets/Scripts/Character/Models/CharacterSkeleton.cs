using System.Collections.Generic;
using UnityEngine;

/*
Bone order shared by every character model, taken from the CharacterArmature in characters.fbx
	characters.fbx is imported with Strip Bones on, so each part only lists the bones it's weighted to, in its own order.
	Part bones are matched to this order by name, never by index
*/
public static class CharacterSkeleton {
	private static Dictionary<string, int> boneMap;
	private static List<string> boneNames;

	public static readonly string ROOT_BONE_NAME = "Hips";


	public static Dictionary<string, int> GetBoneMap(){
		Init();
		return boneMap;
	}

	public static int GetBoneCount(){
		Init();
		return boneNames.Count;
	}

	public static int GetRootIndex(){
		Init();
		return boneMap[ROOT_BONE_NAME];
	}

	// Name -> index map of a renderer's own bone list. Matches its mesh's bind poses and bone weights
	public static Dictionary<string, int> BuildMap(Transform[] bones){
		Dictionary<string, int> map = new Dictionary<string, int>(bones.Length);

		for(int i=0; i < bones.Length; i++){
			if(bones[i] != null && !map.ContainsKey(bones[i].name))
				map.Add(bones[i].name, i);
		}

		return map;
	}

	/*
	Shared index of every bone of a part, in the part's order
		Bones missing from the armature are reported and sent to the root bone, so the mesh still deforms instead of collapsing
	*/
	public static int[] GetRemap(Transform[] partBones, string partName){
		Init();

		int[] remap = new int[partBones.Length];
		int index;

		for(int i=0; i < partBones.Length; i++){
			if(partBones[i] != null && boneMap.TryGetValue(partBones[i].name, out index)){
				remap[i] = index;
			}
			else{
				Debug.LogError($"[CharacterSkeleton] Bone \"{(partBones[i] != null ? partBones[i].name : "null")}\" of \"{partName}\" doesn't exist in the CharacterArmature. Its vertices follow {ROOT_BONE_NAME} instead");
				remap[i] = boneMap[ROOT_BONE_NAME];
			}
		}

		return remap;
	}

	// Same filter as ModelHandler.GetArmatureBones: Blender's leaf "end" bones are never weighted
	private static void Init(){
		if(boneMap != null)
			return;

		boneMap = new Dictionary<string, int>();
		boneNames = new List<string>();

		Transform[] transforms = ModelHandler.GetArmatureTemplate().GetComponentsInChildren<Transform>(true);

		for(int i=0; i < transforms.Length; i++){
			if(transforms[i].name.EndsWith("end") || boneMap.ContainsKey(transforms[i].name))
				continue;

			boneMap.Add(transforms[i].name, boneNames.Count);
			boneNames.Add(transforms[i].name);
		}
	}
}
