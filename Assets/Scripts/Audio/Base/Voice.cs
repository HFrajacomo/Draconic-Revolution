using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
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

    public string name;
    public string serializedType;
    public string serializedVolume;
    public string description;
    public string filename;
    public string transcriptFilename;
    private AudioUsecase type;
    private AudioVolume volume;
    private Sound wrapperSound;

    public AudioUsecase GetUsecaseType(){return this.type;}
    public AudioVolume GetVolume(){return this.volume;}
    public Sound GetSound(){return this.wrapperSound;}

    public override string ToString(){
        return $"{this.name}\n{this.description}\n{this.filename}";
    }

    public void PostDeserializationSetup(){
        this.type = Sound.ConvertUsecase(this.serializedType);
        this.volume = Sound.ConvertVolume(this.serializedVolume);
        this.wrapperSound = new Sound(this.name, this.volume, this.type, this.filename);
    }

    public string GetTranscriptPath(){
        return Application.streamingAssetsPath + "/Audio/" + folderMap[this.type] + this.transcriptFilename;
    }
}
