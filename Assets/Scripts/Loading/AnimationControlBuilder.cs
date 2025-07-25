using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Animations;

using Object = UnityEngine.Object;


public class AnimationControlBuilder {
	private Dictionary<string, AnimationControllerSettings> controllerSettings = new Dictionary<string, AnimationControllerSettings>();
	private Dictionary<string, AnimationLayerSettings[]> layerSettings = new Dictionary<string, AnimationLayerSettings[]>();
	private Dictionary<string, AnimationStateSettings[]> stateSettings = new Dictionary<string, AnimationStateSettings[]>();
	private Dictionary<string, AnimationTransitionSettings[]> transitionSettings = new Dictionary<string, AnimationTransitionSettings[]>();

	private Dictionary<string, Dictionary<string, Motion>> animations = new Dictionary<string, Dictionary<string, Motion>>();
	private Dictionary<string, Dictionary<string, int>> layers = new Dictionary<string, Dictionary<string, int>>();
	private Dictionary<string, AnimatorController> controllers = new Dictionary<string, AnimatorController>();

	private static readonly string ANIMATION_FOLDER = "Resources/Animations/";
	private static readonly string ANIMATION_RESFOLDER = "Animations/";
	private static readonly string SAVED_CHARACTER_CONTROLLER_PATH = "Assets/Animations/Character Animations/";

	private static readonly int LAYER_GRAPH_MAX_NODES_IN_ROW = 5;
	private static readonly int LAYER_GRAPH_SPACE_BETWEEN_NODES = 250;
	private static readonly int LAYER_GRAPH_VERTICAL_OFFSET = 60;
	private static readonly int LAYER_GRAPH_STARTING_OFFSET_Y = -400;

	private static readonly Vector3 LAYER_GRAPH_ENTRY_NODE_POS = new Vector3(145, 0, 0);
	private static readonly Vector3 LAYER_GRAPH_ANY_NODE_POS = new Vector3(520, 0, 0);
	private static readonly Vector3 LAYER_GRAPH_EXIT_NODE_POS = new Vector3(885, 0, 0);

	public void Build(){
		LoadControllerSettings();
		LoadLayersSettings();
		LoadStatesSettings();
		LoadTransitionsSettings();

		BuildControllers();
		BuildLayers();
		BuildStates();

		AssetDatabase.SaveAssets();
	}

	private void BuildStates(){
		string controllerName;
		Vector3 graphPosition = Vector3.zero;
		int[] layerIndexes;
		int[] lastPositionInLayer;
		int layerNumber;

		foreach(AnimationControllerSettings acs in this.controllerSettings.Values){
			controllerName = acs.controllerName;
			layerIndexes = CreateArrayOfZeroes(this.layers[controllerName].Count);
			lastPositionInLayer = CreateArrayOfZeroes(this.layers[controllerName].Count);

			// Create all states for the layers
			for(int i=0; i < this.stateSettings[controllerName].Length; i++){
				AnimationStateSettings ass = this.stateSettings[controllerName][i];
				AnimatorState state = ass.Build(this.controllers[controllerName], this.animations[controllerName]);
				layerNumber = this.layers[controllerName][ass.layer];

				graphPosition = AssignGraphPosition(layerIndexes[layerNumber], lastPositionInLayer[layerNumber]);
				layerIndexes[layerNumber]++;
				lastPositionInLayer[layerNumber] = (int)graphPosition.x;

				this.controllers[controllerName].layers[layerNumber].stateMachine.AddState(state, graphPosition);

				if(ass.isDefaultState){
					this.controllers[controllerName].layers[layerNumber].stateMachine.defaultState = state;
				}

				AssetDatabase.AddObjectToAsset(state, this.controllers[controllerName]);
			}

			for(int i=0; i < this.controllers[controllerName].layers.Length; i++){
				this.controllers[controllerName].layers[i].stateMachine.entryPosition = LAYER_GRAPH_ENTRY_NODE_POS;
				this.controllers[controllerName].layers[i].stateMachine.anyStatePosition = LAYER_GRAPH_ANY_NODE_POS;
				this.controllers[controllerName].layers[i].stateMachine.exitPosition = LAYER_GRAPH_EXIT_NODE_POS;
			}
		}
	}

	private Vector3 AssignGraphPosition(int index, int lastX){
		Vector3 outVector = new Vector3(0,0,0);

		outVector.y = LAYER_GRAPH_STARTING_OFFSET_Y + (LAYER_GRAPH_MAX_NODES_IN_ROW - (index/LAYER_GRAPH_MAX_NODES_IN_ROW))*LAYER_GRAPH_VERTICAL_OFFSET;

		if(index % LAYER_GRAPH_MAX_NODES_IN_ROW == 0){
			outVector.x = 0;
		}
		else{
			outVector.x = lastX + LAYER_GRAPH_SPACE_BETWEEN_NODES;
		}

		return outVector;
	}

	private int[] CreateArrayOfZeroes(int size){
		int[] array = new int[size];
		Array.Fill(array, 0);
		return array;
	}

	private void BuildLayers(){
		string controllerName;

		foreach(AnimationControllerSettings acs in this.controllerSettings.Values){
			controllerName = acs.controllerName;
			this.layers.Add(controllerName, new Dictionary<string, int>());
			this.layers[controllerName].Add("Base Layer", 0);

			for(int i=0; i < this.layerSettings[controllerName].Length; i++){
				AnimatorControllerLayer layer = this.layerSettings[controllerName][i].Build();

				this.controllers[controllerName].AddLayer(layer);
				this.layers[controllerName].Add(layer.name, this.layers[controllerName].Count);
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

			LoadAnimationClips(acs);
		}
	}

	private void LoadAnimationClips(AnimationControllerSettings acs){
		this.animations.Add(acs.controllerName, new Dictionary<string, Motion>());

		Object[] allAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(acs.fbxFile);

		AnimationClip clip;

	    foreach(Object asset in allAssets){
	        if(asset is AnimationClip){
	        	clip = (AnimationClip)asset;
	        	
	        	if(acs.Contains(clip.name)){
	        		this.animations[acs.controllerName].Add(SelectAfterPipe(clip.name), clip);
	        	}
	        }
	    }
	}

	private string SelectAfterPipe(string input){
	    int index = input.IndexOf('|');

	    if(index >= 0)
	    	return input.Substring(index + 1);
	    return input;
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
			acs.PostDeserializationSetup();

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

			foreach(AnimationStateSettings stateSettings in ass.data){
				stateSettings.PostDeserializationSetup();
			}

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