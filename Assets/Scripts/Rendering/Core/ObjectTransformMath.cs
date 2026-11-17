using UnityEngine;
using Unity.Mathematics;

/*
Rotations that place Object meshes in chunks, shared by PrepareAssetsJob and ChunkAnimatedObjects so both place Objects the same way.
Angles are in degrees: rotation.x turns in the XZ plane and rotation.y in the YZ plane, applied in that order
*/
public static class ObjectTransformMath{
	public static Vector3 Rotate(Vector3 a, int2 rotation){
		return RotateY(RotateX(a, rotation.x), rotation.y);
	}

	public static Vector4 Rotate(Vector4 a, int2 rotation){
		return RotateY(RotateX(a, rotation.x), rotation.y);
	}

	public static Vector3 RotateX(Vector3 a, int degrees){
		return new Vector3(a.x*Mathf.Cos(degrees *Mathf.Deg2Rad) - a.z*Mathf.Sin(degrees *Mathf.Deg2Rad), a.y, a.x*Mathf.Sin(degrees *Mathf.Deg2Rad) + a.z*Mathf.Cos(degrees *Mathf.Deg2Rad));
	}

	public static Vector4 RotateX(Vector4 a, int degrees){
		return new Vector4(a.x*Mathf.Cos(degrees *Mathf.Deg2Rad) - a.z*Mathf.Sin(degrees *Mathf.Deg2Rad), a.y, a.x*Mathf.Sin(degrees *Mathf.Deg2Rad) + a.z*Mathf.Cos(degrees *Mathf.Deg2Rad), a.w);
	}

	public static Vector3 RotateY(Vector3 a, int degrees){
		return new Vector3(a.x, a.y * Mathf.Cos(degrees * Mathf.Deg2Rad) - a.z * Mathf.Sin(degrees * Mathf.Deg2Rad), a.y * Mathf.Sin(degrees * Mathf.Deg2Rad) + a.z * Mathf.Cos(degrees * Mathf.Deg2Rad));
	}

	public static Vector4 RotateY(Vector4 a, int degrees){
		return new Vector4(a.x, a.y * Mathf.Cos(degrees * Mathf.Deg2Rad) - a.z * Mathf.Sin(degrees * Mathf.Deg2Rad), a.y * Mathf.Sin(degrees * Mathf.Deg2Rad) + a.z * Mathf.Cos(degrees * Mathf.Deg2Rad), a.w);
	}
}
