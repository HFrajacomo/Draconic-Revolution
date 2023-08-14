using UnityEngine;
using UnityEngine.UI;

public class ShaderBorderFillToggle : MonoBehaviour{
	private Image image;
    private Toggle toggle;

	void Awake(){
		this.image = GetComponent<Image>();
        this.toggle = GetComponent<Toggle>();
	}

    public void RefreshToggle()
    {
        if(this.toggle.isOn && this.toggle.interactable){
            this.image.material.SetFloat("_ShouldAnimate", 1f);
        }
        else if(!this.toggle.isOn && this.toggle.interactable){
            this.image.material.SetFloat("_ShouldAnimate", 0f);
        }
    }
}