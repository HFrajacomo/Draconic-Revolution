using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class Sound
{
    private static string audioDir = "file://" + Application.streamingAssetsPath + "/Audio/";
    private static Dictionary<AudioUsecase, string> folderMap = new Dictionary<AudioUsecase, string>(){
        {AudioUsecase.MUSIC_CLIP, "music_clip/"},
        {AudioUsecase.MUSIC_3D, "music_3d/"},
        {AudioUsecase.SFX_CLIP, "sfx_clip/"},
        {AudioUsecase.SFX_3D, "sfx_3d/"},
        {AudioUsecase.VOICE_CLIP, "voice_clip/"},
        {AudioUsecase.VOICE_3D, "voice_3d/"},
        {AudioUsecase.SFX_3D_LOOP, "sfx_3d/"}
    };

    private static readonly Dictionary<string, AudioUsecase> textToUsecase = new Dictionary<string, AudioUsecase>(){
        {"MUSIC_CLIP", AudioUsecase.MUSIC_CLIP},
        {"MUSIC_3D", AudioUsecase.MUSIC_3D},
        {"SFX_CLIP", AudioUsecase.SFX_CLIP},
        {"SFX_3D", AudioUsecase.SFX_3D},
        {"VOICE_CLIP", AudioUsecase.VOICE_CLIP},
        {"VOICE_3D", AudioUsecase.VOICE_3D},
        {"SFX_3D_LOOP", AudioUsecase.SFX_3D_LOOP}
    };

    private static readonly Dictionary<string, AudioVolume> textToVolume = new Dictionary<string, AudioVolume>(){
        {"", AudioVolume.NONE},
        {"NONE", AudioVolume.NONE},
        {"STEALTH", AudioVolume.STEALTH},
        {"VERYLOW", AudioVolume.VERYLOW},
        {"LOW", AudioVolume.LOW},
        {"MIDLOW", AudioVolume.MIDLOW},
        {"MID", AudioVolume.MID},
        {"MIDHIGH", AudioVolume.MIDHIGH},
        {"HIGH", AudioVolume.HIGH},
        {"VERYHIGH", AudioVolume.VERYHIGH},
        {"DEAFENING", AudioVolume.DEAFENING}
    };

    public string name;
    public string description;
    public string filename;
    public string author;
    public string serializedType;
    public string serializedVolume;
    private AudioUsecase type;
    private AudioVolume volume;

    // Constructor to be used in Voices Object
    public Sound(string name, AudioVolume vol, AudioUsecase usecase, string filename){
        this.name = name;
        this.filename = filename;
        this.volume = vol;
        this.type = usecase;
    }

    public static AudioUsecase ConvertUsecase(string text){return Sound.textToUsecase[text];}
    public static AudioVolume ConvertVolume(string text){
        if(text == null)
            return AudioVolume.NONE;
        return Sound.textToVolume[text];
    }

    public AudioUsecase GetUsecaseType(){return this.type;}
    public AudioVolume GetVolume(){return this.volume;}
    public void SetVolume(AudioVolume vol){this.volume = vol;}

    public void PostDeserializationSetup(){
        this.type = Sound.ConvertUsecase(this.serializedType);
        this.volume = Sound.ConvertVolume(this.serializedVolume);
        this.serializedType = "";
        this.serializedVolume = "";
    }

    public string GetFilePath(){
        return Sound.audioDir + folderMap[this.type] + this.filename;
    }

    public override string ToString(){
        return $"{this.author} - {this.name}\n{this.description}\n{this.filename}\n{this.type}\n{this.volume}";
    }
}
