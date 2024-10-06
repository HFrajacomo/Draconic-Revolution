using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class AudioLoader : BaseLoader {
	private static readonly string SOUNDS_RESPATH = "Audio/SOUNDS_LIST";
	private static readonly string VOICES_RESPATH = "Audio/VOICES_LIST";
	private static readonly string DYN_GROUP_RESPATH = "Audio/DYNAMIC_GROUPS_LIST";
	private static readonly string BIOME_RESPATH = "Audio/BIOME_MUSIC_LIST";

	private static bool isClient;

	// Sound Information
	private static Dictionary<string, Sound> sounds = new Dictionary<string, Sound>();
	private static Dictionary<string, Voice> voices = new Dictionary<string, Voice>();
	private static Dictionary<string, DynamicMusic> dynamicMusic = new Dictionary<string, DynamicMusic>();
	private static Dictionary<string, string> biomeMusic = new Dictionary<string, string>();


	public AudioLoader(bool client){
		isClient = client;
	}

	public override bool Load(){
		if(!isClient)
			return true;

		ParseSoundList();
		ParseVoiceList();
		ParseDynamicGroupsList();
		ParseBiomeMusic();

		return true;
	}

	public static Sound GetSound(string codename){return sounds[codename];}
	public static Voice GetVoice(string codename){return voices[codename];}
	public static DynamicMusic GetMusicGroup(string name){return dynamicMusic[name];}
	public static bool IsSound(string name){return sounds.ContainsKey(name);}
	public static bool IsDynamicMusic(string name){return dynamicMusic.ContainsKey(name);}
	public static bool IsVoice(string name){return voices.ContainsKey(name);}
	public static bool MusicGroupContainsSound(string group, string sound){return GetMusicGroup(group).ContainsSound(sound);}

	public static string GetBiomeMusic(string biome){
		if(!biomeMusic.ContainsKey(biome))
			return "";
		return biomeMusic[biome];
	}

	public static bool IsLoop(string codename){
		if(sounds[codename].GetUsecaseType() == AudioUsecase.SFX_3D_LOOP)
			return true;
		return false;
	}

	private static void ParseSoundList(){
		TextAsset soundJson = Resources.Load<TextAsset>(SOUNDS_RESPATH);

		if(soundJson == null){
			Debug.Log("Couldn't Locate the SOUNDS_LIST while loading the ItemLoader");
			Application.Quit();
		}

		Wrapper<Sound> wrapper = JsonUtility.FromJson<Wrapper<Sound>>(soundJson.text);

		for(int i=0; i < wrapper.data.Length; i++){
			AudioLoader.sounds.Add(wrapper.data[i].name, wrapper.data[i]);
			AudioLoader.sounds[wrapper.data[i].name].PostDeserializationSetup();
		}
	}

	private static void ParseVoiceList(){
		TextAsset voicesJson = Resources.Load<TextAsset>(VOICES_RESPATH);

		if(voicesJson == null){
			Debug.Log("Couldn't Locate the VOICES_LIST while loading the ItemLoader");
			Application.Quit();
		}

		Wrapper<Voice> wrapper = JsonUtility.FromJson<Wrapper<Voice>>(voicesJson.text);

		for(int i=0; i < wrapper.data.Length; i++){
			AudioLoader.voices.Add(wrapper.data[i].name, wrapper.data[i]);
			AudioLoader.voices[wrapper.data[i].name].PostDeserializationSetup();
		}
	}

	private static void ParseDynamicGroupsList(){
		TextAsset dynGroupJson = Resources.Load<TextAsset>(DYN_GROUP_RESPATH);

		if(dynGroupJson == null){
			Debug.Log("Couldn't Locate the DYNAMIC_GROUPS_LIST while loading the ItemLoader");
			Application.Quit();
		}

		Wrapper<DynamicMusic> wrapper = JsonUtility.FromJson<Wrapper<DynamicMusic>>(dynGroupJson.text);

		for(int i=0; i < wrapper.data.Length; i++){
			AudioLoader.dynamicMusic.Add(wrapper.data[i].name, wrapper.data[i]);
			AudioLoader.dynamicMusic[wrapper.data[i].name].PostDeserializationSetup();
		}
	}

	private static void ParseBiomeMusic(){
		TextAsset biomeJson = Resources.Load<TextAsset>(BIOME_RESPATH);

		if(biomeJson == null){
			Debug.Log("Couldn't Locate the BIOME_MUSIC_LIST while loading the ItemLoader");
			Application.Quit();
		}

		Wrapper<ValuePair<string, string>> wrapper = JsonUtility.FromJson<Wrapper<ValuePair<string, string>>>(biomeJson.text);

		for(int i=0; i < wrapper.data.Length; i++){
			AudioLoader.biomeMusic.Add(wrapper.data[i].key, wrapper.data[i].value);
		}
	}
}