using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SubtitlesManager : MonoBehaviour
{
    // Unity Reference
    public TextMeshProUGUI subtitles;

    // Strings
    private string transcript2D = "";
    private string transcript3D = "";

    // Audio
    private AudioTrackVoice2D track2D;
    private AudioTrackVoice3D track3D;
    private AudioManager audioManager;

    public void Start()
    {
        this.audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        this.track2D = this.audioManager.GetComponent<AudioTrackVoice2D>();
        this.track3D = this.audioManager.GetComponent<AudioTrackVoice3D>();

        this.track2D.CreateSubtitlesReference(this);
        this.track3D.CreateSubtitlesReference(this);
    } 

    public void Update(){
        if(Configurations.subtitlesOn){
            if(transcript2D != "")
                subtitles.text = transcript2D;
            else if(transcript3D != "")
                subtitles.text = transcript3D;
            else
                subtitles.text = "";
        }
    }

    public void SetTranscript2D(string transcript){this.transcript2D = transcript;}
    public void SetTranscript3D(string transcript){this.transcript3D = transcript;}

}
