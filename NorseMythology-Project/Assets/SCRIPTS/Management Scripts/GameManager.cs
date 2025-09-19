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
    [SerializeField] private string characterSelectorSceneName = "CharacterSelector";
    [SerializeField] private string mainGameSceneName = "MainGame";
    [SerializeField] private string gameOverSceneName = "GameOver";
    [SerializeField] private string winSceneName = "Win";
    [SerializeField] private string levelUpSceneName = "LevelUp";

    private Player player;
    private AbilityUIManager abilityUIManager;
    private EnemySpawner enemySpawner;

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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainGameSceneName)
        {
            StartCoroutine(SetupMainGameScene());
        }
    }

    private IEnumerator SetupMainGameScene()
    {
        yield return null; 

        FindMainGameReferences();
        
        if (player != null)
        {
            player.currentStats = currentPlayerStats;
            
            if (currentPlayerData.abilities != null && currentPlayerData.abilities.Count > 0)
            {
                LoadPlayerData();
            }
            else
            {
                Debug.Log("New Game: Saving default abilities from AbilityManager into persistent data.");
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
    
    public void SetSelectedClass(CharacterClass selectedClass)
    {
        if (selectedClass != null && selectedClass.startingStats != null)
        {
            basePlayerStats = selectedClass.startingStats;
            currentPlayerStats = basePlayerStats.CreateRuntimeCopy();

            // Explicitly copy the attack type and prefabs from the CharacterClass
            currentPlayerStats.attackType = selectedClass.attackType;
            currentPlayerStats.meleeWeaponPrefab = selectedClass.meleeWeaponPrefab;
            currentPlayerStats.projectilePrefab = selectedClass.projectilePrefab;

            Debug.Log($"Selected class '{selectedClass.className}' and applied starting stats.");
        }
        else
        {
            Debug.LogWarning("Selected class or its stats were null. Using default stats.");
            if (basePlayerStats != null)
            {
                currentPlayerStats = basePlayerStats.CreateRuntimeCopy();
            }
        }
    }
    
    private void FindMainGameReferences()
    {
        player = FindFirstObjectByType<Player>();
        abilityUIManager = FindFirstObjectByType<AbilityUIManager>();
        enemySpawner = FindFirstObjectByType<EnemySpawner>();

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.gameManager = this;
            WaveManager.Instance.enemySpawner = enemySpawner;
        }
    }
    
    private void InitialisePersistentPlayerStats()
    {
        if (currentPlayerStats == null && basePlayerStats != null)
        {
            currentPlayerStats = basePlayerStats.CreateRuntimeCopy();
        }
    }

    public void OnWaveCompleted()
    {
        gameActive = false; // Pause player input etc.
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
        gameActive = false;
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
        }
    }

    void LoadPlayerData()
    {
        if (player == null) return;

        player.SetPlayerLevel(currentPlayerData.playerLevel);
        player.transform.position = currentPlayerData.playerPosition;
        player.SetAbilities(currentPlayerData.abilities);
        
        abilityUIManager?.RefreshAllSlots();
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
        
        SceneManager.LoadScene(characterSelectorSceneName);
    }
    
    public void StartGameFromSelector()
    {
        SceneManager.LoadScene(mainGameSceneName);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // --- Getters for other scripts ---
    public PlayerStats GetCurrentPlayerStats() => currentPlayerStats;
    public int GetUpgradePoints() => upgradePoints;
    public AbilityPooler GetAbilityPooler() => AbilityPooler.Instance;

    public bool IsGameActive() => gameActive;

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