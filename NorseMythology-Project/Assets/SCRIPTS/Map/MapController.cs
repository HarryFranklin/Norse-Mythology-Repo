using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public List<GameObject> terrainChunks;
    public GameObject player;
    public float checkerRadius;
    Vector3 noTerrainPosition;
    public LayerMask terrainMask;
    public GameObject currentChunk;
    private PlayerMovement playerMovement;

    [Header("Optimisation")]
    public List<GameObject> spawnedChunks;
    private GameObject latsetChunk;
    public float maxOpDist;
    public float optimiserCooldownDuration = 0.5f;
    public float checkerCooldownDuration = 0.1f;

    void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            Debug.Log("Player not assigned, using GameObject with tag 'Player'");
        }

        playerMovement = player.GetComponent<PlayerMovement>();
        
        // Start coroutines
        StartCoroutine(ChunkCheckerCoroutine());
        StartCoroutine(ChunkOptimiserCoroutine());
    }

    IEnumerator ChunkCheckerCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkerCooldownDuration);
            
            if (!currentChunk)
            {
                continue;
            }

            CheckAndSpawnChunk();
        }
    }

    IEnumerator ChunkOptimiserCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(optimiserCooldownDuration);
            
            OptimiseChunks();
        }
    }

    void CheckAndSpawnChunk()
    {
        Vector2 moveDir = playerMovement.moveDir;
        List<string> directionsToCheck = GetDirectionsToCheck(moveDir);
        
        foreach (string directionName in directionsToCheck)
        {
            Transform directionTransform = currentChunk.transform.Find(directionName);
            if (directionTransform && !Physics2D.OverlapCircle(directionTransform.position, checkerRadius, terrainMask))
            {
                noTerrainPosition = directionTransform.position;
                SpawnChunk();
            }
        }
    }

    List<string> GetDirectionsToCheck(Vector2 moveDir)
    {
        List<string> directions = new List<string>();
        
        // Cardinal directions
        if (moveDir.x > 0) directions.Add("Right");
        if (moveDir.x < 0) directions.Add("Left");
        if (moveDir.y > 0) directions.Add("Up");
        if (moveDir.y < 0) directions.Add("Down");
        
        // Diagonal directions (in addition to cardinal)
        if (moveDir.x > 0 && moveDir.y > 0) directions.Add("UpRight");
        if (moveDir.x < 0 && moveDir.y < 0) directions.Add("DownLeft");
        if (moveDir.x > 0 && moveDir.y < 0) directions.Add("DownRight");
        if (moveDir.x < 0 && moveDir.y > 0) directions.Add("UpLeft");
        
        return directions;
    }

    void SpawnChunk()
    {
        int rand = Random.Range(0, terrainChunks.Count);
        latsetChunk = Instantiate(terrainChunks[rand], noTerrainPosition, Quaternion.identity);
        spawnedChunks.Add(latsetChunk);
    }

    void OptimiseChunks()
    {
        foreach (GameObject chunk in spawnedChunks)
        {
            float sqrDistance = (player.transform.position - chunk.transform.position).sqrMagnitude;
            float maxOpDistSqr = maxOpDist * maxOpDist;
            
            chunk.SetActive(sqrDistance <= maxOpDistSqr);
        }
    }
}