using UnityEngine;

public class MouseFollowerUI : MonoBehaviour{
	private RectTransform rectTransform;
	private Canvas canvas;

	void Awake(){
		this.rectTransform = GetComponent<RectTransform>();
		this.canvas = GetComponentInParent<Canvas>();
	}

	void OnEnable(){
		Vector2 localPoint;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			this.canvas.transform as RectTransform,
			Input.mousePosition,
			this.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
			out localPoint
		);

		this.rectTransform.localPosition = localPoint;
	}

	void Update(){
		Vector2 localPoint;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			this.canvas.transform as RectTransform,
			Input.mousePosition,
			this.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
			out localPoint
		);

		this.rectTransform.localPosition = localPoint;
	}
}
	