#if UNITY_EDITOR

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Animations;

using Object = UnityEngine.Object;


public static class AnimationControlBuilder {
	private static Dictionary<string, AnimationControllerSettings> controllerSettings = new Dictionary<string, AnimationControllerSettings>();
	private static Dictionary<string, AnimationLayerSettings[]> layerSettings = new Dictionary<string, AnimationLayerSettings[]>();
	private static Dictionary<string, AnimationStateSettings[]> stateSettings = new Dictionary<string, AnimationStateSettings[]>();
	private static Dictionary<string, AnimationTransitionSettings[]> transitionSettings = new Dictionary<string, AnimationTransitionSettings[]>();

	private static Dictionary<string, string> controllerPath = new Dictionary<string, string>();

	private static Dictionary<string, Dictionary<string, Motion>> animations = new Dictionary<string, Dictionary<string, Motion>>();
	private static Dictionary<string, Dictionary<string, int>> layers = new Dictionary<string, Dictionary<string, int>>();
	private static Dictionary<string, AnimatorController> controllers = new Dictionary<string, AnimatorController>();
	private static Dictionary<StateLayerKey, AnimatorState> states = new Dictionary<StateLayerKey, AnimatorState>();

	private static AnimatorControllerParameter PLAYER_PARAMETER = new AnimatorControllerParameter{
		name = "ISPLAYER",
		type = AnimatorControllerParameterType.Bool
	};

	private static readonly string ANIMATION_FOLDER = "Resources/Animations/";
	private static readonly string ANIMATION_RESFOLDER = "Animations/";
	private static readonly string SERIALIZED_CONTROLLERS_PATH = "/Resources/SerializedData/AnimatorControllers.json";
	private static readonly string SAVED_CHARACTER_CONTROLLER_PATH = "Assets/Resources/AnimationControllers/Character Animations/";
	private static readonly string SAVED_CHARACTER_CONTROLLER_RESPATH = "AnimationControllers/Character Animations/";
	private static readonly string ANIMATION_CLIPS_PATH = "Assets/Resources/AnimationClips/";

	private static readonly int LAYER_GRAPH_MAX_NODES_IN_ROW = 5;
	private static readonly int LAYER_GRAPH_SPACE_BETWEEN_NODES = 250;
	private static readonly int LAYER_GRAPH_VERTICAL_OFFSET = 60;
	private static readonly int LAYER_GRAPH_STARTING_OFFSET_Y = -400;

	private static readonly Vector3 LAYER_GRAPH_ENTRY_NODE_POS = new Vector3(145, 0, 0);
	private static readonly Vector3 LAYER_GRAPH_ANY_NODE_POS = new Vector3(520, 0, 0);
	private static readonly Vector3 LAYER_GRAPH_EXIT_NODE_POS = new Vector3(885, 0, 0);

	[MenuItem("Editor Tools/Animations/Build Controllers")]
	private static void Build(){
		AssetDatabase.Refresh();
		Cleanup();
		DeleteClips();
		LoadControllerSettings();
		LoadLayersSettings();
		LoadStatesSettings();
		LoadTransitionsSettings();

		BuildControllers();
		BuildLayers();
		BuildStates();
		BuildTransitions();
		BuildStateMachineBehaviours();

		SaveControllerPath();

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		Cleanup();
		Debug.Log("Created Controllers");
	}

	private static void Cleanup(){
			controllerSettings.Clear();
			controllerPath.Clear();
			layerSettings.Clear();
			stateSettings.Clear();
			transitionSettings.Clear();
			controllers.Clear();
			states.Clear();

			foreach(Dictionary<string, Motion> anim in animations.Values){anim.Clear();}
			animations.Clear();
			foreach(Dictionary<string, int> lay in layers.Values){lay.Clear();}
			layers.Clear();
	}

	private static void DeleteClips(){
		AssetDatabase.DeleteAsset("Assets/Resources/AnimationClips");
		AssetDatabase.CreateFolder("Assets/Resources", "AnimationClips");
		AssetDatabase.Refresh();
	}

	private static void SaveControllerPath(){
		ValuePair<string, string>[] pairArray = new ValuePair<string, string>[controllerPath.Count];
		int i=0;

		foreach(string s in controllerPath.Keys){
			pairArray[i].key = s;
			pairArray[i].value = controllerPath[s];
			i++;
		}

		Wrapper<ValuePair<string, string>> wrapper = new Wrapper<ValuePair<string, string>>(pairArray);

		string serializedWrapper = JsonUtility.ToJson(wrapper);

		File.WriteAllText($"{Application.dataPath}{SERIALIZED_CONTROLLERS_PATH}", serializedWrapper);
	}

