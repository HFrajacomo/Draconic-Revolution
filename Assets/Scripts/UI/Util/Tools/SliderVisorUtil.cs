using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
This utility can be added to any Slider to make it update a readonly InputField with it's value
The InputField must be a child of the Slider GameObject 
*/
public class SliderVisorUtil : MonoBehaviour
{
    private Slider slider;
    private InputField inputField;

    /*
    Sets the Slider and InputField on Runtime
    */
    public void Awake(){
        this.slider = this.gameObject.GetComponent<Slider>();
        this.inputField = this.gameObject.GetComponentInChildren<InputField>();
        this.inputField.readOnly = true;
        this.inputField.text = ((int)this.slider.value).ToString();
        this.slider.onValueChanged.AddListener(UpdateValue);
    }

    public void UpdateValue(float number){
        this.inputField.text = ((int)number).ToString();
    }

}
