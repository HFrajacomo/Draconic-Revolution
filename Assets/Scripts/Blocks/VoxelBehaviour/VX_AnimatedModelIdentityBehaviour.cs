using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

/*
Model identity of Animated Objects (ShaderIndex.ANIMATED_OBJECTS). See SOPs/add_new_animated_object.md

The model is a model file in Resources (modelResourcePath) whose meshes are its parts, animated by object transform animations (no armatures or shape keys).
When loaded (Client only), the parts are merged into a single mesh in their rest pose, each vertex keeping the index of its part,
and every animation listed in animations is sampled into the transform of each part relative to its rest pose (AnimatedClipData).
The position of an animation in animations is its index, used by voxel behaviours and network messages.

With fitSize, the model is fitted to its voxel no matter the units it was imported in: the pose given by fitAnimation (its first frame, or the rest pose)
is centered on the voxel in X and Z, put on the voxel floor and scaled so its largest side is fitSize of the voxel. The hitbox is then the box of that pose.
Without it, the model keeps its imported size and the hitbox is a unit cube. Either way, the Object's scaling and hitboxScaling are applied on top
*/
[Serializable]
public class VX_AnimatedModelIdentityBehaviour : VX_ModelIdentityBehaviour{
	public string modelResourcePath;
	public List<string> animations;
	public float fitSize; // Fraction of the voxel taken by the fitted pose on its largest side. 0 doesn't fit the model
	public string fitAnimation; // Animation whose first frame is fitted. Empty fits the model's rest pose
	public Vector3 modelOffset; // Moves the model after the fit (in voxels when fitted), before the Object scaling

	private List<float> vertexParts;
	private List<AnimatedClipData> clips;
	private int partCount;
	private float radius;

	private static readonly float MIN_CLIP_LENGTH = 0.001f;
	private static readonly float DEFAULT_FRAME_RATE = 24f;
	private static readonly float VOXEL_FLOOR = -0.5f;
	private static readonly string PREVIEW_CLIP_PREFIX = "__preview__";
	private static readonly string HITBOX_MESH = "Cube.fbx";


	public override void PostDeserializationSetup(bool isClient){
		if(this.animations == null || this.animations.Count == 0)
			throw new DeserializationErrorException($"[VX_AnimatedModelIdentityBehaviour] Model '{this.modelResourcePath}' has no animations listed");
		if(this.animations.Count > byte.MaxValue)
			throw new DeserializationErrorException($"[VX_AnimatedModelIdentityBehaviour] Model '{this.modelResourcePath}' lists more than {byte.MaxValue} animations");
		if(!string.IsNullOrEmpty(this.fitAnimation) && !this.animations.Contains(this.fitAnimation))
			throw new DeserializationErrorException($"[VX_AnimatedModelIdentityBehaviour] fitAnimation '{this.fitAnimation}' of model '{this.modelResourcePath}' isn't in its animations");

		if(!isClient || this.clips != null)
			return;

		LoadModel();
	}

	public bool IsAnimationLoaded(){return this.clips != null;}
	public int GetAnimationCount(){return this.animations.Count;}
	public int GetAnimationIndex(string animationName){return this.animations.IndexOf(animationName);}
	public AnimatedClipData GetClip(int index){return this.clips[index];}
	public List<float> GetVertexParts(){return this.vertexParts;}
	public int GetPartCount(){return this.partCount;}

	// Largest distance from the model origin reached by any vertex in any animation, in model units
	public float GetRadius(){return this.radius;}

