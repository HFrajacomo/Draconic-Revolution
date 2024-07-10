using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using Unity.Mathematics;

public class AmbientHandler : MonoBehaviour
{
    // Weather Reference
    public WeatherCast weatherCast = new WeatherCast();

    // Unity References
    public GameObject lightObject;
	public Transform skyboxLight;
	public Light skyDirectionalLight;
    public HDAdditionalLightData hdLight;
    public Light moonDirectionalLight;
    public HDAdditionalLightData hdLightMoon;
	public TimeOfDay timer;
    public PlayerPositionHandler playerPositionHandler;
    public LensFlareComponentSRP dayFlare;
    public LensFlareComponentSRP nightFlare;

    // Particles
    private ParticleSystem currentParticleSystem;
    public ParticleSystem rainParticleSystem;
    public ParticleSystem snowParticleSystem;

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

    // Wind
    private GlobalWindHandler windHandler;

    // Particle System Cache
    private ParticleSystem.EmissionModule psEmission;
    private ParticleSystem.VelocityOverLifetimeModule psVelocity;


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

        this.windHandler = new GlobalWindHandler(this.clouds);

        this.currentParticleSystem = this.rainParticleSystem;
        this.psEmission = this.rainParticleSystem.emission;
        this.psVelocity = this.rainParticleSystem.velocityOverLifetime;
        SetParticleSystem();

        currentAmbient = BiomeHandler.GetAmbientGroup(BiomeHandler.BiomeToByte(playerPositionHandler.GetCurrentBiome()));
        lastAmbient = currentAmbient;
        currentPreset = BaseAmbientPreset.GetPreset(currentAmbient);
        lastPreset = currentPreset;

        ROTATE_SUN_FLAG = 0;

        this.skyDirectionalLight.color = currentPreset.GetSunColor(0);
        this.moonDirectionalLight.color = currentPreset.GetMoonColor(0);

        weatherCast.SetFogNoise(this.timer.ToSeconds()*TimeOfDay.ticksForMinute, this.timer.days);
        weatherCast.SetWeatherNoise(this.timer.ToSeconds(), this.timer.days);

        SetStats(this.timer.ToSeconds());
        ApplyWeatherChanges(0, this.timer.ToSeconds(), (int)this.timer.GetFakeTicks(), this.timer.days, currentPreset.IsSurface(), false);
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

            if(currentTick != lastTick){
                this.windHandler.Tick(currentTick, time, (int)timer.days, this.weatherCast.GetWeatherState() == WeatherState.RAINY);

                SetStats(time);
                ApplyWeatherChanges((float)this.updateTimer/FRAMES_TO_CHANGE, time, currentTick, this.timer.days, currentPreset.IsSurface(), isTransitioning);
            }

