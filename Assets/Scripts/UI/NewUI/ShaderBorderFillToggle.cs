using UnityEngine;
using UnityEngine.UI;

public class ShaderBorderFillToggle : MonoBehaviour{
	private Image image;
    private Toggle toggle;

    public void RefreshToggle()
    {
        LinkReferences();

        if(this.toggle.isOn && this.toggle.interactable){
            this.image.material.SetFloat("_ShouldAnimate", 1f);
        }
        else if(!this.toggle.isOn && this.toggle.interactable){
            this.image.material.SetFloat("_ShouldAnimate", 0f);
        }
    }

    public void RefreshToggle(bool flag)
    {
        LinkReferences();

        if(flag)
            this.image.material.SetFloat("_ShouldAnimate", 1f);
        else
            this.image.material.SetFloat("_ShouldAnimate", 0f);
    }

    private void LinkReferences(){
        if(this.image == null)
            this.image = GetComponent<Image>();
        if(this.toggle == null)
            this.toggle = GetComponent<Toggle>();
    }
}