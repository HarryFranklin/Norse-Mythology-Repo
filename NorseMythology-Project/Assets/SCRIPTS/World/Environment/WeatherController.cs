using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class WeatherController : MonoBehaviour
{
    public enum WeatherType { Clear, Rain, Snow }

    [Header("General Settings")]
    public WeatherType currentWeather = WeatherType.Clear;
    public bool autoUpdate = true;
    [Range(0, 1f)] public float masterIntensity = 1f;
    
    [Header("Wind Settings")]
    [Range(0, 1f)] public float windIntensity = 0.5f;
    
    [Header("Rain Settings")]
    [Range(0, 1f)] public float rainIntensity = 1f;
    
    [Header("Snow & Fog Settings")]
    [Range(0, 1f)] public float snowIntensity = 1f;
    [Range(0, 1f)] public float fogIntensity = 1f;
    [Range(0, 7f)] public float snowLevel = 0f;

    // =========================================================
    // TINKER SECTION (Base Configuration)
    // =========================================================
    [Header("Base Configuration (Max Values)")]
    public float baseRainRate = 200f;
    public float baseSnowRate = 110f;
    public float baseWindRate = 14f;
    public float baseSnowWindRadius = 30f;
    
    [Header("Wind Physics Multipliers")]
    public float windSpeedMultiplier = 20f; 
    public float rainWindForce = 30f;      
    public float snowWindForce = 14f;       

    [Header("References")]
    [SerializeField] private Player player;
    public Material snowMaterial;
    
    [Header("Particle Systems")]
    public ParticleSystem rainSystem;
    public ParticleSystem snowSystem;
    public ParticleSystem windSystem; 
    public ParticleSystem fogSystem;

    // Cached Modules
    private ParticleSystem.EmissionModule rainEmission;
    private ParticleSystem.ForceOverLifetimeModule rainForce;
    private ParticleSystem.EmissionModule snowEmission;
    private ParticleSystem.ForceOverLifetimeModule snowForce;
    private ParticleSystem.ShapeModule snowShape;
    private ParticleSystem.EmissionModule windEmission;
    private ParticleSystem.MainModule windMain;
    private ParticleSystem.EmissionModule fogEmission;

    private bool _isInitialised = false;

    void Awake()
    {
        InitModules();
        UpdateWeatherState();
    }

    void Update()
    {
        // 1. Follow Player
        if (player == null) player = FindFirstObjectByType<Player>();
        if (player != null) transform.position = player.transform.position;

        // 2. Live Updates
        if (autoUpdate) UpdateIntensities();
    }

    void OnValidate()
    {
        InitModules();
        UpdateWeatherState();
    }

    void InitModules()
    {
        if (rainSystem != null)
        {
            rainEmission = rainSystem.emission;
            rainForce = rainSystem.forceOverLifetime;
        }

        if (snowSystem != null)
        {
            snowEmission = snowSystem.emission;
            snowForce = snowSystem.forceOverLifetime;
            snowShape = snowSystem.shape;
        }

        if (windSystem != null)
        {
            windEmission = windSystem.emission;
            windMain = windSystem.main;
        }

        if (fogSystem != null)
        {
            fogEmission = fogSystem.emission;
        }
        
        _isInitialised = true;
    }

    public void SetWeather(WeatherType type)
    {
        currentWeather = type;
        UpdateWeatherState();
    }

    private void UpdateWeatherState()
    {
        if (!_isInitialised) InitModules();

        bool isRain = (currentWeather == WeatherType.Rain);
        bool isSnow = (currentWeather == WeatherType.Snow);

        if (rainSystem) rainSystem.gameObject.SetActive(isRain);
        if (snowSystem) snowSystem.gameObject.SetActive(isSnow);
        if (windSystem) windSystem.gameObject.SetActive(isSnow); 
        if (fogSystem) fogSystem.gameObject.SetActive(false); 

        UpdateIntensities();
    }

    private void UpdateIntensities()
    {
        if (!_isInitialised) InitModules();

        // Global
        if (snowMaterial != null) snowMaterial.SetFloat("_SnowLevel", snowLevel);

        // Rain
        if (currentWeather == WeatherType.Rain && rainSystem != null)
        {
            SetRate(rainEmission, baseRainRate * masterIntensity * rainIntensity);
            float forceX = -rainWindForce * windIntensity * masterIntensity;
            rainForce.x = new ParticleSystem.MinMaxCurve(forceX * 0.8f, forceX);
        }

        // Snow
        if (currentWeather == WeatherType.Snow && snowSystem != null)
        {
            SetRate(snowEmission, baseSnowRate * masterIntensity * snowIntensity);
            snowShape.radius = baseSnowWindRadius * Mathf.Clamp(windIntensity, 0.4f, 1f) * masterIntensity;
            float forceX = -snowWindForce * windIntensity;
            snowForce.x = new ParticleSystem.MinMaxCurve(forceX * 0.6f, forceX);
        }

        // Wind
        if (windSystem != null && windSystem.gameObject.activeSelf)
        {
            SetRate(windEmission, baseWindRate * masterIntensity * (windIntensity + fogIntensity));
            windMain.startLifetime = 2f + 6f * (1f - windIntensity);
            float speed = windSpeedMultiplier * windIntensity;
            windMain.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.75f, speed);
        }
    }

    private void SetRate(ParticleSystem.EmissionModule module, float rate)
    {
        module.rateOverTime = rate;
    }

    // ========================================================================================
    // OPTIMIZED EDITOR VISUALIZATION
    // ========================================================================================
#if UNITY_EDITOR
    private double _lastSimTime;

    void OnEnable()
    {
        EditorApplication.update += EditorUpdate;
        _lastSimTime = EditorApplication.timeSinceStartup;
    }

    void OnDisable()
    {
        EditorApplication.update -= EditorUpdate;
    }

    void EditorUpdate()
    {
        if (Application.isPlaying) return;
        
        // If we aren't selecting the object, don't do anything (saves performance)
        if (Selection.activeGameObject != gameObject) return;

        // THROTTLE: Only update ~30 times per second (every 0.033s)
        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - _lastSimTime < 0.033f) return;

        float dt = (float)(currentTime - _lastSimTime);
        _lastSimTime = currentTime;

        // Safety cap for dt to prevent explosion after a lag spike
        if (dt > 0.1f) dt = 0.033f;

        SimulateIfActive(rainSystem, dt);
        SimulateIfActive(snowSystem, dt);
        SimulateIfActive(windSystem, dt);
        
        // Repaint is expensive, so we only do it inside this throttled block
        SceneView.RepaintAll();
    }

    private void SimulateIfActive(ParticleSystem ps, float dt)
    {
        if (ps != null && ps.gameObject.activeInHierarchy)
        {
            ps.Simulate(dt, true, false);
        }
    }
#endif
}