using UnityEngine;

public struct HairlinePlane{
	public Vector3 planeMid;
	public Vector3 planeNormal;
	public Vector3 planeTangent;
	public Vector3 planeBinormal;
	public bool valid;

	public HairlinePlane(string unused){
		this.planeMid = new Vector3(0,10000,0);
		this.planeNormal = Vector3.up;
		this.valid = true;
		this.planeTangent = Vector3.zero;
		this.planeBinormal = Vector3.zero;	
	}

	public HairlinePlane(Vector3 mid, Vector3 n, bool valid){
		this.planeMid = mid;
		this.planeNormal = n;
		this.valid = valid;
		this.planeTangent = Vector3.zero;
		this.planeBinormal = Vector3.zero;

		Vector3 tangReference = ChooseReferenceVector(n);
		this.planeTangent = Vector3.Cross(tangReference, n);
		this.planeBinormal = Vector3.Cross(n, this.planeTangent);

		Vector3.OrthoNormalize(ref this.planeNormal, ref this.planeTangent, ref this.planeBinormal);
	}

	public HairlinePlane(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n){
		this.planeMid = (a + b + c + d)/4;
		this.planeNormal = n;
		this.valid = true;
		this.planeTangent = Vector3.zero;
		this.planeBinormal = Vector3.zero;

		Vector3 tangReference = ChooseReferenceVector(n);
		this.planeTangent = Vector3.Cross(tangReference, n);
		this.planeBinormal = Vector3.Cross(n, this.planeTangent);

		Vector3.OrthoNormalize(ref this.planeNormal, ref this.planeTangent, ref this.planeBinormal);
	}

	// Returns true if a given point in the normal-facing side of the plane
	public bool GetSide(Vector3 input){
		if(!this.valid)
			return true;

		return Vector3.Dot(this.planeNormal, input - this.planeMid) > 0;
	}

	public Vector3 GetClosestPoint(Vector3 input){
		float offset = 0.04f;

		float distance = Vector3.Dot(this.planeNormal, input - this.planeMid);
		Vector3 projection = input - this.planeNormal * distance;

		offset *= Mathf.Clamp(-0.5f, 1, 1-(distance*3));

		// Moves away from origin to puff up
		projection += (this.planeTangent * offset * GetSideInput(projection, this.planeTangent)) + (this.planeBinormal * offset * GetSideInput(projection, this.planeBinormal));

		return projection;
	}

	private Vector3 ChooseReferenceVector(Vector3 normal){
	    // Pick the axis that is least aligned with the normal
	    if (Mathf.Abs(normal.x) > Mathf.Abs(normal.z))
	        return Vector3.forward;
	    else
	        return Vector3.right;
	}

	// Checks whether an input vector is on the positive or negative side of an axis representing a plane
	// Returns 1 for positive side and -1 for negative
	private float GetSideInput(Vector3 input, Vector3 axis){
		float distance = Vector3.Dot(axis, input - this.planeMid);

		if(distance > 0)
			return 1;
		return -1;
	}
}