using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Object = System.Object;

public class ColorPickerPreview : MonoBehaviour {
	public GameObject rainbowObject;
	public Image previewColor;
	public GameObject connectedMenu;

	private Color selectedColor;

	public void ClickPreview(){
		this.rainbowObject.SetActive(!this.rainbowObject.activeSelf);
		this.rainbowObject.GetComponent<ColorPickerRainbow>().SetTargetPicker(this.gameObject);
	}
	public void SetColor(Color c){
		this.selectedColor = c;
		this.previewColor.color = c;

		Object[] arguments = new Object[2];
		arguments[0] = this.selectedColor;
		arguments[1] = this;

		this.connectedMenu.SendMessage("ChangeColor", arguments);
	}
	public Color GetColor(){return this.selectedColor;}
}