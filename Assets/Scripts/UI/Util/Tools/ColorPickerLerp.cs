using System;
using System.Collections;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorPickerLerp : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    // Images
    public Image selectorCaret;
    private Image image;
    private Material material;

    // Colors
    private Color selectedColor;

    // Value
    private float lerpValue;

    // Internal Flag
    private bool isDragging;

    private void Awake()
    {
        this.image = GetComponent<Image>();
        this.material = Instantiate(this.image.material);
        this.image.material = this.material;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        this.isDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        this.isDragging = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            // Check if the drag position is within the bounds of the image
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                // Calculate the normalized x position
                this.lerpValue = Mathf.Clamp01((localPoint.x + image.rectTransform.rect.width / 2) / image.rectTransform.rect.width);                
                this.selectorCaret.rectTransform.localPosition = new Vector3((this.lerpValue - .5f) * (image.rectTransform.rect.width), 0, 0); //image.rectTransform.position.y
            }
        }
    }

    private Color CalculateColor(float val){
        if(val <= .33f){
            return Color.Lerp(Color.red, Color.green, val/.33f);
        }
        else if(val <= .66f){
            return Color.Lerp(Color.green, Color.blue, (val-.33f)/.33f);
        }
        else{
            return Color.Lerp(Color.blue, Color.red, (val-.66f)/.33f);
        }
    }

    public Color GetColor(){return this.selectedColor;}
    public float GetValue(){return this.lerpValue;}
}