	private void LoadModel(){
		GameObject prefab = Resources.Load<GameObject>(this.modelResourcePath);

		if(prefab == null)
			throw new DeserializationErrorException($"[VX_AnimatedModelIdentityBehaviour] Couldn't find the model 'Resources/{this.modelResourcePath}'");

		// The instance keeps the root rotation and scale given by the importer, so axis and unit conversions are part of the rest pose
		GameObject instance = Object.Instantiate(prefab);
		instance.transform.position = Vector3.zero;

		try{
			if(instance.GetComponentInChildren<Animator>(true) != null)
				throw new DeserializationErrorException($"[VX_AnimatedModelIdentityBehaviour] Model '{this.modelResourcePath}' must be imported with the Legacy rig");
			if(instance.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
				throw new DeserializationErrorException($"[VX_AnimatedModelIdentityBehaviour] Model '{this.modelResourcePath}' has armatures or shape keys, which aren't supported");

			DisableComponents(instance);

			Transform[] hierarchy = instance.GetComponentsInChildren<Transform>(true);
			MeshFilter[] parts = instance.GetComponentsInChildren<MeshFilter>(true);
			Vector3[] restPositions = new Vector3[hierarchy.Length];
			Quaternion[] restRotations = new Quaternion[hierarchy.Length];
			Vector3[] restScales = new Vector3[hierarchy.Length];

			if(parts.Length == 0)
				throw new DeserializationErrorException($"[VX_AnimatedModelIdentityBehaviour] Model '{this.modelResourcePath}' has no meshes");

			for(int i=0; i < hierarchy.Length; i++){
				restPositions[i] = hierarchy[i].localPosition;
				restRotations[i] = hierarchy[i].localRotation;
				restScales[i] = hierarchy[i].localScale;
			}

			Matrix4x4[] restInverse = new Matrix4x4[parts.Length];
			int[] partStart = new int[parts.Length+1];
			MergedModel model = MergeParts(parts, restInverse, partStart);
			List<Matrix4x4[]> sampled = SampleClips(instance, parts, hierarchy, restPositions, restRotations, restScales, restInverse);
			Bounds fittedBounds;
			Matrix4x4 fit = CalculateFit(model.vertices, partStart, sampled, out fittedBounds);

			ApplyFit(fit, model.vertices, sampled);
			CalculateRadius(model.vertices, partStart);

			Mesh merged = model.ToMesh(this.modelResourcePath);
			Mesh hitbox = BuildHitbox(fittedBounds);

			SetMeshData(new MeshData(merged, hitbox, VoxelLoader.GetTextureID(this.textureName)));
			Object.Destroy(merged);

			if(this.fitSize > 0f)
				Object.Destroy(hitbox);
		}
		finally{
			Object.Destroy(instance);
		}
	}

	// The instance only exists to be sampled, so it never renders nor plays animations by itself
	private void DisableComponents(GameObject instance){
		foreach(Renderer rend in instance.GetComponentsInChildren<Renderer>(true))
			rend.enabled = false;
		foreach(Animation animation in instance.GetComponentsInChildren<Animation>(true))
			animation.enabled = false;
	}

	// Merges every part in its rest pose. Writes the inverse rest transform of each part and the first vertex of each part
	private MergedModel MergeParts(MeshFilter[] parts, Matrix4x4[] restInverse, int[] partStart){
		MergedModel model = new MergedModel();
		List<Vector3> partVerts = new List<Vector3>();
		List<Vector3> partNormals = new List<Vector3>();
		List<Vector4> partTangents = new List<Vector4>();
		List<Vector2> partUVs = new List<Vector2>();
		List<int> partTris = new List<int>();
		Matrix4x4 rest, normalMatrix;
		Vector3 tangent;
		bool mirrored;
		int first;
		Mesh mesh;

		this.vertexParts = new List<float>();
		this.partCount = parts.Length;

		for(int p=0; p < parts.Length; p++){
			mesh = parts[p].sharedMesh;
			partStart[p] = model.vertices.Count;

			if(mesh == null)
				throw new DeserializationErrorException($"[VX_AnimatedModelIdentityBehaviour] Part '{parts[p].name}' of model '{this.modelResourcePath}' has no mesh");

			rest = parts[p].transform.localToWorldMatrix;
			normalMatrix = rest.inverse.transpose;
			restInverse[p] = rest.inverse;
			mirrored = rest.determinant < 0;
			first = model.vertices.Count;

			mesh.GetVertices(partVerts);
			mesh.GetNormals(partNormals);
			mesh.GetTangents(partTangents);
			mesh.GetUVs(0, partUVs);

			for(int i=0; i < partVerts.Count; i++){
				model.vertices.Add(rest.MultiplyPoint3x4(partVerts[i]));
				model.normals.Add(i < partNormals.Count ? normalMatrix.MultiplyVector(partNormals[i]).normalized : Vector3.up);
				model.uvs.Add(i < partUVs.Count ? partUVs[i] : Vector2.zero);
				this.vertexParts.Add(p);

				if(i < partTangents.Count){
					tangent = rest.MultiplyVector(partTangents[i]).normalized;
					model.tangents.Add(new Vector4(tangent.x, tangent.y, tangent.z, mirrored ? -partTangents[i].w : partTangents[i].w));
				}
				else{
					model.tangents.Add(new Vector4(1, 0, 0, 1));
				}
			}

			// Mirrored parts would turn their faces inside out
			for(int sub=0; sub < mesh.subMeshCount; sub++){
				mesh.GetTriangles(partTris, sub);

				for(int i=0; i+2 < partTris.Count; i+=3){
					model.triangles.Add(partTris[i] + first);
					model.triangles.Add((mirrored ? partTris[i+2] : partTris[i+1]) + first);
					model.triangles.Add((mirrored ? partTris[i+1] : partTris[i+2]) + first);
				}
			}
		}

		partStart[parts.Length] = model.vertices.Count;

		return model;
	}

	/*
	Samples every listed animation in evenly spaced frames at the frame rate of its clip, filling this.clips.
	Returns the transform of each part relative to its rest pose, per animation, ordered [frame][part]. They're only written to the clips by ApplyFit
	*/
	private List<Matrix4x4[]> SampleClips(GameObject instance, MeshFilter[] parts, Transform[] hierarchy, Vector3[] restPositions, Quaternion[] restRotations, Vector3[] restScales,
		Matrix4x4[] restInverse){

		Dictionary<string, AnimationClip> found = FindClips(instance);
		List<Matrix4x4[]> sampled = new List<Matrix4x4[]>();
		Matrix4x4[] transforms;
		AnimationClip clip;
		float length, frameRate;
		int frameCount;

		this.clips = new List<AnimatedClipData>();

		foreach(string animationName in this.animations){
			if(!found.TryGetValue(animationName, out clip))
				throw new DeserializationErrorException($"[VX_AnimatedModelIdentityBehaviour] Model '{this.modelResourcePath}' has no animation '{animationName}'. Found: {string.Join(", ", found.Keys)}");

			length = Mathf.Max(clip.length, MIN_CLIP_LENGTH);
			frameRate = clip.frameRate > 0 ? clip.frameRate : DEFAULT_FRAME_RATE;
			frameCount = Mathf.Max(1, Mathf.CeilToInt(length * frameRate - 0.001f)) + 1;
			transforms = new Matrix4x4[frameCount * parts.Length];

			// Parts not animated by this clip stay in their rest pose
			RestorePose(hierarchy, restPositions, restRotations, restScales);

			for(int frame=0; frame < frameCount; frame++){
				clip.SampleAnimation(instance, length * frame / (frameCount-1));

				for(int p=0; p < parts.Length; p++){
					transforms[frame*parts.Length + p] = parts[p].transform.localToWorldMatrix * restInverse[p];
				}
			}

			this.clips.Add(new AnimatedClipData(animationName, length, clip.isLooping || clip.wrapMode == WrapMode.Loop, frameCount, parts.Length));
			sampled.Add(transforms);
		}

		RestorePose(hierarchy, restPositions, restRotations, restScales);

		return sampled;
	}

	/*
	Transform that fits the model to its voxel (see fitSize), followed by modelOffset. Outputs the box of the fitted pose after the transform.
	Without fitSize it only applies modelOffset
	*/
	private Matrix4x4 CalculateFit(List<Vector3> vertices, int[] partStart, List<Matrix4x4[]> sampled, out Bounds fittedBounds){
		fittedBounds = new Bounds(Vector3.zero, Vector3.one);

		if(this.fitSize <= 0f)
			return Matrix4x4.Translate(this.modelOffset);

		int animation = string.IsNullOrEmpty(this.fitAnimation) ? -1 : this.animations.IndexOf(this.fitAnimation);
		Vector3 min = Vector3.positiveInfinity;
		Vector3 max = Vector3.negativeInfinity;
		Vector3 vert;

		for(int p=0; p < this.partCount; p++){
			for(int v=partStart[p]; v < partStart[p+1]; v++){
				vert = animation < 0 ? vertices[v] : sampled[animation][p].MultiplyPoint3x4(vertices[v]);
				min = Vector3.Min(min, vert);
				max = Vector3.Max(max, vert);
			}
		}

		Vector3 size = max - min;
		float largest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));

