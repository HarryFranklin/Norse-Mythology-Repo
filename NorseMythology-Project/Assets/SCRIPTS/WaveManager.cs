using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class WaveManager : MonoBehaviour
{
    // Singleton instance
    public static WaveManager Instance { get; private set; }
    
    [System.Serializable]
    public enum WaveType
    {
        TimeBased,
        KillCountBased
    }

    [System.Serializable]
    public struct WaveItem
    {
        public WaveType waveType;
        public float criteria;
    }


    [Header("Wave Configuration")]
    [SerializeField] private WaveItem[] waveItems; // Array of wave items for future expansion
    
    [Header("Wave Settings")]
    public int currentWave = 1;
    public WaveType currentWaveType = WaveType.TimeBased; // Default
    public float waveDuration = 60f; // Duration for time-based waves
    public int enemiesPerWave = 30; // Target kills for kill-count-based waves
    
    [Header("Wave Scaling")]
    public float healthScalingMultiplier = 1.05f;
    public float xpScalingMultiplier = 1.05f;
    
    [Header("References")]
    public GameManager gameManager;
    public EnemySpawner enemySpawner;
    
    private float waveTimer;
    private bool waveActive = false;
    private int enemiesKilledThisWave = 0;
    
    public delegate void WaveEvent(int waveNumber);
    public static event WaveEvent OnWaveStart;
    public static event WaveEvent OnWaveComplete;
    
    private void Awake()
    {
        // Singleton pattern - prevent duplicates
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Destroy duplicate WaveManager
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Only run Start logic if this is the singleton instance
        if (Instance == this)
        {
            FindReferences();
        }
    }

    public void FindReferences()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
    }
    
    public void StartWave()
    {
        FindReferences();

        if (waveItems == null || waveItems.Length < currentWave)
        {
            Debug.LogError("WaveManager: No configuration for current wave.");
            return;
        }

        WaveItem waveConfig = waveItems[currentWave - 1];
        currentWaveType = waveConfig.waveType;

        waveActive = true;
        waveTimer = 0f;
        enemiesKilledThisWave = 0;

        // Calculate wave modifiers
        float healthMultiplier = Mathf.Pow(healthScalingMultiplier, currentWave - 1);
        float xpMultiplier = Mathf.Pow(xpScalingMultiplier, currentWave - 1);

        if (enemySpawner != null)
        {
            enemySpawner.SetWaveModifiers(healthMultiplier, xpMultiplier);
            enemySpawner.StartSpawning();
        }

        OnWaveStart?.Invoke(currentWave);

        string waveTypeStr = currentWaveType == WaveType.TimeBased ? "Time-based" : "Kill-count-based";
        Debug.Log($"Wave {currentWave} started! ({waveTypeStr}) Health multiplier: {healthMultiplier:F2}, XP multiplier: {xpMultiplier:F2}");
    }
    
    private void Update()
    {
        if (!waveActive) return;

        if (waveItems == null || waveItems.Length < currentWave)
            return;

        WaveItem waveConfig = waveItems[currentWave - 1];

        switch (waveConfig.waveType)
        {
            case WaveType.TimeBased:
                waveTimer += Time.deltaTime;
                if (waveTimer >= waveConfig.criteria)
                {
                    CompleteWave();
                }
                break;

            case WaveType.KillCountBased:
                if (enemiesKilledThisWave >= waveConfig.criteria)
                {
                    CompleteWave();
                }
                break;
        }
    }

    public void OnEnemyKilled()
    {
        enemiesKilledThisWave++;
    }
    
    private void CompleteWave()
    {
        waveActive = false;
        
        // Stop enemy spawning
        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }
        
        OnWaveComplete?.Invoke(currentWave);
        Debug.Log($"Wave {currentWave} completed!");
        
        // Notify GameManager that wave is complete
        if (gameManager != null)
        {
            gameManager.OnWaveCompleted();
        }
    }
    
    public bool AreAllWavesCompleted()
    {
        return currentWave > waveItems.Length;
    }

    public void PrepareNextWave()
    {
        currentWave++;

        if (AreAllWavesCompleted())
        {
            Debug.Log("All waves completed!");
            // Don't load scenes here - just notify GameManager or handle state
            waveActive = false;
            return;
        }

        Debug.Log($"Prepared for wave {currentWave}");
    }

    public float GetWaveProgress()
    {
        if (!waveActive || waveItems == null || waveItems.Length < currentWave)
            return 0f;

        WaveItem waveConfig = waveItems[currentWave - 1];

        switch (waveConfig.waveType)
        {
            case WaveType.TimeBased:
                return waveTimer / waveConfig.criteria;

            case WaveType.KillCountBased:
                return enemiesKilledThisWave / waveConfig.criteria;

            default:
                return 0f;
        }
    }

    public int GetCurrentWave()
    {
        return currentWave;
    }
    
    public bool IsWaveActive()
    {
        return waveActive;
    }
    
    public WaveType GetWaveType()
    {
        return currentWaveType;
    }
    
    public int GetEnemiesKilledThisWave()
    {
        return enemiesKilledThisWave;
    }
    
    public float GetWaveTimer()
    {
        return waveTimer;
    }
}