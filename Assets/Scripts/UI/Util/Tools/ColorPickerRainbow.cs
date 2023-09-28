using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorPickerRainbow : MonoBehaviour {
	[Header("Offsets")]
	public int xOffset = 0;
	public int yOffset = 0;

	public Image pickerImage;
	public Texture2D colorChart;
	private Color selectedColor;

	private ColorPickerPreview colorPickerPreview;


	public void ColorPick(BaseEventData pointerData){
		PointerEventData pointer = pointerData as PointerEventData;


        // Convert local point to pixel coordinates
        RectTransform imageRect = pickerImage.transform as RectTransform;
        int textureWidth = colorChart.width;
        int textureHeight = colorChart.height;

        Vector3 rectScreenPosition = Camera.main.WorldToScreenPoint(imageRect.position);

        int pixelX = Mathf.FloorToInt((pointer.position.x - rectScreenPosition.x) / imageRect.rect.width * textureWidth);
        int pixelY = Mathf.FloorToInt((pointer.position.y - rectScreenPosition.y) / imageRect.rect.height * textureHeight);
       
		this.selectedColor = colorChart.GetPixel(pixelX, pixelY);

		this.colorPickerPreview.SetColor(this.selectedColor);
		this.gameObject.SetActive(false);
	}

	public void SetTargetPicker(GameObject go){this.colorPickerPreview = go.GetComponent<ColorPickerPreview>();}

	public Color GetColor(){return this.selectedColor;}
}