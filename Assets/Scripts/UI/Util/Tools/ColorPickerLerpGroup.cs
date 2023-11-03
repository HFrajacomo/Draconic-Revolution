using UnityEngine;
using UnityEngine.UI;

public class ColorPickerLerpGroup : MonoBehaviour{
	[Header("Color Pickers")]
	public ColorPickerLerp hue;
	public Slider saturation;
	public Slider value_;

	[Header("Target")]
	public ColorPickerPreview colorPickerPreview;
	
	private Color lastFrameColor;
	private Color finalColor;

	// Cache
	private float color;
	private float sat;
	private float val;

	void Start(){
		this.lastFrameColor = Color.white;
	}

	void Update(){
		this.color = this.hue.GetValue();
		this.sat = this.saturation.value;
		this.val = this.value_.value;

		this.finalColor = Color.HSVToRGB(this.color, this.sat, this.val);

		if(this.lastFrameColor != this.finalColor)
			this.colorPickerPreview.SetColor(this.finalColor);
	}
}