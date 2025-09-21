using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class PropRandomiser : MonoBehaviour
{
    public List<GameObject> propSpawnPoints;
    public List<GameObject> propPrefabs;

    void Start()
    {
        SpawnProps();
    }

    void SpawnProps()
    {
        foreach (GameObject spawnPoint in propSpawnPoints)
        {
            int randomIndex = Random.Range(0, propPrefabs.Count);
            GameObject propPrefab = propPrefabs[randomIndex];
            GameObject prop = Instantiate(propPrefab, spawnPoint.transform.position, Quaternion.identity);
            prop.transform.SetParent(spawnPoint.transform);
        }
    }
}