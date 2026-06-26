using UnityEngine;
using InfiniteRunner.Core;
using InfiniteRunner.Player;

namespace InfiniteRunner.InputSystem
{
    public class InputManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the PlayerController component. If left blank, it will attempt to find it in the scene.")]
        [SerializeField] private PlayerController playerController;

        private void Start()
        {
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
                if (playerController == null)
                {
                    Debug.LogWarning("InputManager: PlayerController reference is missing and could not be found in the scene.");
                }
            }
        }

        private void Update()
        {
            // Verify if game is running and player controller is available before processing input
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Running)
            {
                return;
            }

            if (playerController == null)
            {
                return;
            }

            // Keyboard input reading
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                playerController.TryMoveLeft();
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                playerController.TryMoveRight();
            }
            else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space))
            {
                playerController.Jump();
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                playerController.Roll();
            }
        }
    }
}
