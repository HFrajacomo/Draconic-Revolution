using UnityEngine;
using UnityEngine.UI;

public class TransparentButtonFillerEvent : MonoBehaviour{
	private Image image;

	void Start(){
		this.image = GetComponent<Image>();
	}

    public void HoverEnter()
    {
        this.image.fillCenter = true;
    }

    public void HoverExit()
    {
        this.image.fillCenter = false;
    }
}