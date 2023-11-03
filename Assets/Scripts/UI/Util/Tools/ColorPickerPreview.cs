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

	private Color selectedColor;

	public void ClickPreview(){
		bool current = this.colorPickingMenu.activeSelf;

		this.colorPickingMenu.SetActive(!current);
		this.itemsViewPort.SetActive(current);
		this.itemsScrollBar.SetActive(current);
		this.viewScrollbar.enabled = current;
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