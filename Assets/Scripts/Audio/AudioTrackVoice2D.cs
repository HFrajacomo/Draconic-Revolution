using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioTrackVoice2D : MonoBehaviour
{
    // AudioSource
    private AudioSource audioSource;

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

    private float MAX_VOLUME = 0.2f;
    private bool IS_PLAYING = false;


    public void Awake(){
        audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 0f;
        audioSource.volume = MAX_VOLUME;
        audioSource.spread = 180f;
        audioSource.loop = false;
    }

    public void Update(){
        if(IS_PLAYING)
            if(!audioSource.isPlaying)
                SetTranscriptMessage(0, setEmpty:true);
    }

    /*
    Plays the voice in a given segment and stops playing after a segment has passed
    returns the transcript of the Audio in the given segment
    */
    public void Play(Sound sound, string transcriptPath, AudioClip clip, int segment){
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


        if(audioSource.isPlaying)
            return;

        currentSegment = segment;

        audioSource.time = segmentTime[segment];
        audioSource.SetScheduledEndTime(segmentTime[segment+1]);

        FindExactTranscriptSegment(segmentTime[segment]);
        SetTranscriptMessage(currentTranscriptSegment);

        audioSource.Play();
        IS_PLAYING = true;

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

    /*
    In the future, this function should send segment information to a subtitle drawing object
    */
    private void SetTranscriptMessage(int index, bool setEmpty=false){
        if(!setEmpty)
            currentTranscript = transcriptSegments[index];
        else
            currentTranscript = "";

        Debug.Log("<Transcript> " + currentTranscript);
    }

    private float ConvertToFloat(string number){
        return Convert.ToSingle(number);
    }

    private void ClearAllLists(){
        transcriptTime.Clear();
        transcriptSegments.Clear();
    }
}
