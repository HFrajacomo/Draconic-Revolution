# [SOP] Adding New Items

## Overview

To add new items to the game, the following steps should be done:

1. Create a 32x32 Icon of the item you want to create
2. Add the Icon to the IconAtlas
3. Create an ItemID for the item
4. Create the item class
5. Add the item class to the Item Encyclopedia

## Creating the Icon

Create the icon in any editor in .png format.

## Adding to the Atlas

The icon must be appended to *Assets/Resources/icon.png*. Remember that the anchoring of the IconAtlas is different from the blocks one. The anchor is in the Top-Left.

## Creating and ID

Go to *Assets/Scripts/ItemID.cs* and create a new item ID at the end following the naming standards of the file.

## Create the Item

Create a class for the item in *Assets/Scripts/Items/* and add its functionality.
the SetIcon(x, y) part should be inputed depending on the position of the icon in the atlas.

Remember that the anchoring is always Top-Left, so if an item is in the top row and 3rd column, the SetIcon call would be called for x = 3 and y = 0.

## Add to Encyclopedia

Go to *Assets/Scripts/Item.cs* and add the ItemID to the GenerateItem() call and return the new class