using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    // Add logic for waves
    // Current wave number
    // Wave type
    // Wave timer
    // Wave max enemies (ref to enemy spawner)

    [Header("Player Reference")]
    [SerializeField] private GameObject playerObject;

    [Header("Scenes")]
    [SerializeField] private Scene gameOverScene;
    [SerializeField] private Scene winScene;

    [Header("UI References")]
    [SerializeField] private List<GameObject> UI_to_Hide_When_Dead;
    [SerializeField] private AbilityUIManager abilityUIManager;
    [SerializeField] private HealthXPUIManager healthXPUIManager;

    [Header("Player Stats")]
    [SerializeField] private PlayerStats basePlayerStats;
    [SerializeField] private PlayerStats currentPlayerStats;

    [Header("Enemy Spawner")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("Wave Manager")]
    [SerializeField] private WaveManager waveManager;

    void Start()
    {
        playerObject.GetComponent<PlayerController>().currentStats = basePlayerStats.CreateRuntimeCopy();
    }

    void Update()
    {
        
    }
}