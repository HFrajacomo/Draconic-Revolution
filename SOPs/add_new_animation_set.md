# [SOP] Adding New Animations to Models

## Creating the animation Folder

1. Go over to Assets/Resources/Animations/
2. Create a folder with the desired name of your Character Controller

## Overview

The files needed to be created are as following:

1. **armature.json**
2. **<character_controller_name>.json**
3. **layers.json**
4. **states.json**
5. **mappings.json**
6. **transitions.json**
7. **rigs.json**
8. **anchors.json**


### Armature.json

This file is not a json, per se. In this file, you must insert the name of the armature used in Blender for these animations, and nothing more

### <character_controller_name>.json

The following json format is requested, considering the character controller name is "test_character":

```json
{
	"controllerName": "test_character",
	"armatureName": "armature_name",
	"fbxFile": "Assets/Resources/CharacterModels/test_character.fbx",
	"layersFile": "Animations/test_character/layers",
	"statesFile": "Animations/test_character/states",
	"transitionFile": "Animations/test_character/transitions",
	"animations": [
		{"name": "AnimationClip_name", "loop": true},
		{"name": "AnimationClip2_name", "loop": false}
	]
}
```

| Field | Type | Description |
|:--:|:--:|:--:|
| controllerName | str | The filepath of this file, which should also be the same as the folder name |
| armatureName | str | The name of the armature, as defined in Blender |
| fbxFile | str | The FBX file containing the armature and animations |
| layersFile | str | The filepath of the animation layers file (to be created in following sections) |
| statesFile | str | The filepath of the animation states file (to be created in following sections) |
| transitionFile | str | The filepath of the state transition file (to be created in the following sections) |
| animations | {name: str, loop: bool} | An object that contains AnimationClip name, as defined in Blender and if the clip should loop on itself or should be one-shot |
-----

### layers.json

This file uses the "data" wrapper that contains AnimationLayerSettings objects.
Animation Layers are used to separate body parts that you want to trigger a different animation at the same time as another, in a different layer.

```json
{
	"data": [
		{
			"name": "Bottom",
			"defaultWeight": 1,
			"blendingMode": 0,
			"avatarMask": [
				"TestArmature/Hips",
				"TestArmature/Hips/UpperLeg.L",
				"TestArmature/Hips/UpperLeg.R",
				"TestArmature/Hips/UpperLeg.L/LowerLeg.L",
				"TestArmature/Hips/UpperLeg.R/LowerLeg.R",
				"TestArmature/Hips/UpperLeg.L/LowerLeg.L/Foot.L",
				"TestArmature/Hips/UpperLeg.R/LowerLeg.R/Foot.R",
				"TestArmature/IK_KneePole.L",
				"TestArmature/IK_KneePole.R",
				"TestArmature/IK_KneeTarget.L",
				"TestArmature/IK_KneeTarget.R"
			]
		},
		...
	]
}
```

| Field | Type | Description |
| :--: | :--: | :--: |
| name | str | The name of the layer you want to create* |
| defaultWeight | float | The default weight that a layer has on the whole AnimationController |
| blendingMode | int | 0 = Override Blending; 1 = Additive Blending |
| avatarMask | list[str] | A list of the full bone hierarchy that you want to be affected by the layer

\* The first layer will always be the default BaseLayer that will contain all the bones. This layer doesn't need to be defined

### states.json

This file uses the "data" wrapper that contains AnimationStateSettings objects.
Animation States are used to hold an animation clip and provide possibilities of transitions, Blendtree blending and layered control.

```json
{
	"data": [
		{
			"name": "Idle",
			"layer": "",
			"isBlendTree": false,
			"isDefaultState": true
		},
		{
			"name": "Idle",
			"layer": "Bottom",
			"isBlendTree": false,
			"isDefaultState": true
		},
		{
			"name": "On Air",
			"layer": "",
			"isDefaultState": false,
			"isBlendTree": true,
			"blendTree": {
				"minThreshold": -1,
				"maxThreshold": 1,
				"blendParameter": {
					"parameterName": "Gravity",
					"parameterType": "float"
				}
			}
		},
		...
	]
}
```

