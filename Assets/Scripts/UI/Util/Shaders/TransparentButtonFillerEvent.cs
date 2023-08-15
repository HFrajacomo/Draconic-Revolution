using UnityEngine;
using UnityEngine.UI;

public class TransparentButtonFillerEvent : MonoBehaviour{
	private Image image;
    private Button button;

	void Awake(){
		this.image = GetComponent<Image>();
        this.button = GetComponent<Button>();
	}

    public void HoverEnter()
    {
        if(this.button.interactable)
            this.image.fillCenter = true;
    }

    public void HoverExit()
    {
        this.image.fillCenter = false;
    }
}