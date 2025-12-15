using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 10f;
    private float damage;
    private Vector2 direction;

    public void Initialise(Vector2 shootDirection, float projectileDamage)
    {
        direction = shootDirection.normalized; // Normalize just to be safe
        damage = projectileDamage;
        
        // Calculate the angle in degrees
        // Atan2 returns 0 for (1,0) [Right], and -90 for (0,-1) [Down], which matches arrow sprite (pointing right).
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Apply the rotation around the Z axis
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        Destroy(gameObject, 3f); // Destroy if it lives too long
    }

    private void Update()
    {
        // Now that we rotated the object, we can translate in "local right" or keep using world direction.
        // Using world direction is safer if the rotation code changes later.
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}