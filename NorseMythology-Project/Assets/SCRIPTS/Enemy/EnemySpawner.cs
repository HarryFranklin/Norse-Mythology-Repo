using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject enemyPrefab;
    public float spawnRate = 2f;
    public float spawnRadius = 15f;
    public int maxEnemies = 20;
    public Transform player;
    
    private int currentEnemyCount = 0;
    
    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>().transform;
            
        StartCoroutine(SpawnEnemies());
    }
    
    private System.Collections.IEnumerator SpawnEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f / spawnRate);
            
            if (currentEnemyCount < maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }
    
    private void SpawnEnemy()
    {
        Vector2 spawnPos = GetRandomSpawnPosition();
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        
        // Subscribe to enemy death to decrease count
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        currentEnemyCount++;
        
        // Decrease count when enemy is destroyed
        StartCoroutine(WaitForEnemyDestroy(enemy));
    }
    
    private System.Collections.IEnumerator WaitForEnemyDestroy(GameObject enemy)
    {
        while (enemy != null)
        {
            yield return null;
        }
        currentEnemyCount--;
    }
    
    private Vector2 GetRandomSpawnPosition()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        return (Vector2)player.position + randomDirection * spawnRadius;
    }
}