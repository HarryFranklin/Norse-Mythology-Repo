using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public enum WaveType
    {
        TimeBased,
        KillCountBased
    }
    
    [Header("Wave Settings")]
    public int currentWave = 1;
    public WaveType waveType = WaveType.TimeBased; // Default
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
        // This WaveManager persists between scenes, so don't destroy it
        if (transform.parent == null) // Only if it's not a child of GameManager
        {
            DontDestroyOnLoad(gameObject);
        }
    }
    
    private void Start()
    {
        FindReferences();
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
        // Make sure we have current references
        FindReferences();
        
        waveActive = true;
        waveTimer = 0f;
        enemiesKilledThisWave = 0;
        
        // Calculate wave modifiers
        float healthMultiplier = Mathf.Pow(healthScalingMultiplier, currentWave - 1);
        float xpMultiplier = Mathf.Pow(xpScalingMultiplier, currentWave - 1);
        
        // Apply wave modifiers to enemy spawner
        if (enemySpawner != null)
        {
            enemySpawner.SetWaveModifiers(healthMultiplier, xpMultiplier);
            enemySpawner.StartSpawning();
        }
        else
        {
            Debug.LogWarning("WaveManager: EnemySpawner not found! Trying to find it...");
            FindReferences();
            if (enemySpawner != null)
            {
                enemySpawner.SetWaveModifiers(healthMultiplier, xpMultiplier);
                enemySpawner.StartSpawning();
            }
        }
        
        OnWaveStart?.Invoke(currentWave);
        
        string waveTypeStr = waveType == WaveType.TimeBased ? "Time-based" : "Kill-count-based";
        Debug.Log($"Wave {currentWave} started! ({waveTypeStr}) Health multiplier: {healthMultiplier:F2}, XP multiplier: {xpMultiplier:F2}");
    }
    
    private void Update()
    {
        if (!waveActive) return;
        
        switch (waveType)
        {
            case WaveType.TimeBased:
                waveTimer += Time.deltaTime;
                if (waveTimer >= waveDuration)
                {
                    CompleteWave();
                }
                break;
                
            case WaveType.KillCountBased:
                if (enemiesKilledThisWave >= enemiesPerWave)
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
    
    public void PrepareNextWave()
    {
        currentWave++;
        Debug.Log($"Prepared for wave {currentWave}");
    }
    
    public float GetWaveProgress()
    {
        if (!waveActive) return 0f;
        
        switch (waveType)
        {
            case WaveType.TimeBased:
                return waveTimer / waveDuration;
                
            case WaveType.KillCountBased:
                return (float)enemiesKilledThisWave / enemiesPerWave;
                
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
        return waveType;
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