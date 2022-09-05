using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TEST_AUDIO : MonoBehaviour
{
    public AudioSource source;
    public AudioManager manager;

    private int counter = 0;
    private GameObject goTest;
    private GameObject goTest2;


    public void Awake(){
        this.manager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
    }

    public void Start(){
        this.manager.RegisterAudioSource(source, AudioUsecase.SFX_3D, 0);
    }

    public void Update(){
        if(counter == 600){
            goTest = new GameObject("testEntity");
            goTest.transform.position = new Vector3(20, 95, 0);
            AudioSource source = goTest.AddComponent<AudioSource>();
            
            manager.RegisterAudioSource(source, AudioUsecase.VOICE_3D, 0); 
            manager.Play(AudioName.TEST_VOICE2D, segment:1, finalSegment:3, entity:0);
        }

        if(counter == 700){
            goTest2 = new GameObject("testEntity2");
            goTest2.transform.position = new Vector3(3, 95, 0);
            AudioSource source = goTest2.AddComponent<AudioSource>();
            
            manager.RegisterAudioSource(source, AudioUsecase.VOICE_3D, 1);
            manager.Play(AudioName.TEST_VOICE3D, segment:0, finalSegment:3, entity:1);
        }

        counter++;
    }
}
