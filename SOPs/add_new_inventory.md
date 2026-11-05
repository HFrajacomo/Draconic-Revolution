# [SOP] Adding New Inventories

## Overview

To add new inventory types to the game, you should edit a specific config file found in *Resources/Inventory*

## Basic Inventories

Basic inventories are ones that have no special operations on them. Basically, they just define some un-restricted slots for the an entity to use.

```json
{
	"data": [
		{
			"inventoryType": "PLAYER",
			"isPickupTarget": true,
			"bulkMovedTo": true,
			"mainInventory": false,
			"amountOfSlots": 30,
			"columnCount": 6
		},
		{
			"inventoryType": "HOTBAR",
			"isPickupTarget": true,
			"bulkMovedTo": true,
			"mainInventory": true,
			"amountOfSlots": 9,
			"columnCount": 9
		},
		{
			"inventoryType": "CHEST",
			"isPickupTarget": false,
			"bulkMovedTo": true,
			"mainInventory": false,
			"amountOfSlots": 25,
			"columnCount": 5
		}
	]
}
```

The columnCount field declares how many columns the inventory will have in an UI that displays inventory space.
The mainInventory field has different functionality depending on the entity that has it. At the moment, for player characters, it points towards the hotbar.

## Whitelisted Inventories

If you want to have inventories that have strictly defined global rules, like only magical items or only items with a certain tag, you should have the following:

```json
{
	"inventoryType": "BAG_OF_HOLDING",
	"isPickupTarget": true,
	"bulkMovedTo": true,
	"mainInventory": false,
	"amountOfSlots": 40,
	"columnCount": 5,
	"tagList": ["Magical"]
}
```

The tagList field is a list of all the tags, given as string, that are allowed in this inventory. Items that do not contain at least one of these tags are restricted from being placed into the inventory.

## Whitelisted Slots And Default Icons

In case your inventory has whitelist rules for specific slots, you should not define global tags like the above section. Also, if your slots should have a default texture for when the slot is empty, we define it like this:

```json
{
	"inventoryType": "EQUIPMENT",
	"isPickupTarget": false,
	"bulkMovedTo": false,
	"mainInventory": false,
	"amountOfSlots": 1,
	"columnCount": 1,
	"slotWhiteList": [
		{
			"id": 0,
			"array": ["Null", "Weapon"]
		}
	],
	"slotDefaultIcon": [
		{
			"key": 0,
			"value": "left_hand_slot"
		}
	]
}
```

In this example, slot 0 has a whitelist that allows only weapons and empty space. Not just that, but slot 0 also contains a default icon that points towards *Resources/Inventory/left_hand_slot.png*. Any default slot icons should be placed in this directory.