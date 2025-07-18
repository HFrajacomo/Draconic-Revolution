# Character Models

During this long period of experimentation and coming up with a system to deal with modular character models in Draconic Revolution, plenty of problems and dead-ends were found. I noticed how big of a problem is the whole "Exporting from Blender and importing into Unity" operation, making it possible for so many things to go wrong in black box systems that we, as users, have no access to.

This document is a tutorial and general reminder to anyone working in Draconic Revolution - and in the greater community - to have a simple consultation material on the topic. This is a compilation of information I found on the internet (and figured out myself) that were actually useful to most import/export problems. 


## All models must:
- Not have any transform information not set to 0 (position, rotation, scaling). You can send all the transform information to verts by going on Object Mode: "Object > Apply > All Transforms" or doing "Ctrl + A" and "All Transforms"

- To correctly assign materials in Unity, objects in Blender need to have their VertexID ordered. To do that, Go in face selection in Edit Mode, select all faces and do "Mesh > Sort Elements > By Material > Face"

- To be parented by the respective gender armature. Sometimes, automatic weights are bad and the solution might be simple. So using Empty Groups and weight painting is a big possibility in some models. Also, to completely de-parent an armature, you can delete all Vertex Groups assigned to the object.

- Be exported to fbx with only Armature and Meshes, Forward direction set to "Z Forward", "Apply Unit", "Use Space Transform" and "Apply Transform" checked on.

- In Unity's Import Settings, **do not** use "Weld Vertices" as it will delete most 'empty' submeshes 

- Still in Unity's Import Settings, Legacy Blend Shapes must be off and all normals must be "Imported"

## Objects with empty submeshes:
Unfortunately, there is no such a thing as empty submesh when importing a model into Unity. It will ignore the submesh completely.
So to make it work, whenever you have an empty submesh, create 3 minuscule verts in Blender and fill them up to form a face. If they are small enough, it won't be seen and the submesh will not be ignored in Unity

## Face Objects
For face objects, we have 4 submeshes in Blender, but in Unity, since we have the eye shader that takes two colors, we treat the primary and secondary colors in the picker as eye colors for the 1 shader. Faces always assume that the last material (which is the sclera of the eye) is always going to be white (or later customizable based on race)

Also, note that Faces have ShapeKeys that must be created!

## Hat Objects
Hats are crazy different from other models. Their last vertex group is called the "Hairline". The hairline exists in every hat and is a 4-verts plane. In Unity, everything hair vert on the normal side of the plane gets squashed back next to the plane. This exists to make the hair "acommodate" itself in the hat. In Draconic Revolution, all the processing to manipulate hair is already done and the hairline submesh is removed after processing. So technically, you can have up to 5 materials in a Hat in Blender.

## Armature and Animation
Any changes to armature, even in Rest Pose, must have its animations exported and replaced in Unity's AnimationController. Or else, we'll get a misalignment in Bind Poses, which deforms models wrong, creating visible seams.