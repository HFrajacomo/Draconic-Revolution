using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
Implementation of a Horizontal Carousel ScrollRect
*/
public class CarouselController{
	// References
	private ScrollRect view;
	private GameObject parent;

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
    private GameObject cacheElement;
    private RectTransform cacheRect;


    public CarouselController(ScrollRect view, GameObject parent, int itemSize, float maxLerpTime, bool isBezier=true){
    	this.view = view;
    	this.parent = parent;

    	this.itemSize = itemSize;
    	this.maxLerpTime = maxLerpTime;
    	this.isBezier = isBezier;

    	this.lerpInitX = this.view.normalizedPosition.x;
    }

    public ScrollRect GetView(){return this.view;}

    public void MoveOneAhead(){
    	if(this.lerpEndX + this.itemSize >= this.totalCarouselSize){return;}

    	this.lerpEndX += this.itemSize;
    	this.lerpInitX = this.view.normalizedPosition.x;
    	this.currentLerpTime = 0f;
    	this.isScrolling = true;

    	if(this.isPrevDisabled)
    		this.refreshControllers = true;

    	this.isNextDisabled = false;
    	this.isPrevDisabled = false;


    	if(this.lerpEndX + this.view.content.sizeDelta.x >= this.totalCarouselSize){
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
    	this.lerpInitX = this.view.normalizedPosition.x;
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

    public void AddWorld(GameObject item, string name, string description){
        this.cacheElement = GameObject.Instantiate(item);
        this.cacheElement.name = "[WorldItem] " + name;
        this.cacheElement.transform.SetParent(this.parent.transform);
        this.cacheElement.transform.localScale = Vector3.one;

        this.cacheRect = this.cacheElement.transform as RectTransform;
        this.cacheRect.anchoredPosition3D = Vector3.zero;

        this.cacheElement.GetComponentsInChildren<Text>()[0].text = name;
        this.cacheElement.GetComponentsInChildren<Text>()[1].text = description;

        this.totalCarouselSize += this.itemSize;
    }

    public void ClearCarousel(){
        foreach(Transform children in this.parent.transform){
            children.gameObject.SetActive(false);
            GameObject.Destroy(children.gameObject);
        }
        
        this.view.normalizedPosition = new Vector2(0,0);
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

            this.view.normalizedPosition = new Vector2(Smooth(), 0f);

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