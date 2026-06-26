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

        private int currentLane = 1; // 0 = Left, 1 = Center, 2 = Right
        private Vector3 targetPosition;
        private Vector3 startPosition;

        private void Start()
        {
            startPosition = transform.position;
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
            // Only interpolate movement; input handling is decoupled and managed by InputManager
            UpdateMovement();
        }

        /// <summary>
        /// Moves the player smoothly towards the target lane position.
        /// </summary>
        private void UpdateMovement()
        {
            // Calculate the target X position based on the current lane index
            // Lane 0: -laneDistance
            // Lane 1: 0
            // Lane 2: +laneDistance
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
        }
    }
}
