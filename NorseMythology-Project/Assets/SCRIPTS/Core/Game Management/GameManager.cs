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
    [SerializeField] private string mainMenuSceneName = "2_MainMenu";
    [SerializeField] private string characterSelectorSceneName = "3_CharacterSelector";
    [SerializeField] private string mainGameSceneName = "4_MainGame";
    [SerializeField] private string levelUpSceneName = "5_LevelUp";
    [SerializeField] private string gameOverSceneName = "9_GameOver";
    [SerializeField] private string winSceneName = "9_Win";

    [Header("UI Management")]
    [Tooltip("The Pause Menu Panel.")]
    [SerializeField] private GameObject pauseMenuPanel;
    private List<GameObject> gameplayUIElements = new List<GameObject>();
    private readonly string[] uiNamesToHide = new string[] 
    { 
        "Abilities Canvas", 
        "HealthXP Canvas", 
        "Popup Canvas" 
    };

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
    
    // Pause State
    private bool isPaused = false;
    private float previousTimeScale = 1f;

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

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == mainGameSceneName && gameActive)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isPaused) ResumeGame();
                else PauseGame();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        isPaused = false; 

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        
        // Ensure UI is visible when scene loads
        ToggleGameplayUI(true);

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

        if (returningFromLevelUp) returningFromLevelUp = false;

        gameActive = true;
        WaveManager.Instance?.StartWave();
    }

    public void PauseGame()
    {
        isPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        ToggleGameplayUI(false); 
    }
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = (previousTimeScale > 0) ? previousTimeScale : 1f;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        ToggleGameplayUI(true); // Show all gameplay canvases
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        currentPlayerData = new PlayerData(); 
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    private void ToggleGameplayUI(bool state)
    {
        if (gameplayUIElements == null) return;

        foreach (GameObject ui in gameplayUIElements)
        {
            if (ui != null) ui.SetActive(state);
        }
    }

    private void FindMainGameReferences()
    {
        player = FindFirstObjectByType<Player>();
        abilityUIManager = FindFirstObjectByType<AbilityUIManager>();
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
        
        // Find Pause Menu
        if (pauseMenuPanel == null) 
            pauseMenuPanel = GameObject.Find("PauseMenuPanel");

        if (pauseMenuPanel != null) 
            pauseMenuPanel.SetActive(false);

        gameplayUIElements.Clear();
        foreach (string uiName in uiNamesToHide)
        {
            GameObject foundObj = GameObject.Find(uiName);
            if (foundObj != null)
            {
                gameplayUIElements.Add(foundObj);
                foundObj.SetActive(true); 
            }
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.gameManager = this;
            WaveManager.Instance.enemySpawner = enemySpawner;
        }
    }
    
    public void SetSelectedClass(CharacterClass selectedClass)
    {
        if (selectedClass != null && selectedClass.startingStats != null)
        {
            basePlayerStats = selectedClass.startingStats;
            currentPlayerStats = basePlayerStats.CreateRuntimeCopy();

            currentPlayerStats.attackType = selectedClass.attackType;
            currentPlayerStats.meleeWeaponPrefab = selectedClass.meleeWeaponPrefab;
            currentPlayerStats.projectilePrefab = selectedClass.projectilePrefab;
        }
        else
        {
            if (basePlayerStats != null) currentPlayerStats = basePlayerStats.CreateRuntimeCopy();
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
        gameActive = false; 
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
        if (basePlayerStats != null) currentPlayerStats = basePlayerStats.CreateRuntimeCopy();
        
        upgradePoints = 0;
        gameLevel = 1;
        currentPlayerData = new PlayerData();

        if (WaveManager.Instance != null) WaveManager.Instance.currentWave = 1;
        
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

    public PlayerStats GetCurrentPlayerStats() => currentPlayerStats;
    public int GetUpgradePoints() => upgradePoints;
    public AbilityPooler GetAbilityPooler() => AbilityPooler.Instance;
    public bool IsGameActive() => gameActive;
    public bool IsPaused() => isPaused;

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