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

            float randomScale = (float)GetRandomNumber(0.95, 1.5);
            prop.transform.localScale *= randomScale;
        }
    }

    public double GetRandomNumber(double minimum, double maximum)
    { 
        System.Random random = new System.Random();
        return random.NextDouble() * (maximum - minimum) + minimum;
    }
}