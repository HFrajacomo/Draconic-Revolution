using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public static class CharacterMaterialUtils{
	private static readonly string DOUBLE_SIDED_PROPERTY = "_DoubleSidedEnable";
	private static readonly string DOUBLE_SIDED_KEYWORD = "_DOUBLESIDED_ON";

	// Toggles back face rendering on an HDRP (ShaderGraph) material instance, letting HDRP fix up the cull modes and keywords
	public static void SetDoubleSided(Material material, bool doubleSided){
		if(material == null)
			return;
		if(!material.HasProperty(DOUBLE_SIDED_PROPERTY))
			return;

		if((material.GetFloat(DOUBLE_SIDED_PROPERTY) > 0f) == doubleSided && material.IsKeywordEnabled(DOUBLE_SIDED_KEYWORD) == doubleSided)
			return;

		material.SetFloat(DOUBLE_SIDED_PROPERTY, doubleSided ? 1f : 0f);
		material.doubleSidedGI = doubleSided;

		HDMaterial.ValidateMaterial(material);
	}

	public static void SetDoubleSided(Material[] materials, bool doubleSided){
		if(materials == null)
			return;

		for(int i=0; i < materials.Length; i++){
			SetDoubleSided(materials[i], doubleSided);
		}
	}
}
