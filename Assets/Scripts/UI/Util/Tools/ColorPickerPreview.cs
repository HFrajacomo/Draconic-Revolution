using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorPickerPreview : MonoBehaviour {
	public GameObject rainbowObject;
	public Image previewColor;

	private Color selectedColor;

	public void ClickPreview(){
		this.rainbowObject.SetActive(!this.rainbowObject.activeSelf);
		this.rainbowObject.GetComponent<ColorPickerRainbow>().SetTargetPicker(this.gameObject);
	}
	public void SetColor(Color c){
		this.selectedColor = c;
		this.previewColor.color = c;
	}
	public Color GetColor(){return this.selectedColor;}
}