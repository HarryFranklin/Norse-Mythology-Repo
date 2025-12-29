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
        KillCountBased,
        FixedEnemyCount
    }

    [System.Serializable]
    public class WaveItem
    {
        public WaveType waveType;
        public float criteria; // Duration for TimeBased, kill count for KillCountBased, total enemies for FixedEnemyCount
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

            // Sync current wave with the GameManager's game level
            // If GameManager says it's a new game (Level 1), force WaveManager to Wave 1.
            if (gameManager != null && gameManager.gameLevel == 1)
            {
                currentWave = 1;
                enemiesKilledThisWave = 0;
                waveActive = false;
            }

            // Safety check: Prevent index errors if inspector data was dirty
            if (currentWave < 1) currentWave = 1;
            
            Debug.Log($"WaveManager ready. Current Wave set to: {currentWave}");
        }
    }
    public void FindReferences()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;

        // 1. Find the spawner if we don't have it
        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
            
        // 2. Now that we definitely have it (or tried to), set the reference.
        if (enemySpawner != null)
        {
            enemySpawner.waveManager = this;
            Debug.Log("WaveManager: Linked successfully to Spawner.");
        }
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
            enemySpawner.SetWaveNumber(currentWave); // Set the wave number for max enemy scaling
            
            // Start appropriate spawning type based on wave type
            if (currentWaveType == WaveType.FixedEnemyCount)
            {
                enemySpawner.StartFixedSpawning((int)waveConfig.criteria);
            }
            else
            {
                enemySpawner.StartSpawning();
            }
        }

        OnWaveStart?.Invoke(currentWave);

        string waveTypeStr = GetWaveTypeDescription(currentWaveType);
        Debug.Log($"Wave {currentWave} started! ({waveTypeStr}) Health multiplier: {healthMultiplier:F2}, XP multiplier: {xpMultiplier:F2}");
    }
    
    private string GetWaveTypeDescription(WaveType waveType)
    {
        switch (waveType)
        {
            case WaveType.TimeBased:
                return "Time-based";
            case WaveType.KillCountBased:
                return "Kill-count-based";
            case WaveType.FixedEnemyCount:
                return "Fixed enemy count";
            default:
                return "Unknown";
        }
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
                
            case WaveType.FixedEnemyCount:
                // Check if all enemies have been spawned and defeated
                if (enemySpawner != null && enemySpawner.AreAllEnemiesDefeated())
                {
                    CompleteWave();
                }
                break;
        }
    }

    public void OnEnemyKilled()
    {
        enemiesKilledThisWave++;
        Debug.Log($"Enemy Killed! Count: {enemiesKilledThisWave} / {waveItems[currentWave-1].criteria}");
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
                
            case WaveType.FixedEnemyCount:
                // For fixed enemy count, progress is based on enemies killed vs total spawned
                if (enemySpawner != null && enemySpawner.AreAllEnemiesSpawned())
                {
                    // All enemies have been spawned, progress is based on how many are left alive
                    int totalEnemies = (int)waveConfig.criteria;
                    int enemiesRemaining = enemySpawner.GetCurrentEnemyCount();
                    int enemiesKilled = totalEnemies - enemiesRemaining;
                    return (float)enemiesKilled / totalEnemies;
                }
                else
                {
                    // Still spawning enemies, show spawn progress
                    return enemiesKilledThisWave / waveConfig.criteria;
                }

            default:
                return 0f;
        }
    }
    
    public string GetWaveProgressText()
    {
        if (!waveActive || waveItems == null || waveItems.Length < currentWave)
            return "Wave Inactive";

        WaveItem waveConfig = waveItems[currentWave - 1];

        switch (waveConfig.waveType)
        {
            case WaveType.TimeBased:
                float timeRemaining = waveConfig.criteria - waveTimer;
                return $"Time: {timeRemaining:F1}s";

            case WaveType.KillCountBased:
                int killsRemaining = (int)waveConfig.criteria - enemiesKilledThisWave;
                return $"Kills: {killsRemaining}";
                
            case WaveType.FixedEnemyCount:
                if (enemySpawner != null)
                {
                    int totalEnemies = (int)waveConfig.criteria;
                    int currentEnemies = enemySpawner.GetCurrentEnemyCount();
                    int enemiesKilled = totalEnemies - currentEnemies;
                    
                    if (enemySpawner.AreAllEnemiesSpawned())
                    {
                        return $"Enemies: {currentEnemies} remaining";
                    }
                    else
                    {
                        return $"Enemies: {enemiesKilled}/{totalEnemies} defeated";
                    }
                }
                return $"Enemies: 0/{(int)waveConfig.criteria}";

            default:
                return "Unknown Wave Type";
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