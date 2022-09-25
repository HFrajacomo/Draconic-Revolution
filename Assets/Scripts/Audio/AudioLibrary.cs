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
        {AudioName.TORCHFIRE, new Sound(AudioName.TORCHFIRE, AudioUsecase.SFX_3D_LOOP, "Torch", "torchfire.ogg", AudioVolume.VERYLOW)},
        {AudioName.ANCIENTS, new Sound(AudioName.ANCIENTS, AudioUsecase.MUSIC_3D, "Ancients", "ancients.ogg", AudioVolume.HIGH)},
        {AudioName.SNOW_PLAINS_SEA_SOFT, new Sound(AudioName.SNOW_PLAINS_SEA_SOFT, AudioUsecase.MUSIC_CLIP, "SnowSea", "snow_plains_sea_soft.ogg")},
        {AudioName.SNOW_PLAINS_SEA_MID, new Sound(AudioName.SNOW_PLAINS_SEA_MID, AudioUsecase.MUSIC_CLIP, "SnowSea", "snow_plains_sea_mid.ogg")},
        {AudioName.SNOW_PLAINS_SEA_HEAVY, new Sound(AudioName.SNOW_PLAINS_SEA_HEAVY, AudioUsecase.MUSIC_CLIP, "SnowSea", "snow_plains_sea_heavy.ogg")},
        {AudioName.SNOW_MONTAINS_SOFT, new Sound(AudioName.SNOW_MONTAINS_SOFT, AudioUsecase.MUSIC_CLIP, "SnowMontains", "snow_montains_soft.ogg")},
        {AudioName.SNOW_MONTAINS_MID, new Sound(AudioName.SNOW_MONTAINS_MID, AudioUsecase.MUSIC_CLIP, "SnowMontains", "snow_montains_mid.ogg")},
        {AudioName.SNOW_MONTAINS_HEAVY, new Sound(AudioName.SNOW_MONTAINS_HEAVY, AudioUsecase.MUSIC_CLIP, "SnowMontains", "snow_montains_heavy.ogg")}
    };

    private static Dictionary<AudioName, DynamicMusic> dynamicMusic = new Dictionary<AudioName, DynamicMusic>(){
        {AudioName.SNOW_PLAINS_SEA_GROUP, new DynamicMusic(AudioName.SNOW_PLAINS_SEA_GROUP, AudioUsecase.MUSIC_CLIP,
         "SnowSea", AudioName.SNOW_PLAINS_SEA_SOFT, AudioName.SNOW_PLAINS_SEA_MID, AudioName.SNOW_PLAINS_SEA_HEAVY)},
        {AudioName.SNOW_MONTAINS_GROUP, new DynamicMusic(AudioName.SNOW_MONTAINS_GROUP, AudioUsecase.MUSIC_CLIP,
         "SnowMontains", AudioName.SNOW_MONTAINS_SOFT, AudioName.SNOW_MONTAINS_MID, AudioName.SNOW_MONTAINS_HEAVY)}
    };

    private static Dictionary<AudioName, Voice> voices = new Dictionary<AudioName, Voice>(){
        {AudioName.TEST_VOICE2D, new Voice(AudioName.TEST_VOICE2D, AudioUsecase.VOICE_CLIP, "TestVoice", "testvoice.ogg", "testvoice.tsc")},
        {AudioName.TEST_VOICE3D, new Voice(AudioName.TEST_VOICE3D, AudioUsecase.VOICE_3D, "TestVoice", "testvoice.ogg", "testvoice.tsc", AudioVolume.MID)}
    };

    private static Dictionary<string, AudioName> biomeMusic = new Dictionary<string, AudioName>(){
        {"Ice Ocean", AudioName.SNOW_PLAINS_SEA_GROUP},
        {"Snowy Plains", AudioName.SNOW_PLAINS_SEA_GROUP}
        //{"Snowy Montains", AudioName.SNOW_MONTAINS_GROUP}
    };


    public static Sound GetSound(AudioName name){
        return sounds[name];
    }

    #nullable enable
    public static DynamicMusic? GetMusicGroup(AudioName name){
        if(dynamicMusic.ContainsKey(name))
            return dynamicMusic[name];
        else
            return null;
    }
    #nullable disable

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

    public static AudioName? GetBiomeMusic(string biome){
        if(biomeMusic.ContainsKey(biome))
            return biomeMusic[biome];
        return null;
    }

    #nullable enable
    public static bool MusicGroupContainsSound(AudioName groupName, AudioName queryAudio){
        DynamicMusic? group = GetMusicGroup(groupName);

        if(group == null)
            return false;

        return group.ContainsSound(queryAudio);
    }
    #nullable disable
}
