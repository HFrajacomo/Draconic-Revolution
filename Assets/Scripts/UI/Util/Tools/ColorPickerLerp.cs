using System;
using System.Collections;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorPickerLerp : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    // Images
    public Image selectorCaret;
    private Image image;
    private Material material;

    [Header("Gradient Mode only")]
    public bool isGradient = false;
    public CharacterCreationMenu characterCreationMenu;
    private Gradient gradient;

    // Colors
    private Color selectedColor;

    // Value
    private float lerpValue;

    // Internal Flag
    private bool isDragging;
    private bool isInit = false;

    private void Awake()
    {
        Initialize();

        if(isGradient)
            SetGradient(this.characterCreationMenu.GetSkinColorGradient());

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        this.isDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        this.isDragging = false;
    }

    public void OnPointerClick(PointerEventData eventData){
        this.isDragging = true;
        OnDrag(eventData);
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
                this.selectorCaret.rectTransform.localPosition = new Vector3((this.lerpValue - .5f) * (image.rectTransform.rect.width), 0, 0);
            
                if(this.isGradient){
                    CalculateColor(this.lerpValue);
                }
            }
        }
    }

    private void Initialize(){
        if(!this.isInit){        
            this.image = GetComponent<Image>();
            this.material = Instantiate(this.image.material);
            this.image.material = this.material;
            this.isInit = true;
        }
    }

    private void CalculateColor(float val){
        this.selectedColor = Color.Lerp(this.gradient.color1, this.gradient.color2, val);
    }

    public Color GetColor(){return this.selectedColor;}
    public float GetValue(){return this.lerpValue;}

    public void ChangeGradient(Gradient grad){
        SetGradient(grad);
    }

    private void SetGradient(Gradient grad){
        this.gradient = grad;

        this.material.SetColor("_Color1", this.gradient.color1);
        this.material.SetColor("_Color2", this.gradient.color2);
    }
    
    public void SetValue(float val){
        this.lerpValue = val;
        this.selectorCaret.rectTransform.localPosition = new Vector3((this.lerpValue - .5f) * (image.rectTransform.rect.width), 0, 0); //image.rectTransform.position.y
    }
}