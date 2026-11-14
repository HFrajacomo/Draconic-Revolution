# Networking

Draconic Revolution uses its own TCP protocol (`System.Net.Sockets`). Even singleplayer runs a local Server process that the Client connects to.
Client and Server are always built together from the same project, so message layouts and `NetCode` values can change freely between versions.

## Classes

| Class | Role |
|---|---|
| [NetConnection](../Assets/Scripts/Networking/NetConnection.cs) | A single TCP connection. Streams frames in and out of a socket. Used by Client, Server (one per peer) and InfoClient |
| [NetMessage](../Assets/Scripts/Networking/NetMessage.cs) | Builds outgoing messages and wraps received ones. Holds the `NetCode` enum |
| [NetDecoder](../Assets/Scripts/Networking/NetDecoder.cs) | Binary encoding. Also used by save files |
| [NetBufferPool](../Assets/Scripts/Networking/NetBufferPool.cs) | Pool of the arrays that hold received frames |
| [Client](../Assets/Scripts/Networking/Client.cs) | Game client. Owned by the `ChunkLoader` |
| [Server](../Assets/Scripts/Networking/Server.cs) | Authoritative state. Owned by the `ChunkLoader_Server` |
| [InfoClient](../Assets/Scripts/Networking/InfoClient.cs) | Short connection made in the Menu to check the player's character before loading a world |
| [ChunkStreamer](../Assets/Scripts/Networking/ChunkStreamer.cs) | Pushes the chunks of every client's StreamWindow |
| [ChunkChangeSet](../Assets/Scripts/Networking/ChunkChangeSet.cs) | Voxels of a loaded Server chunk that changed since they were sent |
| [ChunkPayloadCache](../Assets/Scripts/Networking/ChunkPayloadCache.cs) | Compressed `SENDCHUNK` messages, reused while their chunk doesn't change |
| [EntityUpdateBatcher](../Assets/Scripts/Networking/EntityUpdateBatcher.cs) | Groups the movement of entities into one `ENTITYUPDATES` message per client per frame |

## Frames

Every message is sent as a frame: a 4-byte big-endian length, followed by the message. The first byte of a message is its `NetCode`.
Frames bigger than `Constants.NET_MAX_FRAME_SIZE` (or with a length of zero or less) are a protocol violation and close the connection.

## Threading

- **Receiving** runs in IO threads (`SocketAsyncEventArgs`). Every complete frame in the receive buffer is copied into an array rented from `NetBufferPool` and queued in the owner's `ConcurrentQueue<NetMessage>`.
- **Handling** runs in the main thread. The Client handles its queue in `ChunkLoader.HandleClientCommunication`, the Server in `Server.ProcessIncoming` (called by `ChunkLoader_Server.Update`), and InfoClient in `InfoClient.Update` (called by `Redirector`).
- **Sending** is main-thread only. `Send` copies the message into the connection's back buffer, without any socket call. `Flush` writes everything queued in a single async send, and is called once per frame in `LateUpdate` (`ChunkLoader.LateUpdate` → `Client.Flush`, `ChunkLoader_Server.LateUpdate` → `Server.LateTick`). Messages sent after that are written in the next frame.
- Messages are built in the static `NetMessage` buffer, so a message must be sent right after being built. Sending to many clients is one copy per client.
- When a connection is closed by the peer or by an error, a single **close event** (a `NetMessage` without data, `IsCloseEvent()`) is queued after its last messages, so it's handled in order.
- Server connection state (`connections`, `temporaryConnections`) is only touched by the main thread. Accepted sockets are queued by the IO thread and started in `ProcessIncoming`.

### Received buffers

Received arrays come from a pool and are usually **bigger than the message**. Handlers must use `NetMessage.GetSize()` and never `data.Length`.
Arrays are given back to the pool once the message is handled. The only exception is chunk data (`SENDCHUNK`/`STREAMCHUNK`), which the `ChunkLoader` keeps in `toLoad` and gives back after decompressing or discarding it.
Code that keeps a received array after its handler returns must take it out of that flow the same way.

## Connections

1. The Server accepts a socket and gives it a **temporary ID**. It sends `ACCEPTEDCONNECT`.
2. Temporary connections can only send `SENDCLIENTINFO`, `REQUESTCHARACTEREXISTENCE`, `SENDCHARSHEET`, `DISCONNECTINFO` and `HEARTBEAT`.
3. `SENDCLIENTINFO` assigns the AccountID to the connection. The same connection keeps receiving, so messages after it are handled with the AccountID.
4. InfoClient connections end with `DISCONNECTINFO`.

