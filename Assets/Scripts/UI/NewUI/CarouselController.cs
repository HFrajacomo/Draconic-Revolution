using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/*
Implementation of a Horizontal Carousel ScrollView
*/
public class CarouselController{
	// References
	private ScrollView view;
	private VisualElement parent;

	// Carousel animation variables
    private readonly float maxLerpTime;
    private float currentLerpTime = 0f;
    private bool isScrolling = false;
    private float lerpInitX = 0f;
    private int lerpEndX = 0;
    private int totalCarouselSize = 0;
    private int itemSize = 0;

    // Carousel Bezier Smoothness
    private bool isBezier;
    private float midPoint;

    // Controller Flags
    public bool isNextDisabled = false;
    public bool isPrevDisabled = true;
    public bool refreshControllers = true;

    // Cache
    private VisualElement cacheElement;


    public CarouselController(ScrollView view, VisualElement parent, int itemSize, float maxLerpTime, bool isBezier=true){
    	this.view = view;
    	this.parent = parent;

    	this.itemSize = itemSize;
    	this.maxLerpTime = maxLerpTime;
    	this.isBezier = isBezier;

    	this.lerpInitX = this.view.scrollOffset.x;
    }

    public ScrollView GetView(){return this.view;}

    public void MoveOneAhead(){
    	if(this.lerpEndX + this.itemSize >= this.totalCarouselSize){return;}

    	this.lerpEndX += this.itemSize;
    	this.lerpInitX = this.view.scrollOffset.x;
    	this.currentLerpTime = 0f;
    	this.isScrolling = true;

    	if(this.isPrevDisabled)
    		this.refreshControllers = true;

    	this.isNextDisabled = false;
    	this.isPrevDisabled = false;


    	if(this.lerpEndX + this.view.layout.width >= this.totalCarouselSize){
    		this.isNextDisabled = true;
    		this.refreshControllers = true;
    	}

    	CalculateBezierMidpoint();
    }

    public void MoveOneBack(){
		if(this.lerpEndX - this.itemSize < 0){
			return;
		}

		this.lerpEndX -= this.itemSize;
    	this.lerpInitX = this.view.scrollOffset.x;
    	this.currentLerpTime = 0f;
    	this.isScrolling = true;

    	if(this.isNextDisabled)
    		this.refreshControllers = true;

    	this.isPrevDisabled = false;
    	this.isNextDisabled = false;

    	if(this.lerpEndX == 0){
    		this.isPrevDisabled = true;
    		this.refreshControllers = true;
    	}

    	CalculateBezierMidpoint();
    }

    public void AddWorld(VisualTreeAsset item, string name, string description){
        this.cacheElement = item.Instantiate();
        this.cacheElement.Query<Label>("world-name").First().text = name;
        this.cacheElement.Query<Label>("world-description").First().text = description;

        this.parent.Add(this.cacheElement);

        this.totalCarouselSize += this.itemSize;
    }

    public void ClearCarousel(){
        foreach(VisualElement element in this.parent.Children()){
            element.SetEnabled(false);
        }

        this.parent.Clear();
        this.view.scrollOffset = new Vector2(0,0);
        this.totalCarouselSize = 0;
        this.currentLerpTime = 0f;
        this.isScrolling = false;
        this.lerpInitX = 0f;
        this.lerpEndX = 0;
        this.isNextDisabled = false;
        this.isPrevDisabled = true;
        this.refreshControllers = true;
    }

    public void Scroll(){
        if(this.isScrolling){
            this.currentLerpTime += Time.deltaTime/this.maxLerpTime;
            this.currentLerpTime = Mathf.Clamp(this.currentLerpTime, 0, 1);

            this.view.scrollOffset = new Vector2(Smooth(), 0f);

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
}