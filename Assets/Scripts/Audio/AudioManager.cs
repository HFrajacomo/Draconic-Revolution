using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;


public class AudioManager : MonoBehaviour
{
    // Singleton
    private static AudioManager self;

    // Clip Info
    private Dictionary<AudioName, AudioClip> loadedClips = new Dictionary<AudioName, AudioClip>();
    private Dictionary<AudioName, AudioClip> cachedAudioClip = new Dictionary<AudioName, AudioClip>();
    private Dictionary<AudioName, LoadedVoiceSegment> cachedVoiceClips = new Dictionary<AudioName, LoadedVoiceSegment>();

    // Tracks
    public AudioTrackMusic2D audioTrackMusic2D;
    public AudioTrackSFX2D audioTrackSFX2D;
    public AudioTrackVoice2D audioTrackVoice2D;

    // Last Iteration info
    private DynamicMusic lastMusicGroupLoaded;

    // Cached data
    private List<AudioName> cachedAudioList;
    private List<LoadedVoiceSegment> cachedVoiceSegmentList;
    private DynamicMusic cachedMusicGroup;
    private AudioName cachedAudioName;
    private Sound cachedSound;
    private Voice cachedVoice;

    // DEBUGGING
    private int counter = 0;

    

    public void Awake(){
        DontDestroyOnLoad(this.gameObject);

        if(self == null){
            self = this;
        }
        else{
            Destroy(this.gameObject);
        }
    }

    public void Update(){
        RerunLoadedClips();

        counter++;

        if(counter == 1)
            Play(AudioName.TEST_GROUP, dynamicLevel:MusicDynamicLevel.SOFT);
        if(counter == 50)
            Play(AudioName.TEST_VOICE, segment:3);
    }

    public void Play(AudioName name, MusicDynamicLevel dynamicLevel=MusicDynamicLevel.NONE, bool bypassGroup=false, int segment=-1, int finalSegment=-1, bool playAll=false){
        if(AudioLibrary.IsSound(name))
            this.cachedSound = AudioLibrary.GetSound(name);
        else if(AudioLibrary.IsDynamicMusic(name))
            this.cachedSound = AudioLibrary.GetMusicGroup(name).wrapperSound;
        else if(AudioLibrary.IsVoice(name)){
            this.cachedVoice = AudioLibrary.GetVoice(name);
            this.cachedSound = this.cachedVoice.wrapperSound;
        }

        if(this.cachedSound.type == AudioUsecase.MUSIC_CLIP){
            if(dynamicLevel != MusicDynamicLevel.NONE)
                PlayDynamicMusic2D(name, dynamicLevel);
            else
                PlayMusic2D(name, bypassGroup);
        }
        else if(this.cachedSound.type == AudioUsecase.SFX_CLIP){
            PlaySFX2D(name);
        }
        else if(this.cachedSound.type == AudioUsecase.VOICE_CLIP){
            PlayVoice2D(this.cachedVoice, segment, finalSegment, playAll);
        }
    }


    /*
    Plays an AudioClip in AudioTrackMusic2D
        bypassGroup allows individual music from a DynamicMusic group be played standalone
    */
    public void PlayMusic2D(AudioName name, bool bypassGroup=false){
        if(loadedClips.ContainsKey(name)){
            GetClip(name).name = Enum.GetName(typeof(AudioName), name);

            if(!AudioLibrary.MusicGroupContainsSound(this.lastMusicGroupLoaded.groupName, name) || bypassGroup)
                this.audioTrackMusic2D.Play(AudioLibrary.GetSound(name), GetClip(name));
            else
                this.audioTrackMusic2D.Play(this.lastMusicGroupLoaded.wrapperSound, GetClip(name), isDynamic:true);
        }
        else{
            LoadAudioClip(name);
        }
    }

    /*
    Plays an AudioClip from a DynamicMusic based on it's DynamicLevel
    */
    public void PlayDynamicMusic2D(AudioName name, MusicDynamicLevel level){
        if(AudioLibrary.IsSound(name))
            Play(name);

        this.cachedMusicGroup = AudioLibrary.GetMusicGroup(name);
        this.cachedAudioName = this.cachedMusicGroup.Get(level).name;

        if(IsClipLoaded(this.cachedAudioName))
            this.audioTrackMusic2D.Play(this.cachedMusicGroup.wrapperSound, GetClip(this.cachedAudioName), isDynamic:true);
        else
            this.lastMusicGroupLoaded = this.cachedMusicGroup;


        if(!IsClipLoaded(this.cachedMusicGroup.Get(MusicDynamicLevel.SOFT).name))
            ConditionalDynamicClipLoad(MusicDynamicLevel.SOFT, level);
        if(!IsClipLoaded(this.cachedMusicGroup.Get(MusicDynamicLevel.MEDIUM).name))
            ConditionalDynamicClipLoad(MusicDynamicLevel.MEDIUM, level);
        if(!IsClipLoaded(this.cachedMusicGroup.Get(MusicDynamicLevel.HARD).name))
            ConditionalDynamicClipLoad(MusicDynamicLevel.HARD, level);
    }

