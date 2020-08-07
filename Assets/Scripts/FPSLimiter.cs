using UnityEngine;
 
public class FPSLimiter : MonoBehaviour 
{
    public int targetFrameRate = 120;

    private void Start()
    {
        //QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }
}