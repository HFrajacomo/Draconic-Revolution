using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShaderIndex : byte
{
    OPAQUE,
    SPECULAR,
    WATER,
    ASSETS,
    ASSETS_SOLID,
    LEAVES,
    ICE,
    LAVA,
    ANIMATED_OBJECTS // Not a submesh of the chunk mesh. Its material is the last of the ChunkRenderer's, used by the Animated Objects mesh of each chunk (see ChunkAnimatedObjects)
}
