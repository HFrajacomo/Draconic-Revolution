# Draconic Revolution — Project Guidelines

## What the Project Is

Draconic Revolution is a procedurally generated, voxel-based late-medieval **action RPG** made in Unity. It supports singleplayer, LAN and dedicated-server multiplayer. It is a digital adaptation of a homebrew tabletop RPG system created in 2016. The project lead is Henrique Frajacomo, who works with volunteers. See [README.md](../README.md) for the full vision, lore and progress table.

Core design pillars (respect these when proposing features):
- **No levels, only skills**: 21 skills (Alchemy, Combat, Sorcery, Smithing, Thievery, etc.), each going from 1 to 100. Some skills have subclasses. All skills are always unlocked. Attributes (Strength, Precision, Vitality, Evasion, Magic, Charisma) come from skill levels. The player is never locked into a build.
- **Two gamemodes**: Sandbox, which is fully procedural and whose completion marks the end of Beta, and Campaign, a partially preset world with authored cities, NPCs and quests.
- **World**: 4 vertical depth layers stacked as chunks: Surface, Underground, Hells and Core. Chunks are 16×256×16, and the world is fully destructible and buildable. Biomes exist on each layer and drive structures and post-FX.
- **Feel**: real-time action combat with 1st/3rd person and a battle hotbar, plus exploration and a "chill" life-sim (farming, crafting, trade).
- **Art**: 32×32 pixel-art voxels with normal maps, low-poly models, a semi-cartoonish but desaturated look, vibrant magic and biome color correction. HDRP.
- **Audio**: custom audio engine with 2D/3D layers, dynamic music intensity per biome and runtime reverb-zone generation. Music features "Bard" NPCs.
- **Roadmap**: Alpha (current: core engine, inventory, combat/animation, UI, basic AI) → Beta (all 21 skills, cities, dungeons, stores, quests) → Release (Draconic Revolution campaign).

Team values: favor the *best* solution over the fastest one. There are no hard deadlines and no half-solutions. Remake systems when a better approach is found.

## Tech Stack

- Unity **6000.0.25f1** (Unity 6) with the **HDRP** render pipeline (`com.unity.render-pipelines.high-definition` 17.0.3).
- Burst plus Jobs/`NativeArray` for chunk meshing, lighting and world generation. Also used: Input System, Animation Rigging, AI Navigation and UI Toolkit.
- Networking is **custom raw TCP sockets** (`System.Net.Sockets`), not Netcode/Mirror. Even singleplayer runs a local server process.
- See [Packages/manifest.json](../Packages/manifest.json) for dependencies.

## Architecture

All gameplay code is in [Assets/Scripts/](../Assets/Scripts/), organized by domain folder (AI, Animation, Audio, Blocks, Character, Inventory, Items, Networking, Persist, Player, Rendering, UI, VFX, WorldGeneration, etc.). There are **no asmdefs** and **no namespaces**: everything is in the global namespace.

