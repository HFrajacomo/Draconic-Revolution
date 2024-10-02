using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class DynamicMusic
{
    private static string audioDir = "file://" + Application.streamingAssetsPath + "/Audio/";

    private static Dictionary<AudioUsecase, string> folderMap = new Dictionary<AudioUsecase, string>(){
        {AudioUsecase.MUSIC_CLIP, "music_clip/"},
        {AudioUsecase.MUSIC_3D, "music_3d/"},
        {AudioUsecase.SFX_CLIP, "sfx_clip/"},
        {AudioUsecase.SFX_3D, "sfx_3d/"},
        {AudioUsecase.VOICE_CLIP, "voice_clip/"},
        {AudioUsecase.VOICE_3D, "voice_3d/"}
    };

    public string name;
    public string serializedType;
    public string serializedVolume;
    public string soundLight;
    public string soundMid;
    public string soundHeavy;
    private AudioUsecase type;
    private AudioVolume volume;
    private Dictionary<MusicDynamicLevel, Sound> soundByDynamic;


    public Sound Get(MusicDynamicLevel level){
        return this.soundByDynamic[level];
    }

    public void PostDeserializationSetup(){
        this.soundByDynamic = new Dictionary<MusicDynamicLevel, Sound>();
        this.soundByDynamic.Add(MusicDynamicLevel.SOFT, AudioLoader.GetSound(this.soundLight));
        this.soundByDynamic.Add(MusicDynamicLevel.MEDIUM, AudioLoader.GetSound(this.soundMid));
        this.soundByDynamic.Add(MusicDynamicLevel.HARD, AudioLoader.GetSound(this.soundHeavy));

        this.type = Sound.ConvertUsecase(this.serializedType);
        this.volume = Sound.ConvertVolume(this.serializedVolume);
    }

    public override string ToString(){
        return $"{this.name}\n{this.soundLight}\n{this.soundMid}\n{this.soundHeavy}";
    }

    public string GetFilePath(MusicDynamicLevel level){
        return this.soundByDynamic[level].GetFilePath();
    }

    public bool ContainsSound(string audio){
        return (this.soundByDynamic[MusicDynamicLevel.SOFT].name == audio) || (this.soundByDynamic[MusicDynamicLevel.MEDIUM].name == audio) || (this.soundByDynamic[MusicDynamicLevel.HARD].name == audio);
    }
}

public enum MusicDynamicLevel {
    NONE,
    SOFT,
    MEDIUM,
    HARD
}