using System;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioTrackVoice3D : MonoBehaviour
{
    // UnityReference
    public Transform playerPositionReference;
    private SubtitlesManager subManager;

    // AudioSources
    private Dictionary<ulong, AudioSource> sources = new Dictionary<ulong, AudioSource>();
    private Dictionary<ulong, float> distances = new Dictionary<ulong, float>();
    private Dictionary<ulong, Transform> sourcesTransform = new Dictionary<ulong, Transform>(); 

    // Current Execution
    private Dictionary<ulong, AudioName?> currentAudio = new Dictionary<ulong, AudioName?>();
    private Dictionary<ulong, AudioVolume> currentVolume = new Dictionary<ulong, AudioVolume>();

    // Globalization
    private CultureInfo cultureInfo = new CultureInfo("en-US");

    // Transcript info
    private string currentTranscript = "";
    private string fullTranscript;
    private string[] cachedSplitTranscript;
    private HashSet<AudioName> loadedTranscripts = new HashSet<AudioName>();

    // Timing Lists
    private Dictionary<ulong, List<float>> segmentTime = new Dictionary<ulong, List<float>>();
    private Dictionary<ulong, List<string>> transcriptSegments = new Dictionary<ulong, List<string>>();
    private Dictionary<ulong, List<float>> transcriptTime = new Dictionary<ulong, List<float>>();
    private Dictionary<ulong, int> currentSegment = new Dictionary<ulong, int>();
    private Dictionary<ulong, int> currentTranscriptSegment = new Dictionary<ulong, int>();

    // Cache
    private ulong closestEntity;
    private bool foundClosest = false;

    private const float HARD_VOLUME_LIMIT = 0.4f;
    private static float MAX_VOLUME = 0.4f;
    private Dictionary<ulong, bool> IS_PLAYING = new Dictionary<ulong, bool>();


    public void Awake(){
        ChangeVolume();
    }

    public void Update(){
        foundClosest = false;

        foreach(ulong entity in sources.Keys){
            if(IS_PLAYING.ContainsKey(entity) && IS_PLAYING[entity]){
                if(!sources[entity].isPlaying){
                    IS_PLAYING[entity] = false;
                    distances[entity] = 9999999f;
                }
                else{
                    if(playerPositionReference != null){
                        distances[entity] = Vector3.Distance(playerPositionReference.position, sourcesTransform[entity].position);

                        if(distances[closestEntity] >= distances[entity] && distances[entity] <= (int)currentVolume[entity]){
                            closestEntity = entity;
                            foundClosest = true;
                        }

                        HandleNextTranscriptSegment(entity);
                    }
                }          
            }  
        }

        SetTranscriptMessage(!foundClosest);
    }


    public void Play(Sound sound, string transcriptPath, AudioClip clip, int segment, int finalSegment, bool playAll, ulong entity){
        if(currentAudio.ContainsKey(entity)){
            if(currentAudio[entity] != sound.name){
                ClearCurrentAudio(entity);
            }
        }

        if(!IS_PLAYING.ContainsKey(entity))
            IS_PLAYING.Add(entity, false);

        if(!loadedTranscripts.Contains(sound.name)){
            fullTranscript = TranscriptFileHandler.ReadTranscript(transcriptPath);
            loadedTranscripts.Add(sound.name);
            sources[entity].clip = clip;
            SetTimeValues(clip.length, entity);
        }

        if(segmentTime[entity].Count == 0){
            SetTimeValues(clip.length, entity);
        }

        currentSegment[entity] = segment;
        currentAudio[entity] = sound.name;
        currentVolume[entity] = sound.volume;

        FindExactTranscriptSegment(segmentTime[entity][segment], entity);

        sources[entity].maxDistance = (float)sound.volume;
        sources[entity].SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.EaseInOut(0f, 1f, (float)sound.volume, 0f));

        sources[entity].time = segmentTime[entity][segment];
        sources[entity].Play();

        // If is a normal segment run
        if(finalSegment == -1 && !playAll){
            sources[entity].SetScheduledEndTime(AudioSettings.dspTime + (segmentTime[entity][segment+1] - sources[entity].time));
        }
        // If all audio should be played
        else if(playAll){}
        // If is a long run
        else{
            sources[entity].SetScheduledEndTime(AudioSettings.dspTime + (segmentTime[entity][finalSegment+1] - sources[entity].time));
        }

        IS_PLAYING[entity] = true;
    }

    public void RegisterAudioSource(AudioSource source, ulong entity){
        if(!this.sources.ContainsKey(entity)){
            source.spatialBlend = 1f;
            source.volume = MAX_VOLUME;
            source.spread = 60f;
            source.loop = false;

            source.dopplerLevel = 0.04f;
            source.rolloffMode = AudioRolloffMode.Custom;

            this.sources.Add(entity, source);
            this.distances.Add(entity, 9999);
            this.sourcesTransform.Add(entity, sources[entity].gameObject.transform);
            this.segmentTime.Add(entity, new List<float>());
            this.transcriptSegments.Add(entity, new List<string>());
            this.transcriptTime.Add(entity, new List<float>());
            this.currentVolume.Add(entity, AudioVolume.NONE);
            this.currentAudio.Add(entity, null);
        }
    }

    public void UnregisterAudioSource(ulong entity){
        if(this.sources.ContainsKey(entity)){
            this.sources.Remove(entity);
            this.distances.Remove(entity);
            this.sourcesTransform.Remove(entity);
            this.segmentTime.Remove(entity);
            this.transcriptSegments.Remove(entity);
            this.transcriptTime.Remove(entity);
            this.currentVolume.Remove(entity);
            this.currentAudio.Remove(entity);
        }
    }

    public void ChangeVolume(){
        MAX_VOLUME = HARD_VOLUME_LIMIT * (Configurations.voice3DVolume/100f);
    }

    public void SetPlayerPosition(Transform transform){
        this.playerPositionReference = transform;
    }

    public void DestroyTrackInfo(){
        List<ulong> removeList = new List<ulong>(sources.Keys);

        foreach(ulong entity in removeList){
            sources.Remove(entity);
            distances.Remove(entity);
            sourcesTransform.Remove(entity);
            currentAudio.Remove(entity);
            currentVolume.Remove(entity);
            segmentTime.Remove(entity);
            transcriptSegments.Remove(entity);
            transcriptTime.Remove(entity);
            currentSegment.Remove(entity);
            currentTranscriptSegment.Remove(entity);
        }

        loadedTranscripts.Clear();

    }

    /*
    Creates a reference to SubtitlesManager and should be called from within it
    */
    public void CreateSubtitlesReference(SubtitlesManager subManager){
        this.subManager = subManager;
    }

    private void HandleNextTranscriptSegment(ulong entity){
        if(transcriptTime[entity].Count == 0)
            return;

        if(sources[entity].time >= transcriptTime[entity][currentTranscriptSegment[entity]+1]){
            currentTranscriptSegment[entity]++;
        }
    }

    private void SetTranscriptMessage(bool setEmpty=false){
        if(setEmpty){
            if(currentTranscript == "")
                return;

            currentTranscript = "";
        }
        else{                
            currentTranscript = transcriptSegments[closestEntity][currentTranscriptSegment[closestEntity]];
        }

        subManager.SetTranscript3D(currentTranscript);
    }

    /*
    Sets the time for time events in the current track
    */
    private void SetTimeValues(float clipLength, ulong entity){
        bool isTimePair;
        bool isFirstTimePair;

        cachedSplitTranscript = fullTranscript.Split(TranscriptFileHandler.SEGMENT_SEPARATOR);
        
        foreach(string segment in cachedSplitTranscript){
            isTimePair = true;
            isFirstTimePair = true;

            foreach(string halfPair in segment.Split(TranscriptFileHandler.WRAPPER_SEPARATOR)){
                if(isFirstTimePair){
                    segmentTime[entity].Add(ConvertToFloat(halfPair));
                }

                if(isTimePair){
                    transcriptTime[entity].Add(ConvertToFloat(halfPair));
                }
                else{
                    transcriptSegments[entity].Add(halfPair);
                }

                isTimePair = !isTimePair;
                isFirstTimePair = false;
            }
        }

        segmentTime[entity].Add(clipLength);
        transcriptTime[entity].Add(clipLength);
        transcriptSegments[entity].Add("");
    }

    private float ConvertToFloat(string number){
        return Convert.ToSingle(number, cultureInfo);
    }

    /*
    Finds the index of the TranscriptSegment given an exact timestamp
    */
    private void FindExactTranscriptSegment(float currentTime, ulong entity){
        for(int i=0; i < transcriptTime[entity].Count; i++){
            if(transcriptTime[entity][i] == currentTime)
                currentTranscriptSegment[entity] = i;
        }
    }

    private void ClearCurrentAudio(ulong entity){
        AudioName? audio = currentAudio[entity];

        currentAudio.Remove(entity);
        currentVolume.Remove(entity);
        segmentTime[entity].Clear();
        transcriptSegments[entity].Clear();
        transcriptTime[entity].Clear();

        if(audio != null)
            if(loadedTranscripts.Contains((AudioName)audio))
                loadedTranscripts.Remove((AudioName)audio);
    }
}
