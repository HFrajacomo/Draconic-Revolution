using System;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioTrackVoice2D : MonoBehaviour
{
    // Unity Reference
    private SubtitlesManager subManager;

    // AudioSource
    private AudioSource audioSource;

    // Globalization
    private CultureInfo cultureInfo = new CultureInfo("en-US"); 

    // Transcript info
    private AudioName? currentVoice;
    private string currentTranscript = "";
    private string fullTranscript;
    private string[] cachedSplitTranscript;

    // Timing Lists
    private List<float> segmentTime = new List<float>();
    private List<string> transcriptSegments = new List<string>();
    private List<float> transcriptTime = new List<float>();
    private int currentSegment;
    private int currentTranscriptSegment;

    private const float HARD_VOLUME_LIMIT = 0.4f;
    private static float MAX_VOLUME = 0.4f;
    private bool IS_PLAYING = false;


    public void Awake(){
        audioSource = gameObject.AddComponent<AudioSource>();
        
        ChangeVolume();

        audioSource.spatialBlend = 0f;
        audioSource.volume = MAX_VOLUME;
        audioSource.spread = 180f;
        audioSource.loop = false;
        audioSource.bypassReverbZones = true;
    }

    public void Update(){
        if(IS_PLAYING){
            if(!audioSource.isPlaying){
                IS_PLAYING = false;
                SetTranscriptMessage(0, setEmpty:true);
            }
            else{
                HandleNextTranscriptSegment();
            }
        }
    }

    /*
    Plays the voice in a given segment and stops playing after a segment has passed
    returns the transcript of the Audio in the given segment
    */
    public void Play(Sound sound, string transcriptPath, AudioClip clip, int segment, int finalSegment, bool playAll){        
        if(currentVoice == null || currentVoice != sound.name){
            ClearAllLists();

            currentVoice = sound.name;            
            fullTranscript = TranscriptFileHandler.ReadTranscript(transcriptPath);
            audioSource.clip = clip;
            SetTimeValues(clip.length);
        }
        if(segmentTime.Count == 0){
            SetTimeValues(clip.length);
        }


        currentSegment = segment;


        FindExactTranscriptSegment(segmentTime[segment]);
        SetTranscriptMessage(currentTranscriptSegment);

        audioSource.time = segmentTime[segment];
        audioSource.Play();

        // If is a normal segment run
        if(finalSegment == -1 && !playAll){
            audioSource.SetScheduledEndTime(AudioSettings.dspTime + (segmentTime[segment+1] - audioSource.time));
        }
        // If all audio should be played
        else if(playAll){}
        // If is a long run
        else{
            audioSource.SetScheduledEndTime(AudioSettings.dspTime + (segmentTime[finalSegment+1] - audioSource.time));
        }


        IS_PLAYING = true;
    }

    /*
    Creates a reference to SubtitlesManager and should be called from within it
    */
    public void CreateSubtitlesReference(SubtitlesManager subManager){
        this.subManager = subManager;
    }

    public void ChangeVolume(){
        MAX_VOLUME =  HARD_VOLUME_LIMIT * (Configurations.voice2DVolume/100f);
        this.audioSource.volume = MAX_VOLUME;
    }

    /*
    Sets the time for time events in the current track
    */
    private void SetTimeValues(float clipLength){
        bool isTimePair;
        bool isFirstTimePair;

        cachedSplitTranscript = fullTranscript.Split(TranscriptFileHandler.SEGMENT_SEPARATOR);
        
        foreach(string segment in cachedSplitTranscript){
            isTimePair = true;
            isFirstTimePair = true;

            foreach(string halfPair in segment.Split(TranscriptFileHandler.WRAPPER_SEPARATOR)){
                if(isFirstTimePair){
                    segmentTime.Add(ConvertToFloat(halfPair));
                }

                if(isTimePair){
                    transcriptTime.Add(ConvertToFloat(halfPair));
                }
                else{
                    transcriptSegments.Add(halfPair);
                }

                isTimePair = !isTimePair;
                isFirstTimePair = false;
            }
        }

        segmentTime.Add(clipLength);
        transcriptTime.Add(clipLength);
        transcriptSegments.Add("");
    }


    /*
    Finds the index of the TranscriptSegment given an exact timestamp
    */
    private void FindExactTranscriptSegment(float currentTime){
        for(int i=0; i < transcriptTime.Count; i++){
            if(transcriptTime[i] == currentTime)
                currentTranscriptSegment = i;
        }
    }

    private void HandleNextTranscriptSegment(){
        if(transcriptTime.Count == 0)
            return;

        if(audioSource.time >= transcriptTime[currentTranscriptSegment+1]){
            currentTranscriptSegment++;
            SetTranscriptMessage(currentTranscriptSegment);
        }
    }

    /*
    In the future, this function should send segment information to a subtitle drawing object
    */
    private void SetTranscriptMessage(int index, bool setEmpty=false){
        if(!setEmpty)
            currentTranscript = transcriptSegments[index];
        else
            currentTranscript = "";

        subManager.SetTranscript2D(currentTranscript);
    }

    private float ConvertToFloat(string number){
        return Convert.ToSingle(number, cultureInfo);
    }

    private void ClearAllLists(){
        transcriptTime.Clear();
        transcriptSegments.Clear();
    }
}
