using System.Collections.Generic;
using UnityEngine;

public class ChunkTrigger : MonoBehaviour
{
    [SerializeField] private MapController mapController;
    public GameObject targetMap;

    void Start()
    {
        if (mapController == null)
        {
            // Debug.Log("MapController is not assigned in the inspector. Finding it in the scene.");
            mapController = FindFirstObjectByType<MapController>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            mapController.currentChunk = targetMap;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (mapController.currentChunk == targetMap)
            {
                mapController.currentChunk = null;
            }   
        }
    }
}