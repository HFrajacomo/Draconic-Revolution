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
    private Dictionary<string, AudioClip> loadedClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> cachedAudioClip = new Dictionary<string, AudioClip>();
    private Dictionary<string, LoadedVoiceSegment> cachedVoiceClips = new Dictionary<string, LoadedVoiceSegment>();
    private Dictionary<string, List<LoadedSFX>> cachedSFXClips = new Dictionary<string, List<LoadedSFX>>();

    // Tracks
    public AudioTrackMusic2D audioTrackMusic2D;
    public AudioTrackMusic3D audioTrackMusic3D;
    public AudioTrackSFX2D audioTrackSFX2D;
    public AudioTrackSFX3D audioTrackSFX3D;
    public AudioTrackVoice2D audioTrackVoice2D;
    public AudioTrackVoice3D audioTrackVoice3D;

    // Last Iteration info
    private DynamicMusic lastMusicGroupLoaded;

    // Cached data
    private List<string> cachedAudioList;
    private List<LoadedVoiceSegment> cachedVoiceSegmentList;
    private List<string> cachedSFXList;
    private DynamicMusic cachedMusicGroup;
    private string cachedstring;
    private Sound cachedSound;
    private Voice cachedVoice;


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

    public void Play(string name, EntityID? entity, MusicDynamicLevel dynamicLevel=MusicDynamicLevel.NONE, bool bypassGroup=false, int segment=-1, int finalSegment=-1, bool playAll=false, ChunkPos? chunk=null){
        if(AudioLoader.IsSound(name))
            this.cachedSound = AudioLoader.GetSound(name);
        else if(AudioLoader.IsDynamicMusic(name))
            this.cachedSound = AudioLoader.GetMusicGroup(name).Get(dynamicLevel);
        else if(AudioLoader.IsVoice(name)){
            this.cachedVoice = AudioLoader.GetVoice(name);
            this.cachedSound = this.cachedVoice.GetSound();
        }

        if(this.cachedSound.GetUsecaseType() == AudioUsecase.MUSIC_CLIP){
            if(dynamicLevel != MusicDynamicLevel.NONE)
                PlayDynamicMusic2D(name, dynamicLevel);
            else
                PlayMusic2D(name, bypassGroup);
        }
        else if(this.cachedSound.GetUsecaseType() == AudioUsecase.MUSIC_3D){
            PlayMusic3D(name, (EntityID)entity);
        }
        else if(this.cachedSound.GetUsecaseType() == AudioUsecase.SFX_CLIP){
            PlaySFX2D(name);
        }
        else if(this.cachedSound.GetUsecaseType() == AudioUsecase.SFX_3D || this.cachedSound.GetUsecaseType() == AudioUsecase.SFX_3D_LOOP){
            PlaySFX3D(this.cachedSound, (EntityID)entity);
        }
        else if(this.cachedSound.GetUsecaseType() == AudioUsecase.VOICE_CLIP){
            PlayVoice2D(this.cachedVoice, segment, finalSegment, playAll);
        }
        else if(this.cachedSound.GetUsecaseType() == AudioUsecase.VOICE_3D){
            PlayVoice3D(this.cachedVoice, segment, finalSegment, playAll, (EntityID)entity);
        }
    }


    /*
    Plays an AudioClip in AudioTrackMusic2D
        bypassGroup allows individual music from a DynamicMusic group be played standalone
    */
    public void PlayMusic2D(string name, bool bypassGroup=false){
        if(loadedClips.ContainsKey(name)){
            GetClip(name).name = name;

            this.audioTrackMusic2D.Play(AudioLoader.GetSound(name), GetClip(name));
        }
        else{
            LoadAudioClip(name);
        }
    }

    /*
    Plays an AudioClip from a DynamicMusic based on it's DynamicLevel
    */
    public void PlayDynamicMusic2D(string name, MusicDynamicLevel level){
        if(AudioLoader.IsSound(name))
            Play(name, null);

        this.cachedMusicGroup = AudioLoader.GetMusicGroup(name);
        this.cachedstring = this.cachedMusicGroup.Get(level).name;

        if(IsClipLoaded(this.cachedstring))
            this.audioTrackMusic2D.Play(this.cachedMusicGroup.Get(level), GetClip(this.cachedstring), isDynamic:true);
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
    Plays an AudioClip in AudioTrackMusic23
    */
    public void PlayMusic3D(string name, EntityID entity){
        if(loadedClips.ContainsKey(name)){
            GetClip(name).name = Enum.GetName(typeof(string), name);
            this.audioTrackMusic3D.Play(AudioLoader.GetSound(name), GetClip(name), entity);
        }
        else{
            LoadAudioClip(name);
        }
    }

    /*
    Plays an AudioClip in single-shot fashion
    */
    public void PlaySFX2D(string name){
        if(loadedClips.ContainsKey(name)){
            this.audioTrackSFX2D.Play(GetClip(name));
        }
        else{
            LoadAudioClip(name);
        }
    }

    /*
    Plays an SFX AudioClip in the 3D environment
    */
    public void PlaySFX3D(Sound sound, EntityID entity){
        if(loadedClips.ContainsKey(sound.name)){
            this.audioTrackSFX3D.Play(sound, GetClip(sound.name), entity);
        }
        else{
            LoadAudioClip(sound.name, entity);
        }
    }

    /*
    Plays a segment of a voice clip
    */
    public void PlayVoice2D(Voice voice, int segment, int finalSegment, bool playAll){
        if(loadedClips.ContainsKey(voice.name)){
            this.audioTrackVoice2D.Play(voice.GetSound(), voice.GetTranscriptPath(), GetClip(voice.name), segment, finalSegment, playAll);
        }
        else{
            LoadAudioClip(voice.name, segment, finalSegment, playAll);
        }        
    }

    /*
    Plays a segment of a Voice in 3D environment
    */
    public void PlayVoice3D(Voice voice, int segment, int finalSegment, bool playAll, EntityID entity){
        if(loadedClips.ContainsKey(voice.name)){
            this.audioTrackVoice3D.Play(voice.GetSound(), voice.GetTranscriptPath(), GetClip(voice.name), segment, finalSegment, playAll, entity);
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

    /*
    Register an AudioSource to a AudioTrack3D that has the given usecase
    */
    public void RegisterAudioSource(AudioSource source, AudioUsecase type, EntityID entityID){
        if(type == AudioUsecase.MUSIC_3D){
            this.audioTrackMusic3D.RegisterAudioSource(entityID, source);
        }
        else if(type == AudioUsecase.SFX_3D || type == AudioUsecase.SFX_3D_LOOP){
            this.audioTrackSFX3D.RegisterAudioSource(entityID, source);
        }
        else if(type == AudioUsecase.VOICE_3D){
            this.audioTrackVoice3D.RegisterAudioSource(source, entityID);
        }
    }

    /*
    Un-register an AudioSource to a AudioTrack3D that has the given usecase
    */
    public void UnregisterAudioSource(AudioUsecase type, EntityID entityID, ChunkPos? pos=null){
        if(type == AudioUsecase.MUSIC_3D){
            this.audioTrackMusic3D.UnregisterAudioSource(entityID);
        }
        else if(type == AudioUsecase.SFX_3D || type == AudioUsecase.SFX_3D_LOOP){
            this.audioTrackSFX3D.UnregisterAudioSource(entityID);
        }
        else if(type == AudioUsecase.VOICE_3D){
            this.audioTrackVoice3D.UnregisterAudioSource(entityID);
        } 
    }

    // Sets the Player position for 3D voice transcript writing
    public void SetPlayerPositionInVoice3DTrack(Transform transform){
        audioTrackVoice3D.SetPlayerPosition(transform);
    }

    // Sets the volume of all tracks when game begins
    public void RefreshVolume(){
        this.audioTrackVoice3D.ChangeVolume();
        this.audioTrackVoice2D.ChangeVolume();
        this.audioTrackSFX3D.ChangeVolume();
        this.audioTrackSFX2D.ChangeVolume();
        this.audioTrackMusic3D.ChangeVolume();
        this.audioTrackMusic2D.ChangeVolume();
    }

    /*
    Destroys all information in Tracks that would break when reloaded
    */
    public void Destroy(){
        this.audioTrackVoice3D.DestroyTrackInfo();
        this.audioTrackSFX3D.DestroyTrackInfo();
        this.audioTrackMusic3D.DestroyTrackInfo();
    }

    private void StopMusic2D(bool fade){
        this.audioTrackMusic2D.Stop(fade:fade);
    }

    private void RerunLoadedClips(){
        if(cachedAudioClip.Count > 0){
            this.cachedAudioList = new List<string>(cachedAudioClip.Keys);

            foreach(string name in this.cachedAudioList){
                loadedClips.Add(name, this.cachedAudioClip[name]);
                cachedAudioClip.Remove(name);
                this.Play(name, null);
            }

            this.cachedAudioList.Clear();
        }

        if(cachedVoiceClips.Count > 0){
            this.cachedVoiceSegmentList = new List<LoadedVoiceSegment>(cachedVoiceClips.Values);

            foreach(LoadedVoiceSegment lvs in this.cachedVoiceSegmentList){
                loadedClips.Add(lvs.name, lvs.clip);
                cachedVoiceClips.Remove(lvs.name);
                this.Play(lvs.name, null, segment:lvs.segment, finalSegment:lvs.finalSegment, playAll:lvs.playAll);
            }
        }

        if(cachedSFXClips.Count > 0){
            this.cachedSFXList = new List<string>(cachedSFXClips.Keys);

            foreach(string name in this.cachedSFXList){
                foreach(LoadedSFX lsfx in cachedSFXClips[name]){
                    if(loadedClips.ContainsKey(lsfx.name))
                        continue;
                        
                    loadedClips.Add(lsfx.name, lsfx.clip);
                    this.Play(lsfx.name, lsfx.entity, chunk:lsfx.entity.pos);
                }

                cachedSFXClips.Remove(name);
            }
        }
    }

    private AudioClip GetClip(string name){
        return this.loadedClips[name];
    }

    private bool IsClipLoaded(string name){
        return this.loadedClips.ContainsKey(name);
    }

    private void LoadAudioClip(string name, bool autoplay=true){
        StartCoroutine(GetAudioClip(name, autoplay));
    }

    private void LoadAudioClip(string name, int segment, int finalSegment, bool playAll){
        StartCoroutine(GetAudioClip(name, segment, finalSegment, playAll));
    }

    private void LoadAudioClip(string name, EntityID entity){
        StartCoroutine(GetAudioClip(name, entity));        
    }

    // Loads music into AudioClip via Streaming method
    private IEnumerator GetAudioClip(string name, bool autoplay)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(AudioLoader.GetSound(name).GetFilePath(), AudioType.OGGVORBIS))
        {
            yield return www.SendWebRequest();

            if(www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
                yield return 0;
            }

            ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = true;

            if(autoplay){
                AudioClip clip = ((DownloadHandlerAudioClip)www.downloadHandler).audioClip;
                this.cachedAudioClip.Add(name, clip);
            }
            else{
                AudioClip clip = ((DownloadHandlerAudioClip)www.downloadHandler).audioClip;
                this.loadedClips.Add(name, clip);
            }
        }
    }

    // Alternative version of loading voice clips
    private IEnumerator GetAudioClip(string name, int segment, int finalSegment, bool playAll)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(AudioLoader.GetVoice(name).GetSound().GetFilePath(), AudioType.OGGVORBIS))
        {
            yield return www.SendWebRequest();

            if(www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
                yield return 0;
            }

            this.cachedVoiceClips.Add(name, new LoadedVoiceSegment(name, segment, finalSegment, playAll, DownloadHandlerAudioClip.GetContent(www)));
        }
    }

    // Alternative version of loading SFX for blocks or entities
    private IEnumerator GetAudioClip(string name, EntityID entity)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(AudioLoader.GetSound(name).GetFilePath(), AudioType.OGGVORBIS))
        {
            yield return www.SendWebRequest();

            if(www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
                yield return 0;
            }

            if(!this.cachedSFXClips.ContainsKey(name))
                this.cachedSFXClips.Add(name, new List<LoadedSFX>());

            this.cachedSFXClips[name].Add(new LoadedSFX(name, entity, DownloadHandlerAudioClip.GetContent(www)));
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
    public string name;
    public int segment;
    public int finalSegment;
    public bool playAll;
    public AudioClip clip;

    public LoadedVoiceSegment(string name, int segment, int finalSegment, bool playAll, AudioClip clip){
        this.name = name;
        this.segment = segment;
        this.finalSegment = finalSegment;
        this.playAll = playAll;
        this.clip = clip;
    }
}

public struct LoadedSFX{
    public string name;
    public EntityID entity;
    public AudioClip clip;

    public LoadedSFX(string name, EntityID entity, AudioClip clip){
        this.name = name;
        this.entity = entity;
        this.clip = clip;
    }
}