| Field | Type | Description |
| :--: | :--: | :--: |
| name | str | The name of the state you want to define |
| layer | str | The name of the layer that this state should be created* |
| isBlendTree | bool | True if this state blends into 2 animation clips. Example: Blending a walking animation into a running one in the same state, based on a parameter |
| isDefaultState | bool | True if this is the default state in this layer. Each layer should only have one default state |
| blendTree** | {float, float, {str, str}} | Defines the BlendTree object |
| minThreshold | float | Minimum blending value |
| maxThreshold | float | Maximum blending value |
| blendParameter | {str, str} | Defines options of the Blending parameter for this BlendTree |
| parameterName | str | Define the name of a AnimationController parameter to be used to blend this BlendTree |
| parameterType | str | Defines the type of the parameter; "int", "float", "bool" or "trigger" are allowed only |

\* If you want a state to be created in the default layer, leave this field empty
\*\* This attribute should only be defined if 'isBlendTree' is set to true

### mappings.json

Inside the code, we don't have functions to play a given state in a given layer. We only choose to play the state, and the mappings system will figure out in which layer it should play the animation and how the play request will affect other layers.

This file uses the "data" wrapper that contains AnimationStateMapping objects.

```json
{
	"data": [
		{
			"state": "Idle",
			"layers": ["", "Bottom"],
			"priority": 900
		},
		{
			"state": "Empty",
			"layers": ["", "Bottom", "Full Torso", "Right Arm", "Left Arm", "Head", "Pre-Cinematic Add", "Cinematic Override", "Post-Cinematic Add"],
			"priority": 1000
		},
		{
			"state": "Attack 1",
			"layers": ["", "Full Torso"],
			"priority": 1,
			"stopLayer": ["Bottom"]
		},
		...
	]
}
```

| Field | Type | Description |
| :--: | :--: | :--: |
| state | str | The name of the state |
| layers | list[str] | The layers in which this state can be played in, in order of preference |
| priority | int | The priority in which this state will play over others in the same layer. A lower priority score means this state is more important. States with lower priority will be forced into a layer, while the state previously being played in that layer will resume in another registered layer, given its importance order | 
| stopLayer | list[str] | (Optional) Defines all the layers that will stop their animation and back to their default state as this state is played |

### transitions.json

Animation Transitions are transitions that happen automatically between states, without the user manually triggering another state. These transitions happen based on parameter conditions inside the Controller.

This file uses the "data" wrapper that contains AnimationTransitionSettings objects.

```json
{
	"data": [
		{
			"layer": "",
			"sourceState": "On Air",
			"destinationState": "Idle",
			"hasExitTime": false,
			"exitTime": 0.5,
			"duration": 0.1,
			"offset": 0,
			"canTransitionToSelf": false,
			"interruptionSource": "source",
		    "conditions": [
			    {
			        "mode": "if",
			        "parameter": "IsGrounded",
			        "parameterType": "bool"
			    },
			    {
			    	"mode": "if",
			    	"parameter": "ISPLAYER",
			    	"parameterType": "bool"
			    },
			    {
			        "mode": "equals",
			        "threshold": 0,
			        "parameter": "Attack_Combo",
			        "parameterType": "int"
			    },
			]
		},
		...
	]
}
```

| Field | Type | Description |
| :--: | :--: | :--: |
| layer | str | The layer in which this state transition happens |
| sourceState | str | The state that was playing when the transition happens |
| destinationState | str | The state that will be playing when the transition ends |
| hasExitTime | bool | If the transition happens once the source state reaches a certain point in its duration |
| duration | float | The amount of time that Animator will blend between source and dest states until the dest state is played |
| offset | float | The normalized time in which the states blending starts |
| canTransitionToSelf | bool | Whether this transitions is a transition from source state back to source state again |
| interruptionSource | str | While in the middle of a transition, decide how the transition can be interrupted for another transition to start. "none", "source", "destination", "source then destination" and "destination then source" are the only valid values
| conditions | list[obj] | The conditions in which the transitions are triggered |
| mode | str | "if" for bool values being true; "if not" for bool values being false; "greater", "less", "equals", "not equals" for comparisons with the threshold value |
| threshold | int | (Optional) only available if the 'mode' field is a numeric comparison |
| parameter | str | Define the name of the Animator parameter used to check conditions |
| parameterType | str | "int", "float", "bool" and "trigger" are the only valid options |


### rigs.json

This file defines how procedural animations work for this model.
This file uses the "data" wrapper that contains AnimationTransitionSettings objects.

