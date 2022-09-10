using System;
using Object = System.Object;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Configurations
{
    // Settings
    public static bool FULLBRIGHT = false;
    public static ulong accountID;
    public static int music2DVolume;
    public static int music3DVolume;
    public static int sfx2DVolume;
    public static int sfx3DVolume;
    public static int voice2DVolume;
    public static int voice3DVolume;
    public static bool subtitlesOn;

    // Config File
    private static bool firstRun = true;
    private static string configFilePath;
    private static Stream file;
    private static Dictionary<string, DATATYPE> arguments = new Dictionary<string, DATATYPE>();
    private static Dictionary<string, Object> defaults = new Dictionary<string, Object>();
    private static HashSet<string> allArguments = new HashSet<string>();
    private static HashSet<string> readArguments = new HashSet<string>();


    private static void Start(){
        Configurations.AddToDictEntry("accountID", DATATYPE.ULONG, (Object)0UL);
        Configurations.AddToDictEntry("fullbright", DATATYPE.BOOL, (Object)false);
        Configurations.AddToDictEntry("render_distance", DATATYPE.INT, (Object)5);
        Configurations.AddToDictEntry("2d_music_volume", DATATYPE.INT, (Object)100);
        Configurations.AddToDictEntry("3d_music_volume", DATATYPE.INT, (Object)100);
        Configurations.AddToDictEntry("2d_sfx_volume", DATATYPE.INT, (Object)100);
        Configurations.AddToDictEntry("3d_sfx_volume", DATATYPE.INT, (Object)100);
        Configurations.AddToDictEntry("2d_voice_volume", DATATYPE.INT, (Object)100);
        Configurations.AddToDictEntry("3d_voice_volume", DATATYPE.INT, (Object)100);
        Configurations.AddToDictEntry("subtitles", DATATYPE.BOOL, (Object)true);
        Configurations.firstRun = false;
    }


    public static int GetFullbright(){
        if(Configurations.FULLBRIGHT)
            return 1;
        else
            return 0;
    }

    public static void LoadConfigFile(){
        if(firstRun)
            Configurations.Start();

        Configurations.configFilePath = EnvironmentVariablesCentral.clientExeDir + "\\" + "config.cfg";

        if(!File.Exists(Configurations.configFilePath)){
            GenerateConfigFile();
        }
        else{
            ParseConfigFile();
        }
    }

    public static void SaveConfigFile(){
        Configurations.file = File.Open(Configurations.configFilePath, FileMode.Open);

        Configurations.file.SetLength(0);
        CreateUlongField("accountID", accountID);
        CreateBoolField("fullbright", FULLBRIGHT);
        CreateIntField("render_distance", World.renderDistance);
        CreateIntField("2d_music_volume", music2DVolume);
        CreateIntField("3d_music_volume", music3DVolume);
        CreateIntField("2d_sfx_volume", sfx2DVolume);
        CreateIntField("3d_sfx_volume", sfx3DVolume);
        CreateIntField("2d_voice_volume", voice2DVolume);
        CreateIntField("3d_voice_volume", voice3DVolume);
        CreateBoolField("subtitles", subtitlesOn);

        Configurations.file.Close();
    }

    private static void GenerateConfigFile(){
        Configurations.file = File.Open(Configurations.configFilePath, FileMode.Create);

        foreach(string entry in arguments.Keys){
            GenerateConfigFile(entry);
        }

        Configurations.file.Close();
    }

    private static void GenerateConfigFile(string entry){
        switch(arguments[entry]){
            case DATATYPE.STRING:
                CreateStringField(entry, (string)defaults[entry]);
                break;
            case DATATYPE.ULONG:
                CreateUlongField(entry, (ulong)defaults[entry]);
                break;
            case DATATYPE.BOOL:
                CreateBoolField(entry, (bool)defaults[entry]);
                break;
            case DATATYPE.INT:
                CreateIntField(entry, (int)defaults[entry]);
                break;
            default:
                break;
        }        

        HandleConfigDefaults(entry);
    }

    private static void ParseConfigFile(){
        string[] separatedEntries;
        string[] entries = File.ReadAllLines(Configurations.configFilePath);

        for(int i=0; i < entries.Length; i++){
            if(entries[i] == "")
                continue;

            if(!entries[i].Contains(':'))
                continue;

            separatedEntries = entries[i].Split(':');
            
            HandleConfigField(separatedEntries[0], separatedEntries[1]);

            if(allArguments.Contains(separatedEntries[0]))
                readArguments.Add(separatedEntries[0]);
        }

        if(!readArguments.Equals(allArguments))
            FillInMissingConfig();

        readArguments.Clear();
    }

    private static void FillInMissingConfig(){
        Configurations.file = File.Open(Configurations.configFilePath, FileMode.Open);
        Configurations.file.Seek(0, SeekOrigin.End);

        foreach(string arg in allArguments){
            if(!readArguments.Contains(arg)){
                GenerateConfigFile(arg);
            }
        }

        Configurations.file.Close();
    }

    private static void HandleConfigDefaults(string entry){
        // Handles fields
        switch(entry){
            case "accountID":
                Configurations.accountID = (ulong)defaults[entry];
                break;
            case "fullbright":
                Configurations.FULLBRIGHT = (bool)defaults[entry];
                break;
            case "render_distance":
                World.SetRenderDistance((int)defaults[entry]);
                break;
            case "2d_music_volume":
                Configurations.music2DVolume = (int)defaults[entry];
                break;
            case "3d_music_volume":
                Configurations.music3DVolume = (int)defaults[entry];
                break;
            case "2d_sfx_volume":
                Configurations.sfx2DVolume = (int)defaults[entry];
                break;
            case "3d_sfx_volume":
                Configurations.sfx3DVolume = (int)defaults[entry];
                break;
            case "2d_voice_volume":
                Configurations.voice2DVolume = (int)defaults[entry];
                break;
            case "3d_voice_volume":
                Configurations.voice3DVolume = (int)defaults[entry];
                break;
            case "subtitles":
                Configurations.subtitlesOn = (bool)defaults[entry];
                break;
            default:
                break;
        }
    }

    private static void HandleConfigField(string entry, string value){
        // Handles fields
        switch(entry){
            case "accountID":
                Configurations.accountID = ReadUlongField(value);
                break;
            case "fullbright":
                Configurations.FULLBRIGHT = ReadBoolField(value);
                break;
            case "render_distance":
                World.SetRenderDistance(ReadIntField(value));
                break;
            case "2d_music_volume":
                Configurations.music2DVolume = ReadIntField(value);
                break;
            case "3d_music_volume":
                Configurations.music3DVolume = ReadIntField(value);
                break;
            case "2d_sfx_volume":
                Configurations.sfx2DVolume = ReadIntField(value);
                break;
            case "3d_sfx_volume":
                Configurations.sfx3DVolume = ReadIntField(value);
                break;
            case "2d_voice_volume":
                Configurations.voice2DVolume = ReadIntField(value);
                break;
            case "3d_voice_volume":
                Configurations.voice3DVolume = ReadIntField(value);
                break;
            case "subtitles":
                Configurations.subtitlesOn = ReadBoolField(value);
                break;
            default:
                break;
        }
    }

    private static void CreateStringField(string name, string value){
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);
        byte[] valueBytes = Encoding.ASCII.GetBytes(value);

        Configurations.file.Write(nameBytes, 0, nameBytes.Length);
        Configurations.AddSeparator();
        Configurations.file.Write(valueBytes, 0, valueBytes.Length);
        Configurations.AddNewLine();
    }

    private static void CreateIntField(string name, int value){
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);
        byte[] valueBytes = Encoding.ASCII.GetBytes(value.ToString());

        Configurations.file.Write(nameBytes, 0, nameBytes.Length);
        Configurations.AddSeparator();
        Configurations.file.Write(valueBytes, 0, valueBytes.Length);
        Configurations.AddNewLine();       
    }

    private static void CreateUlongField(string name, ulong value){
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);
        byte[] valueBytes = Encoding.ASCII.GetBytes(value.ToString());

        Configurations.file.Write(nameBytes, 0, nameBytes.Length);
        Configurations.AddSeparator();
        Configurations.file.Write(valueBytes, 0, valueBytes.Length);
        Configurations.AddNewLine();        
    }

    private static void CreateBoolField(string name, bool val){
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);
        byte boolVal;
        
        if(val)
            boolVal = 49; // 1 in ASCII
        else
            boolVal = 48; // 0 in ASCII

        Configurations.file.Write(nameBytes, 0, nameBytes.Length);
        Configurations.AddSeparator();
        Configurations.file.WriteByte(boolVal);
        Configurations.AddNewLine();          
    }

    private static string ReadStringField(string value){
        return value;
    }

    private static int ReadIntField(string value){
        try{
            return Convert.ToInt32(value);
        }
        catch{
            return 0;
        }
    }

    private static ulong ReadUlongField(string value){
        try{
            return (ulong)Convert.ToInt64(value);
        }
        catch{
            return 0;
        }
    }

    private static bool ReadBoolField(string value){
        if(value == "0")
            return false;
        return true;
    }

    private static void AddSeparator(){
        byte b = Encoding.ASCII.GetBytes(":")[0];
        Configurations.file.WriteByte(b);
    }

    private static void AddNewLine(){
        byte b = Encoding.ASCII.GetBytes("\n")[0];
        Configurations.file.WriteByte(b);
    }

    private static void AddToDictEntry(string name, DATATYPE type, Object value){
        arguments.Add(name, type);
        defaults.Add(name, value);
        allArguments.Add(name);
    }


    private enum DATATYPE : byte{
        STRING,
        ULONG,
        INT,
        BOOL
    }
}
