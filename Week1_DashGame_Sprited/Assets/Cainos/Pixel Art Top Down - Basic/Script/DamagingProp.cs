using UnityEngine;
using Cainos.PixelArtTopDown_Basic;

/// <summary>
/// Attach this to objects that should damage/punish the player when dashed into.
/// Sequence:
/// 1. Initial freeze (animation stops) + screen shake
/// 2. Character shakes while fading to red
/// 3. Explosion spawns on player
/// </summary>
public class DamagingProp : MonoBehaviour
{
    [Header("Damage Effect Settings")]
    [Tooltip("Prefab to spawn when the damage sequence completes (e.g., Explosion_2_Skull_Red)")]
    public GameObject damageExplosionEffect;
    [Tooltip("Sound to play when the player hits this prop")]
    public AudioClip hitSound;

    [Header("Phase 1: Initial Freeze + Screen Shake")]
    [Tooltip("How long the player is completely frozen (animation stops)")]
    public float initialFreezeDuration = 0.5f;
    
    [Tooltip("Use impact shake (jolt + decay) instead of continuous shake")]
    public bool useImpactShake = true;
    
    [Tooltip("Intensity of the screen shake")]
    public float screenShakeIntensity = 0.4f;
    
    [Tooltip("Duration of the impact shake (how long it shakes after the jolt)")]
    public float impactShakeDuration = 0.15f;

    [Header("Phase 2: Character Shake + Red Flash")]
    [Tooltip("How long the character shakes while fading to red")]
    public float flashDuration = 0.8f;
    
    [Tooltip("Intensity of the character shake")]
    public float characterShakeIntensity = 0.05f;
    
    [Tooltip("How fast the character shake oscillates")]
    public float characterShakeSpeed = 50f;

    [Header("Phase 3: Death + Respawn")]
    [Tooltip("How long after the explosion before the player respawns (gives time to see the explosion)")]
    public float respawnDelay = 3.0f;

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
        // Play hit sound
        if (SoundFXManager.Instance != null && hitSound != null)
        {
            SoundFXManager.Instance.PlaySound(hitSound, transform, 1f);
        }

        // Start screen shake (only during initial freeze phase)
        if (CameraFollow.Instance != null)
        {
            if (useImpactShake)
            {
                // Impact shake: rapid decaying vibration
                CameraFollow.Instance.ImpactShake(screenShakeIntensity, impactShakeDuration);
            }
            else
            {
                // Duration-based shake
                CameraFollow.Instance.Shake(initialFreezeDuration, screenShakeIntensity);
            }
        }

        // Start the damage sequence on the player
        player.TakeDamage(initialFreezeDuration, flashDuration, characterShakeIntensity, characterShakeSpeed, respawnDelay, () => 
        {
            // Spawn explosion effect on the player
            if (damageExplosionEffect != null)
            {
                GameObject explosion = Instantiate(damageExplosionEffect, player.transform.position, Quaternion.identity);
                
                // Force the explosion to render above everything
                SpriteRenderer playerSprite = player.GetComponentInChildren<SpriteRenderer>();
                string sortingLayerName = playerSprite != null ? playerSprite.sortingLayerName : "Default";
                
                foreach (var renderer in explosion.GetComponentsInChildren<Renderer>())
                {
                    renderer.sortingLayerName = sortingLayerName;
                    renderer.sortingOrder = 1000;
                }
            }
        });

        // Allow this prop to damage again after the full sequence completes
        float totalDuration = initialFreezeDuration + flashDuration + respawnDelay + 0.2f;
        Invoke(nameof(ResetTrigger), totalDuration);
    }

    private void ResetTrigger()
    {
        hasTriggered = false;
    }
}
