# [SOP] Adding New Items

## Overview

To add new items to the game, the process can be quite simple now. Let's go through some of those.

## Storing the Texture

Create the 32x32 texture for it and put it in the *Assets\Resources\Textures\Items\\* folder. The name must match the naming convention of the other files.

## Assign Item Behaviour and Information

Along with the texture, you must create a JSON file with the same name as the texture. This file will contain all data for that specific block and its behaviour. Ask help from a Programmer to help you create the block behaviour.

### Item Types

The `--->Type` section at the end of the item JSON defines which class the item is deserialized as:

| Type | Description |
|:--:|:--:|
| Item | A regular item |
| Weapon | An item that can be equipped. Requires `weaponType`, which decides its BattleStyle |
| SubWeapon | A Weapon that can also be carried in the off-hand (Torch, Book, Lantern...) |

### SubWeapons (Carry Items)

SubWeapons have two extra rules:

```json
    // Weapon
    "weaponType": "TORCH",
    // SubWeapon
    "carryStyle": "BASE_Torch",
```

| Field | Type | Description |
|:--:|:--:|:--:|
| weaponType | str | (Optional) If set and mapped to a BattleStyle in `BattleStyleDeterminator`, that style is used when the SubWeapon is the only equipped weapon. If empty, the SubWeapon is carry-only and is carried by the Unarmed style when equipped alone |
| carryStyle | str | (Optional) Name of the Third Person Carry Style in *Assets\Resources\CarryStyles\\* used when this item is carried in the off-hand. See the Battle Style SOP |

At least one of the two fields must be set. Two SubWeapons can never be equipped at the same time, and a SubWeapon can only be carried together with a Weapon whose BattleStyle has `allowsCarry` set.

## Registering the Item

Open the *Assets\Resources\Textures\Items\ITEM_LIST.txt* file and append your item's name to the end of the file. This tells Draconic Revolution that this item is registered and ready to be processed in the game. Items that are not registered in this file are considered "work in progress" and will not be imported.