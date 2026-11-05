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
| allowsCarry | bool | (Optional) Whether a Carry Style can be layered on top of this style when a SubWeapon is equipped in the off-hand. Only set it on Third Person styles of single, one-handed weapons. First Person styles follow their Third Person counterpart |
| overrides | obj | Defined the AnimationState overrides |
| data | list[obj] | Wrapper object to load all the overrides |
| state | str | Name of the AnimationState that will receive this animation override |
| clip | str | The name of the animation clip, as loaded in Unity |
| events | list[obj] | List of AnimationBehaviours to trigger for each clip |
| events.type | str | The name of an AnimationBehaviour |
| json | str | A json-string defining the parameters of the AnimationBehaviour |

# Add Carry Styles

Carry Styles define how a SubWeapon (Torch, Book, Lantern...) is held in the off-hand on top of another BattleStyle, without having to create a BattleStyle for every combination.
They only animate the **Left Arm** layer through the following Animator states, which are played together with the main style's Sheathe/Unsheathe:

| State | Loop | Description |
|:--:|:--:|:--:|
| Carry Draw | no | Left hand takes the item from its stowed anchor and brings it to the hold pose |
| Carry Hold | yes | The item is held while the kit is drawn |
| Carry Stow | no | Left hand puts the item back into its stowed anchor |
| Carry Run | yes | (First Person only) The item is held while running. Played instead of `Carry Hold` while the FP `Run` parameter is set. If omitted, the `Carry Hold` clip is used |

## Process

1. Go to **Assets/Resources/CarryStyles**
2. Create a json file for the Third Person version and another one with the same name and the "-FP" suffix for the First Person version
3. Reference the Third Person name in the `carryStyle` field of every SubWeapon that should use it. Many items can share the same Carry Style

```json
{
	"attachments": [
		{
			"fbxName": "Torch",
			"type": "BASE_CHARACTER_BELT_STRAIGHT_LEFT"
		}
	],
	"overrides": {
		"data": [
			{
				"state": "Carry Draw",
				"clip": "CharacterArmature_Carry Draw - Torch",
				"events": [
					{
						"type": "AN_SwitchAttachmentAnchorBehaviour",
						"json": "{\"normalizedTime\": 0.25, \"originAnchor\": \"BASE_CHARACTER_BELT_STRAIGHT_LEFT\", \"targetAnchor\": \"BASE_CHARACTER_HAND_LEFT\"}"
					}
				]
			},
			{
				"state": "Carry Hold",
				"clip": "CharacterArmature_Carry Hold - Torch"
			},
			{
				"state": "Carry Stow",
				"clip": "CharacterArmature_Carry Stow - Torch",
				"events": [
					{
						"type": "AN_SwitchAttachmentAnchorBehaviour",
						"json": "{\"normalizedTime\": 0.8, \"originAnchor\": \"BASE_CHARACTER_HAND_LEFT\", \"targetAnchor\": \"BASE_CHARACTER_BELT_STRAIGHT_LEFT\"}"
					}
				]
			}
		]
	}
}
```

| Field | Type | Description |
|:--:|:--:|:--:|
| firstPerson | bool | (Optional) Whether the current Carry Style is for an FP or TP Animator |
| attachments | list[obj] | Same as BattleStyle attachments. They are always skinned by the off-hand item and must start in a non-hand (stowed) anchor |
| overrides | obj | Same as BattleStyle overrides, but only `Carry Draw`, `Carry Hold`, `Carry Stow` and (FP only) `Carry Run` can be overridden |

Rules checked when loading:
- Carry attachments must not start in a hand anchor
- The anchors used by a Carry Style (attachments and `AN_SwitchAttachmentAnchorBehaviour` events) can't be used by any BattleStyle with `allowsCarry`, since moving an attachment into an occupied anchor destroys the occupant
- If any clip of a Carry Style is missing (or the Animator Controllers weren't rebuilt with the Carry states), the Carry Style is disabled with a warning and items using it can't be carried

New clips must also be registered in the `animations` list of *Animations/BASE_Character/BASE_Character.json* (and *BASE_Character_FP.json* for "-FP" clips) before running *Editor Tools/Animations/Build Controllers*.