using UnityEngine;
using InfiniteRunner.Core;

namespace InfiniteRunner.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Lane Settings")]
        [Tooltip("Distance between the lanes. Left is -laneDistance, Center is 0, Right is +laneDistance.")]
        [SerializeField] private float laneDistance = 3.0f;
        
        [Tooltip("Speed of the lateral transition between lanes.")]
        [SerializeField] private float laneChangeSpeed = 10.0f;

        [Header("Jump Settings")]
        [Tooltip("Maximum height of the jump.")]
        [SerializeField] private float jumpHeight = 2.5f;

        [Tooltip("Gravity acceleration affecting the jump.")]
        [SerializeField] private float gravity = 25.0f;

        [Header("Roll Settings")]
        [Tooltip("Duration of the roll/slide action in seconds.")]
        [SerializeField] private float rollDuration = 0.8f;

        private int currentLane = 1; // 0 = Left, 1 = Center, 2 = Right
        private Vector3 targetPosition;
        private Vector3 startPosition;

        // Physics variables
        private float yVelocity = 0.0f;
        private bool isGrounded = true;

        // Rolling variables
        private bool isRolling = false;
        private float rollTimer = 0.0f;
        private float originalColliderHeight;
        private Vector3 originalColliderCenter;

        // Component references
        private Animator animator;
        private BoxCollider boxCollider;

        private void Start()
        {
            startPosition = transform.position;
            
            // Cache component references
            animator    = GetComponentInChildren<Animator>();
            boxCollider = GetComponent<BoxCollider>();

            // CRITICAL: disable Root Motion at runtime so baked motion curves in
            // the Roll / Jump FBX clips never physically rotate or translate the
            // GameObject. All movement is driven exclusively by this script.
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }
            
            if (boxCollider != null)
            {
                originalColliderHeight = boxCollider.size.y;
                originalColliderCenter = boxCollider.center;
            }

            ResetPlayer();

            // Subscribe to GameManager events if available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameRestarted += ResetPlayer;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameRestarted -= ResetPlayer;
            }
        }

        private void Update()
        {
            // Safety: ensure Root Motion never gets re-enabled by Unity internals.
            // The Roll_Dodge FBX bakes a 360° root rotation; this guard prevents
            // the Animator from ever applying it to the Transform.
            if (animator != null && animator.applyRootMotion)
            {
                animator.applyRootMotion = false;
            }

            // Only update movement if game is running or initialized
            UpdateMovement();
            UpdateJumpAndRoll();
            UpdateAnimator();
        }

        /// <summary>
        /// Moves the player smoothly towards the target lane position.
        /// </summary>
        private void UpdateMovement()
        {
            float targetX = (currentLane - 1) * laneDistance;
            targetPosition = new Vector3(targetX, transform.position.y, transform.position.z);

            // Interpolate X position smoothly
            transform.position = Vector3.Lerp(
                transform.position, 
                targetPosition, 
                Time.deltaTime * laneChangeSpeed
            );
        }

        /// <summary>
        /// Updates the vertical jump physics and roll timer.
        /// </summary>
        private void UpdateJumpAndRoll()
        {
            // 1. Gravity and jump movement
            if (!isGrounded)
            {
                yVelocity -= gravity * Time.deltaTime;
                transform.position += Vector3.up * yVelocity * Time.deltaTime;

                // Ground check
                if (transform.position.y <= startPosition.y)
                {
                    transform.position = new Vector3(transform.position.x, startPosition.y, transform.position.z);
                    isGrounded = true;
                    yVelocity = 0.0f;
                }
            }

            // 2. Roll timer decay
            if (isRolling)
            {
                rollTimer -= Time.deltaTime;
                if (rollTimer <= 0.0f)
                {
                    StopRolling();
                }
            }
        }

        /// <summary>
        /// Syncs variables with the Animator component.
        /// </summary>
        private void UpdateAnimator()
        {
            if (animator == null)
            {
                // Try to find it if it was loaded dynamically later
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                bool isRunning = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Running;
                animator.SetBool("IsRunning", isRunning);
            }
        }

        /// <summary>
        /// Initiates a jump if the player is grounded.
        /// </summary>
        public void Jump()
        {
            if (isGrounded && !isRolling)
            {
                // v = sqrt(2 * g * h)
                yVelocity = Mathf.Sqrt(2.0f * gravity * jumpHeight);
                isGrounded = false;

                if (animator != null)
                {
                    animator.SetTrigger("Jump");
                }
            }
        }

        /// <summary>
        /// Initiates a roll/slide. If the player is in the air, pulls them down instantly.
        /// </summary>
        public void Roll()
        {
            if (!isRolling)
            {
                isRolling = true;
                rollTimer = rollDuration;

                if (animator != null)
                {
                    animator.SetTrigger("Roll");
                }

                // Shrink collider to half height
                if (boxCollider != null)
                {
                    boxCollider.size = new Vector3(
                        boxCollider.size.x,
                        originalColliderHeight * 0.5f,
                        boxCollider.size.z
                    );
                    boxCollider.center = new Vector3(
                        originalColliderCenter.x, 
                        originalColliderCenter.y * 0.5f, 
                        originalColliderCenter.z
                    );
                }
            }

            // Instant drop from air (Subway Surfers physics)
            if (!isGrounded)
            {
                yVelocity = -Mathf.Sqrt(2.0f * gravity * jumpHeight) * 1.5f; // Fast downward force
            }
        }

        /// <summary>
        /// Restores collider size and resets roll state.
        /// </summary>
        private void StopRolling()
        {
            isRolling = false;
            if (boxCollider != null)
            {
                boxCollider.size = new Vector3(
                    boxCollider.size.x,
                    originalColliderHeight,
                    boxCollider.size.z
                );
                boxCollider.center = originalColliderCenter;
            }
        }

        /// <summary>
        /// Attempts to change the lane to the left.
        /// </summary>
        public void TryMoveLeft()
        {
            if (currentLane > 0)
            {
                currentLane--;
            }
        }

        /// <summary>
        /// Attempts to change the lane to the right.
        /// </summary>
        public void TryMoveRight()
        {
            if (currentLane < 2)
            {
                currentLane++;
            }
        }

        /// <summary>
        /// Instantly resets the player to the center lane and original starting position.
        /// </summary>
        public void ResetPlayer()
        {
            currentLane = 1;
            float targetX = (currentLane - 1) * laneDistance;
            transform.position = new Vector3(targetX, startPosition.y, startPosition.z);
            targetPosition = transform.position;

            yVelocity = 0.0f;
            isGrounded = true;

            if (isRolling)
            {
                StopRolling();
            }

            if (animator != null)
            {
                animator.Rebind(); // Reset animator state machine
            }
        }
    }
}