    /*
    Plays an AudioClip in single-shot fashion
    */
    public void PlaySFX2D(AudioName name){
        if(loadedClips.ContainsKey(name)){
            this.audioTrackSFX2D.Play(GetClip(name));
        }
        else{
            LoadAudioClip(name);
        }
    }

    /*
    Plays a segment of a voice clip
    */
    public void PlayVoice2D(Voice voice, int segment, int finalSegment, bool playAll){
        if(loadedClips.ContainsKey(voice.name)){
            this.audioTrackVoice2D.Play(voice.wrapperSound, voice.GetTranscriptPath(), GetClip(voice.name), segment, finalSegment, playAll);
        }
        else{
            LoadAudioClip(voice.name, segment, finalSegment, playAll);
        }        
    }

    public void Stop(AudioUsecase type, bool fade=false){
        switch(type){
            case AudioUsecase.MUSIC_CLIP:
                StopMusic2D(fade:fade);
                break;
            default:
                StopMusic2D(fade:fade);
                break;
        }
    }

    private void StopMusic2D(bool fade){
        this.audioTrackMusic2D.Stop(fade:fade);
    }

    private void RerunLoadedClips(){
        if(cachedAudioClip.Count > 0){
            this.cachedAudioList = new List<AudioName>(cachedAudioClip.Keys);

            foreach(AudioName name in this.cachedAudioList){
                loadedClips.Add(name, this.cachedAudioClip[name]);
                cachedAudioClip.Remove(name);
                this.Play(name);
            }

            this.cachedAudioList.Clear();
        }

        if(cachedVoiceClips.Count > 0){
            this.cachedVoiceSegmentList = new List<LoadedVoiceSegment>(cachedVoiceClips.Values);

            foreach(LoadedVoiceSegment lvs in this.cachedVoiceSegmentList){
                loadedClips.Add(lvs.name, lvs.clip);
                cachedVoiceClips.Remove(lvs.name);
                this.Play(lvs.name, segment:lvs.segment, finalSegment:lvs.finalSegment, playAll:lvs.playAll);
            }
        }
    }

    private AudioClip GetClip(AudioName name){
        return this.loadedClips[name];
    }

    private bool IsClipLoaded(AudioName name){
        return this.loadedClips.ContainsKey(name);
    }

    private void LoadAudioClip(AudioName name, bool autoplay=true){
        StartCoroutine(GetAudioClip(name, autoplay));
    }

    private void LoadAudioClip(AudioName name, int segment, int finalSegment, bool playAll){
        StartCoroutine(GetAudioClip(name, segment, finalSegment, playAll));
    }

    private IEnumerator GetAudioClip(AudioName name, bool autoplay)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(AudioLibrary.GetSound(name).GetFilePath(), AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if(www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
            }
            else
            {
                if(autoplay)
                    this.cachedAudioClip.Add(name, DownloadHandlerAudioClip.GetContent(www));
                else
                    this.loadedClips.Add(name, DownloadHandlerAudioClip.GetContent(www));
            }
        }
    }

    // Alternative version of loading voice clips
    private IEnumerator GetAudioClip(AudioName name, int segment, int finalSegment, bool playAll)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(AudioLibrary.GetVoice(name).GetFilePath(), AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if(www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
            }
            else
            {
                this.cachedVoiceClips.Add(name, new LoadedVoiceSegment(name, segment, finalSegment, playAll, DownloadHandlerAudioClip.GetContent(www)));
            }
        }
    }

    private void ConditionalDynamicClipLoad(MusicDynamicLevel evaluatedLevel, MusicDynamicLevel trueLevel){
        if(trueLevel == evaluatedLevel)
            LoadAudioClip(this.cachedMusicGroup.Get(evaluatedLevel).name);
        else
            LoadAudioClip(this.cachedMusicGroup.Get(evaluatedLevel).name, autoplay:false);     
    }
}


public struct LoadedVoiceSegment{
    public AudioName name;
    public int segment;
    public int finalSegment;
    public bool playAll;
    public AudioClip clip;

    public LoadedVoiceSegment(AudioName name, int segment, int finalSegment, bool playAll, AudioClip clip){
        this.name = name;
        this.segment = segment;
        this.finalSegment = finalSegment;
        this.playAll = playAll;
        this.clip = clip;
    }
}