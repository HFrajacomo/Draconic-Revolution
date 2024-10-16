using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
Implementation of a Horizontal Carousel ScrollRect
*/
public class CarouselController{
	// References
	public RectTransform view;
	private GameObject parent;

    // Carousel Elements
    public List<GameObject> elements = new List<GameObject>();

	// Carousel animation variables
    private readonly float maxLerpTime;
    private float currentLerpTime = 0f;
    private bool isScrolling = false;
    private float lerpInitX = 0f;
    private int lerpEndX = 0;
    private int totalCarouselSize = 0;
    private int itemSize = 0;
    private int viewportSize;
    private float carouselEmptySize;

    // Carousel Bezier Smoothness
    private bool isBezier;
    private float midPoint;

    // Carousel mouse movement checker
    private float lastValidXPos = 0;

    // Controller Flags
    public bool isNextDisabled = false;
    public bool isPrevDisabled = true;
    public bool refreshControllers = true;
    public bool initialPositionSet = false;

    // Cache
    private GameObject cacheElement;
    private RectTransform cacheRect;
    private GameObject cacheButton;


    public CarouselController(RectTransform view, GameObject parent, int itemSize, int viewportSize, float maxLerpTime, bool isBezier=true){
    	this.view = view;
    	this.parent = parent;

        this.viewportSize = viewportSize;
    	this.itemSize = itemSize;
    	this.maxLerpTime = maxLerpTime;
    	this.isBezier = isBezier;

        this.carouselEmptySize = this.view.rect.width;
    }

    public RectTransform GetView(){return this.view;}

    public void MoveOneAhead(){
    	if(this.lerpEndX - this.itemSize <= (-this.view.rect.width/2 + this.viewportSize)){
            this.isNextDisabled = true;
            this.refreshControllers = true;
            this.lerpEndX = (int)(-this.view.rect.width/2 + this.viewportSize);
        }
        else{
            if(this.isPrevDisabled)
                this.refreshControllers = true;

            this.isNextDisabled = false;
            this.isPrevDisabled = false;
            this.lerpEndX -= this.itemSize;
        }

    	this.lerpInitX = this.view.anchoredPosition.x;
    	this.currentLerpTime = 0f;
    	this.isScrolling = true;
        this.lastValidXPos = this.lerpEndX;

    	CalculateBezierMidpoint();
    }

    public void MoveOneBack(){
		if(this.lerpEndX + this.itemSize >= this.totalCarouselSize){
            this.isPrevDisabled = true;
            this.refreshControllers = true;
            this.lerpEndX = this.totalCarouselSize;
		}
        else{
            if(this.isNextDisabled)
                this.refreshControllers = true;

            this.isNextDisabled = false;
            this.isPrevDisabled = false;
            this.lerpEndX += this.itemSize;
        }

    	this.lerpInitX = this.view.anchoredPosition.x;
    	this.currentLerpTime = 0f;
    	this.isScrolling = true;
        this.lastValidXPos = this.lerpEndX;

    	CalculateBezierMidpoint();
    }

    // Specific function to add WorldItems to the Select_World Menu carousel
    public GameObject AddWorld(GameObject item, string name, string description){
        this.cacheElement = GameObject.Instantiate(item);
        this.cacheElement.name = "[WorldItem] " + name;
        this.cacheElement.transform.SetParent(this.parent.transform);
        this.cacheElement.transform.localScale = Vector3.one;
        this.cacheButton = GameObject.Find("/SelectWorldMenu/Content/MainLayout/VerticalGroup/Scroll View/Viewport/Content/" + this.cacheElement.name + "/PlayButton");
        this.cacheButton.transform.localScale = Vector3.one;

        this.cacheRect = this.cacheElement.transform as RectTransform;
        this.cacheRect.anchoredPosition3D = Vector3.zero;

        this.cacheElement.GetComponentsInChildren<Text>()[0].text = name;
        this.cacheElement.GetComponentsInChildren<Text>()[1].text = description;

        // Generates 
        this.cacheElement.GetComponentsInChildren<Image>()[1].sprite = WorldImageLoader.GetWorldImage(name);

        this.elements.Add(this.cacheElement);

        return this.cacheElement;
    }

