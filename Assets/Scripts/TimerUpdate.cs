using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerUpdate : MonoBehaviour
{
	public TimeOfDay timer;
	public Text str;

    // Update is called once per frame
    void Update()
    {
    	str.text = timer.ToString();    
    }
}
