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
    [SerializeField] private string winSceneName = "Win";
    [SerializeField] private string levelUpSceneName = "LevelUp";
    
    // References that get found dynamically in each scene
    private GameObject playerObject;
    private Player player;
    private List<GameObject> UI_to_Hide_When_Dead;
    private AbilityUIManager abilityUIManager;
    private HealthXPUIManager healthXPUIManager;
    private EnemySpawner enemySpawner;
    private AbilityPooler abilityPooler;

    [Header("Player Stats")]
    [SerializeField] private PlayerStats basePlayerStats;
    [SerializeField] private PlayerStats currentPlayerStats; // This will persist between scenes
    [SerializeField] private int upgradePoints = 0; // Points available for upgrades
    
    [Header("Game State")]
    public int gameLevel = 1; // Overall game progression level
    private bool gameActive = true;
    private bool returningFromLevelUp = false;

    // Persistent player data that survives scene changes (for ability system)
    [System.Serializable]
    public class PlayerData
    {
        public int playerLevel = 1;
        public List<Ability> abilities = new List<Ability>();
        public Vector3 playerPosition;

        public PlayerData()
        {
            abilities = new List<Ability>();
        }
    }

    public PlayerData currentPlayerData = new PlayerData();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialise persistent player stats if not already done
            InitialisePersistentPlayerStats();
            
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitialisePersistentPlayerStats()
    {
        if (currentPlayerStats == null && basePlayerStats != null)
        {
            // Create a runtime copy that will persist
            currentPlayerStats = basePlayerStats.CreateRuntimeCopy();
            Debug.Log("Initialised persistent player stats");
        }
        
        // Initialize player data if needed
        if (currentPlayerData == null)
        {
            currentPlayerData = new PlayerData();
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-find references in the new scene
        StartCoroutine(FindSceneReferences());
    }
    
    private IEnumerator FindSceneReferences()
    {
        yield return null;

        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"Scene loaded: {currentSceneName}");

        if (WaveManager.Instance != null)
        {
            // Load wave number from PlayerPrefs
            if (returningFromLevelUp && PlayerPrefs.HasKey("CurrentWave"))
            {
                WaveManager.Instance.currentWave = PlayerPrefs.GetInt("CurrentWave", 1);
            }
        }

        if (currentSceneName == mainGameSceneName)
        {
            FindMainGameReferences();

            if (returningFromLevelUp)
            {
                returningFromLevelUp = false;

                // Start next wave after leveling up
                if (WaveManager.Instance != null)
                    WaveManager.Instance.StartWave();
            }
            else
            {
                // Fresh start
                if (WaveManager.Instance != null)
                    WaveManager.Instance.StartWave();
            }
        }
        else if (currentSceneName == levelUpSceneName)
        {
            // Find ability pooler in level up scene
            abilityPooler = FindFirstObjectByType<AbilityPooler>();
            
            // Ensure we have valid player data for ability selection
            if (currentPlayerData == null)
            {
                currentPlayerData = new PlayerData();
                Debug.LogWarning("No player data found, creating new PlayerData");
            }
        }
    }
    
    public void EndCurrentWave()
    {
        SavePlayerData();
        SavePlayerProgress();
        SceneManager.LoadScene(levelUpSceneName);
    }

    public void ReturnFromLevelUp()
    {
        gameLevel++;
        returningFromLevelUp = true;
        SceneManager.LoadScene(mainGameSceneName);
    }

    private void FindMainGameReferences()
    {
        // Find player
        player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            playerObject = player.gameObject;

            // Use the persistent stats instead of creating new ones
            if (currentPlayerStats != null)
            {
                player.currentStats = currentPlayerStats;
                // Set current health to max health if this is a fresh start or returning from level up
                if (returningFromLevelUp)
                {
                    player.currentHealth = currentPlayerStats.maxHealth; // Full heal on wave transition
                }
                else if (player.currentHealth <= 0)
                {
                    player.currentHealth = currentPlayerStats.maxHealth;
                }
            }
            else
            {
                // Fallback - create new stats from base
                if (basePlayerStats != null)
                {
                    currentPlayerStats = basePlayerStats.CreateRuntimeCopy();
                    player.currentStats = currentPlayerStats;
                    player.currentHealth = currentPlayerStats.maxHealth;
                }
            }
        }

        if (returningFromLevelUp)
        {
            LoadPlayerData();
        }

        // Find ability pooler
        abilityPooler = FindFirstObjectByType<AbilityPooler>();

        // Find UI managers
        healthXPUIManager = FindFirstObjectByType<HealthXPUIManager>();
        abilityUIManager = FindFirstObjectByType<AbilityUIManager>();

        // Find enemy spawner
        enemySpawner = FindFirstObjectByType<EnemySpawner>();

        // Update WaveManager references using singleton
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.gameManager = this;
            WaveManager.Instance.enemySpawner = enemySpawner;
        }

        FindUIElementsToHide();
    }
    
    private void FindUIElementsToHide()
    {
        UI_to_Hide_When_Dead = new List<GameObject>();
        GameObject[] uiElements = GameObject.FindGameObjectsWithTag("HideOnDeath");
        UI_to_Hide_When_Dead.AddRange(uiElements);
    }
    
    public void OnWaveCompleted()
    {
        if (WaveManager.Instance != null)
        {
            int currentWave = WaveManager.Instance.GetCurrentWave();
            Debug.Log($"Wave {currentWave} completed! Handling transition...");
        }
        else
        {
            Debug.LogWarning("WaveManager is null in OnWaveCompleted!");
        }

        // Check if all waves completed
        if (WaveManager.Instance.AreAllWavesCompleted())
        {
            Debug.Log("All waves completed! Loading Win scene...");
            SceneManager.LoadScene(winSceneName);
            return;
        }

        // Normal wave complete flow:
        if (player != null)
        {
            upgradePoints += player.ProcessPendingExperienceAndReturnLevelUps();
            Debug.Log($"Player earned {upgradePoints} total upgrade points");
        }

        SavePlayerProgress();
        SavePlayerData(); // Save ability data as well

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
        
        // Reset persistent stats for new game
        if (basePlayerStats != null)
        {
            currentPlayerStats = basePlayerStats.CreateRuntimeCopy();
            upgradePoints = 0; // Reset upgrade points on death
            gameLevel = 1; // Reset game level on death
            currentPlayerData = new PlayerData(); // Reset ability data on death
        }
        
        // Load game over scene
        SceneManager.LoadScene(gameOverSceneName);
    }

    public void ContinueToNextWave()
    {
        // This is called from the level up scene
        Debug.Log("ContinueToNextWave called");
        
        // Prepare next wave (increment wave number) BEFORE loading the scene
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.PrepareNextWave();
            // Update PlayerPrefs with new wave number
            PlayerPrefs.SetInt("CurrentWave", WaveManager.Instance.GetCurrentWave());
            PlayerPrefs.Save();
        }
        
        returningFromLevelUp = true;
        
        // Load back to the main game scene
        SceneManager.LoadScene(mainGameSceneName);
    }

    private void SavePlayerProgress()
    {
        // Save player stats to PlayerPrefs for persistence across sessions
        if (currentPlayerStats != null)
        {
            PlayerPrefs.SetInt("PlayerLevel", currentPlayerStats.level);
            PlayerPrefs.SetFloat("PlayerExperience", currentPlayerStats.experience);
            PlayerPrefs.SetFloat("PlayerExpToNext", currentPlayerStats.experienceToNextLevel);
            PlayerPrefs.SetFloat("PlayerMaxHealth", currentPlayerStats.maxHealth);
            PlayerPrefs.SetFloat("PlayerMoveSpeed", currentPlayerStats.moveSpeed);
            PlayerPrefs.SetFloat("PlayerHealthRegen", currentPlayerStats.healthRegen);
            PlayerPrefs.SetFloat("PlayerAttackDamage", currentPlayerStats.attackDamage);
            PlayerPrefs.SetFloat("PlayerAttackSpeed", currentPlayerStats.attackSpeed);
            PlayerPrefs.SetFloat("PlayerMeleeRange", currentPlayerStats.meleeRange);
            PlayerPrefs.SetFloat("PlayerProjectileSpeed", currentPlayerStats.projectileSpeed);
            PlayerPrefs.SetFloat("PlayerProjectileRange", currentPlayerStats.projectileRange);
            PlayerPrefs.SetFloat("PlayerAbilityCDR", currentPlayerStats.abilityCooldownReduction);
            PlayerPrefs.SetInt("UpgradePoints", upgradePoints);
            PlayerPrefs.Save();

            Debug.Log($"Saved player progress - Level: {currentPlayerStats.level}, XP: {currentPlayerStats.experience}, Upgrade Points: {upgradePoints}");
        }
    }
    
    public void LoadPlayerProgress()
    {
        // Load player stats from PlayerPrefs if they exist
        if (PlayerPrefs.HasKey("PlayerLevel"))
        {
            if (currentPlayerStats == null)
            {
                currentPlayerStats = ScriptableObject.CreateInstance<PlayerStats>();
            }
            
            currentPlayerStats.level = PlayerPrefs.GetInt("PlayerLevel", 1);
            currentPlayerStats.experience = PlayerPrefs.GetFloat("PlayerExperience", 0f);
            currentPlayerStats.experienceToNextLevel = PlayerPrefs.GetFloat("PlayerExpToNext", 100f);
            currentPlayerStats.maxHealth = PlayerPrefs.GetFloat("PlayerMaxHealth", 100f);
            currentPlayerStats.moveSpeed = PlayerPrefs.GetFloat("PlayerMoveSpeed", 5f);
            currentPlayerStats.healthRegen = PlayerPrefs.GetFloat("PlayerHealthRegen", 1f);
            currentPlayerStats.attackDamage = PlayerPrefs.GetFloat("PlayerAttackDamage", 10f);
            currentPlayerStats.attackSpeed = PlayerPrefs.GetFloat("PlayerAttackSpeed", 1f);
            currentPlayerStats.meleeRange = PlayerPrefs.GetFloat("PlayerMeleeRange", 2f);
            currentPlayerStats.projectileSpeed = PlayerPrefs.GetFloat("PlayerProjectileSpeed", 8f);
            currentPlayerStats.projectileRange = PlayerPrefs.GetFloat("PlayerProjectileRange", 10f);
            currentPlayerStats.abilityCooldownReduction = PlayerPrefs.GetFloat("PlayerAbilityCDR", 0f);
            upgradePoints = PlayerPrefs.GetInt("UpgradePoints", 0);
            
            Debug.Log($"Loaded player progress - Level: {currentPlayerStats.level}, XP: {currentPlayerStats.experience}, Upgrade Points: {upgradePoints}");
        }
    }

    // Save current player state (for ability system)
    void SavePlayerData()
    {
        if (player != null)
        {
            currentPlayerData.playerLevel = player.GetPlayerLevel();
            currentPlayerData.abilities = player.GetAbilities(); // Assuming you have a method like this
            currentPlayerData.playerPosition = player.transform.position;
            Debug.Log("Saved player data: Level " + currentPlayerData.playerLevel + ", Abilities: " + currentPlayerData.abilities.Count);
        }
    }

    // Load player data back into the player on scene reload
    void LoadPlayerData()
    {
        if (player != null && currentPlayerData != null)
        {
            player.SetPlayerLevel(currentPlayerData.playerLevel); // Assuming you have a setter
            player.SetAbilities(currentPlayerData.abilities);     // Assuming you have a setter
            player.transform.position = currentPlayerData.playerPosition;
            Debug.Log("Loaded player data: Level " + currentPlayerData.playerLevel + ", Abilities: " + currentPlayerData.abilities.Count);
        }
        else
        {
            Debug.LogWarning("LoadPlayerData failed: Missing player or data.");
        }
    }
    
    // Method to start a new game (resets stats)
    public void StartNewGame()
    {
        if (basePlayerStats != null)
        {
            currentPlayerStats = basePlayerStats.CreateRuntimeCopy();
        }

        upgradePoints = 0;

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.currentWave = 1;
        }

        // Clear saved progress
        PlayerPrefs.DeleteKey("PlayerLevel");
        PlayerPrefs.DeleteKey("CurrentWave");
        PlayerPrefs.DeleteKey("UpgradePoints");

        SceneManager.LoadScene(mainGameSceneName);
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // Getter methods for other scripts
    public WaveManager GetWaveManager() => WaveManager.Instance; // Use singleton instead
    public EnemySpawner GetEnemySpawner() => enemySpawner;
    public HealthXPUIManager GetHealthXPUIManager() => healthXPUIManager;
    public AbilityUIManager GetAbilityUIManager() => abilityUIManager;
    public GameObject GetPlayerObject() => playerObject;
    public PlayerStats GetCurrentPlayerStats() => currentPlayerStats;
    public int GetUpgradePoints() => upgradePoints;
    
    // Method to spend upgrade points
    public bool SpendUpgradePoint()
    {
        if (upgradePoints > 0)
        {
            upgradePoints--;
            return true;
        }
        return false;
    }
}