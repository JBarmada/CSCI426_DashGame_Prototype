using UnityEngine;
using Cainos.PixelArtTopDown_Basic;

/// <summary>
/// Attach this to objects that should damage/punish the player when dashed into.
/// When hit, triggers screen shake, freezes player, flashes them red, then spawns explosion.
/// </summary>
public class DamagingProp : MonoBehaviour
{
    [Header("Damage Effect Settings")]
    [Tooltip("Prefab to spawn when the damage sequence completes (e.g., Explosion_2_Skull_Red)")]
    public GameObject damageExplosionEffect;

    [Header("Screen Shake")]
    [Tooltip("How long the screen shakes")]
    public float shakeDuration = 1.0f;
    
    [Tooltip("Intensity of the screen shake")]
    public float shakeIntensity = 0.15f;

    [Header("Player Freeze & Flash")]
    [Tooltip("Total time the player is frozen")]
    public float freezeDuration = 1.2f;
    
    [Tooltip("How long it takes for the red to fully fade in")]
    public float flashDuration = 1.0f;

    private bool hasTriggered = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we were hit by the player while they're dashing
        TopDownCharacterController player = collision.gameObject.GetComponent<TopDownCharacterController>();
        
        if (player != null && player.IsDashing && !hasTriggered)
        {
            hasTriggered = true;
            TriggerDamage(player);
        }
    }

    private void TriggerDamage(TopDownCharacterController player)
    {
        // Start screen shake
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.Shake(shakeDuration, shakeIntensity);
        }

        // Freeze player and start red flash, spawn explosion when flash completes
        player.TakeDamage(freezeDuration, flashDuration, () => 
        {
            // Spawn explosion effect on the player
            if (damageExplosionEffect != null)
            {
                GameObject explosion = Instantiate(damageExplosionEffect, player.transform.position, Quaternion.identity);
                
                // Force the explosion to render above everything
                // Get player's sorting layer to match it, then use high order
                SpriteRenderer playerSprite = player.GetComponentInChildren<SpriteRenderer>();
                string sortingLayerName = playerSprite != null ? playerSprite.sortingLayerName : "Default";
                
                foreach (var renderer in explosion.GetComponentsInChildren<Renderer>())
                {
                    renderer.sortingLayerName = sortingLayerName;
                    renderer.sortingOrder = 1000;
                }
            }
        });

        // Allow this prop to damage again after the sequence completes
        Invoke(nameof(ResetTrigger), freezeDuration + 0.1f);
    }

    private void ResetTrigger()
    {
        hasTriggered = false;
    }
}
