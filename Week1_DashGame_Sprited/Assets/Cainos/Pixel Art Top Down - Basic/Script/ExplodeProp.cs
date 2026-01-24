using UnityEngine;
using Cainos.PixelArtTopDown_Basic;

public class Explodable : MonoBehaviour
{
    public float explosionForce = 8f;
    public float destroyDelay = 0.05f;
    public GameObject explosionEffect;

    private Rigidbody2D rb;
    private bool exploded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Collided with: " + collision.gameObject.name);

        if (exploded) return;

        // Check if the thing that hit us is the player
        TopDownCharacterController player =
            collision.gameObject.GetComponent<TopDownCharacterController>();

        if (player == null) return;

        // Only explode if player is dashing
        if (!player.IsDashing) return;

        Explode(collision.contacts[0].point);
    }

    void Explode(Vector2 hitPoint)
    {
        exploded = true;

        // Push away from impact
        if (rb != null)
        {
            Vector2 dir = (Vector2)transform.position - hitPoint;
            rb.AddForce(dir.normalized * explosionForce, ForceMode2D.Impulse);
        }

        // Visual effect
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        // Prevent multiple triggers
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        Destroy(gameObject, destroyDelay);
    }
}
