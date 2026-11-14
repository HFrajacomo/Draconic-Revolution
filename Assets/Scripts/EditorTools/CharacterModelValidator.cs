#if UNITY_EDITOR

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

/*
Checks that every model in characters.fbx can be stitched together by CharacterBuilder without seams or wrong deformation
	- Every bone of a part must exist in the CharacterArmature (bones are matched by name, see CharacterSkeleton)
	- Every part must use the same bind pose for the same bone. Different bind poses make parts drift apart when the joint bends
	- Bind poses should match the armature's rest pose in the file
	- Every vertex must be weighted and its weights must add up to 1
*/
public static class CharacterModelValidator {
	private static readonly string CHARACTERS_RESPATH = "CharacterModels/characters";
	private static readonly string ARMATURE_NAME = "CharacterArmature";
	// Matrix elements are compared relative to their size, since the armature is in centimeters
	private static readonly float MATRIX_TOLERANCE = 0.001f;
	private static readonly float WEIGHT_SUM_TOLERANCE = 0.01f;
	private static readonly float ZERO_WEIGHT = 0.0001f;

	private struct BindposeSource {
		public Matrix4x4 pose;
		public string part;
	}


	[MenuItem("Editor Tools/Characters/Validate Models")]
	private static void Validate(){
		GameObject prefab = Resources.Load<GameObject>(CHARACTERS_RESPATH);

		if(prefab == null){
			EditorUtility.DisplayDialog("Character Model Validator", $"Could not load Resources/{CHARACTERS_RESPATH}", "OK");
			return;
		}

		GameObject instance = Object.Instantiate(prefab);
		instance.hideFlags = HideFlags.HideAndDontSave;

		int errors = 0;
		int warnings = 0;
		int partCount = 0;

		try{
			Transform armature = instance.transform.Find(ARMATURE_NAME);

			if(armature == null){
				Debug.LogError($"[CharacterModelValidator] {ARMATURE_NAME} was not found in characters.fbx");
				errors++;
				return;
			}

			HashSet<string> armatureBones = GetArmatureBones(armature);
			Dictionary<string, BindposeSource> bindposes = new Dictionary<string, BindposeSource>();
			SkinnedMeshRenderer[] renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

			foreach(SkinnedMeshRenderer smr in renderers){
				partCount++;
				ValidatePart(smr, armatureBones, bindposes, ref errors, ref warnings);
			}
		}
		finally{
			Object.DestroyImmediate(instance);
		}

		string summary = $"Checked {partCount} character models: {errors} error(s), {warnings} warning(s). Details are in the Console.";

		if(errors > 0)
			Debug.LogError($"[CharacterModelValidator] {summary}");
		else if(warnings > 0)
			Debug.LogWarning($"[CharacterModelValidator] {summary}");
		else
			Debug.Log($"[CharacterModelValidator] {summary}");

		EditorUtility.DisplayDialog("Character Model Validator", summary, "OK");
	}

