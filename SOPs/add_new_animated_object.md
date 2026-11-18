# [SOP] Adding New Animated Objects

Animated Objects are Object voxels (`ShaderIndex` 8, `ANIMATED_OBJECTS`) whose model has animations, like a chest whose lid opens. They take one voxel and are identified like any other Object. Their animations are played on the GPU, in a mesh of their own in each chunk, so playing an animation never rebuilds the chunk. See the "Animated Objects" section of [Chunk_Renderer.md](../Game%20Details/Chunk_Renderer.md).

The Chest (`BASE_Chest`) is the reference Animated Object.

## Modelling in Blender

1. Every **mesh object** of the file is a **part** of the model. Parts are merged into a single mesh in the game, each vertex remembering its part.
2. Animate the parts with **object transform keyframes** (location, rotation, scale), one Action per animation. Armatures, shape keys and keyframed vertex positions are **not supported**.
3. Put the origin of a rotating part on its pivot (a lid on its hinge), so it rotates in an arc.
4. Export the FBX with **Bake Animation** on and the Actions exported as NLA strips / all actions. The take names become the animation names (Blender names them `Object|Action`, like `Lid|BASE_Lid Open`).
5. Save the FBX somewhere under `Assets/Resources/` (the Chest is `Assets/Resources/ItemModels/Chest.fbx`).

