using System.Collections.Generic;
using UnityEngine;

/*
Client only. Samples the Voxel Light around an entity (players, dropped items) and sends it to the
_EntityLightNatural and _EntityLightExtra properties of its renderers, so entities are as dark as the blocks around them.
Values are applied per material through MaterialPropertyBlocks, merging with blocks set by other systems (AttachmentInstantiator)
*/
public class EntityVoxelLight : MonoBehaviour{
	private ChunkLoader cl;
	private MaterialPropertyBlock block;
	private List<Renderer> renderers = new List<Renderer>();
	private List<Material> materials = new List<Material>();

	private float natural = 1f;
	private float extra = 0f;
	private float targetNatural = 1f;
	private float targetExtra = 0f;
	private float sampleTimer = 0f;
	private bool hasSample = false;

	private static readonly float SAMPLE_INTERVAL = 0.2f;
	private static readonly float SMOOTHING_SPEED = 3f;
	private static readonly float CHANGE_THRESHOLD = 0.002f;
	private static readonly float SAMPLE_HEIGHT = 0.25f;
	private static readonly int NATURAL_ID = Shader.PropertyToID("_EntityLightNatural");
	private static readonly int EXTRA_ID = Shader.PropertyToID("_EntityLightExtra");


	public void SetChunkLoader(ChunkLoader cl){
		this.cl = cl;
	}

	void Awake(){
		this.block = new MaterialPropertyBlock();
	}

	void Update(){
		if(this.cl == null)
			return;

		this.sampleTimer -= Time.deltaTime;

		if(this.sampleTimer <= 0f){
			this.sampleTimer = SAMPLE_INTERVAL;

			if(Sample()){
				// The first sample is applied right away, so new entities don't fade in from full light
				if(!this.hasSample){
					this.natural = this.targetNatural;
					this.extra = this.targetExtra;
					this.hasSample = true;
				}
			}

			// Renderers change with equipment and held items, and other systems may replace their property blocks
			Apply();
			return;
		}

		float lastNatural = this.natural;
		float lastExtra = this.extra;
		float step = Time.deltaTime * SMOOTHING_SPEED;

		this.natural = Mathf.MoveTowards(this.natural, this.targetNatural, step);
		this.extra = Mathf.MoveTowards(this.extra, this.targetExtra, step);

		if(Mathf.Abs(lastNatural - this.natural) > CHANGE_THRESHOLD || Mathf.Abs(lastExtra - this.extra) > CHANGE_THRESHOLD)
			Apply();
	}

	// Takes the brightest of the block the entity is in and the one above it, since its pivot may touch the ground block
	private bool Sample(){
		Vector3 pos = this.transform.position + Vector3.up * SAMPLE_HEIGHT;
		CastCoord lower = WorldOrigin.ToCastCoord(pos);
		CastCoord upper = WorldOrigin.ToCastCoord(pos + Vector3.up);

		int naturalLevel = Mathf.Max(this.cl.GetLight(lower), this.cl.GetLight(upper));
		int extraLevel = Mathf.Max(this.cl.GetLight(lower, isNatural:false), this.cl.GetLight(upper, isNatural:false));

		if(naturalLevel < 0 || extraLevel < 0)
			return false;

		// Same normalization as the Light UV of chunk meshes (see VoxelLightIntensity.hlsl)
		this.targetNatural = naturalLevel / 15f;
		this.targetExtra = Mathf.Max(0, extraLevel - 1) / 15f;
		return true;
	}

	private void Apply(){
		GetComponentsInChildren<Renderer>(true, this.renderers);

		foreach(Renderer rend in this.renderers){
			if(!(rend is MeshRenderer) && !(rend is SkinnedMeshRenderer))
				continue;

			rend.GetSharedMaterials(this.materials);

			for(int i=0; i < this.materials.Count; i++){
				rend.GetPropertyBlock(this.block, i);
				this.block.SetFloat(NATURAL_ID, this.natural);
				this.block.SetFloat(EXTRA_ID, this.extra);
				rend.SetPropertyBlock(this.block, i);
			}
		}
	}
}
