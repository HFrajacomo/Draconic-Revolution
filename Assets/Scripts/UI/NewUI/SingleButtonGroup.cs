using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SingleButtonGroup : MonoBehaviour {
	public Button[] buttons;
	private Button triggered;

	public void ActivateButtonInGroup(Button butt){
		foreach(Button b in this.buttons){
			if(b != butt){
				b.GetComponent<ShaderAnimationButtonController>().StopShaderAnimation();
			}
		}
	}
}