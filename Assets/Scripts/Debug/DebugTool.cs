using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class DebugTool{
	private static string FILENAME = "debugLog.txt";

    public static void Log(string content){
		string fullDir = EnvironmentVariablesCentral.clientExeDir + FILENAME;

        // Check if the file exists
        if (!File.Exists(fullDir)){
            // If the file doesn't exist, create it
            using (StreamWriter sw = File.CreateText(fullDir)){
                sw.WriteLine(content);
            }
        }
        else{
            // If the file exists, open it and append the string
            using (StreamWriter sw = File.AppendText(fullDir)){
                sw.WriteLine(content);
            }
        }
    }

    public static void Log<T>(List<T> l){
		string fullDir = EnvironmentVariablesCentral.clientExeDir + FILENAME;
		string result = "[" + string.Join(",", l) + "]";

        // Check if the file exists
        if (!File.Exists(fullDir)){
            // If the file doesn't exist, create it
            using (StreamWriter sw = File.CreateText(fullDir)){
                sw.WriteLine(result);
            }
        }
        else{
            // If the file exists, open it and append the string
            using (StreamWriter sw = File.AppendText(fullDir)){
                sw.WriteLine(result);
            }
        }    	
    }
}