		if(largest <= 0f)
			throw new DeserializationErrorException($"[VX_AnimatedModelIdentityBehaviour] Model '{this.modelResourcePath}' has no size to fit");

		float scale = this.fitSize / largest;
		Vector3 center = (min + max) * 0.5f;
		Vector3 translation = new Vector3(-center.x * scale, VOXEL_FLOOR - min.y * scale, -center.z * scale) + this.modelOffset;

		fittedBounds = new Bounds(center * scale + translation, size * scale);

		return Matrix4x4.TRS(translation, Quaternion.identity, Vector3.one * scale);
	}

	// Moves the merged vertices by the fit, and writes the part transforms to the clips in the fitted space. The fit has a uniform scale, so directions don't change
	private void ApplyFit(Matrix4x4 fit, List<Vector3> vertices, List<Matrix4x4[]> sampled){
		Matrix4x4 inverseFit = fit.inverse;
		int partCount;

		for(int i=0; i < vertices.Count; i++)
			vertices[i] = fit.MultiplyPoint3x4(vertices[i]);

		for(int c=0; c < this.clips.Count; c++){
			partCount = this.clips[c].partCount;

			for(int frame=0; frame < this.clips[c].frameCount; frame++){
				for(int p=0; p < partCount; p++){
					this.clips[c].SetPart(frame, p, fit * sampled[c][frame*partCount + p] * inverseFit);
				}
			}
		}
	}

	private void CalculateRadius(List<Vector3> vertices, int[] partStart){
		AnimatedClipData clip;
		Matrix4x4 transform;
		int index;

		this.radius = 0f;

		for(int i=0; i < vertices.Count; i++)
			this.radius = Mathf.Max(this.radius, vertices[i].magnitude);

		for(int c=0; c < this.clips.Count; c++){
			clip = this.clips[c];

			for(int frame=0; frame < clip.frameCount; frame++){
				for(int p=0; p < clip.partCount; p++){
					index = (frame*clip.partCount + p)*3;
					transform = Matrix4x4.identity;
					transform.SetRow(0, clip.rows[index]);
					transform.SetRow(1, clip.rows[index+1]);
					transform.SetRow(2, clip.rows[index+2]);

					for(int v=partStart[p]; v < partStart[p+1]; v++)
						this.radius = Mathf.Max(this.radius, transform.MultiplyPoint3x4(vertices[v]).magnitude);
				}
			}
		}
	}

	// Fitted models get the box of their fitted pose. The others get the unit cube, like other Objects
	private Mesh BuildHitbox(Bounds fittedBounds){
		Mesh cube = Resources.GetBuiltinResource<Mesh>(HITBOX_MESH);

		if(this.fitSize <= 0f)
			return cube;

		List<Vector3> verts = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();
		Mesh hitbox = new Mesh();

		// The builtin cube spans [-0.5, 0.5]
		cube.GetVertices(verts);
		cube.GetNormals(normals);

		for(int i=0; i < verts.Count; i++)
			verts[i] = fittedBounds.center + Vector3.Scale(verts[i], fittedBounds.size);

		hitbox.SetVertices(verts);
		hitbox.SetNormals(normals);
		hitbox.SetTriangles(cube.GetTriangles(0), 0);

		return hitbox;
	}

	private Dictionary<string, AnimationClip> FindClips(GameObject instance){
		Dictionary<string, AnimationClip> found = new Dictionary<string, AnimationClip>();

		foreach(AnimationClip clip in Resources.LoadAll<AnimationClip>(this.modelResourcePath))
			AddClip(found, clip);

		// Legacy models also list their clips in their Animation component
		foreach(Animation animation in instance.GetComponentsInChildren<Animation>(true)){
			foreach(AnimationState state in animation){
				AddClip(found, state.clip);
			}
		}

		return found;
	}

	private void AddClip(Dictionary<string, AnimationClip> found, AnimationClip clip){
		if(clip == null || clip.name.StartsWith(PREVIEW_CLIP_PREFIX) || found.ContainsKey(clip.name))
			return;

		found.Add(clip.name, clip);
	}

	private void RestorePose(Transform[] hierarchy, Vector3[] restPositions, Quaternion[] restRotations, Vector3[] restScales){
		for(int i=0; i < hierarchy.Length; i++){
			hierarchy[i].localPosition = restPositions[i];
			hierarchy[i].localRotation = restRotations[i];
			hierarchy[i].localScale = restScales[i];
		}
	}

	// Parts merged in their rest pose, before becoming a Mesh
	private class MergedModel{
		public List<Vector3> vertices = new List<Vector3>();
		public List<Vector3> normals = new List<Vector3>();
		public List<Vector4> tangents = new List<Vector4>();
		public List<Vector2> uvs = new List<Vector2>();
		public List<int> triangles = new List<int>();

		public Mesh ToMesh(string name){
			Mesh mesh = new Mesh();

			mesh.name = name;
			mesh.indexFormat = this.vertices.Count >= ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
			mesh.SetVertices(this.vertices);
			mesh.SetNormals(this.normals);
			mesh.SetTangents(this.tangents);
			mesh.SetUVs(0, this.uvs);
			mesh.SetTriangles(this.triangles, 0);

			return mesh;
		}
	}
}
