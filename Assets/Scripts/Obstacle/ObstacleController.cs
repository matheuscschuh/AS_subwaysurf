using UnityEngine;
using InfiniteRunner.Core;
using InfiniteRunner.Difficulty;


namespace InfiniteRunner.Obstacle
{
    /// <summary>
    /// Attached to each obstacle instance. Moves it backwards along with
    /// the track and detects collision with the player to end the game.
    /// </summary>
    public class ObstacleController : MonoBehaviour
    {
        /// <summary>
        /// Z position below which the obstacle is considered off-screen
        /// and should be destroyed to free memory.
        /// </summary>
        private const float DESPAWN_Z = -80f;

        /// <summary>
        /// Speed at which this obstacle moves towards the player.
        /// Set by the ObstacleSpawner at spawn time so it matches the track speed.
        /// </summary>
        [HideInInspector] public float moveSpeed;

        private void Update()
        {
            // Only move while the game is running
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Running)
                return;

            float currentSpeed = moveSpeed;
            if (DifficultyManager.Instance != null)
            {
                currentSpeed = DifficultyManager.Instance.CurrentSpeed;
            }

            transform.Translate(0, 0, -currentSpeed * Time.deltaTime);

            // Self-destroy when far behind the camera
            if (transform.position.z < DESPAWN_Z)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if the colliding object is the player
            if (other.CompareTag("Player"))
            {
                Debug.Log("ObstacleController: Player hit an obstacle! Game Over.");

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RestartGame();
                }
            }
        }
    }
}