- **Boot flow**: the `Loader` scene runs [GameLoader.cs](../Assets/Scripts/Loading/GameLoader.cs). It instantiates every `*Loader` ([BaseLoader](../Assets/Scripts/Loading/BaseLoader.cs) subclasses: Shader, Item, Voxel, Audio, Structure, Animation, Inventory, Action) and runs `Load()` and then `RunPostDeserializationRoutine()`. After that it opens `Menu` (client) or `Server` (when build index 1 is the Server scene). The scenes are `Loader`, `Menu`, `Game`, `Client`, `Server` and `Testing`.
- **Client/Server split**: `World.isClient` and the `isClient` flags passed to loaders gate client-only work such as rendering and audio. [Server.cs](../Assets/Scripts/Networking/Server.cs) owns authoritative state. [Client.cs](../Assets/Scripts/Networking/Client.cs) talks to it with [NetMessage](../Assets/Scripts/Networking/NetMessage.cs) packets, using a `NetCode` byte header plus a length prefix, and [NetDecoder.cs](../Assets/Scripts/Networking/NetDecoder.cs). Any new gameplay state change usually needs a `NetCode`, server handling and client handling. Server CLI args are `-Local` and `-World <name>`, and dedicated servers read `server.cfg`.
- **Chunks**: [ChunkLoader.cs](../Assets/Scripts/ChunkLoader.cs) (client) and `ChunkLoader_Server.cs` (server), plus `Rendering/Core/Chunk.cs`, `ChunkRenderer.cs` and Burst jobs in `Rendering/Burst/`. Block updates go through `BUDScheduler`/`BUDSignal`.
- **Persistence**: region files, defragmentation and character/inventory/entity files are in `Assets/Scripts/Persist/`.
- **Data-driven content**: blocks, objects, items, actions, structures, battle styles and inventories are JSON plus assets under [Assets/Resources/](../Assets/Resources/). They are deserialized at load time, and their behaviour is defined by C# behaviour classes (for example `Blocks/VoxelBehaviour/`, `Items/ItemBehaviour/` and `EntityAction/EntityActionBehaviour/`).
- **Client Build**: is done by changing the order of scenes in Unity's Build Settings, ensuring that the `Loader` scene is first, followed by `Menu`, `Game`, `Client`. The `Server` and `Testing` scenes are not included in the client build.
- **Server Build**: is done by changing the order of scenes in Unity's Build Settings, ensuring that the `Loader` scene is first, followed by `Server`. The `Menu`, `Game`, `Client` and `Testing` scenes are not included in the server build. After running the Server build, fix_server.bat should be executed to correct any path or configuration issues.
- **ChunkLoader and ChunkLoader_Server classes**: although no main classes are contained in a Unity Project, these classes are responsible for managing the game session of the client and server respectively, handling chunk loading, unloading, and updates. They are also responsible for holding a reference to every other MonoBehaviour that is part of the game session.


Deep-dive technical docs (read them before changing these systems):
- [Game Details/Chunk_Renderer.md](../Game%20Details/Chunk_Renderer.md)
- [Game Details/Voxel_Lighting.md](../Game%20Details/Voxel_Lighting.md)
- [Game Details/World_Generation.md](../Game%20Details/World_Generation.md)
- [Game Details/Block_Updates.md](../Game%20Details/Block_Updates.md)
- [Game Details/World_Save.md](../Game%20Details/World_Save.md)
- [Game Details/Optimization.md](../Game%20Details/Optimization.md), which covers Burst, unsafe NativeTools and IL2CPP

## Adding Content

Follow the SOPs in [SOPs/](../SOPs/): blocks, items, audio, animation sets, battle styles, character models and inventories. Content is **only imported if registered** in its list file:
- `Assets/Resources/Voxels/Blocks/BLOCK_LIST.txt`, plus `NORMAL_INTENSITY.txt`
- `Assets/Resources/Voxels/Objects/OBJECT_LIST.txt`
- `Assets/Resources/Items/ITEM_LIST.txt`
- `Assets/Resources/Actions/ACTION_LIST.txt`
- `Assets/Resources/Structures/Standalone/STRUCTURE_LIST.txt`

Some SOPs reference older `Resources/Textures/...` paths. The list files above are the current locations.

## Code Style

Match the existing C# style (see [NetMessage.cs](../Assets/Scripts/Networking/NetMessage.cs)):
- Tabs for indentation, opening braces on the same line (`void Start(){`) and no space before `(` in `if(`/`foreach(`.
- PascalCase for types and methods, camelCase for fields, `UPPER_SNAKE` for `static readonly` constants. Explicit `this.` is common.
- Global namespace, with one primary type per file and the file named after it.
- Prefer allocation-free, Burst-friendly code in hot paths (chunk, lighting and world-gen), using `NativeArray` and structs.

## Build and Validation

- There is **no CLI build and no automated test suite**. Code is compiled and validated by opening the project in the Unity Editor (6000.0.25f1). Builds go to `Build/`, which is git-ignored.
- Do not touch `Library/`, `Logs/`, `Temp/` or generated `*.csproj`/`*.sln` files.
- When adding or moving assets, keep the matching `.meta` files and never regenerate or alter existing GUIDs, because this breaks scene and prefab references.
- Helper Python scripts are in [DevScripts/](../DevScripts/) for code generation and checks.
- Branches are named after features (for example `BasicCombatUI`), and `master` is the main branch.
