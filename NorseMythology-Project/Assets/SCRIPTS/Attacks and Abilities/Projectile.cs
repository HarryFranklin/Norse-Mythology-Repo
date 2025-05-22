using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float maxRange;
    private float damage;
    private bool isReturning;
    private Transform player;
    
    private Vector2 startPosition;
    private float traveledDistance;
    private bool returningToPlayer;
    
    public void Initialise(Vector2 dir, float spd, float range, float dmg, bool returning, Transform playerTransform)
    {
        direction = dir;
        speed = spd;
        maxRange = range;
        damage = dmg;
        isReturning = returning;
        player = playerTransform;
        startPosition = transform.position;
        
        // Rotate projectile to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
    
    private void Update()
    {
        if (!returningToPlayer)
        {
            // Move forward
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
            traveledDistance = Vector2.Distance(startPosition, transform.position);
            
            // Check if should return or be destroyed
            if (traveledDistance >= maxRange)
            {
                if (isReturning && player != null)
                {
                    returningToPlayer = true;
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
        else
        {
            // Return to player
            Vector2 returnDirection = (player.position - transform.position).normalized;
            transform.Translate(returnDirection * speed * Time.deltaTime, Space.World);
            
            // Check if reached player
            if (Vector2.Distance(transform.position, player.position) < 0.5f)
            {
                Destroy(gameObject);
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!returningToPlayer && other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                
                if (!isReturning)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}