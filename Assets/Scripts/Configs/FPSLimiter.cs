using UnityEngine;
 
public class FPSLimiter : MonoBehaviour 
{
    public int targetFrameRate = 60;

    private void Start()
    {
        // Headless processes (-batchmode) have no swapchain to sync with, and targetFrameRate is ignored while vSync is on
        QualitySettings.vSyncCount = Application.isBatchMode ? 0 : 1;
        Application.targetFrameRate = targetFrameRate;
    }
}