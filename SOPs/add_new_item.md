# [SOP] Adding New Items

## Overview

To add new items to the game, the process can be quite simple now. Let's go through some of those.

## Storing the Texture

Create the 32x32 texture for it and put it in the *Assets\Resources\Textures\Items\\* folder. The name must match the naming convention of the other files.

## Assign Item Behaviour and Information

Along with the texture, you must create a JSON file with the same name as the texture. This file will contain all data for that specific block and its behaviour. Ask help from a Programmer to help you create the block behaviour.

## Registering the Block

Open the *Assets\Resources\Textures\Items\ITEM_LIST.txt* file and append your item's name to the end of the file. This tells Draconic Revolution that this item is registered and ready to be processed in the game. Items that are not registered in this file are considered "work in progress" and will not be imported.