	private static void ValidatePart(SkinnedMeshRenderer smr, HashSet<string> armatureBones, Dictionary<string, BindposeSource> bindposes, ref int errors, ref int warnings){
		string part = smr.gameObject.name;
		Mesh mesh = smr.sharedMesh;

		if(mesh == null){
			Debug.LogError($"[CharacterModelValidator] {part}: has no mesh");
			errors++;
			return;
		}

		Transform[] bones = smr.bones;
		Matrix4x4[] meshBindposes = mesh.bindposes;
		HashSet<string> seen = new HashSet<string>();
		Matrix4x4 meshToWorld = smr.transform.localToWorldMatrix;
		BindposeSource source;

		if(meshBindposes.Length != bones.Length){
			Debug.LogError($"[CharacterModelValidator] {part}: has {bones.Length} bones but {meshBindposes.Length} bind poses");
			errors++;
			return;
		}

		for(int i=0; i < bones.Length; i++){
			if(bones[i] == null){
				Debug.LogError($"[CharacterModelValidator] {part}: bone {i} is missing");
				errors++;
				continue;
			}

			string bone = bones[i].name;

			if(!seen.Add(bone)){
				Debug.LogError($"[CharacterModelValidator] {part}: bone \"{bone}\" is listed more than once");
				errors++;
			}

			if(!armatureBones.Contains(bone)){
				Debug.LogError($"[CharacterModelValidator] {part}: bone \"{bone}\" doesn't exist in {ARMATURE_NAME}");
				errors++;
				continue;
			}

			// Different bind poses between parts are what makes stitched parts separate when a joint bends
			if(bindposes.TryGetValue(bone, out source)){
				if(!AreEqual(source.pose, meshBindposes[i])){
					Debug.LogError($"[CharacterModelValidator] {part}: bind pose of \"{bone}\" is different from the one in {source.part}. Re-export it with the same rest pose");
					errors++;
				}
			}
			else{
				bindposes.Add(bone, new BindposeSource{pose = meshBindposes[i], part = part});
			}

			if(!AreEqual(bones[i].worldToLocalMatrix * meshToWorld, meshBindposes[i])){
				Debug.LogWarning($"[CharacterModelValidator] {part}: bind pose of \"{bone}\" doesn't match the rest pose of {ARMATURE_NAME} in the file");
				warnings++;
			}
		}

		ValidateWeights(part, mesh, bones.Length, ref errors, ref warnings);
	}

	private static void ValidateWeights(string part, Mesh mesh, int boneCount, ref int errors, ref int warnings){
		BoneWeight[] weights = mesh.boneWeights;
		int unweighted = 0;
		int unnormalized = 0;
		int badIndex = 0;
		float sum;

		if(weights.Length == 0){
			Debug.LogError($"[CharacterModelValidator] {part}: has no bone weights");
			errors++;
			return;
		}

		for(int i=0; i < weights.Length; i++){
			sum = weights[i].weight0 + weights[i].weight1 + weights[i].weight2 + weights[i].weight3;

			if(sum < ZERO_WEIGHT)
				unweighted++;
			else if(Mathf.Abs(sum - 1f) > WEIGHT_SUM_TOLERANCE)
				unnormalized++;

			if(IsBadIndex(weights[i].boneIndex0, weights[i].weight0, boneCount) || IsBadIndex(weights[i].boneIndex1, weights[i].weight1, boneCount) ||
			   IsBadIndex(weights[i].boneIndex2, weights[i].weight2, boneCount) || IsBadIndex(weights[i].boneIndex3, weights[i].weight3, boneCount))
				badIndex++;
		}

		if(badIndex > 0){
			Debug.LogError($"[CharacterModelValidator] {part}: {badIndex} vertices are weighted to bones that don't exist");
			errors++;
		}
		if(unweighted > 0){
			Debug.LogError($"[CharacterModelValidator] {part}: {unweighted} of {weights.Length} vertices have no weights and won't follow the animation");
			errors++;
		}
		if(unnormalized > 0){
			Debug.LogWarning($"[CharacterModelValidator] {part}: {unnormalized} of {weights.Length} vertices have weights that don't add up to 1. Normalize All in Blender");
			warnings++;
		}
	}

	private static bool IsBadIndex(int index, float weight, int boneCount){
		return weight > 0f && (index < 0 || index >= boneCount);
	}

	// Same filter as ModelHandler.GetArmatureBones: Blender's leaf "end" bones are never weighted
	private static HashSet<string> GetArmatureBones(Transform armature){
		HashSet<string> names = new HashSet<string>();

		foreach(Transform t in armature.GetComponentsInChildren<Transform>(true)){
			if(!t.name.EndsWith("end"))
				names.Add(t.name);
		}

		return names;
	}

	private static bool AreEqual(Matrix4x4 a, Matrix4x4 b){
		for(int i=0; i < 16; i++){
			if(Mathf.Abs(a[i] - b[i]) > MATRIX_TOLERANCE * Mathf.Max(1f, Mathf.Abs(a[i])))
				return false;
		}

		return true;
	}
}

#endif
