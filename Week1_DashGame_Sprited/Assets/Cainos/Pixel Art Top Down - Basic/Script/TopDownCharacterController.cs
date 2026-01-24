using UnityEngine;

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

        private Rigidbody2D rb;
        private Animator animator;

        private Vector2 moveDir;
        private Vector2 lastMoveDir = Vector2.down;

        public bool IsDashing => isDashing;
        private bool isDashing;

        private float dashTimer;
        private float dashCooldownTimer;

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            // Cooldown timer
            if (dashCooldownTimer > 0)
                dashCooldownTimer -= Time.deltaTime;

            if (!isDashing)
            {
                HandleMovementInput();
            }

            // Dash input
            if (Input.GetKeyDown(KeyCode.Space) && dashCooldownTimer <= 0)
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
                    isDashing = false;
                }
            }
            else
            {
                rb.linearVelocity = moveDir * speed;
            }
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
            //animator.SetTrigger("Dash");
            // Optional animation hook
            animator.SetBool("IsMoving", false);
            rb.excludeLayers = LayerMask.GetMask("Enemy");

        }
    }
}
