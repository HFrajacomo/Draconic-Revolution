using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TEST_AUDIO : MonoBehaviour
{
    public AudioSource source;
    public AudioManager manager;

    private int counter = 0;

    public void Awake(){
        this.manager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
    }

    public void Start(){
        this.manager.RegisterAudioSource(source, AudioUsecase.SFX_3D, 0);
    }

    public void Update(){
        if(counter % 100 == 0)
            this.manager.Play(AudioName.HAT, entity:0);

        if(counter == 200)
            this.manager.Play(AudioName.BZZT, entity:1);

        counter++;
    }
}
