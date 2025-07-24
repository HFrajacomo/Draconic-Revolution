using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Animations;

public class AnimationControlBuilder {
	private Dictionary<string, AnimationControllerSettings> controllerSettings = new Dictionary<string, AnimationControllerSettings>();
	private Dictionary<string, AnimationLayerSettings[]> layerSettings = new Dictionary<string, AnimationLayerSettings[]>();
	private Dictionary<string, AnimationStateSettings[]> stateSettings = new Dictionary<string, AnimationStateSettings[]>();
	private Dictionary<string, AnimationTransitionSettings[]> transitionSettings = new Dictionary<string, AnimationTransitionSettings[]>();

	private Dictionary<string, AnimatorController> controllers = new Dictionary<string, AnimatorController>();

	private static readonly string ANIMATION_FOLDER = "Resources/Animations/";
	private static readonly string ANIMATION_RESFOLDER = "Animations/";
	private static readonly string SAVED_CHARACTER_CONTROLLER_PATH = "Assets/Animations/Character Animations/";

	public void Build(){
		LoadControllerSettings();
		LoadLayersSettings();
		LoadStatesSettings();
		LoadTransitionsSettings();

		BuildControllers();
		BuildLayers();
		AssetDatabase.SaveAssets();
	}


	private void BuildLayers(){
		string controllerName;

		foreach(AnimationControllerSettings acs in this.controllerSettings.Values){
			controllerName = acs.controllerName;

			for(int i=0; i < this.layerSettings[controllerName].Length; i++){
				AnimatorControllerLayer layer = this.layerSettings[controllerName][i].Build();

				this.controllers[controllerName].AddLayer(layer);
				AssetDatabase.AddObjectToAsset(layer.stateMachine, this.controllers[controllerName]);
				AssetDatabase.AddObjectToAsset(layer.avatarMask, this.controllers[controllerName]);
			}
		}
	}

	private void BuildControllers(){
		AnimatorController controller;
		string path;

		foreach(AnimationControllerSettings acs in this.controllerSettings.Values){
			path = $"{SAVED_CHARACTER_CONTROLLER_PATH}{acs.controllerName}.controller";

			if(File.Exists(path))
				File.Delete(path);

			controller = AnimatorController.CreateAnimatorControllerAtPath(path);
			this.controllers.Add(acs.controllerName, controller);
		}
	}

	private void LoadControllerSettings(){
		string[] folders = FetchAnimationControllerSettings();
		string controllerName;
		AnimationControllerSettings acs;

		for(int i=0; i < folders.Length; i++){
			controllerName = new DirectoryInfo(folders[i]).Name;

			TextAsset controllerJson = Resources.Load<TextAsset>($"{ANIMATION_RESFOLDER}{controllerName}/{controllerName}");

			if(controllerJson == null){
				Debug.Log($"Couldn't locate the ControllerSettings: {ANIMATION_RESFOLDER}{controllerName}/{controllerName} while building AnimationControllers");
				EditorApplication.isPlaying = false;
			}

			acs = JsonUtility.FromJson<AnimationControllerSettings>(controllerJson.text);

			this.controllerSettings.Add(controllerName, acs);
		}
	}

	private void LoadLayersSettings(){
		Wrapper<AnimationLayerSettings> als;

		foreach(AnimationControllerSettings acs in this.controllerSettings.Values){
			TextAsset layerJson = Resources.Load<TextAsset>(acs.layersFile);

			if(layerJson == null){
				Debug.Log($"Couldn't locate the LayerSettings: {acs.layersFile} while building AnimationControllers");
				EditorApplication.isPlaying = false;
			}

			als = JsonUtility.FromJson<Wrapper<AnimationLayerSettings>>(layerJson.text);

			foreach(AnimationLayerSettings layerSettings in als.data){
				layerSettings.PostDeserializationSetup();
			}

			this.layerSettings.Add(acs.controllerName, als.data);
		}
	}

	private void LoadStatesSettings(){
		Wrapper<AnimationStateSettings> ass;

		foreach(AnimationControllerSettings acs in this.controllerSettings.Values){
			TextAsset statesJson = Resources.Load<TextAsset>(acs.statesFile);

			if(statesJson == null){
				Debug.Log($"Couldn't locate the StateSettings: {acs.statesFile} while building AnimationControllers");
				EditorApplication.isPlaying = false;
			}

			ass = JsonUtility.FromJson<Wrapper<AnimationStateSettings>>(statesJson.text);

			this.stateSettings.Add(acs.controllerName, ass.data);
		}
	}

	private void LoadTransitionsSettings(){
		Wrapper<AnimationTransitionSettings> ats;

		foreach(AnimationControllerSettings acs in this.controllerSettings.Values){
			TextAsset transitionsJson = Resources.Load<TextAsset>(acs.transitionFile);

			if(transitionsJson == null){
				Debug.Log($"Couldn't locate the TransitionSettings: {acs.transitionFile} while building AnimationControllers");
				EditorApplication.isPlaying = false;
			}

			ats = JsonUtility.FromJson<Wrapper<AnimationTransitionSettings>>(transitionsJson.text);

			this.transitionSettings.Add(acs.controllerName, ats.data);
		}
	}

	private string[] FetchAnimationControllerSettings(){
		return Directory.GetDirectories($"{Application.dataPath}/{ANIMATION_FOLDER}");
	}
}