using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioLibrary
{
    private static Dictionary<AudioName, Sound> sounds = new Dictionary<AudioName,Sound>(){
        {AudioName.RAINFALL, new Sound(AudioName.RAINFALL, AudioUsecase.MUSIC_CLIP, "TestTrack", "rainfall.mp3")}
    };

    public static Sound GetSound(AudioName name){
        return sounds[name];
    }
}
