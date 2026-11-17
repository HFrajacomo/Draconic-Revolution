using UnityEngine;
using Unity.Mathematics;

/*
Space taken by a player, relative to its root (the position sent to the Server), used to deny placing voxels inside players.
Mirrors the player's CharacterController in Game.unity (Height 3.8, Center (0, 2.1, -0.3), Radius 0.4), scaled the way Unity scales
a CharacterController by the player's transform scale (see PlayerModelHandler.Rescale). Must follow that CharacterController if it changes.
Its bounding box is tested, so a capsule sliding off a ledge still occupies the voxel its rounded bottom dips into
*/
public static class PlayerHitbox{
	private static readonly float HEIGHT = 3.8f;
	private static readonly float CENTER_Y = 2.1f;
	// Local z of the capsule's center, which turns with the player's yaw
	private static readonly float CENTER_Z = -0.3f;
	private static readonly float RADIUS = 0.4f;

	// Overlap needed to count, so a player that only touches a voxel's face (standing on it, leaning on it) doesn't occupy it
	private static readonly float EPSILON = 0.02f;
	// How far above the ground (the bottom of the voxel at their feet) a jumping player's feet must be to place a block beneath themselves.
	// Must stay above the CharacterController's skin width and below the feet height at the peak of a jump, or pillaring stops working
	public static readonly float MIN_FEET_CLEARANCE = 0.8f;

	// Same scale PlayerModelHandler.Rescale gives the player of a race
	public static Vector3 GetScale(Race r){
		switch(r){
			case Race.DWARF:
				return RaceManager.GetSettings(Race.DWARF).scaling * Constants.PLAYER_MODEL_SCALING_FACTOR;
			case Race.HALFLING:
				return RaceManager.GetSettings(Race.HALFLING).scaling * Constants.PLAYER_MODEL_SCALING_FACTOR;
			default:
				return RaceManager.GetSettings(Race.HUMAN).scaling * Constants.PLAYER_MODEL_SCALING_FACTOR;
		}
	}

	// Distance from the root to the bottom of the hitbox
	public static float GetBottomOffset(Vector3 scale){
		return (CENTER_Y - HEIGHT/2f) * scale.y;
	}

	/*
	Whether a player with its root in rootPos (absolute coordinates), yaw in degrees and transform scale is inside the voxel x,y,z (world coordinates).
	ignoreFeetVoxel skips the voxel at the feet once they're MIN_FEET_CLEARANCE above its bottom, so a jumping player can place a block beneath themselves
	*/
	public static bool OverlapsVoxel(float3 rootPos, float yaw, Vector3 scale, int x, int y, int z, bool ignoreFeetVoxel=false){
		float rad = math.radians(yaw);
		float radius = RADIUS * Mathf.Max(scale.x, scale.z);
		float offsetZ = CENTER_Z * scale.z;
		float centerX = rootPos.x + math.sin(rad)*offsetZ;
		float centerZ = rootPos.z + math.cos(rad)*offsetZ;
		float bottom = rootPos.y + (CENTER_Y - HEIGHT/2f) * scale.y;
		float top = rootPos.y + (CENTER_Y + HEIGHT/2f) * scale.y;

		if(centerX + radius <= x - 0.5f + EPSILON || centerX - radius >= x + 0.5f - EPSILON)
			return false;
		if(centerZ + radius <= z - 0.5f + EPSILON || centerZ - radius >= z + 0.5f - EPSILON)
			return false;
		if(top <= y - 0.5f + EPSILON || bottom >= y + 0.5f - EPSILON)
			return false;
		if(ignoreFeetVoxel && y - 0.5f + MIN_FEET_CLEARANCE <= bottom)
			return false;

		return true;
	}
}