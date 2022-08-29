using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;


public class AudioManager : MonoBehaviour
{
    // Singleton
    private static AudioManager self;

    private Dictionary<AudioName, AudioClip> loadedClips = new Dictionary<AudioName, AudioClip>();
    private Dictionary<AudioName, AudioClip> cachedAudioClip = new Dictionary<AudioName, AudioClip>();
    private List<AudioName> cachedAudioList;

    public AudioTrackMusic2D audioTrackMusic2D;


    public void Awake(){
        DontDestroyOnLoad(this.gameObject);

        if(self == null){
            self = this;
        }
        else{
            Destroy(this.gameObject);
        }
    }

    public void Update(){
        RerunLoadedClips();
    }


    public void Play(AudioName name){
        if(loadedClips.ContainsKey(name)){
            this.audioTrackMusic2D.Play(AudioLibrary.GetSound(name), GetClip(name));
        }
        else{
            LoadAudioClip(name);
        }
    }

    public void Stop(AudioUsecase type, bool fade=false){
        switch(type){
            case AudioUsecase.MUSIC_CLIP:
                StopMusic2D(fade:fade);
                break;
            default:
                StopMusic2D(fade:fade);
                break;
        }
    }

    private void StopMusic2D(bool fade){
        this.audioTrackMusic2D.Stop(fade:fade);
    }

    private void RerunLoadedClips(){
        if(cachedAudioClip.Count > 0){
            this.cachedAudioList = new List<AudioName>(cachedAudioClip.Keys);
            foreach(AudioName name in this.cachedAudioList){
                loadedClips.Add(name, this.cachedAudioClip[name]);
                cachedAudioClip.Remove(name);
                this.Play(name);
            }

            this.cachedAudioList.Clear();
        }
    }

    private AudioClip GetClip(AudioName name){
        return this.loadedClips[name];
    }

    private void LoadAudioClip(AudioName name){
        StartCoroutine(GetAudioClip(name));
    }

    private IEnumerator GetAudioClip(AudioName name)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(AudioLibrary.GetSound(name).GetFilePath(), AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
            }
            else
            {
                this.cachedAudioClip.Add(name, DownloadHandlerAudioClip.GetContent(www));
            }
        }
    }
}
