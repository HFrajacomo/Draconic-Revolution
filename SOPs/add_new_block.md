
# [SOP] Adding New Blocks

## Overview

In order to add new blocks to Draconic Revolution, there are a few things to keep in mind.
Let's go through some of those.

## Block Types

The first thing you should realize is that blocks are assigned classes depending on the material property.
The properties (at the moment) are:

1. Fully Opaque
2. Transparent
3. Special

Go give a few examples, let's say you made a new Stone block. That block is opaque, therefore, falls under category 1. If you made some new Leaves, that's a transparent block and falls under category 2.
Now, blocks complex enough to require a shader of their own fall under the special category. Some special blocks include Water and Ice.

If you are dealing with category 1 and 2, then skip to their very own sections below. If you are dealing with a special block, then I'm sorry, but this SOP is not for you. Ask your Shader Artist for some help!

## Appending to Texture Atlas

### Fully Opaque

If the block you are creating is fully opaque, then create the 32x32 texture for it and append to the Texture Atlas. It can be found in: *Assets/Resources/texture_atlas.png*.

### Transparent

If the block you are creating has transparency, then create the 32x32 texture for it with alpha and append to the Transparent Atlas. It can be found in: *Assets/Resources/transparent_atlas.png*.


## Adding a BlockID

In *Assets/Scripts/BlockID.cs* create add a new entry at the bottom of the ID list. Use the naming standard of all uppercase and *_* for spaces.
Follow the value standard and you should be good to go.

Example: Cobalt Bricks would have the id **COBALT_BRICKS**.

## Creating the Block Class

Go to *Assets/Scripts/Blocks/* and create your new block classes there. Using Clay block as example, the file should be *Clay_Block.cs*.

## Registering the Block

Go to *Assets/Scripts/Blocks.cs* and increase the blockCount in 1. In case the Atlas has changed shape (a new row or column has been added), change the atlasX and atlasY variables to match that.
In the **public static Blocks Block()** function, add the new block and its ID to the return.