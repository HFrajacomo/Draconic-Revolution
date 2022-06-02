using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Redirector : MonoBehaviour
{

    void Start()
    {
        System.GC.Collect();
        Resources.UnloadUnusedAssets();

        if(World.GetSceneFlag()){
            SceneManager.LoadScene("Menu");
        }
        else{
            SceneManager.LoadScene("Game");
        }
    }

}