**Heartbeats and timeouts:** any received frame counts as activity. A `HEARTBEAT` is only sent when nothing else was sent for `NET_HEARTBEAT_INTERVAL_SECONDS`.
Clients disconnect after `NET_CLIENT_TIMEOUT_SECONDS` without receiving anything. The Server drops clients after `NET_SERVER_TIMEOUT_SECONDS`, and temporary connections after `NET_TEMPORARY_TIMEOUT_SECONDS`.

**Backpressure:** a connection with more than `NET_MAX_PENDING_BYTES` unsent is closed. The `ChunkStreamer` stops streaming to a client while it has more than `NET_STREAM_MAX_PENDING_BYTES` unsent.

## Entity IDs

Players are identified in the network by a **NetID**, a `uint` given by the Server for the session (`Server.GetPlayerNetID`). It's informed to its owner in `SENDSERVERINFO`.
Clients map NetIDs to AccountIDs when they receive `PLAYERSPAWN`, and forget them on `ENTITYDELETE`/`ENTITYDELETEBATCH`.
Dropped items use their entity code (`uint`).

Messages about an entity a client doesn't know are ignored, so an entity must be spawned before its other messages are sent:
- `PLAYERSPAWN` is sent whenever a client starts seeing a player (`Server.ShowPlayerTo`), followed by snapshots of its item in hand, BattleStyle, attachment anchors, animations, animator parameters, looping SFX, VFX and moving voxel light.

Only the latest animation state of each layer is kept, so effects created by Animation Events (SFX, VFX and moving voxel lights, like a lit torch) can't be rebuilt by replaying animations. They are **owner-authoritative**: only the owner runs those events and sends `ENTITYSFX`/`ENTITYVFX`/`ENTITYVOXELLIGHT`. The Server tracks the active ones and other clients rebuild them in the player model (`EntityHandler.PlayerSFX`/`PlayerVFX`/`PlayerVoxelLight`). VFX are children of attachments, so both the Server and clients forget them whenever attachments are reset by a BattleStyle or carry style change.

Since attachment switches and weapon effects come from the events of the owner's Third Person `Weapon Sheathe`/`Weapon Unsheathe` clips, those states are played explicitly from code (`PlayerActionController.ForceSheathe`) instead of being reached through Animator transitions, so they always run no matter what the Base layer was doing.
- `DROPSPAWN` is sent when a drop is created (`EntityHandler_Server.AddItem`), when a chunk is sent to a client, and to the clients that only see the new chunk of a drop that moved between chunks (`Server.MoveDropBetweenChunks`).

**Movement** is not sent right away. Moved players (`CLIENTPLAYERPOSITION`) and drops (`Server.MarkDropMoved`) are collected during the frame and sent in `LateTick` as a single `ENTITYUPDATES` message per client. Every entry keeps the timestamp of its movement, used by `SmoothMovement` for snapshot interpolation.

**Positions** in every message and save file are absolute world coordinates. The client uses a Floating Origin (see Chunk_Renderer.md, Far from the World Origin), so it converts at the boundary: received positions go through `WorldOrigin.ToUnity` in `Client.cs` before reaching `EntityHandler` and `SmoothMovement`, and sent positions (`CLIENTPLAYERPOSITION`, `DROPITEM`) go through `WorldOrigin.ToAbsolute`. Chunk and block coordinates are always absolute.

## Chunk synchronization

**Streaming:** see `ChunkStreamer`. Chunks are sent as `STREAMCHUNK` and acknowledged with `CHUNKACK`.

**Compressed chunks** are kept in the `ChunkPayloadCache`, so a chunk is compressed once no matter how many times or to how many clients it's sent.
Chunks read from disk are already compressed the way clients receive them, so their message is built from the disk data (`Server.SeedChunkPayload`) without compressing anything.
An entry is valid while its chunk keeps the `ChunkChangeSet` version it was built with.

**Changes:** every loaded Server chunk has a `ChunkChangeSet` (`Chunk.EnableChangeTracking`, called by `ChunkLoader_Server.AddChunk`). It's fed by `VoxelData.SetCell` and the `VoxelMetadata` setters.
Every change increases the chunk version. Changes that replace whole arrays mark the chunk as entirely changed, and `RegionFileHandler.SaveChunk` also increases the version, so changes made directly in the arrays (like Structures) are never served from an old copy.

