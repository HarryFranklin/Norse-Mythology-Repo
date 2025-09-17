using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerAbilityState
    {
        public Ability ability; 
        public int level;       

        public PlayerAbilityState(Ability ab, int lvl)
        {
            ability = ab;
            level = lvl;
        }
    }

    public static GameManager Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string mainGameSceneName = "MainGame";
    [SerializeField] private string gameOverSceneName = "GameOver";
    [SerializeField] private string winSceneName = "Win";
    [SerializeField] private string levelUpSceneName = "LevelUp";

    private Player player;
    private AbilityUIManager abilityUIManager;
    private AbilityPooler abilityPooler;

    [Header("Player Stats")]
    [SerializeField] private PlayerStats basePlayerStats;
    [SerializeField] private PlayerStats currentPlayerStats;

    [Header("Game State")]
    public int gameLevel = 1;
    private bool gameActive = true;
    private bool returningFromLevelUp = false;
    [SerializeField] private int upgradePoints = 0;

    [System.Serializable]
    public class PlayerData
    {
        public int playerLevel = 1;
        public List<PlayerAbilityState> abilities = new List<PlayerAbilityState>();
        public Vector3 playerPosition;
    }

    public PlayerData currentPlayerData = new PlayerData();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
            currentPlayerStats = basePlayerStats.CreateRuntimeCopy();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainGameSceneName)
        {
            StartCoroutine(SetupMainGameScene());
        }
        else if (scene.name == levelUpSceneName)
        {
            abilityPooler = FindFirstObjectByType<AbilityPooler>();
        }
    }

    private IEnumerator SetupMainGameScene()
    {
        yield return null; 

        FindMainGameReferences();
        
        if (player != null)
        {
            player.currentStats = currentPlayerStats;
            
            // --- FIX: This is the core logic change ---
            // If we have saved abilities, load them.
            if (currentPlayerData.abilities != null && currentPlayerData.abilities.Count > 0)
            {
                LoadPlayerData();
            }
            else // Otherwise, this must be a new game.
            {
                // Save the default abilities from the AbilityManager into our persistent data.
                Debug.Log("No abilities found in PlayerData, saving defaults from AbilityManager.");
                SavePlayerData();
            }
        }

        if (returningFromLevelUp)
        {
            returningFromLevelUp = false;
        }

        gameActive = true;
        WaveManager.Instance?.StartWave();
    }


    private void FindMainGameReferences()
    {
        player = FindFirstObjectByType<Player>();
        abilityPooler = FindFirstObjectByType<AbilityPooler>();
        abilityUIManager = FindFirstObjectByType<AbilityUIManager>();
        
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.gameManager = this;
            WaveManager.Instance.enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }
    }

    public void OnWaveCompleted()
    {
        if (WaveManager.Instance.AreAllWavesCompleted())
        {
            SceneManager.LoadScene(winSceneName);
            return;
        }

        if (player != null)
        {
            upgradePoints += player.ProcessPendingExperienceAndReturnLevelUps();
        }
        
        SavePlayerData(); 
        SceneManager.LoadScene(levelUpSceneName);
    }

    public void OnPlayerDied()
    {
        if (!gameActive) return;
        gameActive = false;

        if (basePlayerStats != null)
        {
            currentPlayerStats = basePlayerStats.CreateRuntimeCopy();
            upgradePoints = 0;
            gameLevel = 1;
            currentPlayerData = new PlayerData();
        }
        SceneManager.LoadScene(gameOverSceneName);
    }
    
    public void ContinueToNextWave()
    {
        gameLevel++;
        WaveManager.Instance?.PrepareNextWave();
        returningFromLevelUp = true;
        SceneManager.LoadScene(mainGameSceneName);
    }

    void SavePlayerData()
    {
        if (player != null)
        {
            currentPlayerData.playerLevel = player.GetPlayerLevel();
            currentPlayerData.abilities = player.GetAbilities();
            currentPlayerData.playerPosition = player.transform.position;
            Debug.Log($"Player data saved. Abilities count: {currentPlayerData.abilities.Count}");
        }
    }

    void LoadPlayerData()
    {
        if (player == null) return;

        player.SetPlayerLevel(currentPlayerData.playerLevel);
        player.transform.position = currentPlayerData.playerPosition;
        player.SetAbilities(currentPlayerData.abilities);
        
        abilityUIManager?.RefreshAllSlots();
        Debug.Log($"Player data loaded. Abilities count: {currentPlayerData.abilities.Count}");
    }


    public void StartNewGame()
    {
        if (basePlayerStats != null)
        {
            currentPlayerStats = basePlayerStats.CreateRuntimeCopy();
        }
        upgradePoints = 0;
        gameLevel = 1;
        currentPlayerData = new PlayerData();

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.currentWave = 1;
        }

        SceneManager.LoadScene(mainGameSceneName);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public PlayerStats GetCurrentPlayerStats() => currentPlayerStats;
    public int GetUpgradePoints() => upgradePoints;
    public bool IsGameActive() => gameActive;
    public AbilityPooler GetAbilityPooler() => abilityPooler;
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