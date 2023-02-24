using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

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

    public AudioName groupName;
    public AudioUsecase type;
    public string description;
    public Sound wrapperSound;
    public Dictionary<MusicDynamicLevel, Sound> soundByDynamic;


    public DynamicMusic(AudioName groupName, AudioUsecase usecase, string description, AudioName name1, AudioName name2, AudioName name3){
        this.soundByDynamic = new Dictionary<MusicDynamicLevel, Sound>();
        this.soundByDynamic.Add(MusicDynamicLevel.SOFT, AudioLibrary.GetSound(name1));
        this.soundByDynamic.Add(MusicDynamicLevel.MEDIUM, AudioLibrary.GetSound(name2));
        this.soundByDynamic.Add(MusicDynamicLevel.HARD, AudioLibrary.GetSound(name3));

        this.groupName = groupName;
        this.type = usecase;
        this.description = description;

        this.wrapperSound = new Sound(groupName, usecase, description, "");
    }

    public Sound Get(MusicDynamicLevel level){
        return this.soundByDynamic[level];
    }

    public string GetFilePath(MusicDynamicLevel level){
        return this.soundByDynamic[level].GetFilePath();
    }

    public bool ContainsSound(AudioName audio){
        return (this.soundByDynamic[MusicDynamicLevel.SOFT].name == audio) || (this.soundByDynamic[MusicDynamicLevel.MEDIUM].name == audio) || (this.soundByDynamic[MusicDynamicLevel.HARD].name == audio);
    }
}

public enum MusicDynamicLevel {
    NONE,
    SOFT,
    MEDIUM,
    HARD
}