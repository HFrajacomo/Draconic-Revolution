using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class InventoryFileHandler{
    private Stream file;
    private Stream indexFile;

    private string filePath;
    private string indexFilePath;
    private string worldDir;
    private string worldsPath;

    private const string FILENAME = "inventory.invf";
    private const string INDEXNAME = "index.iind";

    private byte[] buffer = new byte[10950]; // Buffer size that would cover a worst-case scenario inventory

    private FragmentationHandler fragmentationHandler;

    public InventoryFileHandler(){
        #if UNITY_EDITOR
            this.worldsPath = "Worlds\\";
            this.worldDir = "Worlds\\" + World.worldName + "\\";
            this.filePath = this.worldDir + FILENAME;
            this.indexFilePath = this.worldDir + INDEXNAME;
        #else
            // If is in Dedicated Server
            if(!World.isClient){
                this.worldsPath = "Worlds\\";
                this.worldDir = "Worlds\\" + World.worldName + "\\";
                this.filePath = this.worldDir + FILENAME;
                this.indexFilePath = this.worldDir + INDEXNAME;
            }
            // If it's a Local Server
            else{
                this.worldsPath = EnvironmentVariablesCentral.clientExeDir + "\\Worlds\\";
                this.worldDir = this.worldsPath + World.worldName;
                this.filePath = this.worldDir + FILENAME;
                this.indexFilePath = this.worldDir + INDEXNAME;   
            }
        #endif
    
        LoadFiles();
    }



    private void LoadFiles(){
        if(!Directory.Exists(this.worldsPath))
            Directory.CreateDirectory(this.worldsPath);
        if(!Directory.Exists(this.worldDir))
            Directory.CreateDirectory(this.worldDir);

        if(File.Exists(this.filePath))
            this.file = File.Open(this.filePath, FileMode.Open);
        else
            this.file = File.Open(this.filePath, FileMode.Create);

        if(File.Exists(this.indexFilePath))
            this.indexFile = File.Open(this.indexFilePath, FileMode.Open);
        else
            this.indexFile = File.Open(this.indexFilePath, FileMode.Create);
    }
}
