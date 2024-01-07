using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RotateAvatar : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    // Images
    public Transform target;
    private Image image;

    // Positional
    private float startPos = 0f;
    private float currentDraggingPos = 0f;

    // Internal Flag
    private bool isDragging;

    private static readonly float ROTATION_MULTIPLIER = 0.01f; 

    void Start(){
        this.image = GetComponent<Image>();
    }

    void Update(){
        if (isDragging){
            this.target.Rotate(Vector3.up, -(this.currentDraggingPos - this.startPos) * ROTATION_MULTIPLIER);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        this.startPos = eventData.position.x;
    }

    public void OnPointerUp(PointerEventData eventData){}

    public void OnBeginDrag(PointerEventData eventData)
    {
        this.isDragging = true;
        this.startPos = eventData.position.x;
    }

    public void OnEndDrag(PointerEventData eventData){
        this.isDragging = false;
        this.currentDraggingPos = 0f;
        this.startPos = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Check if the drag position is within the bounds of the image
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint)){
            this.currentDraggingPos = eventData.position.x;
        }
        else{
            OnEndDrag(eventData);
        }
    }
}