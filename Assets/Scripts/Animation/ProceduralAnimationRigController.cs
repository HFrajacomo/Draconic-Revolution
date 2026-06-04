using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ProceduralAnimationRigController {
	private string controllerName;
	private string currentState;
	private GameObject parent;
	private GameObject animatorParent;
	private Transform eyeTracker;
	private Transform armature;
	private GameObject proceduralRig;
	private RigBuilder rigBuilder;
	private MultiAimData[] multiAimData;
	private List<MultiAimConstraint> multiAimConstraints;
	private GameObject camera;

	private static Transform parentEyeTrackers;

	public ProceduralAnimationRigController(GameObject characterObject, GameObject animatorParent, string controllerName){
		this.parent = characterObject;
		this.animatorParent = animatorParent;
		this.controllerName = controllerName;
		this.multiAimConstraints = new List<MultiAimConstraint>();

		Transform cam = this.parent.transform.parent.Find("Camera");

		if(cam == null)
			this.camera = this.parent.transform.parent.gameObject;
		else
			this.camera = cam.gameObject;
		
		this.armature = this.animatorParent.transform.Find(AnimationLoader.GetArmatureName(controllerName));

		if(parentEyeTrackers == null){
			parentEyeTrackers = GameObject.Find("EyeTrackers").transform;
		}

		GenerateEyeTrackerObject();
	}

	public void Delete(){
		GameObject.Destroy(this.eyeTracker.gameObject);
	}

	public GameObject GetEyeTracker(){return this.eyeTracker.gameObject;}
	public GameObject GetCamera(){return this.camera;}

	public void ChangeState(string state){
		if(currentState == state)
			return;

		for(int i=0; i < this.multiAimConstraints.Count; i++){
			if(this.multiAimData[i].HasState(state))
				this.multiAimConstraints[i].weight = 1f;
			else
				this.multiAimConstraints[i].weight = 0f;
		}
	}

	public void AssignHeadTrackingSource(Transform t){
		WeightedTransformArray wta;
		MultiAimConstraintData data;

		for(int i=0; i < this.multiAimData.Length; i++){
			if(this.multiAimData[i].intensity == 0)
				continue;

			data = this.multiAimConstraints[i].data;
			wta = new WeightedTransformArray();

			wta.Add(new WeightedTransform(this.eyeTracker, 1 - this.multiAimData[i].intensity));
			wta.Add(new WeightedTransform(t, this.multiAimData[i].intensity));
			data.sourceObjects = wta;
			this.multiAimConstraints[i].data = data;
		}

		this.rigBuilder.Build();
	}

	public void Build(){
		if(!AnimationLoader.ContainsRig(this.controllerName))
			return;

		MultiAimConstraint current;
		Rig rig;

		this.proceduralRig = new GameObject();
		this.proceduralRig.name = "Procedural Rig";
		this.proceduralRig.transform.parent = this.animatorParent.transform;
		rig = this.proceduralRig.AddComponent<Rig>();
		this.rigBuilder = this.animatorParent.AddComponent<RigBuilder>();
		this.multiAimData = AnimationLoader.GetRig(this.controllerName);

		for(int i=0; i < this.multiAimData.Length; i++){
			GameObject go = new GameObject();
			go.name = multiAimData[i].rig_name;
			go.transform.parent = this.proceduralRig.transform;
			current = multiAimData[i].BuildConstraint(this.armature, go, this.eyeTracker);
			this.multiAimConstraints.Add(current);
		}

		this.rigBuilder.layers.Add(new RigLayer(rig));
		this.rigBuilder.Build();
	}

	private void GenerateEyeTrackerObject(){
		GameObject go = new GameObject();

		go.name = "Eye Tracker";
		go.transform.parent = parentEyeTrackers;
		go.transform.localPosition = new Vector3(0,0,10);
		go.AddComponent<CameraViewTarget>().SetCamera(this.camera.transform);

		this.eyeTracker = go.transform;
	}
}
