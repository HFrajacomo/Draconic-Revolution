using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Debug = UnityEngine.Debug;

public class Redirector : MonoBehaviour
{
    private InfoClient socket;
    private static bool SERVER_STARTED = false;

    // Windows External Process
    public Process lanServerProcess;

    // Const Strings
    private string serverFile = "Server.exe";


    void Start()
    {
        System.GC.Collect();
        Resources.UnloadUnusedAssets();

        if(World.GetSceneFlag()){
            SceneManager.LoadScene("Menu");
        }
        else{
            TryStartServer();
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

    private void TryStartServer(){
        if(SERVER_STARTED)
            return;

        // If game world is in client
        if(World.isClient){
            // Unity edition only
            #if UNITY_EDITOR
                // Startup local server
                this.lanServerProcess = new Process();
                this.lanServerProcess.StartInfo.Arguments = $"-Local -World {World.worldName}";

                if(File.Exists(EnvironmentVariablesCentral.serverDir + serverFile))
                    this.lanServerProcess.StartInfo.FileName = EnvironmentVariablesCentral.serverDir + serverFile;
                else{
                    Panic();
                }

                try{
                    this.lanServerProcess.Start();
                }
                catch{
                    Panic();
                }

            #else
                string invisLauncher = "invisLaunchHelper.bat";

                EnvironmentVariablesCentral.WriteInvisLaunchScript(World.worldName);

                if(File.Exists(EnvironmentVariablesCentral.serverDir + serverFile))
                    Application.OpenURL($"{EnvironmentVariablesCentral.serverDir}{invisLauncher}");
                else
                    Panic();
            #endif


            World.SetConnectionIP(new IPAddress(new byte[4]{127, 0, 0, 1}));
            SERVER_STARTED = true;
        }

        // If game world is in server
        else{
            string[] segmentedIP = World.IP.Split('.');
            byte[] connectionIP = new byte[4];

            // If it's not a valid IPv4
            if(segmentedIP.Length != 4){
                Panic();
            }
            // Tailors the IP
            else{
                for(int i=0; i < 4; i++){
                    try{
                        connectionIP[i] = (byte)Convert.ToInt16(segmentedIP[i]);
                    }
                    catch(Exception e){
                        Debug.Log(e);
                        Panic();
                    }
                }

                World.SetConnectionIP(new IPAddress(connectionIP));
            }
        }
    }

    // Triggers hazard protection and sends user back to menu screen
    public void Panic(){
        Debug.Log("Panic");
        SceneManager.LoadScene("Menu");
    }
}