	private static void BuildTransitions(){
		string controllerName;
		int layerNumber;

		foreach(AnimationControllerSettings acs in controllerSettings.Values){
			controllerName = acs.controllerName;

			for(int i=0; i < transitionSettings[controllerName].Length; i++){
				AnimationTransitionSettings ats = transitionSettings[controllerName][i];
				AnimatorState destinationState = states[new StateLayerKey(controllerName, ats.layer, ats.destinationState)];
				AnimatorStateTransition finalTransition;

				layerNumber = layers[controllerName][ats.layer];

				// If transition is linked to Any state
				if(ats.IsTransitionFromAny()){
					finalTransition = controllers[controllerName].layers[layerNumber].stateMachine.AddAnyStateTransition(destinationState);
				}
				else{
					AnimatorState sourceState = states[new StateLayerKey(controllerName, ats.layer, ats.sourceState)];
					finalTransition = sourceState.AddTransition(destinationState);
				}


				ats.Copy(controllers[controllerName], finalTransition);
			}
		}
	}

	private static void BuildStates(){
		string controllerName;
		Vector3 graphPosition = Vector3.zero;
		int[] layerIndexes;
		int[] lastPositionInLayer;
		int layerNumber;

		animations.Add("BASE_Default", new Dictionary<string, Motion>());

		foreach(AnimationControllerSettings acs in controllerSettings.Values){
			controllerName = acs.controllerName;
			layerIndexes = CreateArrayOfZeroes(layers[controllerName].Count);
			lastPositionInLayer = CreateArrayOfZeroes(layers[controllerName].Count);

			// Create all states for the layers
			for(int i=0; i < stateSettings[controllerName].Length; i++){
				AnimationStateSettings ass = stateSettings[controllerName][i];
				AnimatorState state = ass.Build(controllers[controllerName], animations["BASE_Default"], ANIMATION_CLIPS_PATH);
				layerNumber = layers[controllerName][ass.layer];

				graphPosition = AssignGraphPosition(layerIndexes[layerNumber], lastPositionInLayer[layerNumber]);
				layerIndexes[layerNumber]++;
				lastPositionInLayer[layerNumber] = (int)graphPosition.x;

				controllers[controllerName].layers[layerNumber].stateMachine.AddState(state, graphPosition);

				if(ass.isDefaultState){
					controllers[controllerName].layers[layerNumber].stateMachine.defaultState = state;
				}

				states.Add(new StateLayerKey(controllerName, ass.layer, state.name), state);
				AssetDatabase.AddObjectToAsset(state, controllers[controllerName].layers[layerNumber].stateMachine);
			}

			for(int i=0; i < controllers[controllerName].layers.Length; i++){
				controllers[controllerName].layers[i].stateMachine.entryPosition = LAYER_GRAPH_ENTRY_NODE_POS;
				controllers[controllerName].layers[i].stateMachine.anyStatePosition = LAYER_GRAPH_ANY_NODE_POS;
				controllers[controllerName].layers[i].stateMachine.exitPosition = LAYER_GRAPH_EXIT_NODE_POS;
			}

		}
	}

	private static void BuildStateMachineBehaviours(){
		foreach(AnimatorController control in controllers.Values){
			foreach(AnimatorControllerLayer layer in control.layers){
				foreach(ChildAnimatorState cState in layer.stateMachine.states){
					cState.state.AddStateMachineBehaviour<AnimatorStateMessageCallback>();
				}
			}

			EditorUtility.SetDirty(control);
		}
	}

	private static Vector3 AssignGraphPosition(int index, int lastX){
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

	private static int[] CreateArrayOfZeroes(int size){
		int[] array = new int[size];
		Array.Fill(array, 0);
		return array;
	}

	private static void BuildLayers(){
		string controllerName;

		foreach(AnimationControllerSettings acs in controllerSettings.Values){
			controllerName = acs.controllerName;
			layers.Add(controllerName, new Dictionary<string, int>());
			layers[controllerName].Add("Base Layer", 0);

			for(int i=0; i < layerSettings[controllerName].Length; i++){
				AnimatorControllerLayer layer = layerSettings[controllerName][i].Build();

				controllers[controllerName].AddLayer(layer);
				layers[controllerName].Add(layer.name, layers[controllerName].Count);
				AssetDatabase.AddObjectToAsset(layer.stateMachine, controllers[controllerName]);
				AssetDatabase.AddObjectToAsset(layer.avatarMask, controllers[controllerName]);
			}
		}
	}

	private static void BuildControllers(){
		AnimatorController controller;
		string path;

		foreach(AnimationControllerSettings acs in controllerSettings.Values){
			path = $"{SAVED_CHARACTER_CONTROLLER_PATH}{acs.controllerName}";

			if(File.Exists($"{path}.controller")){
				AssetDatabase.DeleteAsset($"{path}.controller");
				controller = AnimatorController.CreateAnimatorControllerAtPath($"{path}.controller");
			}
			else{
				controller = AnimatorController.CreateAnimatorControllerAtPath($"{path}.controller");
			}
			
			controller.AddParameter(PLAYER_PARAMETER);
			controllers.Add(acs.controllerName, controller);
			controllerPath.Add(acs.controllerName, $"{SAVED_CHARACTER_CONTROLLER_RESPATH}{acs.controllerName}");

			LoadAnimationClips(acs);
		}
	}

	private static void LoadAnimationClips(AnimationControllerSettings acs){
		animations.Add(acs.controllerName, new Dictionary<string, Motion>());

		Object[] allAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(acs.fbxFile);

		AnimationClip clip, auxClip;

		foreach(Object asset in allAssets){
			if(asset is AnimationClip){
				clip = (AnimationClip)asset;
				
				if(acs.Contains(clip.name)){
					AnimationClip newClip = new AnimationClip();
					EditorUtility.CopySerialized(clip, newClip);
					AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(newClip);
					settings.loopTime = acs.GetLoop(SelectAfterPipe(clip.name));
					AnimationUtility.SetAnimationClipSettings(newClip, settings);

					string clipFilename = newClip.name.Replace('|', '_');

					auxClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ANIMATION_CLIPS_PATH}{clipFilename}.anim");

					if(auxClip == null){
						newClip.name = clipFilename;
						AssetDatabase.CreateAsset(newClip, $"{ANIMATION_CLIPS_PATH}{clipFilename}.anim");
						animations[acs.controllerName].Add(SelectAfterUnderscore(newClip.name), newClip);
					}
					else{
						animations[acs.controllerName].Add(SelectAfterUnderscore(auxClip.name), auxClip);
					}
				}
			}
		}
	}

