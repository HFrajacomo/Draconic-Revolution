using UnityEngine;
using UnityEngine.UI;

public class ShaderAnimationButtonController : MonoBehaviour{
	private Material material;
    private Image image;
    private Button button;
    public SingleButtonGroup group;

	void Awake(){
        this.image = GetComponent<Image>();
		this.material = Instantiate(this.image.material);
        this.image.material = this.material;
        this.button = GetComponent<Button>();
	}

    public void HoverEnter()
    {
        if(this.button.interactable){
            this.group.ActivateButtonInGroup(this.button);
            this.material.SetFloat("_ShouldAnimate", 1f);
        }
    }

    public void StopShaderAnimation()
    {
        this.material.SetFloat("_ShouldAnimate", 0f);
    }

    public void OnSelect()
    {
        if(this.button.interactable){
            this.group.ActivateButtonInGroup(this.button);
            this.material.SetFloat("_ShouldAnimate", 1f);
        }
    }
}