```json
{
	"data": [
		{
			"rig_name": "spine_tracking",
			"constrained_bone": "Hips/LowerTorso/UpperTorso",
			"active_states": ["normal", "attack"],
			"constrainedXAxis": true,
			"constrainedYAxis": false,
			"constrainedZAxis": false,
			"maintain_offset": false,
			"intensity": 0.5,
			"limits": {
				"x": 0,
				"y": 50
			},
			"offset": {
				"x": 0,
				"y": 0,
				"z": 0
			}
		},
		{
			"rig_name": "head_tracking",
			"constrained_bone": "Hips/LowerTorso/UpperTorso/Neck/Head",
			"active_states": ["normal", "attack"],
			"constrainedXAxis": true,
			"constrainedYAxis": true,
			"constrainedZAxis": false,
			"maintain_offset": false,
			"intensity": 0,
			"limits": {
				"x": -60,
				"y": 60
			},
			"offset": {
				"x": 0,
				"y": 0,
				"z": 0
			}
		}
	]
}
```

| Field | Type | Description |
| :--: | :--: | :--: |
| rig_name | str | The name of the rig object to be created |
| constrained_bone | str | The bone affected by the procedural animation |
| active_states | list[str] | (Unused at the moment) Define in which states the Rig Controller operates on |
| constrainedXAxis | bool | Whether the X rotation axis has constraints |
| constrainedYAxis | bool | Whether the Y rotation axis has constraints |
| constrainedZAxis | bool | Whether the Z rotation axis has constraints | 
| maintain_offset | bool | Whether the original rotation of the object is taken into consideration before applying procedural rotation |
| intensity | float | The inverted intensity this rig on the constrained bone |
| limits | {float, float} |  The maximum rotation allowed in the constrained bones |
| offset | {float, float, float} | Addition rotation apart from the procedural rotation |

### Anchors

This file defines the armature bones that are anchor points for attachment objects.
This file uses the "data" wrapper that contains BoneAnchorPoint objects.

```json
{
	"data": [
		{"type": "BASE_CHARACTER_BELT_CURVED_RIGHT", "bonePath": "CharacterArmature/Hips/LowerTorso/UpperTorso/BackCurved.R"},
		{"type": "BASE_CHARACTER_BELT_CURVED_LEFT", "bonePath": "CharacterArmature/Hips/LowerTorso/UpperTorso/BackCurved.L"},
		{"type": "BASE_CHARACTER_BELT_STRAIGHT_RIGHT", "bonePath": "CharacterArmature/Hips/LowerTorso/UpperTorso/BackStraight.R"},
		{"type": "BASE_CHARACTER_BELT_STRAIGHT_LEFT", "bonePath": "CharacterArmature/Hips/LowerTorso/UpperTorso/BackStraight.L"},
		{"type": "BASE_CHARACTER_BACK_CURVED_RIGHT", "bonePath": "CharacterArmature/Hips/LowerTorso/UpperTorso/BackCurved.R"},
		{"type": "BASE_CHARACTER_BACK_CURVED_LEFT", "bonePath": "CharacterArmature/Hips/LowerTorso/UpperTorso/BackCurved.L"},
		{"type": "BASE_CHARACTER_BACK_STRAIGHT", "bonePath": "CharacterArmature/Hips/LowerTorso/UpperTorso/BackStraight"},
		{"type": "BASE_CHARACTER_HAND_RIGHT", "bonePath": "CharacterArmature/Hips/LowerTorso/UpperTorso/Shoulder.R/UpperArm.R/LowerArm.R/Item.R"},
		{"type": "BASE_CHARACTER_HAND_LEFT", "bonePath": "CharacterArmature/Hips/LowerTorso/UpperTorso/Shoulder.L/UpperArm.L/LowerArm.L/Item.L"}
	]
}
```

| Field | Type | Description |
| :--: | :--: | :--: |
| type | BoneAnchorType | An enum that defines an entry point for attachment objects |
| bonePath | str | The path to the bone |

### First-Person

if the model and animations you are creating have a first person model too, the method to include it is the same. The only difference is that, when creating your folder in **Assets/Resources/Animations/**, use the same name and add the suffix **"_FP"** as the name of the controller.

Example: If the Third Person controller is called "TestCharacter", then the First Person version is "TestCharacter_FP". This automatically binds this controller to the other one.

After that, the entire process is the same.

### Auto-Build Controllers

In order to transform all the data provided into ready-to-use Animation Controllers linked to our Custom Animation and Mappings system, you should hop on the Unity Editor, and go to **Editor Tools > Animations > Build Controllers**.

This Editor script will generate all the Controllers and these will automatically be imported by the *AnimationLoader* in the loading phase of runtime