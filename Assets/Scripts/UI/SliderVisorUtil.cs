using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
This utility can be added to any Slider to make it update a readonly InputField with it's value
The InputField must be a child of the Slider GameObject 
*/
public class SliderVisorUtil : MonoBehaviour
{
    public Slider slider;
    public TMP_InputField inputField;

    /*
    Sets the Slider and InputField on Runtime
    */
    public void Awake(){
        this.slider = this.gameObject.GetComponent<Slider>();
        this.inputField = this.gameObject.GetComponentInChildren<TMP_InputField>();
        this.inputField.readOnly = true;
        this.inputField.text = ((int)this.slider.value).ToString();
    }

    public void UpdateValue(){
        this.inputField.text = ((int)this.slider.value).ToString();
    }

}
