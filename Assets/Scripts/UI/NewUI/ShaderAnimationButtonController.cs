using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShaderAnimationButtonController : MonoBehaviour{
	private Material material;
    private Image image;
    private Button button;

	void Awake(){
        this.image = GetComponent<Image>();
		this.material = Instantiate(this.image.material);
        this.image.material = this.material;
        this.button = GetComponent<Button>();
	}

    public void HoverEnter()
    {
        if(this.button.interactable){
            this.button.Select();
            this.material.SetFloat("_ShouldAnimate", 1f);
        }
    }

    public void HoverExit()
    {
        if(EventSystem.current.currentSelectedGameObject != this.gameObject)
            this.material.SetFloat("_ShouldAnimate", 0f);
    }

    public void OnDeselect()
    {
        this.material.SetFloat("_ShouldAnimate", 0f);
    }
}