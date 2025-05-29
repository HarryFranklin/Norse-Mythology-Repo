using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 10f;
    private float damage;
    private Vector2 direction;

    public void Initialise(Vector2 shootDirection, float projectileDamage)
    {
        direction = shootDirection;
        damage = projectileDamage;
        Destroy(gameObject, 3f); // Destroy if it lives too long
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
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