            lastAmbient = currentAmbient;
            lastTick = currentTick;
            lastTime = time;

        }
        else{
            this.updateTimer++;

            if(currentTick != lastTick){
                LerpStatus(time);
                ApplyWeatherChanges((float)this.updateTimer/FRAMES_TO_CHANGE, time, currentTick, this.timer.days, currentPreset.IsSurface(), isTransitioning);
            }
            
            if(this.updateTimer == FRAMES_TO_CHANGE){
                isTransitioning = false;
                this.updateTimer = 0;
                lastAmbient = currentAmbient;
                lastPreset = currentPreset;
            }

            lastTick = currentTick;
        } 
    }

    // Changes the current Particle System depending on the Current Ambient Preset
    private void SetParticleSystem(){
        if(this.currentPreset == null)
            return;

        if(this.currentPreset.IsSnowInstead() && this.currentParticleSystem != this.snowParticleSystem){
            this.snowParticleSystem.gameObject.SetActive(true);
            this.currentParticleSystem = this.snowParticleSystem;
            this.psEmission = this.snowParticleSystem.emission;
            this.psVelocity = this.snowParticleSystem.velocityOverLifetime;
            this.rainParticleSystem.gameObject.SetActive(false);
        }
        else if(!this.currentPreset.IsSnowInstead() && this.currentParticleSystem != this.rainParticleSystem){
            this.rainParticleSystem.gameObject.SetActive(true);
            this.currentParticleSystem = this.rainParticleSystem;
            this.psEmission = this.rainParticleSystem.emission;
            this.psVelocity = this.rainParticleSystem.velocityOverLifetime;
            this.snowParticleSystem.gameObject.SetActive(false);
        }
    }

    // Sets and changes Fog Attenuation based on Biome and Weather component
    private void ApplyWeatherChanges(float currentStep, int time, int currentTick, uint days, bool isSurface, bool isTransition){
        // Sampling the Weather Noise
        if(currentTick % 6 == 0){
            weatherCast.SetFogNoise((int)((days*TimeOfDay.ticksForMinute*1440) + (time*TimeOfDay.ticksForMinute+currentTick)), days);
            weatherCast.SetWeatherNoise((int)((days*1440) + time), days);
        }

        // Fog Shape and Density & Rain
        else if(currentTick % 6 == 5){
            if(!isSurface){
                this.fog.meanFreePath.value = currentPreset.GetFogAttenuation(time);
                this.fog.maximumHeight.value = currentPreset.GetFogMaxHeight(time);
                this.fog.baseHeight.value = currentPreset.GetFogBaseHeight(time);
                this.psEmission.rateOverTime = 0;
                //this.psVelocity.x = 0;
                //this.psVelocity.z = 0;
                return;
            }
            
            if(isTransition){
                this.fog.meanFreePath.value = AddFog(Mathf.Lerp(lastPreset.GetFogAttenuation(time), currentPreset.GetFogAttenuation(time), currentStep), this.weatherCast.GetAdditionalFog());
                this.fog.maximumHeight.value = AddFog(Mathf.Lerp(lastPreset.GetFogMaxHeight(time), currentPreset.GetFogMaxHeight(time), currentStep), this.weatherCast.GetMaximumHeight());
                this.fog.baseHeight.value = AddFog(Mathf.Lerp(lastPreset.GetFogBaseHeight(time), currentPreset.GetFogBaseHeight(time), currentStep), this.weatherCast.GetBaseHeight());
                SetParticleSystem();
                this.psEmission.rateOverTime = Mathf.Lerp(lastPreset.GetRainSpawnRate(this.weatherCast), currentPreset.GetRainSpawnRate(this.weatherCast), currentStep);
                //this.psVelocity.x = this.windHandler.GetGlobalWind().x;
                //this.psVelocity.z = this.windHandler.GetGlobalWind().y;
            }
            else{
                this.fog.meanFreePath.value = AddFog(currentPreset.GetFogAttenuation(time), this.weatherCast.GetAdditionalFog());
                this.fog.maximumHeight.value = AddFog(currentPreset.GetFogMaxHeight(time), this.weatherCast.GetMaximumHeight());
                this.fog.baseHeight.value = AddFog(currentPreset.GetFogBaseHeight(time), this.weatherCast.GetBaseHeight());
                SetParticleSystem();
                this.psEmission.rateOverTime = currentPreset.GetRainSpawnRate(this.weatherCast);
                //this.psVelocity.x = this.windHandler.GetGlobalWind().x;
                //this.psVelocity.z = this.windHandler.GetGlobalWind().y;
            }
        }

        // Fog Color and Clouds
        else if(currentTick % 6 == 1){
            // Runs everytime
            this.clouds.layerA.opacityA.value = this.weatherCast.GetCloudLocalOpacity();
            this.clouds.layerB.opacityA.value = this.weatherCast.GetCloudLocalOpacity();
            this.clouds.opacity.value = this.weatherCast.GetCloudGlobalOpacity();

            if(!isSurface){
                this.fog.albedo.value = currentPreset.GetFogAlbedo(time);
                this.clouds.layerB.tint.value = currentPreset.GetCloudTint(time);
                return;
            }
            
            if(isTransition){
                this.fog.albedo.value = SubColor(Color.Lerp(lastPreset.GetFogAlbedo(time), currentPreset.GetFogAlbedo(time), currentStep), this.weatherCast.GetSubtractiveFogColor());
                this.clouds.layerB.tint.value = MultiplyColor(Color.Lerp(lastPreset.GetCloudTint(time), currentPreset.GetCloudTint(time), currentStep), this.weatherCast.GetCloudBMultiplier());
      
            }
            else{
                this.fog.albedo.value = SubColor(currentPreset.GetFogAlbedo(time), this.weatherCast.GetSubtractiveFogColor());
                this.clouds.layerB.tint.value = MultiplyColor(currentPreset.GetCloudTint(time), this.weatherCast.GetCloudBMultiplier());      
            }
        }        
    }

    // Calculates the status of ambientation features while there's a change in preset happening
    private void LerpStatus(int time){
        float currentStep = (float)this.updateTimer/FRAMES_TO_CHANGE;

        if(currentTick % 6 == 0){
            this.pbsky.horizonTint.value = Color.Lerp(lastPreset.GetHorizonTint(time), currentPreset.GetHorizonTint(time), currentStep);
            this.pbsky.zenithTint.value = Color.Lerp(lastPreset.GetZenithTint(time), currentPreset.GetZenithTint(time), currentStep);

            EnableShadow(time);
        }
        else if(currentTick % 6 == 1){
            this.fog.globalLightProbeDimmer.value = Mathf.Lerp(lastPreset.GetFogAmbientLight(time), currentPreset.GetFogAmbientLight(time), currentStep);
            this.clouds.layerA.tint.value = Color.Lerp(lastPreset.GetCloudTint(time), currentPreset.GetCloudTint(time), currentStep);
        }
        else if(currentTick % 6 == 2){
            this.whiteBalance.temperature.value = Mathf.Lerp(lastPreset.GetWhiteBalanceTemperature(), currentPreset.GetWhiteBalanceTemperature(), currentStep);
            this.whiteBalance.tint.value = Mathf.Lerp(lastPreset.GetWhiteBalanceTint(), currentPreset.GetWhiteBalanceTint(), currentStep);
            this.lgg.gain.value = LerpFloat4(lastPreset.GetGain(time), currentPreset.GetGain(time), currentStep);     
        }
        else if(currentTick % 6 == 4){
            this.pbsky.spaceEmissionMultiplier.value = currentPreset.GetStarMapMultiplier(time);
            this.pbsky.spaceRotation.value = currentPreset.GetStarMapRotation(time);
        }
        else if(currentTick % 4 == 3){
            this.skyDirectionalLight.intensity = Mathf.Lerp(lastPreset.GetSunIntensity(time), currentPreset.GetSunIntensity(time), currentStep);
            this.hdLight.angularDiameter = currentPreset.GetSunDiameter(time);
            this.moonDirectionalLight.intensity = Mathf.Lerp(lastPreset.GetMoonIntensity(time), currentPreset.GetMoonIntensity(time), currentStep);
            this.hdLightMoon.angularDiameter = currentPreset.GetMoonDiameter(time);
            Shader.SetGlobalFloat("_SkyLightMultiplier", currentPreset.GetFloorLighting(time));

            if(ROTATE_SUN_FLAG == 0){
                this.cachedRotation = currentPreset.GetSunRotation(time);
                this.skyDirectionalLight.transform.rotation = Quaternion.Euler(cachedRotation.x, 0, cachedRotation.y);
                this.cachedRotation = currentPreset.GetMoonRotation(time);
                this.moonDirectionalLight.transform.rotation = Quaternion.Euler(cachedRotation.x, 0, cachedRotation.y);
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
                SetLensFlareIntensity(time, this.weatherCast);
            }
            else{
                DisableFlare();
            }

            EnableShadow(time);
        }
        else if(currentTick % 6 == 1){
            this.fog.globalLightProbeDimmer.value = currentPreset.GetFogAmbientLight(finalTime);
            this.clouds.layerA.tint.value = currentPreset.GetCloudTint(finalTime);
        }
        else if(currentTick % 6 == 2){
            this.whiteBalance.temperature.value = currentPreset.GetWhiteBalanceTemperature();
            this.whiteBalance.tint.value = currentPreset.GetWhiteBalanceTint();
            this.lgg.gain.value = currentPreset.GetGain(finalTime);
        }
        else if(currentTick % 6 == 4){
            this.pbsky.spaceEmissionMultiplier.value = currentPreset.GetStarMapMultiplier(finalTime);
            this.pbsky.spaceRotation.value = currentPreset.GetStarMapRotation(finalTime);
        }
        else if(currentTick % 4 == 3){
            this.skyDirectionalLight.intensity = currentPreset.GetSunIntensity(finalTime);
            this.hdLight.angularDiameter = currentPreset.GetSunDiameter(time);
            this.moonDirectionalLight.intensity = currentPreset.GetMoonIntensity(time);
            this.hdLightMoon.angularDiameter = currentPreset.GetMoonDiameter(time);
            Shader.SetGlobalFloat("_SkyLightMultiplier", currentPreset.GetFloorLighting(time));

            if(ROTATE_SUN_FLAG == 0){
                this.cachedRotation = currentPreset.GetSunRotation(finalTime);
                this.skyDirectionalLight.transform.rotation = Quaternion.Euler(cachedRotation.x, 0, cachedRotation.y);
                this.cachedRotation = currentPreset.GetMoonRotation(finalTime);
                this.moonDirectionalLight.transform.rotation = Quaternion.Euler(cachedRotation.x, 0, cachedRotation.y);
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

    // An operation that adds Biome fog to Random and Weather fog only if in surface chunks
    private float AddFog(float biomeFog, float additionalFog){
        if(this.currentPreset.IsSurface()){
            return biomeFog+additionalFog;
        }
        return biomeFog;
    }

    // Subtracts two color
    private Color SubColor(Color main, Color sub){
        return main - sub;
    }

    // Multiply color with scalar
    private Color MultiplyColor(Color main, float scalar){
        return main*scalar;
    }

    private float4 LerpFloat4(float4 a, float4 b, float t){
        Color x = new Color(a.x, a.y, a.z, a.w);
        Color y = new Color(b.x, b.y, b.z, b.w);
        Color output = Color.Lerp(x, y, t);

        return new float4(output.r, output.g, output.b, output.a);
    }

    // Sets the current Lens Flare
    private void SetFlare(int x){
        if(x >= 240 && x <= 1200)
            this.dayFlare.enabled = true;
        else
            this.dayFlare.enabled = false;

        if(x >= 1020 || x < 240)
            this.nightFlare.enabled = true;
        else
            this.nightFlare.enabled = false;
    }

    // Enable Sun/Moon Shadow
    private void EnableShadow(int x){
        if(x >= 240 && x <= 1200){
            this.hdLight.EnableShadows(true);
            this.hdLightMoon.EnableShadows(false);
        }
        else{
            this.hdLight.EnableShadows(false);
            this.hdLightMoon.EnableShadows(true);
        }
    }

    // Sets the current Lens Flare
    private void DisableFlare(){
        this.dayFlare.enabled = false;
        this.nightFlare.enabled = false;
    }

    // Sets Lens Flare intensity
    private void SetLensFlareIntensity(int x, WeatherCast wc){
        if(x >= 360 && x < 420){
            nightFlare.intensity = Mathf.Lerp(1f, 0.4f, (x-360)/60f) * wc.GetFlareMultiplier();
        }
        else if(x >= 1020 && x < 1080){
            nightFlare.intensity = Mathf.Lerp(0.4f, 1f, (x-1020)/60f) * wc.GetFlareMultiplier();
        }
        else{
            nightFlare.intensity = 1f * wc.GetFlareMultiplier();
        }

        if(x >= 240 && x <= 300){
            dayFlare.intensity = Mathf.Lerp(0.4f, 1f, (x-240)/60f) * wc.GetFlareMultiplier();
        }
        else if(x >= 1140 && x < 1200){
            dayFlare.intensity = Mathf.Lerp(1f, 0.4f, (x-1140)/60f) * wc.GetFlareMultiplier();
        }
        else{
            dayFlare.intensity = 1f * wc.GetFlareMultiplier();
        }
    }
}
