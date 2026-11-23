using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

/*
Client only. Draws a round contact shadow under every visible dropped item (ItemEntity), all of them in a single procedural draw call.

Every item gets a box hanging below it, built by ItemShadow.shader from the item position (no mesh, no GameObject).
The shader reads the scene depth inside the box and darkens the up-facing surfaces it finds, so the shadow follows the voxels below the item.
The top of the box is below the lowest point a spinning item reaches, so items never shadow themselves
*/
public class ItemShadowRenderer{
	private static readonly string MATERIAL_RESPATH = "Materials/ItemShadow";
	private static readonly string DROPPED_ITEM_LAYER = "DroppedItem";
	private static readonly int INITIAL_CAPACITY = 64;
	private static readonly int PIVOT_STRIDE = sizeof(float) * 4;
	private static readonly int BOX_VERTICES = 36;
	private static readonly int PIVOTS_ID = Shader.PropertyToID("_ItemShadowPivots");
	private static readonly int BOX_ID = Shader.PropertyToID("_ItemShadowBox");

	// Dropped item quads are 0.5 wide, so a spinning item never reaches more than ~0.18 below its pivot
	private static readonly float SHADOW_DIAMETER = 0.55f;
	private static readonly float BOX_TOP = 0.2f;
	private static readonly float BOX_BOTTOM = 3f;
	// Resting items float 0.9 above the floor (DroppedItemBehaviour). The shadow has full strength down to it
	private static readonly float REST_HEIGHT = 0.9f;

	private Material material;
	private GraphicsBuffer buffer;
	private Vector4[] pivots;
	private RenderParams renderParams;


	public ItemShadowRenderer(){
		Material sharedMaterial = Resources.Load<Material>(MATERIAL_RESPATH);

		if(sharedMaterial == null){
			Debug.LogError($"[ItemShadowRenderer] Couldn't load the {MATERIAL_RESPATH} material. Dropped items won't have shadows");
			return;
		}

		// Buffers given through MaterialPropertyBlocks aren't bound in every pass, and DX12 skips draws with unbound buffers
		this.material = new Material(sharedMaterial);
		this.material.SetVector(BOX_ID, new Vector4(SHADOW_DIAMETER, BOX_TOP, BOX_BOTTOM, REST_HEIGHT));

		this.pivots = new Vector4[INITIAL_CAPACITY];
		this.buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, INITIAL_CAPACITY, PIVOT_STRIDE);
		this.material.SetBuffer(PIVOTS_ID, this.buffer);

		this.renderParams = new RenderParams(this.material);
		this.renderParams.layer = LayerMask.NameToLayer(DROPPED_ITEM_LAYER);
		this.renderParams.shadowCastingMode = ShadowCastingMode.Off;
		this.renderParams.receiveShadows = false;
		this.renderParams.motionVectorMode = MotionVectorGenerationMode.ForceNoMotion;
		this.renderParams.lightProbeUsage = LightProbeUsage.Off;
		this.renderParams.reflectionProbeUsage = ReflectionProbeUsage.Off;
	}

	// Must run after the items were moved this frame
	public void Render(Dictionary<ulong, ItemEntity>.ValueCollection items){
		if(this.material == null)
			return;

		int count = 0;
		Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		Vector3 pos;

		foreach(ItemEntity item in items){
			if(item == null || !item.IsVisible())
				continue;

			if(count == this.pivots.Length)
				Grow();

			pos = item.GetModelPosition();
			this.pivots[count] = new Vector4(pos.x, pos.y, pos.z, 0f);
			min = Vector3.Min(min, pos);
			max = Vector3.Max(max, pos);
			count++;
		}

		if(count == 0)
			return;

		this.buffer.SetData(this.pivots, 0, 0, count);

		float radius = SHADOW_DIAMETER * 0.5f;
		Bounds bounds = new Bounds();
		bounds.SetMinMax(new Vector3(min.x - radius, min.y - BOX_BOTTOM, min.z - radius), new Vector3(max.x + radius, max.y - BOX_TOP, max.z + radius));
		this.renderParams.worldBounds = bounds;

		Graphics.RenderPrimitives(this.renderParams, MeshTopology.Triangles, BOX_VERTICES, count);
	}

	public void Dispose(){
		if(this.buffer != null)
			this.buffer.Release();

		if(this.material != null)
			Object.Destroy(this.material);

		this.buffer = null;
		this.material = null;
		this.pivots = null;
	}

	// Doubles the capacity. The pivots are written again before the draw
	private void Grow(){
		int capacity = this.pivots.Length * 2;

		Array.Resize(ref this.pivots, capacity);

		this.buffer.Release();
		this.buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity, PIVOT_STRIDE);
		this.material.SetBuffer(PIVOTS_ID, this.buffer);
	}
}
