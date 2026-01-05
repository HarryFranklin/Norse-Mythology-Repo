using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;

    // --- SCREEN SHAKE VARIABLES ---
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;
    private float shakeDamping = 1.0f; // Controls how fast the shake settles

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        // Ensure offset is set if it hasn't been set in Inspector
        if (offset == Vector3.zero) 
            offset = new Vector3(0, 0, -10);
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 finalPosition = target.position + offset;

        if (shakeDuration > 0)
        {
            // Create a random offset
            Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeOffset.z = 0; // Keep Z locked so we don't clip through the background
            
            finalPosition += shakeOffset;

            // Reduce shake over time
            shakeDuration -= Time.deltaTime * shakeDamping;
        }
        else
        {
            shakeDuration = 0f;
        }

        // Apply
        transform.position = finalPosition;
    }
    
    // Called by HammerSlamAbility
    public void TriggerShake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }
}