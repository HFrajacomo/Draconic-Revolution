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
    private Exposure exposure;
    private IndirectLightingController indirectLighting;
    private float sunVolumetricDimmer;
    private float moonVolumetricDimmer;

    // Cave Factor: 0 under the open sky, 1 when the camera is enclosed (see UpdateCaveFactor)
    private ChunkLoader cl;
    private float caveFactor = 0f;
    private float caveTarget = 0f;
    private float caveSampleTimer = 0f;
    private static readonly float CAVE_SAMPLE_INTERVAL = 0.25f;
    private static readonly float CAVE_FADE_SPEED = 0.5f;
    private static readonly int CAVE_LIGHT_DARK = 4;
    private static readonly int CAVE_LIGHT_OPEN = 12;
    // Fraction of the fog's sky light kept inside caves
    private static readonly float CAVE_FOG_AMBIENT_KEPT = 0f;
    // Dark Adaptation (see VoxelLightIntensity.hlsl): faint glow of unlit surfaces near the camera inside caves, in nits at the camera
    private static readonly float DARK_ADAPTATION_INTENSITY = 0.045f;
    private static readonly float DARK_ADAPTATION_RADIUS = 30f;
    private static readonly int DARK_ADAPTATION_ID = Shader.PropertyToID("_DarkAdaptation");
    private static readonly int DARK_ADAPTATION_CENTER_ID = Shader.PropertyToID("_DarkAdaptationCenter");

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
        this.fog = null;
        this.lgg = null;
        this.exposure = null;
        this.indirectLighting = null;
        this.cl = null;
    }

    public void SetChunkLoader(ChunkLoader cl){
        this.cl = cl;
    }

    void Start(){
        this.volume.TryGet<PhysicallyBasedSky>(out this.pbsky);
        this.volume.TryGet<CloudLayer>(out this.clouds);
        this.volume.TryGet<WhiteBalance>(out this.whiteBalance);
        this.volume.TryGet<Fog>(out this.fog);
        this.volume.TryGet<LiftGammaGain>(out this.lgg);
        this.volume.TryGet<Exposure>(out this.exposure);
        this.volume.TryGet<IndirectLightingController>(out this.indirectLighting);

        this.exposure.compensation.value = BaseAmbientPreset.EXPOSURE_METERING_COMPENSATION;
        this.sunVolumetricDimmer = this.hdLight.volumetricDimmer;
        this.moonVolumetricDimmer = this.hdLightMoon.volumetricDimmer;

        this.windHandler = new GlobalWindHandler(this.clouds);

        this.currentParticleSystem = this.rainParticleSystem;
        this.psEmission = this.rainParticleSystem.emission;
        SetParticleSystem();

        currentAmbient = BiomeHandler.GetAmbientGroup(playerPositionHandler.GetCurrentBiomeCode());
        lastAmbient = currentAmbient;
        currentPreset = BaseAmbientPreset.GetPreset(currentAmbient);
        lastPreset = currentPreset;

        ROTATE_SUN_FLAG = 0;

        this.skyDirectionalLight.color = currentPreset.GetSunColor(0);
        this.moonDirectionalLight.color = currentPreset.GetMoonColor(0);

        this.weatherCast.SetFogNoise(this.timer.ToSeconds()*TimeOfDay.ticksForMinute, this.timer.days);
        this.weatherCast.SetWeatherNoise(this.timer.ToSeconds(), this.timer.days);

        this.windHandler.InitRain(this.weatherCast.GetWeatherState() == WeatherState.RAINY);

        SetStats(this.timer.ToSeconds());
        ApplyWeatherChanges(0, this.timer.ToSeconds(), (int)this.timer.GetFakeTicks(), this.timer.days, currentPreset.IsSurface(), false);
    }

    void Update(){
        int time = timer.ToSeconds();
        currentTick = (int)timer.GetFakeTicks();
        delta += Time.deltaTime/2;

        UpdateCaveFactor();

        if(!isTransitioning){
            currentAmbient = BiomeHandler.GetAmbientGroup(playerPositionHandler.GetCurrentBiomeCode());
            
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
                this.windHandler.Tick(currentTick, time, (int)timer.days, this.weatherCast.GetWeatherState() == WeatherState.RAINY);
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
            this.rainParticleSystem.gameObject.SetActive(false);
        }
        else if(!this.currentPreset.IsSnowInstead() && this.currentParticleSystem != this.rainParticleSystem){
            this.rainParticleSystem.gameObject.SetActive(true);
            this.currentParticleSystem = this.rainParticleSystem;
            this.psEmission = this.rainParticleSystem.emission;
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
            Vector3 playerPos = playerPositionHandler.GetPlayerWorldPosition();

            if(!isSurface){
                this.fog.meanFreePath.value = currentPreset.GetFogAttenuation(time);
                this.fog.maximumHeight.value = currentPreset.GetFogMaxHeight(time);
                this.fog.baseHeight.value = currentPreset.GetFogBaseHeight(time);
                this.fog.anisotropy.value = currentPreset.GetFogAnisotropy(time) * this.weatherCast.GetFogAnisotropyMultiplier();
                this.psEmission.rateOverTime = 0;
                this.currentParticleSystem.gameObject.transform.localPosition = new Vector3(playerPos.x, playerPos.y + 260, playerPos.z);
                this.currentParticleSystem.gameObject.transform.rotation = Quaternion.Euler(-90, 0, 0);
                return;
            }
            
            float mult = 3f;
            Vector2 globalWind = this.windHandler.GetGlobalWind();

            if(isTransition){
                this.fog.meanFreePath.value = AddFog(Mathf.Lerp(lastPreset.GetFogAttenuation(time), currentPreset.GetFogAttenuation(time), currentStep), this.weatherCast.GetAdditionalFog());
                this.fog.maximumHeight.value = AddFog(Mathf.Lerp(lastPreset.GetFogMaxHeight(time), currentPreset.GetFogMaxHeight(time), currentStep), this.weatherCast.GetMaximumHeight());
                this.fog.baseHeight.value = AddFog(Mathf.Lerp(lastPreset.GetFogBaseHeight(time), currentPreset.GetFogBaseHeight(time), currentStep), this.weatherCast.GetBaseHeight());
                this.fog.anisotropy.value = Mathf.Lerp(lastPreset.GetFogAnisotropy(time) * this.weatherCast.GetFogAnisotropyMultiplier(), currentPreset.GetFogAnisotropy(time) * this.weatherCast.GetFogAnisotropyMultiplier(), currentStep);
                SetParticleSystem();
                this.psEmission.rateOverTime = Mathf.Lerp(lastPreset.GetRainSpawnRate(this.weatherCast), currentPreset.GetRainSpawnRate(this.weatherCast), currentStep);
                this.currentParticleSystem.gameObject.transform.position = new Vector3(playerPos.x - globalWind.x * mult, playerPos.y+260, playerPos.z  - globalWind.y * mult);
                this.currentParticleSystem.gameObject.transform.rotation = Quaternion.Euler(Mathf.Lerp(-90, -40, GetWindMax(globalWind)), GetAngle(globalWind.normalized), 0);
            }
            else{
                this.fog.meanFreePath.value = AddFog(currentPreset.GetFogAttenuation(time), this.weatherCast.GetAdditionalFog());
                this.fog.maximumHeight.value = AddFog(currentPreset.GetFogMaxHeight(time), this.weatherCast.GetMaximumHeight());
                this.fog.baseHeight.value = AddFog(currentPreset.GetFogBaseHeight(time), this.weatherCast.GetBaseHeight());
                this.fog.anisotropy.value = currentPreset.GetFogAnisotropy(time) * this.weatherCast.GetFogAnisotropyMultiplier();
                SetParticleSystem();
                this.psEmission.rateOverTime = currentPreset.GetRainSpawnRate(this.weatherCast);
                this.currentParticleSystem.gameObject.transform.position = new Vector3(playerPos.x - globalWind.x * mult, playerPos.y+260, playerPos.z  - globalWind.y * mult);
                this.currentParticleSystem.gameObject.transform.rotation = Quaternion.Euler(Mathf.Lerp(-90, -40, GetWindMax(globalWind)), GetAngle(globalWind.normalized), 0);
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
            this.fog.globalLightProbeDimmer.value = Mathf.Lerp(lastPreset.GetFogAmbientLight(time), currentPreset.GetFogAmbientLight(time), currentStep) * GetCaveFogMultiplier();
            this.clouds.layerA.tint.value = Color.Lerp(lastPreset.GetCloudTint(time), currentPreset.GetCloudTint(time), currentStep);
            SetExposureLimits(Mathf.Lerp(lastPreset.GetExposureBase(), currentPreset.GetExposureBase(), currentStep));
            SetIndirectDiffuse(Mathf.Lerp(lastPreset.GetIndirectDiffuse(time), currentPreset.GetIndirectDiffuse(time), currentStep));
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
            SetLightVolumetrics(Mathf.Lerp(lastPreset.GetLightVolumetricDimmer(), currentPreset.GetLightVolumetricDimmer(), currentStep));
            Shader.SetGlobalFloat("_SkyLightMultiplier", currentPreset.GetFloorLighting(time));
            Shader.SetGlobalFloat("_VoxelLightFloor", Mathf.Lerp(lastPreset.GetVoxelLightFloor(), currentPreset.GetVoxelLightFloor(), currentStep));

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
            this.fog.globalLightProbeDimmer.value = currentPreset.GetFogAmbientLight(finalTime) * GetCaveFogMultiplier();
            this.clouds.layerA.tint.value = currentPreset.GetCloudTint(finalTime);
            SetExposureLimits(currentPreset.GetExposureBase());
            SetIndirectDiffuse(currentPreset.GetIndirectDiffuse(finalTime));
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
            SetLightVolumetrics(currentPreset.GetLightVolumetricDimmer());
            Shader.SetGlobalFloat("_SkyLightMultiplier", currentPreset.GetFloorLighting(time));
            Shader.SetGlobalFloat("_VoxelLightFloor", currentPreset.GetVoxelLightFloor());

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

    // Limits auto-exposure to a small window around the preset's base EV, so dark scenes are not normalized to mid-gray
    // Inside caves the camera can't brighten at all, so they stay dark until lit
    private void SetExposureLimits(float baseEV){
        this.exposure.limitMin.value = baseEV - BaseAmbientPreset.EXPOSURE_ADAPT_BRIGHTEN * (1f - this.caveFactor);
        this.exposure.limitMax.value = baseEV + BaseAmbientPreset.EXPOSURE_ADAPT_DARKEN;
    }

    private void SetIndirectDiffuse(float multiplier){
        if(this.indirectLighting == null)
            return;

        this.indirectLighting.indirectDiffuseLightingMultiplier.value = multiplier;
    }

    // Sun and Moon don't light the fog of caves
    private void SetLightVolumetrics(float dimmer){
        dimmer *= 1f - this.caveFactor;
        this.hdLight.volumetricDimmer = this.sunVolumetricDimmer * dimmer;
        this.hdLightMoon.volumetricDimmer = this.moonVolumetricDimmer * dimmer;
    }

    private float GetCaveFogMultiplier(){
        return Mathf.Lerp(1f, CAVE_FOG_AMBIENT_KEPT, this.caveFactor);
    }

    /*
    Measures how enclosed the camera is through the Natural Voxel Light around it. Caves of every layer and biome
    have low natural light, so their fog loses the sky's light and auto-exposure can't brighten them.
    Keeps the last value while the light of the chunk isn't calculated
    */
    private void UpdateCaveFactor(){
        if(this.cl == null)
            return;

        this.caveSampleTimer -= Time.deltaTime;

        if(this.caveSampleTimer <= 0f){
            this.caveSampleTimer = CAVE_SAMPLE_INTERVAL;

            int level = Mathf.Max(
                this.cl.GetLight(new CastCoord(this.playerPositionHandler.cameraTransform.position)),
                this.cl.GetLight(new CastCoord(this.playerPositionHandler.GetPlayerMiddlePoint().position))
            );

            if(level >= 0)
                this.caveTarget = 1f - Mathf.Clamp01((float)(level - CAVE_LIGHT_DARK) / (CAVE_LIGHT_OPEN - CAVE_LIGHT_DARK));
        }

        this.caveFactor = Mathf.MoveTowards(this.caveFactor, this.caveTarget, Time.deltaTime * CAVE_FADE_SPEED);

        Vector3 camera = this.playerPositionHandler.cameraTransform.position;
        Shader.SetGlobalFloat(DARK_ADAPTATION_ID, DARK_ADAPTATION_INTENSITY * this.caveFactor);
        Shader.SetGlobalVector(DARK_ADAPTATION_CENTER_ID, new Vector4(camera.x, camera.y, camera.z, DARK_ADAPTATION_RADIUS));
    }

    private float GetAngle(Vector2 a){
        if(a.x > 0)
            return 360 - Vector2.Angle(Vector2.down, a);
        return Vector2.Angle(Vector2.down, a);
    }

    private float GetWindMax(Vector2 v){
        float a = Mathf.Abs(v.x);
        float b = Mathf.Abs(v.y);

        return Mathf.Max(a, b) / this.windHandler.GetMaxWindPower();
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
