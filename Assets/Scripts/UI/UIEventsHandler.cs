using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
This Component is meant to react to any UI event
that needs triggering in a static class (example: changing the World.RenderDistance based on Slider OnValueChanged())
*/
public class UIEventsHandler : MonoBehaviour
{
    public void UpdateRenderDistance(){
        World.SetRenderDistance((int)this.gameObject.GetComponent<Slider>().value);
    }
}
