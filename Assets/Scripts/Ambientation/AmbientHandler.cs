using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using Unity.Mathematics;

public class AmbientHandler : MonoBehaviour
{
    // Unity References
    public GameObject lightObject;
	public Transform skyboxLight;
	public Light skyDirectionalLight;
    public HDAdditionalLightData hdLight;
	public TimeOfDay timer;
    public PlayerPositionHandler playerPositionHandler;
    public LensFlareComponentSRP dayFlare;
    public LensFlareComponentSRP nightFlare;

    // Skybox Parameters
    public VolumeProfile volume;
    private PhysicallyBasedSky pbsky;
    private CloudLayer clouds;
    private WhiteBalance whiteBalance;
    private Fog fog;
    private LiftGammaGain lgg;

    // Update variables
    private const int FRAMES_TO_CHANGE = 180;
    private int updateTimer = 0;
    private bool isTransitioning = false;
    private float2 cachedRotation;
    private int currentTick;
    private int lastTick;
    private int lastTime;
    private float delta;
    private byte ROTATE_SUN_FLAG;
    private byte ROTATE_FLAG_MAX = 3;

    // Ambient Groups
    private AmbientGroup currentAmbient;
    private AmbientGroup lastAmbient;
    private BaseAmbientPreset currentPreset;
    private BaseAmbientPreset lastPreset;

    void OnDestroy(){
        GameObject.Destroy(this.lightObject);
        this.lightObject = null;
        this.skyDirectionalLight = null;
        this.hdLight = null;
        this.timer = null;
        this.dayFlare = null;
        this.nightFlare = null;
        this.volume = null;
        this.pbsky = null;
        this.clouds = null;
        this.whiteBalance = null;
    }

    void Start(){
        this.volume.TryGet<PhysicallyBasedSky>(out this.pbsky);
        this.volume.TryGet<CloudLayer>(out this.clouds);
        this.volume.TryGet<WhiteBalance>(out this.whiteBalance);
        this.volume.TryGet<Fog>(out this.fog);
        this.volume.TryGet<LiftGammaGain>(out this.lgg);

        currentAmbient = BiomeHandler.GetAmbientGroup(BiomeHandler.BiomeToByte(playerPositionHandler.GetCurrentBiome()));
        lastAmbient = currentAmbient;
        currentPreset = BaseAmbientPreset.GetPreset(currentAmbient);
        lastPreset = currentPreset;

        ROTATE_SUN_FLAG = 0;

        SetStats(this.timer.ToSeconds());
    }

    void Update(){
        int time = timer.ToSeconds();
        currentTick = (int)timer.GetFakeTicks();
        delta += Time.deltaTime/2;

        if(!isTransitioning){
            currentAmbient = BiomeHandler.GetAmbientGroup(BiomeHandler.BiomeToByte(playerPositionHandler.GetCurrentBiome()));
            
            if(currentAmbient != lastAmbient){
                isTransitioning = true;
                currentPreset = BaseAmbientPreset.GetPreset(currentAmbient);
                return;
            }

            if(time != lastTime){
                delta = 0f;
            }

            if(currentTick != lastTick)
                SetStats(time);

            lastAmbient = currentAmbient;
            lastTick = currentTick;
            lastTime = time;

        }
        else{
            this.updateTimer++;

            if(currentTick != lastTick)
                LerpStatus(time);
            
            if(this.updateTimer == FRAMES_TO_CHANGE){
                isTransitioning = false;
                this.updateTimer = 0;
                lastAmbient = currentAmbient;
                lastPreset = currentPreset;
            }

            lastTick = currentTick;
        } 
    }

    // Calculates the status of ambientation features while there's a change in preset happening
    private void LerpStatus(int time){
        float currentStep = (float)this.updateTimer/FRAMES_TO_CHANGE;

        if(currentTick % 6 == 0){
            this.pbsky.horizonTint.value = Color.Lerp(lastPreset.GetHorizonTint(time), currentPreset.GetHorizonTint(time), currentStep);
            this.pbsky.zenithTint.value = Color.Lerp(lastPreset.GetZenithTint(time), currentPreset.GetZenithTint(time), currentStep);
        }
        else if(currentTick % 6 == 1){
            this.fog.meanFreePath.value = Mathf.Lerp(lastPreset.GetFogAttenuation(time), currentPreset.GetFogAttenuation(time), currentStep);
            this.fog.albedo.value = Color.Lerp(lastPreset.GetFogAlbedo(time), currentPreset.GetFogAlbedo(time), currentStep);
            this.fog.globalLightProbeDimmer.value = Mathf.Lerp(lastPreset.GetFogAmbientLight(time), currentPreset.GetFogAmbientLight(time), currentStep);            
        }
        else if(currentTick % 6 == 2){
            this.clouds.layerA.tint.value = Color.Lerp(lastPreset.GetCloudTint(time), currentPreset.GetCloudTint(time), currentStep);
            this.whiteBalance.temperature.value = Mathf.Lerp(lastPreset.GetWhiteBalanceTemperature(), currentPreset.GetWhiteBalanceTemperature(), currentStep);
            this.whiteBalance.tint.value = Mathf.Lerp(lastPreset.GetWhiteBalanceTint(), currentPreset.GetWhiteBalanceTint(), currentStep);
            this.lgg.gain.value = LerpFloat4(lastPreset.GetGain(time), currentPreset.GetGain(time), currentStep);     
        }
        else if(currentTick % 4 == 3){
            this.skyDirectionalLight.intensity = Mathf.Lerp(lastPreset.GetSunIntensity(time), currentPreset.GetSunIntensity(time), currentStep);
            this.skyDirectionalLight.color = Color.Lerp(lastPreset.GetSunColor(time), currentPreset.GetSunColor(time), currentStep);
            this.hdLight.angularDiameter = currentPreset.GetSunDiameter(time);
            Shader.SetGlobalFloat("_SkyLightMultiplier", currentPreset.GetFloorLighting(time));

            if(ROTATE_SUN_FLAG == 0){
                this.cachedRotation = currentPreset.GetSunRotation(time);
                this.skyDirectionalLight.transform.rotation = Quaternion.Euler(cachedRotation.x, 0, cachedRotation.y);
                ROTATE_SUN_FLAG++;
            }
            else if(ROTATE_SUN_FLAG == ROTATE_FLAG_MAX){
                ROTATE_SUN_FLAG = 0;
            }
            else{
                ROTATE_SUN_FLAG++;
            }
        }
    }

