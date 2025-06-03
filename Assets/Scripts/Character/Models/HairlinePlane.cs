using UnityEngine;

public struct HairlinePlane{
	public Vector3 planeMid;
	public Vector3 planeNormal;
	public bool valid;

	public HairlinePlane(Vector3 mid, Vector3 n, bool valid){
		this.planeMid = mid;
		this.planeNormal = n;
		this.valid = valid;
	}

	public HairlinePlane(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n){
		Debug.Log($"(Plane Formants): {a} -- {b} -- {c} -- {d}");

		this.planeMid = (a + b + c + d)/4;
		this.planeNormal = n;
		this.valid = true;
	}

	// Returns true if a given point in the normal-facing side of the plane
	public bool GetSide(Vector3 input){
		if(!this.valid)
			return true;

		Debug.Log($"(GETSIDE) Mid: {this.planeMid} -- Normal: {this.planeNormal} -- Hair: {input} -- Dot: {Vector3.Dot(this.planeNormal, input - this.planeMid)} -- IsAbove: {Vector3.Dot(this.planeNormal, input - this.planeMid) > 0}");

		return Vector3.Dot(this.planeNormal, input - this.planeMid) > 0;
	}

	public Vector3 GetClosestPoint(Vector3 input){
		float distance = Vector3.Dot(this.planeNormal, input - this.planeMid);
		return input - this.planeNormal * distance;
	}
}