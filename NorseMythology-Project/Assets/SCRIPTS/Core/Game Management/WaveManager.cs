using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }
    
    [System.Serializable]
    public enum WaveType { TimeBased, KillCountBased, FixedEnemyCount }

    [System.Serializable]
    public class WaveItem
    {
        public WaveType waveType;
        public float criteria; 
    }

    [Header("Wave Configuration")]
    [SerializeField] private WaveItem[] waveItems; 
    
    [Header("Wave Settings")]
    public int currentWave = 1;
    public WaveType currentWaveType = WaveType.TimeBased; 
    public float waveDuration = 60f; 
    
    [Header("Wave Scaling")]
    public float healthScalingMultiplier = 1.05f;
    public float xpScalingMultiplier = 1.05f;

    // Removed 'waveFinishTimeScale' as we are no longer using it.

    [Header("References")]
    public GameManager gameManager;
    public EnemySpawner enemySpawner;
    private WaveAnnouncementUI waveUI;
    
    private float waveTimer;
    private bool waveActive = false;
    private int enemiesKilledThisWave = 0;
    
    public delegate void WaveEvent(int waveNumber);
    public static event WaveEvent OnWaveStart;
    public static event WaveEvent OnWaveComplete;
    
    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }
    
    private void Start()
    {
        if (Instance == this)
        {
            FindReferences();
            if (gameManager != null && gameManager.gameLevel == 1)
            {
                currentWave = 1;
                enemiesKilledThisWave = 0;
            }
        }
    }

    public void FindReferences()
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        if (enemySpawner == null) enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner != null) enemySpawner.waveManager = this;
        if (waveUI == null) waveUI = FindFirstObjectByType<WaveAnnouncementUI>();
    }
    
    public void StartWave() { StartCoroutine(StartWaveRoutine()); }

    private IEnumerator StartWaveRoutine()
    {
        FindReferences();

        if (waveItems == null || waveItems.Length < currentWave) yield break;

        WaveItem waveConfig = waveItems[currentWave - 1];
        currentWaveType = waveConfig.waveType;

        // 1. Show UI Announcement
        if (waveUI != null)
        {
            yield return StartCoroutine(waveUI.ShowWaveStart(currentWave, GetObjectiveString(waveConfig)));
        }
        else
        {
            yield return new WaitForSeconds(1f); 
        }

        // 2. Begin Wave Logic
        waveActive = true;
        waveTimer = 0f;
        enemiesKilledThisWave = 0;

        float healthMult = Mathf.Pow(healthScalingMultiplier, currentWave - 1);
        float xpMult = Mathf.Pow(xpScalingMultiplier, currentWave - 1);

        if (enemySpawner != null)
        {
            enemySpawner.SetWaveModifiers(healthMult, xpMult);
            enemySpawner.SetWaveNumber(currentWave);
            
            if (currentWaveType == WaveType.FixedEnemyCount)
                enemySpawner.StartFixedSpawning((int)waveConfig.criteria);
            else
                enemySpawner.StartSpawning();
        }

        OnWaveStart?.Invoke(currentWave);
        Debug.Log($"Wave {currentWave} Started!");
    }
    
    private void Update()
    {
        if (!waveActive) return;
        if (waveItems == null || waveItems.Length < currentWave) return;

        WaveItem waveConfig = waveItems[currentWave - 1];

        switch (waveConfig.waveType)
        {
            case WaveType.TimeBased:
                waveTimer += Time.deltaTime;
                if (waveTimer >= waveConfig.criteria) CompleteWave();
                break;

            case WaveType.KillCountBased:
                if (enemiesKilledThisWave >= waveConfig.criteria) CompleteWave();
                break;
                
            case WaveType.FixedEnemyCount:
                if (enemySpawner != null && enemySpawner.AreAllEnemiesDefeated()) CompleteWave();
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
        if (enemySpawner != null) enemySpawner.StopSpawning();
        StartCoroutine(CompleteWaveRoutine());
    }

    private IEnumerator CompleteWaveRoutine()
    {
        OnWaveComplete?.Invoke(currentWave);
        Debug.Log($"Wave {currentWave} completed!");

        Enemy[] activeEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.target = null;
                enemy.damage = 0f;

                Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero; 
            }
        }

        // --- SHOW UI ---
        if (waveUI != null)
        {
            yield return StartCoroutine(waveUI.ShowWaveCompleted());
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        // --- FINISH ---
        if (gameManager != null)
        {
            gameManager.OnWaveCompleted();
        }
    }
    
    public bool AreAllWavesCompleted() => currentWave > waveItems.Length;

    public void PrepareNextWave()
    {
        currentWave++;
        if (AreAllWavesCompleted())
        {
            waveActive = false;
            return;
        }
    }

    private string GetObjectiveString(WaveItem config)
    {
        switch (config.waveType)
        {
            case WaveType.TimeBased: return $"Survive for {config.criteria}s";
            case WaveType.KillCountBased: return $"Defeat {config.criteria} Enemies";
            case WaveType.FixedEnemyCount: return "Clear the Wave";
            default: return "Survive";
        }
    }

    // ========================================================================
    //                         GETTERS & HELPERS
    // ========================================================================

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
                return (float)enemiesKilledThisWave / waveConfig.criteria;
                
            case WaveType.FixedEnemyCount:
                if (enemySpawner != null && enemySpawner.AreAllEnemiesSpawned())
                {
                    int totalEnemies = (int)waveConfig.criteria;
                    int enemiesRemaining = enemySpawner.GetCurrentEnemyCount();
                    int enemiesKilled = totalEnemies - enemiesRemaining;
                    return (float)enemiesKilled / totalEnemies;
                }
                else
                {
                    return (float)enemiesKilledThisWave / waveConfig.criteria;
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

    public int GetCurrentWave() => currentWave;
    public bool IsWaveActive() => waveActive;
    public WaveType GetWaveType() => currentWaveType;
    public int GetEnemiesKilledThisWave() => enemiesKilledThisWave;
    public float GetWaveTimer() => waveTimer;
}