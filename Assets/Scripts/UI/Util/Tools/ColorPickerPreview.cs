using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Object = System.Object;

public class ColorPickerPreview : MonoBehaviour {
	public GameObject colorPickingMenu;
	public GameObject itemsViewPort;
	public GameObject itemsScrollBar;
	public Image viewScrollbar;
	public Image previewColor;
	public GameObject connectedMenu;
	public ColorPickerLerpGroup pickerGroup;

	public string pickerText;

	private Color selectedColor;

	public void ClickPreview(){
		bool current = this.colorPickingMenu.activeSelf;

		this.colorPickingMenu.SetActive(!current);
		this.itemsViewPort.SetActive(current);
		this.itemsScrollBar.SetActive(current);
		this.viewScrollbar.enabled = current;

		this.pickerGroup.SetTarget(this);
		this.pickerGroup.SetText(this.pickerText);
		this.pickerGroup.SetHSV(this.selectedColor);
	}

	public void ResetPreview(){
		if(this.colorPickingMenu.activeSelf){
			this.colorPickingMenu.SetActive(false);
			this.itemsViewPort.SetActive(true);
			this.itemsScrollBar.SetActive(true);
			this.viewScrollbar.enabled = true;

			this.pickerGroup.SetTarget(null);
			this.pickerGroup.SetText("");	
		}
	}

	public void SetColor(Color c){
		this.selectedColor = c;
		this.previewColor.color = c;

		Object[] arguments = new Object[2];
		arguments[0] = this.selectedColor;
		arguments[1] = this;

		this.connectedMenu.SendMessage("ChangeColor", arguments);
	}

	// No callback
	public void SetDefiniteColor(Color c){
		this.selectedColor = c;
		this.previewColor.color = c;
	}

	public Color GetColor(){return this.selectedColor;}
}