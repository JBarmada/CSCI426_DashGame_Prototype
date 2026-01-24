using UnityEngine;
using Cainos.PixelArtTopDown_Basic;
using System.Collections;

public class Explodable : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionForce = 8f;
    public GameObject explosionEffect;

    [Header("Hitstop Settings")]
    [Tooltip("How long the object gets pushed before freezing")]
    public float pushDuration = 0.05f;
    
    [Tooltip("How long the object shakes and turns white before exploding")]
    public float freezeDuration = 1f;
    
    [Tooltip("Intensity of the shake effect")]
    public float shakeIntensity = 0.05f;
    
    [Tooltip("How fast the shake oscillates")]
    public float shakeSpeed = 75f;

    [Header("Flash Effect")]
    [Tooltip("Color to flash to (usually white)")]
    public Color flashColor = Color.white;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private bool exploded;
    private Vector3 originalPosition;
    private CollisionDetectionMode2D originalCollisionDetection;

    // Material for flash effect
    private Material flashMaterial;
    private static Shader flashShader;
    
    // Default explosion effect (loaded from Resources or found in project)
    private static GameObject defaultExplosionEffect;
    private static bool triedLoadingDefault = false;

    public bool HasExploded => exploded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        
        // Find SpriteRenderer - check this object first, then children
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Store original collision detection mode
        if (rb != null)
            originalCollisionDetection = rb.collisionDetectionMode;

        // Set up the flash material
        SetupFlashMaterial();
        
        // Try to load default explosion effect if none assigned
        LoadDefaultExplosionEffect();
    }

    void LoadDefaultExplosionEffect()
    {
        // If explosion effect is already assigned, use it
        if (explosionEffect != null) return;
        
        // Try to load default only once
        if (!triedLoadingDefault)
        {
            triedLoadingDefault = true;
            
            // Try to find the ExplosionEffect prefab in the project
            #if UNITY_EDITOR
            // In editor, we can search for assets
            string[] guids = UnityEditor.AssetDatabase.FindAssets("ExplosionEffect t:Prefab");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                defaultExplosionEffect = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            #endif
        }
        
        // Use the default if found
        if (defaultExplosionEffect != null)
        {
            explosionEffect = defaultExplosionEffect;
        }
    }

    void SetupFlashMaterial()
    {
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[Explodable] No SpriteRenderer found on {gameObject.name}!");
            return;
        }

        // Find the flash shader
        if (flashShader == null)
        {
            flashShader = Shader.Find("Custom/SpriteFlash");
        }

        // If shader exists, create a material instance with it
        if (flashShader != null)
        {
            flashMaterial = new Material(flashShader);
            flashMaterial.SetColor("_FlashColor", flashColor);
            flashMaterial.SetFloat("_FlashAmount", 0f);
            
            // Copy the main texture from original material
            if (spriteRenderer.material != null && spriteRenderer.material.mainTexture != null)
            {
                flashMaterial.mainTexture = spriteRenderer.material.mainTexture;
            }
            
            // Also copy the sprite's texture directly as backup
            if (spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null)
            {
                flashMaterial.mainTexture = spriteRenderer.sprite.texture;
            }
            
            // Apply the flash material
            spriteRenderer.material = flashMaterial;
        }
        else
        {
            Debug.LogWarning("SpriteFlash shader not found! Make sure the shader exists at: Assets/Cainos/Pixel Art Top Down - Basic/Shader/SpriteFlash.shader");
        }
    }

    /// <summary>
    /// Call this method to trigger the explosion from an external source (like the player).
    /// This avoids race conditions with OnCollisionEnter2D callbacks.
    /// </summary>
    public void TriggerExplosion(Vector2 hitPoint)
    {
        if (exploded) return;

        exploded = true;

        // Start the hitstop sequence
        StartCoroutine(HitstopSequence(hitPoint));
    }

    IEnumerator HitstopSequence(Vector2 hitPoint)
    {
        // === PHASE 1: PUSH ===
        // Set collision detection to Continuous to prevent clipping through walls
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Apply initial push force
        Vector2 pushDir = ((Vector2)transform.position - hitPoint).normalized;
        if (rb != null)
        {
            rb.AddForce(pushDir * explosionForce, ForceMode2D.Impulse);
        }

        // Let it move for a brief moment (collider stays enabled so it hits walls)
        yield return new WaitForSeconds(pushDuration);

        // === PHASE 2: FREEZE ===
        // Now disable collider to prevent further interactions
        if (col != null)
            col.enabled = false;

        // Stop all movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.collisionDetectionMode = originalCollisionDetection;
            rb.bodyType = RigidbodyType2D.Kinematic; // Freeze in place
        }

        // Store position for shake offset
        originalPosition = transform.position;

        // === PHASE 3: SHAKE + WHITE FLASH ===
        float elapsed = 0f;
        while (elapsed < freezeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / freezeDuration;

            // Shake effect - oscillate position
            float shakeX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity * (1f - progress * 0.5f);
            float shakeY = Mathf.Cos(Time.time * shakeSpeed * 1.1f) * shakeIntensity * (1f - progress * 0.5f);
            transform.position = originalPosition + new Vector3(shakeX, shakeY, 0f);

            // Flash to white using the shader
            if (flashMaterial != null)
            {
                flashMaterial.SetFloat("_FlashAmount", progress);
            }
            // Fallback: also try setting sprite renderer color (for objects without shader)
            else if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(Color.white, flashColor, progress);
            }

            yield return null;
        }

        // Reset position before spawning effect
        transform.position = originalPosition;

        // === PHASE 4: EXPLODE ===
        // Visual effect
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