| Server call | What clients receive |
|---|---|
| `Server.SendBlockUpdate` | A single voxel with its complete data (`BLOCKUPDATE`). The voxel is removed from the change set |
| `Server.SendChunkChanges` | Every changed voxel of the chunk (`BLOCKBATCH`), or the whole chunk if the changes are unknown or more than `BLOCKBATCH_MAX_CHANGES` |
| `Server.QueueChunkChanges` | Same as `SendChunkChanges`, once at the end of the frame, merging every change made until then. Used by liquids |
| `Server.SendWholeChunk` | The whole chunk (`SENDCHUNK`). Used after changes that can't be tracked per voxel |

`BUDScheduler` propagation, trees and `RegisterChunkToSend` use `SendChunkChanges`.

**Placement prediction:** a placed block only gets collision after the Server's `BLOCKUPDATE` arrives and its chunk is rebuilt and its collider cooked. Until then, `PlacementPredictor` (client) puts a box collider in the voxel when `PLACEBLOCK` is sent, so a player that places a block beneath themselves while jumping lands on it. The box is removed when a chunk collider built after the voxel was received is swapped in, when `PLACEMENTDENIED` echoes the voxel back, when the chunk unloads, or after a timeout.
The box only becomes active once the player's hitbox has left the voxel. If the player's feet are still inside the placed voxel when its chunk collider is swapped in, `PlayerMovement.PlaceFeetOn` nudges them on top of that block (even if their head clips into terrain above), since a `CharacterController` inside a mesh collider falls through the terrain. No other voxel ever moves the player.

## Message layouts

Positions inside a chunk use 2 bytes (`NetDecoder.WriteLocalPos`: `x<<4 | z`, then `y`), so they must be local coordinates.
Angles use 2 bytes (`NetDecoder.WriteAngle`). Strings are UTF-8 prefixed by their size (`String8`/`String16`).
Animation states and animator parameters are identified by `Animator.StringToHash`.

