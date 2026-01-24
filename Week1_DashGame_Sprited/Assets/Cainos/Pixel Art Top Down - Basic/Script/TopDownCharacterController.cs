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

        private Rigidbody2D rb;
        private Animator animator;

        private Vector2 moveDir;
        private Vector2 lastMoveDir = Vector2.down;

        public bool IsDashing => isDashing;
        private bool isDashing;
        private bool isBouncing;

        private float dashTimer;
        private float dashCooldownTimer;
        
        private LayerMask originalExcludeLayers;

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            originalExcludeLayers = rb.excludeLayers;
        }

        private void Update()
        {
            // Reset the scene when R is pressed
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetScene();
                return;
            }

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
    }
}
