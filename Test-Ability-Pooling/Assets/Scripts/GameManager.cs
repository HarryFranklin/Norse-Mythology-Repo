using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public int gameLevel = 1;
    public Player player;
    public AbilityPooler abilityPooler;

    // Persistent player data that survives scene changes
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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Find components if not assigned
        if (player == null)
            player = FindObjectOfType<Player>();
        if (abilityPooler == null)
            abilityPooler = FindObjectOfType<AbilityPooler>();

        // Load player data if we're in the game scene
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameScene")
        {
            LoadPlayerData();
        }
    }

    // Called when going to ability selection (save data first)
    public void GoToAbilitySelection()
    {
        SavePlayerData();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }

    // Called when ending the current wave - save player data and go to ability selection
    public void EndCurrentWave()
    {
        SavePlayerData();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }

    // Called when starting next level - increment game level and go to game scene
    public void StartNextLevel()
    {
        gameLevel++;
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    // Save current player state
    void SavePlayerData()
    {
        if (player != null)
        {
            currentPlayerData.playerLevel = player.playerLevel;
            currentPlayerData.abilities = new List<Ability>(player.abilities);
            currentPlayerData.playerPosition = player.transform.position;
            
            Debug.Log($"Saved player data: Level {currentPlayerData.playerLevel}, Abilities: {currentPlayerData.abilities.Count}");
        }
        else
        {
            Debug.LogWarning("Player not found when trying to save data!");
        }
    }

    // Load player state when entering game scene
    void LoadPlayerData()
    {
        if (player != null && currentPlayerData != null)
        {
            player.playerLevel = currentPlayerData.playerLevel;
            player.abilities = new List<Ability>(currentPlayerData.abilities);
            player.transform.position = currentPlayerData.playerPosition;
            
            Debug.Log($"Loaded player data: Level {player.playerLevel}, Abilities: {player.abilities.Count}");
        }
        else
        {
            Debug.LogWarning("Player or currentPlayerData is null when trying to load!");
        }
    }

    // Get current player data for menu scene - with safety check
    public PlayerData GetPlayerData()
    {
        // If we're in the game scene and have a player, update the data first
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameScene" && player != null)
        {
            SavePlayerData();
        }
        
        // Ensure we always return valid data
        if (currentPlayerData == null)
        {
            currentPlayerData = new PlayerData();
        }
        
        return currentPlayerData;
    }

    // Update player data from menu scene
    public void UpdatePlayerData(PlayerData newData)
    {
        currentPlayerData = newData;
        Debug.Log($"Updated player data: Level {currentPlayerData.playerLevel}, Abilities: {currentPlayerData.abilities.Count}");
    }

    // Called when scene changes
    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");
        
        // Re-find components after scene load
        if (scene.name == "GameScene")
        {
            player = FindObjectOfType<Player>();
            abilityPooler = FindObjectOfType<AbilityPooler>();
            LoadPlayerData();
        }
        else if (scene.name == "MenuScene")
        {
            abilityPooler = FindObjectOfType<AbilityPooler>();
            // Ensure we have valid player data for ability selection
            if (currentPlayerData == null)
            {
                currentPlayerData = new PlayerData();
                Debug.LogWarning("No player data found, creating new PlayerData");
            }
        }
    }
}