    public void ClearCarousel(){
        foreach(Transform children in this.parent.transform){
            children.gameObject.SetActive(false);
            GameObject.Destroy(children.gameObject);
        }
        
        this.view.anchoredPosition = new Vector2(this.totalCarouselSize, 0);
        this.currentLerpTime = 0f;
        this.isScrolling = false;
        this.lerpInitX = 0f;
        this.lerpEndX = 0;
        this.isNextDisabled = false;
        this.isPrevDisabled = true;
        this.refreshControllers = true;
        this.totalCarouselSize = (int)this.view.rect.width;
        this.elements.Clear();
    }

    public void Scroll(){
        if(this.isScrolling){
            this.currentLerpTime += Time.deltaTime/this.maxLerpTime;
            this.currentLerpTime = Mathf.Clamp(this.currentLerpTime, 0, 1);

            this.view.anchoredPosition = new Vector2(Smooth(), 0f);

            if(this.currentLerpTime == 1){
                this.isScrolling = false;
                this.lerpInitX = this.lerpEndX;
            }
        }
    }

    public void ResetRefresh(){this.refreshControllers = false;}

    private void CalculateBezierMidpoint(){
    	if(this.isBezier){
    		float diffSmoothness = (this.lerpEndX - this.lerpInitX)*.8f;
    		this.midPoint = this.lerpInitX + diffSmoothness;   
    	}
    }

    private float Smooth(){
    	// Lerp Approach
    	if(!this.isBezier){
    		return Mathf.Lerp(this.lerpInitX, this.lerpEndX, this.currentLerpTime);
    	}
    	// Bezier Approach
    	else{
    		float a, b;

    		a = Mathf.Lerp(this.lerpInitX, this.midPoint, this.currentLerpTime);
    		b = Mathf.Lerp(this.midPoint, this.lerpEndX, this.currentLerpTime);
    		return Mathf.Lerp(a, b, this.currentLerpTime);
    	}
    }

    // A fix for a Unity problem where RectTransform won't move until a certain amount of frames passes from the Awake/Start phase
    public void InitialMovementOfView(){
        if(this.view.rect.width != this.carouselEmptySize){
            this.view.anchoredPosition = new Vector2(this.view.rect.width/2, 0f);
            this.lastValidXPos = this.view.anchoredPosition.x;
            this.totalCarouselSize = (int)this.view.rect.width/2;
            this.lerpInitX = this.totalCarouselSize;
            this.lerpEndX = this.totalCarouselSize;
            this.initialPositionSet = true;

            ResetControllers();
        }
    }

    public void HandleMouseMovement(){
        if(this.lastValidXPos != this.view.anchoredPosition.x && !this.isScrolling){
            this.lerpInitX = this.view.anchoredPosition.x;
            this.lerpEndX = (int)this.view.anchoredPosition.x;
            this.isScrolling = false;

            this.lastValidXPos = this.view.anchoredPosition.x;

            ResetControllers();
        }
    }

    private void ResetControllers(){
        // Disable Prev?
        if(this.lerpEndX + this.itemSize >= this.totalCarouselSize){
            this.isPrevDisabled = true;
            this.refreshControllers = true;
            this.lerpEndX = this.totalCarouselSize;
        }
        else{
            this.isPrevDisabled = false;
            this.refreshControllers = true; 
        }
        // Disable Next?
        if(this.lerpEndX - this.itemSize <= (-this.view.rect.width/2 + this.viewportSize)){
            this.isNextDisabled = true;
            this.refreshControllers = true;
            this.lerpEndX = (int)(-this.view.rect.width/2 + this.viewportSize);
        }
        else{
            this.isNextDisabled = false;
            this.refreshControllers = true;
        }
    }
}