using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioLibrary
{
    private static Dictionary<AudioName, Sound> sounds = new Dictionary<AudioName, Sound>(){
        {AudioName.RAINFALL, new Sound(AudioName.RAINFALL, AudioUsecase.MUSIC_CLIP, "TestTrack", "rainfall.ogg")},
        {AudioName.AGES, new Sound(AudioName.AGES, AudioUsecase.MUSIC_CLIP, "TestTrack2", "ages.ogg")},
        {AudioName.DONTFINDIT, new Sound(AudioName.DONTFINDIT, AudioUsecase.MUSIC_CLIP, "TestTrack3", "dontfindit.ogg")},
        {AudioName.SURPRISEMOTAFAKA, new Sound(AudioName.SURPRISEMOTAFAKA, AudioUsecase.SFX_CLIP, "TestSFX", "surprisemotafaka.ogg")},
        {AudioName.HAT, new Sound(AudioName.HAT, AudioUsecase.SFX_3D, "TestSFX3D", "hat.ogg", AudioVolume.MIDLOW)},
        {AudioName.BZZT, new Sound(AudioName.BZZT, AudioUsecase.SFX_3D_LOOP, "BZZZT!", "bzzt.ogg", AudioVolume.STEALTH)},
        {AudioName.ANCIENTS, new Sound(AudioName.ANCIENTS, AudioUsecase.MUSIC_3D, "Ancients", "ancients.ogg", AudioVolume.HIGH)},
    };

    private static Dictionary<AudioName, DynamicMusic> dynamicMusic = new Dictionary<AudioName, DynamicMusic>(){
        {AudioName.TEST_GROUP, new DynamicMusic(AudioName.TEST_GROUP, AudioUsecase.MUSIC_CLIP, "TestGroup", AudioName.RAINFALL, AudioName.AGES, AudioName.DONTFINDIT)}
    };

    private static Dictionary<AudioName, Voice> voices = new Dictionary<AudioName, Voice>(){
        {AudioName.TEST_VOICE2D, new Voice(AudioName.TEST_VOICE2D, AudioUsecase.VOICE_CLIP, "TestVoice", "testvoice.ogg", "testvoice.tsc")},
        {AudioName.TEST_VOICE3D, new Voice(AudioName.TEST_VOICE3D, AudioUsecase.VOICE_3D, "TestVoice", "testvoice.ogg", "testvoice.tsc", AudioVolume.MID)}
    };


    public static Sound GetSound(AudioName name){
        return sounds[name];
    }

    public static DynamicMusic GetMusicGroup(AudioName name){
        return dynamicMusic[name];
    }

    public static Voice GetVoice(AudioName name){
        return voices[name];
    }

    public static bool IsSound(AudioName name){
        return sounds.ContainsKey(name);
    }

    public static bool IsDynamicMusic(AudioName name){
        return dynamicMusic.ContainsKey(name);
    }

    public static bool IsVoice(AudioName name){
        return voices.ContainsKey(name);
    }

    public static bool MusicGroupContainsSound(AudioName groupName, AudioName queryAudio){
        DynamicMusic group = GetMusicGroup(groupName);
        return group.ContainsSound(queryAudio);
    }
}
