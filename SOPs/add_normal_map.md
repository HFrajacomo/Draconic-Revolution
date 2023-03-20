# [SOP] Adding New Normal Maps to Blocks

## Overview

Normal Maps are bluish kind of textures that encode normal vector information to over a texture. They control how light behaves in the surface, being able to simulate bumps and crevices.

In order to generate them, we will need the GIMP, the free software for Image Manipulation.

## Getting the original block image

To create a normal map for a texture, simply start by having the texture open on GIMP. We can do that by:

1. Opening the Texture Atlas using GIMP, or dragging the atlas to GIMP.
2. Then, clich the "**Select Retangle tool**" and select the texture you want to create a normal map to. Press "**Ctrl + C**" to copy
3. Press "**Ctrl + N** to create a new file and use 32x32 as size.
4. On the layers section, right click the "Background" layer and select "**Add Alpha Channel**". Then, on the image, press "**Ctrl + A**" to select all and press "*Delete*".
5. Now paste the texture onto the screen with **Ctrl + V**.

## Creating the Normal Map

Now that you have the original texture, all we gotta do is create a normal map out of it. To do that:

1. Press "**Ctrl + A**"" to select the entire texture.
2. Go to the head menu and hit **Filters > Generic > Normal Map**.
3. Adjust the scale from 1 to 10. The higher the scale, the more bumpy the image will be.
4. Hit "Ok".

## Appending Normal Map
Now that you have the image, copy and paste it to the original Normal Atlas in the position the original texture is in the original texture atlas and you are done!
