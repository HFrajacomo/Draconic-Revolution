using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TEST_AUDIO : MonoBehaviour
{
    public int counter = 0;
    public AudioManager audioManager;

    void Start(){
        this.audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if(counter == 500)
            audioManager.Play("Grass_Mountains_Group", dynamicLevel:MusicDynamicLevel.SOFT);

        if(counter == 5000)
            audioManager.Play("Grass_Mountains_Group", dynamicLevel:MusicDynamicLevel.MEDIUM);

        if(counter == 10000)
            audioManager.Play("Grass_Mountains_Group", dynamicLevel:MusicDynamicLevel.HARD);

        if(counter == 15000)
            audioManager.Play("Grass_Mountains_Group", dynamicLevel:MusicDynamicLevel.MEDIUM);

        if(counter == 20000)
            audioManager.Play("Grass_Mountains_Group", dynamicLevel:MusicDynamicLevel.SOFT);

        counter++;
    }
}
