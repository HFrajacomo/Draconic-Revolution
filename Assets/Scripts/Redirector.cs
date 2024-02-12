using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Redirector : MonoBehaviour
{
    private InfoClient socket;

    void Start()
    {
        System.GC.Collect();
        Resources.UnloadUnusedAssets();

        if(World.GetSceneFlag()){
            SceneManager.LoadScene("Menu");
        }
        else{
            this.socket = new InfoClient();
        }
    }

    void Update(){
        // If should redirect from Game to Menu
        if(World.GetSceneFlag()){
            return;
        }

        if(this.socket.ended){
            SceneManager.LoadScene("Game");
        }
        if(this.socket.backToMenu){
            SceneManager.LoadScene("Menu");
        }

        this.socket.HandleReceivedMessages();
    }

}
