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
    private Dictionary<AudioName, List<LoadedSFX>> cachedSFXClips = new Dictionary<AudioName, List<LoadedSFX>>();

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
    private List<AudioName> cachedAudioList;
    private List<LoadedVoiceSegment> cachedVoiceSegmentList;
    private List<AudioName> cachedSFXList;
    private DynamicMusic cachedMusicGroup;
    private AudioName cachedAudioName;
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

    public void Play(AudioName name, MusicDynamicLevel dynamicLevel=MusicDynamicLevel.NONE, bool bypassGroup=false, int segment=-1, int finalSegment=-1, bool playAll=false, ulong entity=0, ChunkPos? chunk=null){
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
        else if(this.cachedSound.type == AudioUsecase.MUSIC_3D){
            PlayMusic3D(name, entity);
        }
        else if(this.cachedSound.type == AudioUsecase.SFX_CLIP){
            PlaySFX2D(name);
        }
        else if(this.cachedSound.type == AudioUsecase.SFX_3D || this.cachedSound.type == AudioUsecase.SFX_3D_LOOP){
            PlaySFX3D(this.cachedSound, entity, (ChunkPos)chunk);
        }
        else if(this.cachedSound.type == AudioUsecase.VOICE_CLIP){
            PlayVoice2D(this.cachedVoice, segment, finalSegment, playAll);
        }
        else if(this.cachedSound.type == AudioUsecase.VOICE_3D){
            PlayVoice3D(this.cachedVoice, segment, finalSegment, playAll, entity);
        }
    }


    /*
    Plays an AudioClip in AudioTrackMusic2D
        bypassGroup allows individual music from a DynamicMusic group be played standalone
    */
    public void PlayMusic2D(AudioName name, bool bypassGroup=false){
        if(loadedClips.ContainsKey(name)){
            GetClip(name).name = Enum.GetName(typeof(AudioName), name);

            this.audioTrackMusic2D.Play(AudioLibrary.GetSound(name), GetClip(name));
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
    Plays an AudioClip in AudioTrackMusic23
    */
    public void PlayMusic3D(AudioName name, ulong entity){
        if(loadedClips.ContainsKey(name)){
            GetClip(name).name = Enum.GetName(typeof(AudioName), name);
            this.audioTrackMusic3D.Play(AudioLibrary.GetSound(name), GetClip(name), entity);
        }
        else{
            LoadAudioClip(name);
        }
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
    Plays an SFX AudioClip in the 3D environment
    */
    public void PlaySFX3D(Sound sound, ulong entity, ChunkPos pos){
        if(loadedClips.ContainsKey(sound.name)){
            this.audioTrackSFX3D.Play(sound, GetClip(sound.name), entity, pos);
        }
        else{
            LoadAudioClip(sound.name, pos, entity);
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

    /*
    Plays a segment of a Voice in 3D environment
    */
    public void PlayVoice3D(Voice voice, int segment, int finalSegment, bool playAll, ulong entity){
        if(loadedClips.ContainsKey(voice.name)){
            this.audioTrackVoice3D.Play(voice.wrapperSound, voice.GetTranscriptPath(), GetClip(voice.name), segment, finalSegment, playAll, entity);
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
    public void RegisterAudioSource(AudioSource source, AudioUsecase type, ulong entityCode, ChunkPos? pos=null){
        if(type == AudioUsecase.MUSIC_3D){
            this.audioTrackMusic3D.RegisterAudioSource(entityCode, source);
        }
        else if(type == AudioUsecase.SFX_3D || type == AudioUsecase.SFX_3D_LOOP){
            this.audioTrackSFX3D.RegisterAudioSource(entityCode, source, (ChunkPos)pos);
        }
        else if(type == AudioUsecase.VOICE_3D){
            this.audioTrackVoice3D.RegisterAudioSource(source, entityCode);
        }
    }

    /*
    Un-register an AudioSource to a AudioTrack3D that has the given usecase
    */
    public void UnregisterAudioSource(AudioUsecase type, ulong entityCode, ChunkPos? pos=null){
        if(type == AudioUsecase.MUSIC_3D){
            this.audioTrackMusic3D.UnregisterAudioSource(entityCode);
        }
        else if(type == AudioUsecase.SFX_3D || type == AudioUsecase.SFX_3D_LOOP){
            this.audioTrackSFX3D.UnregisterAudioSource(entityCode, (ChunkPos)pos);
        }
        else if(type == AudioUsecase.VOICE_3D){
            this.audioTrackVoice3D.UnregisterAudioSource(entityCode);
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

        if(cachedSFXClips.Count > 0){
            this.cachedSFXList = new List<AudioName>(cachedSFXClips.Keys);

            foreach(AudioName name in this.cachedSFXList){
                foreach(LoadedSFX lsfx in cachedSFXClips[name]){
                    if(loadedClips.ContainsKey(lsfx.name))
                        continue;
                        
                    loadedClips.Add(lsfx.name, lsfx.clip);
                    this.Play(lsfx.name, chunk:lsfx.pos, entity:lsfx.entityCode);
                }

                cachedSFXClips.Remove(name);
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

    private void LoadAudioClip(AudioName name, ChunkPos pos, ulong entity){
        StartCoroutine(GetAudioClip(name, pos, entity));        
    }

    // Loads music into AudioClip via Streaming method
    private IEnumerator GetAudioClip(AudioName name, bool autoplay)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(AudioLibrary.GetSound(name).GetFilePath(), AudioType.OGGVORBIS))
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
    private IEnumerator GetAudioClip(AudioName name, int segment, int finalSegment, bool playAll)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(AudioLibrary.GetVoice(name).GetFilePath(), AudioType.OGGVORBIS))
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

    // Alternative version of loading SFX for blocks
    private IEnumerator GetAudioClip(AudioName name, ChunkPos pos, ulong entity)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(AudioLibrary.GetSound(name).GetFilePath(), AudioType.OGGVORBIS))
        {
            yield return www.SendWebRequest();

            if(www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
                yield return 0;
            }

            if(!this.cachedSFXClips.ContainsKey(name))
                this.cachedSFXClips.Add(name, new List<LoadedSFX>());

            this.cachedSFXClips[name].Add(new LoadedSFX(name, pos, entity, DownloadHandlerAudioClip.GetContent(www)));
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

public struct LoadedSFX{
    public AudioName name;
    public ChunkPos pos;
    public ulong entityCode;
    public AudioClip clip;

    public LoadedSFX(AudioName name, ChunkPos pos, ulong code, AudioClip clip){
        this.name = name;
        this.pos = pos;
        this.entityCode = code;
        this.clip = clip;
    }
}