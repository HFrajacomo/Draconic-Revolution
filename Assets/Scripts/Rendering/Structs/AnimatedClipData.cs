using UnityEngine;

/*
An animation of an Animated Object, sampled when it's loaded (see VX_AnimatedModelIdentityBehaviour).
Every frame holds the transform of each part of the model relative to its rest pose, as the 3 rows of an affine matrix.
rows is ordered [frame][part][row]. Frames are evenly spaced, the first at time 0 and the last at length
*/
public class AnimatedClipData{
	public string name;
	public float length;
	public bool loop;
	public int frameCount;
	public int partCount;
	public Vector4[] rows;

	public AnimatedClipData(string name, float length, bool loop, int frameCount, int partCount){
		this.name = name;
		this.length = length;
		this.loop = loop;
		this.frameCount = frameCount;
		this.partCount = partCount;
		this.rows = new Vector4[frameCount*partCount*3];
	}

	public void SetPart(int frame, int part, Matrix4x4 transform){
		int index = (frame*this.partCount + part)*3;

		this.rows[index] = transform.GetRow(0);
		this.rows[index+1] = transform.GetRow(1);
		this.rows[index+2] = transform.GetRow(2);
	}
}