The model's rest pose is only shown in states that have no animation. The Chest file rests with its lid open, but it's never shown that way, because its states always show the last frame of an animation (closed when placed). `VX_FacePlayerBehaviour` expects the front of the model to face Z+ once imported in Unity (check it in the model's Inspector preview); otherwise, correct it with `frontYaw`.

## Importing in Unity

Select the FBX and set:

1. **Rig** tab: Animation Type = **Legacy**. Models imported with an Animator are refused when they load.
2. **Model** tab: **Read/Write** on (the mesh is read when the game loads, which only works in builds if this is on). **Bake Axis Conversion** on is recommended. Either way, the root transform given by the importer is part of the rest pose.
3. **Animation** tab: Import Animation on. Keep the take names (or rename the clips and use the new names in the JSON). Turn **Loop Time** on only for animations that loop forever (windmills, flags). Looping animations play on their own when a state rests on them, each voxel at its own phase.

## Creating the Object file

Create `Assets/Resources/Voxels/Objects/<CODENAME>.json` like any Object, with `"shaderIndex": 8`. Then:

1. `scaling` scales the model and `hitboxScaling` scales the hitbox, on top of the fit below. Keep both at 1 for fitted models. Solid Animated Objects (`"solid": true`) collide with the hitbox, not with their model.
2. Use `VX_AnimatedModelIdentityBehaviour` as `modelIdentity`:

```json
--->VX_AnimatedModelIdentityBehaviour
{
    "modelResourcePath": "ItemModels/Chest",
    "textureName": "BASE_Chest",
    "animations": ["Lid|BASE_Lid Open", "Lid|BASE_Lid Close"],
    "fitSize": 0.9,
    "fitAnimation": "Lid|BASE_Lid Open"
}
```

- `modelResourcePath`: path of the model inside `Resources`, without extension.
- `textureName`: texture of the Object, loaded like other Object textures (`<textureName>.png` and `<textureName>-PBR.png`, 32×32, in `Assets/Resources/Voxels/Objects/`).
- `animations`: every animation of the model used by the game. Its position in this list is the animation index used by behaviours and network messages, so **only append** to it.
- `fitSize`: fits the model to its voxel, whatever units it was imported in. The fitted pose is centered on the voxel in X and Z, its bottom is put on the voxel floor, and it's scaled so its largest side is `fitSize` of the voxel (1 touches the voxel walls). The hitbox becomes the box of the fitted pose. 0 (or missing) keeps the imported size and a unit cube hitbox.
- `fitAnimation`: animation whose **first frame** is the fitted pose. Empty fits the model's rest pose. Other poses may leave the fitted box: the Chest is fitted closed (first frame of `Lid|BASE_Lid Open`), so its open lid sticks out behind the voxel.
- `modelOffset` (optional): moves the model after the fit, in voxels for fitted models (in model units otherwise).

3. Make the Object play its animations with a behaviour. `VX_AnimationQueueBehaviour` plays its animations in order, one per interaction, cycling back to the first. It must be listed in both `onInteract` and `animationPose`:

```json
--->Behaviours
{
    "modelIdentity": "VX_AnimatedModelIdentityBehaviour",
    "onInteract": "VX_AnimationQueueBehaviour",
    "animationPose": "VX_AnimationQueueBehaviour"
}
--->VX_AnimationQueueBehaviour
{
    "animations": ["Lid|BASE_Lid Open", "Lid|BASE_Lid Close"],
    "defaultAnimation": "Lid|BASE_Lid Close"
}
```

- `animations`: the queue, played in order.
- `defaultAnimation`: the Object rests on the last frame of this animation when placed or generated, so the first interaction plays the next one in the queue. Empty uses the last of the queue. The Chest rests closed, so it goes closed → Open → Close → ...

It uses the states below the amount of queued animations (`count`): the state is the amount of interactions since the default pose, so state 0 is always the default pose. Higher parts of the state belong to other behaviours and are kept. Interacting while an animation plays starts the next one from the normalized time the current one reached.

4. To make the Object face the player that places it, use `VX_FacePlayerBehaviour` in `onPlayerPlace` and `rotationValue`, and set `"needsRotation": true` and `"maximumRotationScaleState"` to 4 × `statesPerDirection` (8 for the Chest):

```json
--->Behaviours
{
    "onPlayerPlace": "VX_FacePlayerBehaviour",
    "rotationValue": "VX_FacePlayerBehaviour"
}
--->VX_FacePlayerBehaviour
{
    "statesPerDirection": 2,
    "frontYaw": 0
}
```

- When a player places the Object, its front turns to that player, along the horizontal axis (X or Z) that points most directly at them. It uses the `onPlayerPlace` hook, which the Server runs right after `onPlace` with the ID of the placing player (their position is in `cl.regionHandler.allPlayerData`). Objects placed by anything else (structures, world generation) face Z+.
- `statesPerDirection`: states used by the Object's other behaviours. For `VX_AnimationQueueBehaviour`, the amount of queued animations (2 for the Chest).
- `frontYaw` (optional): degrees, a multiple of 90, added to the rotation for models whose front isn't Z+ in Unity.

The voxel state is `direction × statesPerDirection + rest`, `direction` being 0 = Z+, 1 = X+, 2 = Z-, 3 = X-. It works with any Object (not only Animated ones) whose other behaviours only use the states below `statesPerDirection`.

5. To play sounds on interaction (any voxel, not only Animated Objects), register the sounds in `Assets/Resources/Audio/SOUNDS_LIST.json` (as `SFX_3D`) and use one of these behaviours in both `onInteract` and `onSFXPlay`:
   - `VX_InteractSoundBehaviour` (`"sound": "Name"`): plays the same sound on every interaction.
   - `VX_InteractSoundSequenceBehaviour` (`"sounds": [...]`, `"countInteractions": bool`): plays the sounds in order, one per interaction, cycling back to the first. It reads the interaction count from the same part of the state as `VX_AnimationQueueBehaviour`, so with the same amount of sounds as queued animations, each sound matches its animation. List the queue **before** it in `onInteract` and keep `countInteractions` false (loading fails otherwise). Without a queue, set `countInteractions` true so it counts interactions itself.

   The Chest plays `ChestOpen` and `ChestClose` with its animations:

```json
--->Behaviours
{
    "onInteract": ["VX_AnimationQueueBehaviour", "VX_InteractSoundSequenceBehaviour"],
    "onSFXPlay": "VX_InteractSoundSequenceBehaviour"
}
--->VX_InteractSoundSequenceBehaviour
{
    "sounds": ["ChestOpen", "ChestClose"],
    "countInteractions": false,
    "randomPitch": true
}
```

   Both also take `"randomPitch"` (default `true`): each play gets a random pitch between 0.9 and 1.1 (`SFXLoader.MIN_RANDOM_PITCH`/`MAX_RANDOM_PITCH`), so repeated sounds don't sound identical. Set it to `false` to play them at their normal pitch. Each Client picks its own pitch.

   The Server sends `SFXPLAY` with the voxel's new state to every Client that has the chunk, and every `onSFXPlay` behaviour of the voxel receives it, so a voxel should only have one sound behaviour. A voxel plays a single sound at a time: a new interaction replaces the sound still playing. Non-looping block sounds are removed once they finish (`SFXLoader`).

6. Register the Object at the **end** of `Assets/Resources/Voxels/Objects/OBJECT_LIST.txt`, so the codes of existing Objects don't change.

## Writing other animation behaviours

- `animationPose` (`VoxelBehaviour.GetAnimationPose(state)`) returns the index of the animation whose last frame is shown in a state, or -1 for the rest pose.
- Behaviours that need the model identity resolve animation names in `PostObjectSetup(owner)`, through `VX_AnimatedModelIdentityBehaviour.GetAnimationIndex`.
- Server hooks play animations by changing the voxel state and sending `NetCode.VOXELANIMATION` (`NetMessage.VoxelAnimation`) to the clients of the chunk. Return 2 from the hook (save only): a `BLOCKUPDATE` would rebuild the chunk on every client for nothing.
- Client code plays animations with `ChunkLoader.animatedObjects.Play(chunk, x, y, z, blockCode, state, animation, continueNormalized)`.
- An Object using both `needsRotation` and an animation behaviour shares the voxel state between them, so their states must be planned together.

## One-time setup of the Animated Objects material

Already done once the first Animated Object works. Animated Objects need their own Shader Graph, because the chunk materials read the vertex position as is:

1. Duplicate `Assets/Shader/Game Shaders/Objects.shadergraph` as `AnimatedObjects.shadergraph`.
2. In the vertex stage, add a **Custom Function** node: Type = File, Source = `Assets/Scripts/Rendering/ShaderFunctions/AnimatedObjects.hlsl`, Name = `AnimatedObjectVertex`.
   - Inputs: `positionOS` (Vector3) ← Position node (Object), `normalOS` (Vector3) ← Normal Vector node (Object), `tangentOS` (Vector3) ← Tangent Vector node (Object), `partSlot` (Vector2) ← UV node channel **UV2**, `time` (Float) ← **Time** node (Time output).
   - Outputs: `position`, `normal`, `tangent` (Vector3) and `natural`, `extra`, `dampness` (Float).
3. Connect `position`, `normal` and `tangent` to the Vertex **Position**, **Normal** and **Tangent** blocks (Object space).
4. Everywhere the graph reads the Light UV (the UV node on channel **UV3**), use the new outputs instead: `natural` for its x, `extra` for its y and `dampness` for its z. They have the same values UV3 has for Objects, so the existing `VoxelLightNatural`/`VoxelLightExtra` custom interpolators and the rain dampness keep working as they are. Keep calling `VoxelLightShade`.
5. In the Graph Settings, turn on **Additional Velocity Changes / Motion Vectors for Vertex Animation**, so TAA and motion blur follow moving parts. Set **Double-Sided Mode** to **Flipped Normals**, so the insides of open parts (like the Chest's lid) are drawn. Afterwards, check that the material's Surface Options show Double-Sided.
6. Duplicate `Assets/Materials/Game Materials/ObjectAtlasMaterial.mat` as `AnimatedObjectsMaterial.mat` and set its shader to the new graph. Check that motion vectors are enabled on the material.
7. In `Game.unity`, add `AnimatedObjectsMaterial` to the materials of the `ChunkRenderer`'s MeshRenderer as **material 8**, so materials stay indexed by `ShaderIndex`. Chunks only use the first 8 materials (one per submesh of the chunk mesh, `ChunkRenderer.GetChunkMaterials`), and the Animated Objects meshes use material 8 (`ChunkRenderer.GetAnimatedObjectsMaterial`). `VoxelLoader.SetAtlasTextures` gives it the texture arrays of `ANIMATED_OBJECTS` when the game starts.

## Checklist

1. FBX imported as Legacy with Read/Write on.
2. `<textureName>.png` and `<textureName>-PBR.png` in `Assets/Resources/Voxels/Objects/`.
3. Object JSON with `"shaderIndex": 8`, its model identity and its animation behaviour, registered at the end of `OBJECT_LIST.txt`.
4. An item that places it (`IT_PlaceBlockBehaviour`), registered at the end of `ITEM_LIST.txt`, if players should place it.
5. Rebuild the **Server**: Singleplayer and LAN run the prebuilt Server, which must know the new Object and behaviours. A Server older than the client shows up as wrong animations: for example, a chest that snaps open and then closes on the first interaction, because the old Server picks the animation with the old state layout.
