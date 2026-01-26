using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Cainos.PixelArtTopDown_Basic
{
    public class TopDownCharacterController : MonoBehaviour
    {
        [Header("Movement")]
        public float speed = 5f;

        [Header("Dash")]
        public float dashSpeed = 15f;
        public float dashDuration = 0.15f;
        public float dashCooldown = 0.4f;

        [Header("Bounce Back")]
        [Tooltip("How hard the player bounces back when hitting something")]
        public float bounceForce = 8f;
        
        [Tooltip("How long the player is stunned/bouncing after hitting something")]
        public float bounceStunDuration = 0.15f;

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

        private void StartDash()
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            animator.SetBool("IsMoving", false);
            rb.excludeLayers = LayerMask.GetMask("Enemy");
        }

        private void EndDash()
        {
            isDashing = false;
            rb.excludeLayers = originalExcludeLayers;
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
                
                // Stop the dash and bounce back
                StartCoroutine(BounceBack(collision.contacts[0].normal));
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
