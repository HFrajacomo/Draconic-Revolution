using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
This Component is meant to react to any UI event
that needs triggering in a static class (example: changing the World.RenderDistance based on Slider OnValueChanged())
*/
public class UIEventsHandler : MonoBehaviour
{
    public void UpdateRenderDistance(){
        World.SetRenderDistance((int)this.gameObject.GetComponent<Slider>().value);
    }

    public void UpdateAccountID(){
        string text = this.gameObject.GetComponent<TMP_InputField>().text;

        if(text == "")
            return;

        World.SetAccountID(text);
        Configurations.accountID = World.accountID;
    }

    public void Update2DMusicVolume(){
        Configurations.music2DVolume = (int)this.gameObject.GetComponent<Slider>().value;
    }

    public void Update3DMusicVolume(){
        Configurations.music3DVolume = (int)this.gameObject.GetComponent<Slider>().value;
    }

    public void Update2DSFXVolume(){
        Configurations.sfx2DVolume = (int)this.gameObject.GetComponent<Slider>().value;
    }

    public void Update3DSFXVolume(){
        Configurations.sfx3DVolume = (int)this.gameObject.GetComponent<Slider>().value;
    }

    public void Update2DVoiceVolume(){
        Configurations.voice2DVolume = (int)this.gameObject.GetComponent<Slider>().value;
    }

    public void Update3DVoiceVolume(){
        Configurations.voice3DVolume = (int)this.gameObject.GetComponent<Slider>().value;
    }
}