| NetCode | Direction | Layout after the code |
|---|---|---|
| ACCEPTEDCONNECT | S→C | — |
| HEARTBEAT | Both | — |
| DISCONNECT / DISCONNECTINFO | C→S | — |
| SENDCLIENTINFO | C→S | AccountID 8, RenderDistance 4, Seed 4, WorldName String16 |
| SENDSERVERINFO | S→C | Pos 12, Dir 12, Day 4, Hour 1, Minute 1, NetID 4 |
| REQUESTCHUNKLOAD | C→S | ChunkPos 9 |
| SENDCHUNK / STREAMCHUNK | S→C | ChunkPos 9, Biome 1, BlockSize 4, HPSize 4, StateSize 4, compressed data |
| STREAMCENTER | C→S | ChunkPos 9, VerticalLayer 1 |
| CHUNKACK | C→S | Consumed 2, Backlog 2 |
| BLOCKUPDATE | S→C | ChunkPos 9, Local 2, BUDCode 1, Block 2, State 2, HP 2 |
| BLOCKBATCH | S→C | ChunkPos 9, Count 2, Count × (Local 2, Block 2, State 2, HP 2) |
| PLACEBLOCK | C→S | ChunkPos 9, Local 2, Facing 1, Block 2, Slot 1, NewQuantity 1, InventoryCode 1, ItemID 2, ActionSlot 1, ActionID 2 |
| PLACEMENTDENIED | S→C | ItemID 2, InventoryCode 1, Slot 1, ActionSlot 1, ActionID 2, ChunkPos 9, Local 2 |
| BATCHLOADBUD | C→S | ChunkPos 9, Count 2, Count × Local 2 |
| INTERACT | C→S | ChunkPos 9, Local 2, Facing 1 |
| BLOCKDAMAGE | C→S | ChunkPos 9, Local 2, Damage 2 |
| BLOCKDAMAGE | S→C | ChunkPos 9, Local 2, HP 2, ShouldRedraw 1 |
| VFXDATA / VFXCHANGE | S→C | ChunkPos 9, Local 2, Facing 1, Block 2, State 2 |
| VFXBREAK / SFXPLAY | S→C | ChunkPos 9, Local 2, Block 2, State 2 |
| SENDGAMETIME | S→C | Day 4, Hour 1, Minute 1 |
| SENDNOISE | S→C | Seed 4, Noise |
| CLIENTPLAYERPOSITION | C→S | Pos 12, Pitch 2, Yaw 2, Timestamp 4 |
| CLIENTCHUNK | C→S | LastChunkPos 9, NewChunkPos 9 |
| PLAYERSPAWN | S→C | NetID 4, AccountID 8, Pos 12, Pitch 2, Yaw 2, Timestamp 4, network CharacterSheet with compact appearance |
| DROPSPAWN | S→C | ID 4, Pos 12, Standing 1, RotY 2, RotZ 2, ItemID 2, Amount 1, Timestamp 4 |
| ENTITYUPDATES | S→C | PlayerCount 2, PlayerCount × (NetID 4, Pos 12, Pitch 2, Yaw 2, Timestamp 4), DropCount 2, DropCount × (ID 4, Pos 12, Standing 1, Timestamp 4) |
| ENTITYDELETE | S→C | EntityType 1, ID 4 |
| ENTITYDELETEBATCH | S→C | PlayerCount 2, NetIDs, DropCount 2, IDs |
| DROPITEM | C→S | Pos 12, Move 12, ItemID 2, Amount 1, Slot 1 |
| SENDINVENTORY | Both | Inventory bytes (save layout) |
| REQUESTCHARACTEREXISTENCE / REQUESTCHARACTERSHEET | C→S | AccountID 8 |
| SENDCHARACTERPRELOAD | S→C | Exists 1, then if it exists: CharacterAppearance 247, IsMale 1 |
| SENDCHARSHEET | Both | AccountID 8, network CharacterSheet with full appearance |
| SENDHOTBARPOSITION | C→S | HotbarSlot 1, AttackSlot 1, IsNormalHotbar 1 |
| SENDITEMINHAND | S→C | NetID 4, IsItem 1, ItemID 2, ConnectedItem 2, Amount 1 |
| SENDANIMATIONLAYER | C→S / S→C | (NetID 4), Layer 1, StateHash 4 |
| SENDBATTLESTYLE | S→C | NetID 4, Style 4, RightHand 2, LeftHand 2 |
| SENDANIMATORPARAMETER | C→S / S→C | (NetID 4), ParameterHash 4, Value 4 |
| EQUIPITEM | C→S / S→C | (NetID 4), previous Item (deep), new Item (deep) |
| ENTITYSFX | C→S / S→C | (NetID 4), Operation 1, IsLoop 1, HasAnchor 1, Anchor 1, Offset 12, Name String8 |
| SENDATTACHMENTANCHOR | C→S / S→C | (NetID 4), Origin 1, Target 1, Flip 1, HeightOffset 4 |
| ENTITYVFX | C→S / S→C | (NetID 4), Operation 1, Anchor 1, Offset 12, Rotation 12, Name String8, PrefabPath String8 |
| ENTITYVOXELLIGHT | C→S / S→C | (NetID 4), Operation 1, Radius 4 |

Clients only send animation, equipment, SFX, VFX and voxel light messages about their own player. The Server uses the AccountID of the connection and relays them with the player's NetID.

The **network CharacterSheet** (`NetDecoder.WriteCharacterSheetNet`) has a String8 name, the effect count and only the active effects. The compact appearance (76 bytes instead of 247) stores colors with 8 bits per channel and is only used for other players.

## Save files

`NetDecoder` is also the encoder of save files (characters, entities, players, inventories and region headers). Methods used by `Persist/` must keep producing the same bytes.
Compact encodings that only travel through the network have their own methods (`*Net`, `*Compact`, `LocalPos`, `Angle`, `String8/16`).

CharacterSheets of online players are kept in memory by `EntityHandler_Server`. Changes mark them dirty (`MarkSheetDirty`), and they're written to disk every `SERVER_SHEET_FLUSH_SECONDS`, when the player disconnects and when the Server closes.

## Adding a message

1. Add the `NetCode` and its builder method in `NetMessage`, writing its layout in this document.
2. Handle it in `Client.HandleReceivedMessage` and/or `Server.HandleReceivedMessage`. Use `GetSize()` instead of `data.Length`.
3. Messages about an entity must use its network ID and must only be sent to clients that already received its spawn.
4. Messages that change voxels should go through the change tracking methods above instead of building chunk messages by hand.
