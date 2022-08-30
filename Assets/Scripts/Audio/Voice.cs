using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voice
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
    public string transcriptPath;
    public Sound wrapperSound;


    public Voice(AudioName name, AudioUsecase type, string desc, string file, string tName){
        this.name = name;
        this.type = type;
        this.description = desc;
        this.filename = file;
        this.transcriptPath = tName;

        this.wrapperSound = new Sound(name, type, description, "");
    }

    public string GetFilePath(){
        return Voice.audioDir + folderMap[this.type] + this.filename;
    }

    public string GetTranscriptPath(){
        return Voice.audioDir + folderMap[this.type] + this.transcriptPath;
    }
}
