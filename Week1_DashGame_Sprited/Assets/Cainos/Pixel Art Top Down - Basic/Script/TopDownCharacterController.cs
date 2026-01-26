using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Cainos.PixelArtTopDown_Basic
{
    public class TopDownCharacterController : MonoBehaviour
    {   
        [Header("Audio")]
        [SerializeField] private AudioClip[] walkAudios;
        [SerializeField] private AudioClip dashSound;
        [SerializeField] private float footstepInterval = 0.5f;
        [Header("Movement")]
        public float speed = 5f;

        [Header("Dash")]
        public float dashSpeed = 15f;
        public float dashDuration = 0.15f;
        public float dashCooldown = 0.4f;
        

        [Header("Dash Trail")]
        [Tooltip("Sprite to spawn as trail during dash (e.g., rune glow sprite)")]
        public Sprite dashTrailSprite;
        
        [Tooltip("How many runes to spawn per second during dash")]
        public float trailSpawnRate = 30f;
        
        [Tooltip("How long each trail rune stays visible")]
        public float trailFadeDuration = 0.3f;
        
        [Tooltip("Scale of the trail sprites (height/length)")]
        public float trailScale = 1f;
        
        [Tooltip("Width multiplier for the trail sprites (1 = normal, 2 = twice as wide)")]
        public float trailWidthScale = 1f;
        
        [Tooltip("Offset for trail sorting order relative to player (negative = behind player)")]
        public int trailSortingOrderOffset = -1;

        [Header("Dash Start Effect")]
        [Tooltip("Effect to spawn when dash starts (e.g., Explosion_Blue)")]
        public GameObject dashStartEffect;
        
        [Tooltip("Scale of the dash start effect")]
        public float dashStartEffectScale = 0.5f;
        
        [Tooltip("Opacity of the dash start effect (0-1)")]
        [Range(0f, 1f)]
        public float dashStartEffectOpacity = 0.6f;

        [Header("Impact Effect")]
        [Tooltip("Effect to spawn when hitting an object (e.g., Explosion_Yellow)")]
        public GameObject impactEffect;
        
        [Tooltip("Scale of the impact effect")]
        public float impactEffectScale = 0.5f;
        
        [Tooltip("Opacity of the impact effect (0-1)")]
        [Range(0f, 1f)]
        public float impactEffectOpacity = 0.8f;

        [Header("Bounce Back")]
        [Tooltip("How hard the player bounces back when hitting something")]
        public float bounceForce = 8f;
        
        [Tooltip("How long the player is stunned/bouncing after hitting something")]
        public float bounceStunDuration = 0.15f;

        [Header("Dash Flash")]
        [Tooltip("Color to tint the player during dash")]
        public Color dashFlashColor = new Color(0.3f, 0.5f, 1f, 1f); // Light blue
        
        [Tooltip("Maximum intensity of the dash flash (0-1)")]
        [Range(0f, 1f)]
        public float dashFlashIntensity = 0.4f;

        [Header("Damage Flash")]
        [Tooltip("Color to flash when taking damage")]
        public Color damageFlashColor = Color.red;

        private Rigidbody2D rb;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private Material flashMaterial;

        private Vector2 moveDir;
        private Vector2 lastMoveDir = Vector2.down;

        public bool IsDashing => isDashing;
        public Vector2 LastMoveDirection => lastMoveDir;
        private bool isDashing;
        private bool isBouncing;
        private bool isFrozen;

        private float dashTimer;
        private float dashCooldownTimer;
        private float trailSpawnTimer;
        private float footstepTimer;
        private AudioSource currentFootstepSource;
        
        private LayerMask originalExcludeLayers;
        
        // Spawn point for respawning after death
        private Vector3 spawnPosition;
        private int spawnLayer;
        private string spawnSortingLayer;

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            originalExcludeLayers = rb.excludeLayers;
            
            // Store initial spawn position and layer settings
            spawnPosition = transform.position;
            spawnLayer = gameObject.layer;
            
            // Set up flash material for damage effect
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            // Store initial sorting layer
            if (spriteRenderer != null)
                spawnSortingLayer = spriteRenderer.sortingLayerName;
            
            SetupFlashMaterial();
        }

        private void SetupFlashMaterial()
        {
            if (spriteRenderer == null) return;
            
            Shader flashShader = Shader.Find("Custom/SpriteFlash");
            if (flashShader != null)
            {
                flashMaterial = new Material(flashShader);
                flashMaterial.SetColor("_FlashColor", damageFlashColor);
                flashMaterial.SetFloat("_FlashAmount", 0f);
                
                if (spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null)
                {
                    flashMaterial.mainTexture = spriteRenderer.sprite.texture;
                }
                
                spriteRenderer.material = flashMaterial;
            }
        }

        private void Update()
        {
            // Reset the scene when R is pressed
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetScene();
                return;
            }

            // Don't allow any input while frozen
            if (isFrozen) return;

            // Cooldown timer
            if (dashCooldownTimer > 0)
                dashCooldownTimer -= Time.deltaTime;

            // Don't allow input during dash or bounce
            if (!isDashing && !isBouncing)
            {
                HandleMovementInput();
                HandleFootsteps();
            }

            // Dash input
            if (Input.GetKeyDown(KeyCode.Space) && dashCooldownTimer <= 0 && !isBouncing)
            {
                StartDash();
            }
        }

        private void FixedUpdate()
        {
            // No movement while frozen
            if (isFrozen)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
            
            if (isDashing)
            {
                rb.linearVelocity = lastMoveDir * dashSpeed;
                dashTimer -= Time.fixedDeltaTime;
                
                // Apply dash flash - fade in first half, fade out second half
                if (flashMaterial != null && dashFlashIntensity > 0f)
                {
                    // Progress goes from 1 (start) to 0 (end)
                    float progress = dashTimer / dashDuration;
                    // Use sine wave for smooth in/out - peaks at 0.5 (middle of dash)
                    float flashAmount = Mathf.Sin(progress * Mathf.PI) * dashFlashIntensity;
                    flashMaterial.SetColor("_FlashColor", dashFlashColor);
                    flashMaterial.SetFloat("_FlashAmount", flashAmount);
                }
                
                // Spawn trail sprites at regular intervals
                if (dashTrailSprite != null)
                {
                    trailSpawnTimer -= Time.fixedDeltaTime;
                    if (trailSpawnTimer <= 0f)
                    {
                        SpawnTrailSprite();
                        trailSpawnTimer = 1f / trailSpawnRate;
                    }
                }

                if (dashTimer <= 0)
                {
                    EndDash();
                }
            }
            else if (!isBouncing)
            {
                rb.linearVelocity = moveDir * speed;
            }
            // During bounce, let physics handle the velocity (don't override it)
        }

        private void HandleMovementInput()
        {
            moveDir = Vector2.zero;

            if (Input.GetKey(KeyCode.A))
            {
                moveDir.x = -1;
                animator.SetInteger("Direction", 3);
            }
            else if (Input.GetKey(KeyCode.D))
            {
                moveDir.x = 1;
                animator.SetInteger("Direction", 2);
            }

            if (Input.GetKey(KeyCode.W))
            {
                moveDir.y = 1;
                animator.SetInteger("Direction", 1);
            }
            else if (Input.GetKey(KeyCode.S))
            {
                moveDir.y = -1;
                animator.SetInteger("Direction", 0);
            }

            moveDir.Normalize();

            if (moveDir != Vector2.zero)
                lastMoveDir = moveDir;

            animator.SetBool("IsMoving", moveDir != Vector2.zero);
        }

        private void HandleFootsteps()
        {
            if (moveDir != Vector2.zero)
            {
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0)
                {
                    if (walkAudios != null && walkAudios.Length > 0)
                    {
                        currentFootstepSource = SoundFXManager.Instance.PlayRandomSound(walkAudios, transform, 1f);
                    }
                    footstepTimer = footstepInterval;
                }
            }
            else
            {
                footstepTimer = 0;
            }
        }

        private void StopFootsteps()
        {
            if (currentFootstepSource != null)
            {
                currentFootstepSource.Stop();
                Destroy(currentFootstepSource.gameObject);
                currentFootstepSource = null;
            }
        }

        private void StartDash()
        {
            StopFootsteps();
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            trailSpawnTimer = 0f; // Spawn first trail immediately
            animator.SetBool("IsMoving", false);
            rb.excludeLayers = LayerMask.GetMask("Enemy");
            SoundFXManager.Instance.PlaySound(dashSound, transform, 1f);
            // Spawn dash start effect
            if (dashStartEffect != null)
            {
                GameObject effect = Instantiate(dashStartEffect, transform.position, Quaternion.identity);
                
                // Apply scale to the root object
                effect.transform.localScale = Vector3.one * dashStartEffectScale;
                
                // Handle all renderers (sprites)
                foreach (var renderer in effect.GetComponentsInChildren<Renderer>())
                {
                    // Match player's sorting layer, render BEHIND player
                    if (spriteRenderer != null)
                    {
                        renderer.sortingLayerName = spriteRenderer.sortingLayerName;
                        renderer.sortingOrder = spriteRenderer.sortingOrder - 1;
                    }
                    
                    // Apply opacity to SpriteRenderers
                    if (renderer is SpriteRenderer sr)
                    {
                        Color c = sr.color;
                        sr.color = new Color(c.r, c.g, c.b, c.a * dashStartEffectOpacity);
                    }
                }
                
                // Handle all particle systems
                foreach (var ps in effect.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = ps.main;
                    
                    // Apply scale to particle system - modify both multiplier and base size
                    main.startSizeMultiplier *= dashStartEffectScale;
                    
                    // Also scale startSize directly for better small scale support
                    if (main.startSize.mode == ParticleSystemCurveMode.Constant)
                    {
                        main.startSize = main.startSize.constant * dashStartEffectScale;
                    }
                    else if (main.startSize.mode == ParticleSystemCurveMode.TwoConstants)
                    {
                        main.startSize = new ParticleSystem.MinMaxCurve(
                            main.startSize.constantMin * dashStartEffectScale,
                            main.startSize.constantMax * dashStartEffectScale
                        );
                    }
                    
                    // Apply opacity to particle system's start color
                    Color startColor = main.startColor.color;
                    main.startColor = new Color(startColor.r, startColor.g, startColor.b, startColor.a * dashStartEffectOpacity);
                    
                    // Match sorting layer for particle renderer
                    var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (psRenderer != null && spriteRenderer != null)
                    {
                        psRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
                        psRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
                    }
                }
            }
        }

        private void EndDash()
        {
            isDashing = false;
            rb.excludeLayers = originalExcludeLayers;
            
            // Reset dash flash
            if (flashMaterial != null)
            {
                flashMaterial.SetFloat("_FlashAmount", 0f);
                flashMaterial.SetColor("_FlashColor", damageFlashColor); // Restore damage color
            }
        }

        private void SpawnTrailSprite()
        {
            // Create a new GameObject for the trail sprite
            GameObject trailObj = new GameObject("DashTrail");
            trailObj.transform.position = transform.position;
            // X = width (perpendicular to direction), Y = height (along direction)
            trailObj.transform.localScale = new Vector3(trailScale * trailWidthScale, trailScale, 1f);
            
            // Calculate rotation based on dash direction
            // The rune sprite is vertical by default (up/down = 0 degrees)
            // Atan2 gives angle from positive X axis, so we need to adjust
            // Up (0,1) should be 0°, Right (1,0) should be 90°, etc.
            float angle = Mathf.Atan2(lastMoveDir.x, lastMoveDir.y) * Mathf.Rad2Deg;
            trailObj.transform.rotation = Quaternion.Euler(0f, 0f, -angle);
            
            // Add SpriteRenderer and configure it
            SpriteRenderer sr = trailObj.AddComponent<SpriteRenderer>();
            sr.sprite = dashTrailSprite;
            
            // Use the player's current sorting layer (follows player up/down stairs)
            if (spriteRenderer != null)
            {
                sr.sortingLayerName = spriteRenderer.sortingLayerName;
                sr.sortingOrder = spriteRenderer.sortingOrder + trailSortingOrderOffset;
            }
            
            // Start the fade coroutine
            StartCoroutine(FadeAndDestroyTrail(trailObj, sr));
        }

        private IEnumerator FadeAndDestroyTrail(GameObject trailObj, SpriteRenderer sr)
        {
            float elapsed = 0f;
            Color startColor = sr.color;
            
            while (elapsed < trailFadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / trailFadeDuration);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
            
            Destroy(trailObj);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Only process collision if we're dashing
            if (!isDashing) return;

            // Check if we hit an Explodable object
            Explodable explodable = collision.gameObject.GetComponent<Explodable>();
            if (explodable != null && !explodable.HasExploded)
            {
                // Trigger the explosion BEFORE ending the dash (avoids race condition)
                Vector2 hitPoint = collision.contacts[0].point;
                explodable.TriggerExplosion(hitPoint);
                
                // Spawn impact effect at hit point
                SpawnImpactEffect(hitPoint);
                
                // Quick impact vignette flash
                if (DamageVignette.Instance != null)
                {
                    DamageVignette.Instance.Flash();
                }
                
                // Stop the dash and bounce back
                StartCoroutine(BounceBack(collision.contacts[0].normal));
            }
        }
        
        private void SpawnImpactEffect(Vector2 position)
        {
            if (impactEffect == null) return;
            
            GameObject effect = Instantiate(impactEffect, position, Quaternion.identity);
            
            // Apply scale to the root object
            effect.transform.localScale = Vector3.one * impactEffectScale;
            
            // Handle all renderers (sprites) - render ABOVE player
            foreach (var renderer in effect.GetComponentsInChildren<Renderer>())
            {
                if (spriteRenderer != null)
                {
                    renderer.sortingLayerName = spriteRenderer.sortingLayerName;
                    renderer.sortingOrder = spriteRenderer.sortingOrder + 10; // Above player
                }
                
                // Apply opacity to SpriteRenderers
                if (renderer is SpriteRenderer sr)
                {
                    Color c = sr.color;
                    sr.color = new Color(c.r, c.g, c.b, c.a * impactEffectOpacity);
                }
            }
            
            // Handle all particle systems
            foreach (var ps in effect.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                
                // Apply scale to particle system - modify both multiplier and base size
                main.startSizeMultiplier *= impactEffectScale;
                
                // Also scale startSize directly for better small scale support
                if (main.startSize.mode == ParticleSystemCurveMode.Constant)
                {
                    main.startSize = main.startSize.constant * impactEffectScale;
                }
                else if (main.startSize.mode == ParticleSystemCurveMode.TwoConstants)
                {
                    main.startSize = new ParticleSystem.MinMaxCurve(
                        main.startSize.constantMin * impactEffectScale,
                        main.startSize.constantMax * impactEffectScale
                    );
                }
                
                // Apply opacity to particle system's start color
                Color startColor = main.startColor.color;
                main.startColor = new Color(startColor.r, startColor.g, startColor.b, startColor.a * impactEffectOpacity);
                
                // Match sorting layer for particle renderer - ABOVE player
                var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
                if (psRenderer != null && spriteRenderer != null)
                {
                    psRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
                    psRenderer.sortingOrder = spriteRenderer.sortingOrder + 10;
                }
            }
        }

        private IEnumerator BounceBack(Vector2 collisionNormal)
        {
            // End dash state
            EndDash();
            
            // Enter bounce state
            isBouncing = true;
            animator.SetBool("IsMoving", false);

            // Apply bounce force in the direction away from the collision
            rb.linearVelocity = collisionNormal * bounceForce;

            // Wait for stun duration
            yield return new WaitForSeconds(bounceStunDuration);

            // Slow down gradually
            rb.linearVelocity = Vector2.zero;
            
            // Exit bounce state
            isBouncing = false;
        }

        private void ResetScene()
        {
            // Reload the current scene to reset everything
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }

        /// <summary>
        /// Called when the player hits a damaging object.
        /// Phase 1: Complete freeze (animation stops) + screen shake
        /// Phase 2: Character shakes while fading to red
        /// Phase 3: Explosion spawns, player disappears
        /// Phase 4: Respawn at start position
        /// </summary>
        public void TakeDamage(float initialFreezeDuration, float flashDuration, float characterShakeIntensity, float characterShakeSpeed, float respawnDelay, System.Action onFlashComplete)
        {
            if (isFrozen) return; // Already taking damage
            
            StartCoroutine(DamageSequence(initialFreezeDuration, flashDuration, characterShakeIntensity, characterShakeSpeed, respawnDelay, onFlashComplete));
        }

        private IEnumerator DamageSequence(float initialFreezeDuration, float flashDuration, float shakeIntensity, float shakeSpeed, float respawnDelay, System.Action onFlashComplete)
        {
            StopFootsteps();

            // End any current dash
            if (isDashing)
                EndDash();
            
            // Freeze the player
            isFrozen = true;
            isBouncing = false;
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsMoving", false);

            // Store original position for shake
            Vector3 originalPosition = transform.position;

            // === PHASE 1: COMPLETE FREEZE ===
            // Stop the animator completely (no breathing animation)
            animator.speed = 0f;
            
            // Show damage vignette (impact flash)
            if (DamageVignette.Instance != null)
            {
                DamageVignette.Instance.Show();
            }
            
            // Wait for initial freeze duration (screen shake happens externally)
            yield return new WaitForSeconds(initialFreezeDuration);

            // === PHASE 2: CHARACTER SHAKE + RED FLASH ===
            // Resume animator (breathing can play during this phase)
            animator.speed = 1f;
            
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / flashDuration;
                
                // Flash to red
                if (flashMaterial != null)
                {
                    flashMaterial.SetFloat("_FlashAmount", progress);
                }
                
                // Shake the character model (intensity decreases slightly as we approach the end)
                float currentShakeIntensity = shakeIntensity * (1f - progress * 0.3f);
                float shakeX = Mathf.Sin(Time.time * shakeSpeed) * currentShakeIntensity;
                float shakeY = Mathf.Cos(Time.time * shakeSpeed * 1.1f) * currentShakeIntensity;
                transform.position = originalPosition + new Vector3(shakeX, shakeY, 0f);
                
                yield return null;
            }

            // Reset position before explosion
            transform.position = originalPosition;

            // === PHASE 3: EXPLOSION + DISAPPEAR ===
            // Hide the player
            SetPlayerVisible(false);
            
            // Callback for when flash is complete (spawn explosion, etc.)
            onFlashComplete?.Invoke();

            // Wait for respawn delay (time to see the explosion)
            yield return new WaitForSeconds(respawnDelay);

            // === PHASE 4: RESPAWN ===
            // Move to spawn position
            transform.position = spawnPosition;
            
            // Reset layer settings (in case we were on stairs when we died)
            gameObject.layer = spawnLayer;
            SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in allRenderers)
            {
                sr.sortingLayerName = spawnSortingLayer;
            }
            
            // Reset flash effect
            if (flashMaterial != null)
            {
                flashMaterial.SetFloat("_FlashAmount", 0f);
            }
            
            // Fade out damage vignette
            if (DamageVignette.Instance != null)
            {
                DamageVignette.Instance.Hide();
            }
            
            // Show the player
            SetPlayerVisible(true);

            // Unfreeze
            isFrozen = false;
        }

        private void SetPlayerVisible(bool visible)
        {
            // Hide/show all sprite renderers on the player
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in renderers)
            {
                sr.enabled = visible;
            }
        }
    }
}