    // Sets the ambient status based on a time value passed
    private void SetStats(int time){
        float finalTime = time + this.delta;

        if(currentTick % 6 == 0){
            this.pbsky.horizonTint.value = currentPreset.GetHorizonTint(finalTime);
            this.pbsky.zenithTint.value = currentPreset.GetZenithTint(finalTime);

            if(currentPreset.HasFlare()){
                SetFlare(time);
                SetLensFlareIntensity(time);
            }
            else{
                DisableFlare();
            }
        }
        else if(currentTick % 6 == 1){
            this.fog.meanFreePath.value = currentPreset.GetFogAttenuation(finalTime);
            this.fog.albedo.value = currentPreset.GetFogAlbedo(finalTime);
            this.fog.globalLightProbeDimmer.value = currentPreset.GetFogAmbientLight(finalTime);
        }
        else if(currentTick % 6 == 2){
            this.clouds.layerA.tint.value = currentPreset.GetCloudTint(finalTime);
            this.whiteBalance.temperature.value = currentPreset.GetWhiteBalanceTemperature();
            this.whiteBalance.tint.value = currentPreset.GetWhiteBalanceTint();
            this.lgg.gain.value = currentPreset.GetGain(finalTime);
        }
        else if(currentTick % 4 == 3){
            this.skyDirectionalLight.intensity = currentPreset.GetSunIntensity(finalTime);
            this.skyDirectionalLight.color = currentPreset.GetSunColor(finalTime);
            this.hdLight.angularDiameter = currentPreset.GetSunDiameter(time);
            Shader.SetGlobalFloat("_SkyLightMultiplier", currentPreset.GetFloorLighting(time));

            if(ROTATE_SUN_FLAG == 0){
                this.cachedRotation = currentPreset.GetSunRotation(finalTime);
                this.skyDirectionalLight.transform.rotation = Quaternion.Euler(cachedRotation.x, 0, cachedRotation.y);
                ROTATE_SUN_FLAG++;
            }
            else if(ROTATE_SUN_FLAG == ROTATE_FLAG_MAX){
                ROTATE_SUN_FLAG = 0;
            }
            else{
                ROTATE_SUN_FLAG++;
            }
        }
    }

    private void TransformDelta(){
        if(this.delta > 1f)
            this.delta = 1f;
    }

    private float4 LerpFloat4(float4 a, float4 b, float t){
        Color x = new Color(a.x, a.y, a.z, a.w);
        Color y = new Color(b.x, b.y, b.z, b.w);
        Color output = Color.Lerp(x, y, t);

        return new float4(output.r, output.g, output.b, output.a);
    }

    // Sets the current Lens Flare
    private void SetFlare(int x){
        if(x >= 240 && x <= 1200){
            this.dayFlare.enabled = true;
            this.nightFlare.enabled = false;
        }
        else{
            this.dayFlare.enabled = false;
            this.nightFlare.enabled = true;
        }
    }

    // Sets the current Lens Flare
    private void DisableFlare(){
        this.dayFlare.enabled = false;
        this.nightFlare.enabled = false;
    }

    // Sets Lens Flare intensity
    private void SetLensFlareIntensity(int x){
        if(x >= 180 && x < 240){
            nightFlare.intensity = Mathf.Lerp(1f, 0.4f, (x-180)/60f);
        }
        else if(x >= 1200 && x < 1260){
            nightFlare.intensity = Mathf.Lerp(0.4f, 1f, (x-1200)/60f);
        }
        else{
            nightFlare.intensity = 1f;
        }

        if(x >= 240 && x <= 300){
            dayFlare.intensity = Mathf.Lerp(0.4f, 1f, (x-240)/60f);
        }
        else if(x >= 1140 && x < 1200){
            dayFlare.intensity = Mathf.Lerp(1f, 0.4f, (x-1140)/60f);
        }
        else{
            dayFlare.intensity = 1f;
        }
    }
}