using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Sound
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

    public AudioName name;
    public AudioUsecase type;
    public string description;
    public string filename;


    public Sound(AudioName name, AudioUsecase type, string desc, string file){
        this.name = name;
        this.type = type;
        this.description = desc;
        this.filename = file;
    }

    public string GetFilePath(){
        return Sound.audioDir + folderMap[this.type] + this.filename;
    }
}
