using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TEST_AUDIO : MonoBehaviour
{
    public AudioManager manager;

    private int counter = 0;


    public void Awake(){
        this.manager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
    }

    public void Update(){
        if(counter == 450){
            manager.Play(AudioName.TEST_VOICE2D, segment:0, playAll:true);
            Debug.Log("playing voice");
        }

        counter++;
    }
}
