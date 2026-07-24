using UnityEngine;

public class MouseFollowerUI : MonoBehaviour{
	private RectTransform rectTransform;
	private Canvas canvas;
	public float forward = 0;

	void Awake(){
		this.rectTransform = GetComponent<RectTransform>();
		this.canvas = GetComponentInParent<Canvas>();
	}

	void Update(){
		Vector2 localPoint;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			this.canvas.transform as RectTransform,
			Input.mousePosition,
			this.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
			out localPoint
		);

		this.rectTransform.localPosition = Transform(localPoint);
	}

	private Vector3 Transform(Vector3 input){return new Vector3(input.x, input.y, this.forward);}
}
	