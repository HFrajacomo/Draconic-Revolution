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

    // Tracks
    public AudioTrackMusic2D audioTrackMusic2D;

    // Last Iteration info
    private DynamicMusic lastMusicGroupLoaded;

    // Cached data
    private List<AudioName> cachedAudioList;
    private DynamicMusic cachedMusicGroup;
    private AudioName cachedAudioName;

    

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
    }


    /*
    Plays an AudioClip in AudioTrackMusic2D
        bypassGroup allows individual music from a DynamicMusic group be played standalone
    */
    public void Play(AudioName name, bool bypassGroup=false){
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
    public void Play(AudioName name, MusicDynamicLevel level){
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

    private IEnumerator GetAudioClip(AudioName name, bool autoplay)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(AudioLibrary.GetSound(name).GetFilePath(), AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError)
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

    private void ConditionalDynamicClipLoad(MusicDynamicLevel evaluatedLevel, MusicDynamicLevel trueLevel){
        if(trueLevel == evaluatedLevel)
            LoadAudioClip(this.cachedMusicGroup.Get(evaluatedLevel).name);
        else
            LoadAudioClip(this.cachedMusicGroup.Get(evaluatedLevel).name, autoplay:false);     
    }
}
