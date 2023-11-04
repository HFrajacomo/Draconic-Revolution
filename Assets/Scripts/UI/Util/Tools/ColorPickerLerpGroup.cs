using UnityEngine;
using UnityEngine.UI;

public class ColorPickerLerpGroup : MonoBehaviour{
	[Header("Color Pickers")]
	public ColorPickerLerp colorGradient;
	public Slider saturation;
	public Slider value_;
	public Text mainLabel;

	[Header("Target")]
	public ColorPickerPreview colorPickerPreview;
	public Menu connectedMenu;
	
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
		if(!this.colorGradient.isGradient){
			this.color = this.colorGradient.GetValue();
			this.sat = this.saturation.value;
			this.val = this.value_.value;

			this.finalColor = Color.HSVToRGB(this.color, this.sat, this.val);

			if(this.lastFrameColor != this.finalColor)
				this.colorPickerPreview.SetColor(this.finalColor);
		}
		else{
			if(this.finalColor != this.colorGradient.GetColor()){
				this.finalColor = this.colorGradient.GetColor();
				this.connectedMenu.SendMessage("ChangeMainColor", (object)this.finalColor);
			}
		}
	}

	public void SetTarget(ColorPickerPreview preview){
		this.colorPickerPreview = preview;
	}

	public void SetText(string text){
		this.mainLabel.text = text;
	}

	public void SetHSV(Color color){
		Color.RGBToHSV(color, out this.color, out this.sat, out this.val);

		this.colorGradient.SetValue(this.color);
		this.saturation.value = this.sat;
		this.value_.value = this.val;
	}
}