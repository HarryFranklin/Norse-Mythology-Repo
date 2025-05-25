using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string mainGameSceneName = "MainGame";
    [SerializeField] private string gameOverSceneName = "GameOver";
    [SerializeField] private string winSceneName = "Win"; // Not sure if needed
    [SerializeField] private string levelUpSceneName = "LevelUp"; // New scene for between waves

    [Header("Persistent Managers")]
    [SerializeField] private WaveManager waveManager;
    
    // References that get found dynamically in each scene
    private GameObject playerObject;
    private List<GameObject> UI_to_Hide_When_Dead;
    private AbilityUIManager abilityUIManager;
    private HealthXPUIManager healthXPUIManager;
    private EnemySpawner enemySpawner;

    [Header("Player Stats")]
    [SerializeField] private PlayerStats basePlayerStats;
    [SerializeField] private PlayerStats currentPlayerStats;
    
    // Game state
    private bool gameActive = true;
    private bool returningFromLevelUp = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Make WaveManager persist too
            if (waveManager != null)
            {
                DontDestroyOnLoad(waveManager.gameObject);
            }
            
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-find references in the new scene
        StartCoroutine(FindSceneReferences());
    }
    
    private IEnumerator FindSceneReferences()
    {
        // Wait one frame to ensure all objects are initialized
        yield return null;
        
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        if (currentSceneName == mainGameSceneName)
        {
            // Find main game references
            FindMainGameReferences();
            
            if (returningFromLevelUp)
            {
                // We're returning from level up, start next wave
                returningFromLevelUp = false;
                if (waveManager != null)
                {
                    waveManager.StartWave();
                }
            }
            else
            {
                // Fresh start, begin first wave
                if (waveManager != null)
                {
                    waveManager.StartWave();
                }
            }
        }
        else if (currentSceneName == levelUpSceneName)
        {
            // No specific references needed for level up scene
            // LevelUpManager handles its own UI
        }
    }
    
    private void FindMainGameReferences()
    {
        // Find player
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerObject = playerController.gameObject;
            
            // Set up player stats
            if (playerController.currentStats == null && basePlayerStats != null)
            {
                playerController.currentStats = basePlayerStats.CreateRuntimeCopy();
            }
            
            // Load saved progress if returning from level up
            LoadPlayerProgress();
        }
        
        // Find UI managers
        healthXPUIManager = FindFirstObjectByType<HealthXPUIManager>();
        abilityUIManager = FindFirstObjectByType<AbilityUIManager>();
        
        // Find enemy spawner
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
        
        // Update WaveManager references
        if (waveManager != null)
        {
            waveManager.gameManager = this;
            waveManager.enemySpawner = enemySpawner;
        }
        
        FindUIElementsToHide();
    }
    
    private void FindUIElementsToHide()
    {
        UI_to_Hide_When_Dead = new List<GameObject>();
        GameObject[] uiElements = GameObject.FindGameObjectsWithTag("HideOnDeath");
        UI_to_Hide_When_Dead.AddRange(uiElements);
    }

    void Start()
    {

    }

    void Update()
    {

    }
    
    public void OnWaveCompleted()
    {
        Debug.Log($"Wave {waveManager.GetCurrentWave()} completed! Loading level up scene...");
        
        // Save current player stats and wave progress before transitioning
        SavePlayerProgress();
        
        // Load the level up scene
        SceneManager.LoadScene(levelUpSceneName);
    }
    
    public void OnPlayerDied()
    {
        gameActive = false;
        
        // Hide UI elements
        if (UI_to_Hide_When_Dead != null)
        {
            foreach (GameObject uiElement in UI_to_Hide_When_Dead)
            {
                if (uiElement != null)
                    uiElement.SetActive(false);
            }
        }
        
        // Load game over scene
        SceneManager.LoadScene(gameOverSceneName);
    }
    
    public void ContinueToNextWave()
    {
        // This is called from the level up scene
        Debug.Log("ContinueToNextWave called");
        
        // Prepare next wave (increment wave number) BEFORE loading the scene
        if (waveManager != null)
        {
            waveManager.PrepareNextWave();
            // Update PlayerPrefs with new wave number
            PlayerPrefs.SetInt("CurrentWave", waveManager.GetCurrentWave());
            PlayerPrefs.Save();
        }
        
        returningFromLevelUp = true;
        
        // Load back to the main game scene
        SceneManager.LoadScene(mainGameSceneName);
    }

    private void SavePlayerProgress()
    {

    }
    
    public void LoadPlayerProgress()
    {

    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // Getter methods for other scripts
    public WaveManager GetWaveManager() => waveManager;
    public EnemySpawner GetEnemySpawner() => enemySpawner;
    public HealthXPUIManager GetHealthXPUIManager() => healthXPUIManager;
    public AbilityUIManager GetAbilityUIManager() => abilityUIManager;
    public GameObject GetPlayerObject() => playerObject;
}