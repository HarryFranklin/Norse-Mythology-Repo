using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// A class to hold a prefab and a boolean to enable/disable it.
[System.Serializable]
public class SelectablePrefab
{
    public GameObject prefab;
    public bool enabled = true;

    // Constructor to allow creating one from a GameObject
    public SelectablePrefab(GameObject prefab)
    {
        this.prefab = prefab;
        this.enabled = true;
    }
}

// A class to define a category of props, e.g. "Trees" or "Rocks".
[System.Serializable]
public class PropCategory
{
    public string name;
    public bool enabled = true;

    [Tooltip("The relative chance for this category to be chosen for spawning.")]
    [Range(0f, 1f)]
    public float spawnProportion = 1f;

    public bool allPrefabsEnabled = true;
    public List<SelectablePrefab> prefabs = new List<SelectablePrefab>();
}

public class PropRandomiser : MonoBehaviour
{
    [Tooltip("A list of empty GameObjects where props can be spawned.")]
    public List<GameObject> propSpawnPoints;

    [Tooltip("If true, the proportion sliders will automatically adjust to always sum to 1.")]
    public bool autoNormaliseProportions = true;

    [Tooltip("Define your categories of props here.")]
    public List<PropCategory> propCategories = new List<PropCategory>();

    // We store a separate System.Random instance to avoid issues with Unity's static Random class.
    private System.Random random = new System.Random();

    void Start()
    {
        SpawnProps();
    }

    void SpawnProps()
    {
        // 1. Create a list of categories that are actually enabled
        List<PropCategory> activeCategories = propCategories.Where(cat => cat.enabled && cat.prefabs.Any(p => p.enabled && p.prefab != null)).ToList();

        if (activeCategories.Count == 0)
        {
            Debug.LogWarning("PropRandomiser: No active prop categories with enabled prefabs to spawn.", this);
            return;
        }
        
        // Normalise proportions for spawning logic regardless of the auto-normalise setting in the editor.
        float totalProportionSum = activeCategories.Sum(cat => cat.spawnProportion);
        if (totalProportionSum <= 0)
        {
             Debug.LogWarning("PropRandomiser: The sum of spawn proportions for active categories is zero. No props will be spawned.", this);
             return;
        }

        // 2. Create a weighted list of all possible prefabs
        Dictionary<GameObject, float> weightedPrefabList = new Dictionary<GameObject, float>();

        foreach (var category in activeCategories)
        {
            var enabledPrefabs = category.prefabs.Where(p => p.enabled && p.prefab != null).ToList();
            if (enabledPrefabs.Count > 0)
            {
                // The weight for each individual prefab is the category's normalised proportion divided by the number of prefabs in it.
                float normalisedProportion = category.spawnProportion / totalProportionSum;
                float weightPerPrefab = normalisedProportion / enabledPrefabs.Count;

                foreach (var selectablePrefab in enabledPrefabs)
                {
                    if (!weightedPrefabList.ContainsKey(selectablePrefab.prefab))
                    {
                        weightedPrefabList.Add(selectablePrefab.prefab, weightPerPrefab);
                    }
                }
            }
        }

        if (weightedPrefabList.Count == 0)
        {
            Debug.LogWarning("PropRandomiser: No enabled prefabs found in any active category.", this);
            return;
        }

        // 3. Spawn a prop for each spawn point
        foreach (GameObject spawnPoint in propSpawnPoints)
        {
            if (spawnPoint == null) continue;

            // Select a random prefab based on the calculated weights
            GameObject propPrefab = GetRandomWeightedPrefab(weightedPrefabList);
            if (propPrefab == null) continue;

            // Instantiate and position the prop
            GameObject prop = Instantiate(propPrefab, spawnPoint.transform.position, Quaternion.identity);
            prop.transform.SetParent(spawnPoint.transform);

            // Apply a random scale
            float randomScale = (float)GetRandomNumber(0.95, 1.5);
            prop.transform.localScale *= randomScale;
        }
    }

    // This function selects a prefab from the dictionary based on its weight.
    private GameObject GetRandomWeightedPrefab(Dictionary<GameObject, float> weightedList)
    {
        // The sum of weights should already be 1 (or very close) due to normalisation.
        float randomValue = (float)random.NextDouble();
        float cumulativeWeight = 0f;

        foreach (var item in weightedList)
        {
            cumulativeWeight += item.Value;
            if (randomValue < cumulativeWeight)
            {
                return item.Key;
            }
        }
        return weightedList.LastOrDefault().Key; // Fallback to the last item
    }

    // Using System.Random for generating the random number.
    public double GetRandomNumber(double minimum, double maximum)
    {
        return random.NextDouble() * (maximum - minimum) + minimum;
    }
}