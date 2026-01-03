using UnityEngine;

public class SpriteRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 720f; // degrees per second

    // Allow this script to ignore time freeze
    public bool useAbilityTimeScale = false;
    
    private void Update()
    {
        float deltaTime = Time.deltaTime;

        // Check if we should override the time scale
        if (useAbilityTimeScale && FreezeTimeAbility.IsTimeFrozen)
        {
            deltaTime = Time.unscaledDeltaTime * FreezeTimeAbility.GlobalRechargeMultiplier;
        }

        // Rotate around Z-axis using calculated delta
        transform.Rotate(0, 0, rotationSpeed * deltaTime);
    }
}