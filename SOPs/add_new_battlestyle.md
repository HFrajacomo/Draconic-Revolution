# Add Battle Styles

Battle Styles in Draconic Revolution are used to override animations in an AnimatorController.
That way, depending on what style a character has, certain Animation States are gonna take up a different animation clip.

## Process

To add a new battlestyle, the following steps need to be done:

1. Go to **Assets/Resources/BattleStyles**
2. Create a new json file with the name of the battle style you want to create. It's important to note that different models and animations may take a different battle style (like male and female versions)

The Json structure is as follows:

```json
{
	"attachments": [
		{
			"fbxName": "Pickaxe",
			"type": "BASE_CHARACTER_BELT_STRAIGHT_RIGHT"
		}	
	],
	"firstPerson": true,
	"combo_hits": 3,
	"overrides": {
		"data": [
			{
				"state": "Idle Weapon",
				"clip": "CharacterArmature_Idle - Pickaxe -FP"
			},
			{
				"state": "Moving Forward",
				"clip": "CharacterArmature_Run Forward - Pickaxe -FP"
			},
			{
				"state": "Weapon Sheathe",
				"clip": "CharacterArmature_Sheathe - Pickaxe -FP",
				"events": [
					{
						"type": "AnimatorSetBehaviour",
						"json": "{\"normalizedTime\": 1.0, \"fieldName\": \"IsSheathing\", \"type\": \"bool\", \"boolValue\": false}"
					}
				]
			},
			{
				"state": "Attack 1",
				"clip": "CharacterArmature_Attack X - Pickaxe -FP"
			},
		]
	}
}
```

| Field | Type | Description |
|:--:|:--:|:--:|
| attachments | list[obj] | List of attachments that this style have |
| fbxName | str | The name of the fbx model in *Resources/ItemModels* |
| attachments.type | str | The string representation of a AttachmentAnchorPoint |
| firstPerson | bool | (Optional) Whether the current battlestyle is for an FP or TP Animator |
| combo_hits | str | How many attacks are in a combo with this style |
| overrides | obj | Defined the AnimationState overrides |
| data | list[obj] | Wrapper object to load all the overrides |
| state | str | Name of the AnimationState that will receive this animation override |
| clip | str | The name of the animation clip, as loaded in Unity |
| events | list[obj] | List of AnimationBehaviours to trigger for each clip |
| events.type | str | The name of an AnimationBehaviour |
| json | str | A json-string defining the parameters of the AnimationBehaviour |