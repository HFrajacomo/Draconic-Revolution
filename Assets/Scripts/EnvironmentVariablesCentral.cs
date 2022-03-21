using System.IO;
using System.Text;
using UnityEngine;


public static class EnvironmentVariablesCentral
{
    public static string clientDir;
    public static string clientExeDir;
    public static string gameDir;
    public static string serverDir;
    public static string compiledServerDir;
    private static string invisScript = "start /min powershell \"start-process $env:APPDATA\\DraconicRevolution\\Server\\Server.exe -Arg -Local -WindowStyle hidden\"";

    public static void Start(){
        clientDir = GetClientDir();
        clientExeDir = GetClientExeDir();
        compiledServerDir = clientDir + "Build\\Server";
        gameDir = GetAppdataDir() + "\\DraconicRevolution\\";
        serverDir = gameDir + "Server\\";

        if(!Directory.Exists(gameDir))
            Directory.CreateDirectory(gameDir);

        if(Directory.Exists(compiledServerDir)){
            if(Directory.Exists(serverDir)){
                Directory.Delete(serverDir, true);
            }

            Directory.Move(compiledServerDir, serverDir);
        }

        if(!Directory.Exists(serverDir)){
            if(Directory.Exists(clientDir + "\\Build\\Server")){
                Directory.Move(clientDir + "\\Build\\Server", serverDir);
            }
            else{
                Application.Quit();
            }
        }
    }

    public static void WriteInvisLaunchScript(){
        byte[] bytes = Encoding.ASCII.GetBytes(invisScript);
        Stream invisFile = File.Open(serverDir + "invisLaunchHelper.bat", FileMode.Create);
        invisFile.Write(bytes, 0, bytes.Length);
        invisFile.Close();
    }

    private static string GetAppdataDir(){
        return GetParent(Application.persistentDataPath, iterations:3) + "\\Roaming";
    }

    private static string GetClientDir(){
        string workDir = Application.dataPath;
        string[] splittedDir = workDir.Split("/");
        string accumulatedDir = "";
        int i = 0;

        while(i < splittedDir.Length){
            accumulatedDir += splittedDir[i] + "\\";

            if(splittedDir[i] == "Draconic-Revolution")
                break;

            i++;
        }

        return accumulatedDir;
    }

    private static string GetClientExeDir(){
        string workDir = Application.dataPath;
        string[] splittedDir = workDir.Split("/");
        string accumulatedDir = "";
        int i = 0;

        while(i < splittedDir.Length-1){
            accumulatedDir += splittedDir[i] + "\\";
            i++;
        }

        return accumulatedDir;
    }

    private static string GetParent(string path, int iterations=2){
        string newPath = path;

        while(iterations > 0){
            newPath = Directory.GetParent(newPath).ToString();
            iterations--;
        }

        return newPath;
    }
}
