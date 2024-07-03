
# [SOP] Adding New Blocks

## Overview

In order to add new blocks to Draconic Revolution, there are a few things to keep in mind.
Let's go through some of those.

## Block Types

The first thing you should realize is that blocks are assigned categories depending on the material property.
The properties (at the moment) are:

1. Fully Opaque
2. Transparent
3. Special

To give a few examples, let's say you made a new Stone block. That block is opaque, therefore, falls under category 1. If you made some new Leaves, that's a transparent block and falls under category 2.
Now, blocks complex enough to require a shader of their own fall under the special category. Some special blocks include Water and Ice.

If you are dealing with category 1 and 2, then skip to their very own sections below. If you are dealing with a special block, then I'm sorry, but this SOP is not for you. Ask your Shader Artist for some help!

## Storing the Texture

If the block you are creating is fully opaque, then create the 32x32 texture for it and put it in the *Assets\Resources\Textures\Voxels\Blocks* folder. The name must match the naming convention of the other files.

## Assign Block Behaviour and Information

Along with the texture, you must create a JSON file with the same name as the texture. This file will contain all data for that specific block and its behaviour. Ask help from a Programmer to help you create the block behaviour.

## Registering the Block

Open the *Assets\Resources\Textures\Voxels\Blocks\BLOCK_LIST.txt* file and append your block's name to the end of the file. This tells Draconic Revolution that this block is registered and ready to be processed in the game. Blocks that are not registered in this file are considered "work in progress" and will not be imported.


## Registering Normal Map (Optional)

In this version of Draconic Revolution, normal maps are generated from the texture. But you can adjust the normal intensity in the *Assets\Resources\Textures\Voxels\Blocks\NORMAL_INTENSITY.txt*. In this file, you can reference the block id, followed by a tab and the normal intensity. It is recommended to keep values in-between 0 and 2. If a block has no entry in the normal intensity, that means its intensity is 1.