	private static string SelectAfterPipe(string input){
		int index = input.IndexOf('|');

		if(index >= 0)
			return input.Substring(index + 1);
		return input;
	}

	private static string SelectAfterUnderscore(string input){
		int index = input.IndexOf('_');

		if(index >= 0)
			return input.Substring(index + 1);
		return input;
	}

	private static void LoadControllerSettings(){
		string[] folders = FetchAnimationControllerFolders();
		string controllerName;
		AnimationControllerSettings acs;

		for(int i=0; i < folders.Length; i++){
			controllerName = new DirectoryInfo(folders[i]).Name;

			TextAsset controllerJson = Resources.Load<TextAsset>($"{ANIMATION_RESFOLDER}{controllerName}/{controllerName}");

			if(controllerJson == null){
				Debug.Log($"Couldn't locate the ControllerSettings: {ANIMATION_RESFOLDER}{controllerName}/{controllerName} while building AnimationControllers");
				EditorApplication.isPlaying = false;
			}

			acs = JsonUtility.FromJson<AnimationControllerSettings>(JsonFormatter.RemoveComments(controllerJson.text));
			acs.PostDeserializationSetup();

			controllerSettings.Add(controllerName, acs);
		}
	}

	private static void LoadLayersSettings(){
		Wrapper<AnimationLayerSettings> als;

		foreach(AnimationControllerSettings acs in controllerSettings.Values){
			TextAsset layerJson = Resources.Load<TextAsset>(acs.layersFile);

			if(layerJson == null){
				Debug.Log($"Couldn't locate the LayerSettings: {acs.layersFile} while building AnimationControllers");
				EditorApplication.isPlaying = false;
			}

			als = JsonUtility.FromJson<Wrapper<AnimationLayerSettings>>(JsonFormatter.RemoveComments(layerJson.text));

			foreach(AnimationLayerSettings layerSettings in als.data){
				layerSettings.PostDeserializationSetup();
			}

			layerSettings.Add(acs.controllerName, als.data);
		}
	}

	private static void LoadStatesSettings(){
		Wrapper<AnimationStateSettings> ass;

		foreach(AnimationControllerSettings acs in controllerSettings.Values){
			TextAsset statesJson = Resources.Load<TextAsset>(acs.statesFile);

			if(statesJson == null){
				Debug.Log($"Couldn't locate the StateSettings: {acs.statesFile} while building AnimationControllers");
				EditorApplication.isPlaying = false;
			}

			ass = JsonUtility.FromJson<Wrapper<AnimationStateSettings>>(JsonFormatter.RemoveComments(statesJson.text));

			foreach(AnimationStateSettings stateSettings in ass.data){
				stateSettings.PostDeserializationSetup();
			}

			stateSettings.Add(acs.controllerName, ass.data);
		}
	}

	private static void LoadTransitionsSettings(){
		Wrapper<AnimationTransitionSettings> ats = null;

		foreach(AnimationControllerSettings acs in controllerSettings.Values){
			TextAsset transitionsJson = Resources.Load<TextAsset>(acs.transitionFile);

			if(transitionsJson == null){
				Debug.LogError($"Couldn't locate the TransitionSettings: {acs.transitionFile} while building AnimationControllers");
			}

			try{
				ats = JsonUtility.FromJson<Wrapper<AnimationTransitionSettings>>(JsonFormatter.RemoveComments(transitionsJson.text));
			}
			catch(ArgumentException ex){
				Debug.LogError($"Failed to load controller: {acs.controllerName}. {ex}");
				throw;
			}

			foreach(AnimationTransitionSettings transitionSettings in ats.data){
				transitionSettings.PostDeserializationSetup();
			}

			transitionSettings.Add(acs.controllerName, ats.data);
		}
	}

	private static string[] FetchAnimationControllerFolders(){
		return Directory.GetDirectories($"{Application.dataPath}/{ANIMATION_FOLDER}");
	}
}
#endif