using UnityEngine;
using UnityEngine.UI;

public class TextEraserUI : MonoBehaviour{
	private Text mainText;


	void Awake(){
		this.mainText = this.gameObject.GetComponent<Text>();
	}

	void OnDisable(){
		this.mainText.text = "";
		Debug.Log("DISABLED ERASER");
	}
}