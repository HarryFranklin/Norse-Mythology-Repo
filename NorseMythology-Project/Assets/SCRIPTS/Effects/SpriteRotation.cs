using UnityEngine;

public class SpriteRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 720f; // degrees per second
    
    private void Update()
    {
        // Rotate around Z-axis
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}