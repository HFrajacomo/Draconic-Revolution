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
			"fbxName": "BastardSword",
			"type": "BASE_CHARACTER_BELT_STRAIGHT_RIGHT"
		}	
	],
	"combo_hits": 3,
	"overrides": {
		"data": [
			{
				"state": "T-Pose",
				"clip": "ManArmt_0TPose-Man"
			},
			{
				"state": "Idle",
				"clip": "ManArmt_Idle-Man"
			},
			{
				"state": "Idle Hand",
				"clip": "ManArmt_IdleHand-Man"
			},
			{
				"state": "Attack 1",
				"clip": "ManArmt_Punch1-Normal-Man",
				"direction": "forward",
				"momentum": 3
			}
		]
	}
} 
```

| Field | Type | Description |
|:--:|:--:|:--:|
| attachments | list[obj] | List of attachments that this style have
| fbxName | str | The name of the fbx model in *Resources/ItemModels*
| type | str | The string representation of a AttachmentAnchorPoint
| combo_hits | str | How many attacks are in a combo with this style |
| overrides | obj | Defined the AnimationState overrides |
| data | list[obj] | Wrapper object to load all the overrides |
| state | str | Name of the AnimationState that will receive this animation override |
| clip | str | The name of the animation clip, as loaded in Unity |
| direction | str | (Optional) The direction in which a character will get added momentum towards. Only "forward" is available |
| momentum | float | (Optional) The force in which the character will get dislocate |