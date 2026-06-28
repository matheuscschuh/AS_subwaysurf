using System.Collections.Generic;
using UnityEngine;
using InfiniteRunner.Core;

namespace InfiniteRunner.Obstacle
{
    /// <summary>
    /// Spawns obstacle cubes at regular intervals on one of the 3 lanes.
    /// The chosen lane is randomised but never repeats twice in a row,
    /// guaranteeing the player always has a chance to dodge.
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("How often (in seconds) a new obstacle is spawned.")]
        [SerializeField] private float spawnInterval = 5.0f;

        [Tooltip("Initial delay (in seconds) after the game starts before the first obstacle appears.")]
        [SerializeField] private float initialDelay = 10.0f;

        [Tooltip("Z distance ahead of the player where obstacles appear.")]
        [SerializeField] private float spawnDistance = 80.0f;

        [Tooltip("Speed at which spawned obstacles move towards the player (should match track speed).")]
        [SerializeField] private float obstacleSpeed = 15.0f;

        [Header("Obstacle Appearance")]
        [Tooltip("Size of the obstacle cube.")]
        [SerializeField] private Vector3 obstacleSize = new Vector3(1.5f, 1.5f, 1.5f);

        [Header("Lane Settings")]
        [Tooltip("Lateral distance between lanes (must match PlayerController.laneDistance).")]
        [SerializeField] private float laneDistance = 3.0f;

        private float spawnTimer;
        private float delayTimer;
        private bool delayElapsed;
        private int lastLane = -1; // -1 = no previous lane

        // Keep references to spawned obstacles so we can clean them up on restart
        private List<GameObject> spawnedObstacles = new List<GameObject>();

        // Cached material (created once at runtime)
        private Material obstacleMaterial;

        private void Start()
        {
            ResetTimers();

            // Create a simple black material once
            obstacleMaterial = new Material(Shader.Find("Standard"));
            obstacleMaterial.color = Color.black;
            if (obstacleMaterial.HasProperty("_Glossiness")) obstacleMaterial.SetFloat("_Glossiness", 0.0f);
            if (obstacleMaterial.HasProperty("_Metallic"))   obstacleMaterial.SetFloat("_Metallic",   0.0f);

            // Subscribe to restart events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameRestarted += ClearAllObstacles;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameRestarted -= ClearAllObstacles;
            }

            if (obstacleMaterial != null)
            {
                Destroy(obstacleMaterial);
            }
        }

        private void Update()
        {
            // Only spawn while the game is running
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Running)
                return;

            // Wait for the initial delay before starting to spawn
            if (!delayElapsed)
            {
                delayTimer -= Time.deltaTime;
                if (delayTimer <= 0f)
                {
                    delayElapsed = true;
                    spawnTimer = spawnInterval;
                }
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnObstacle();
                spawnTimer = spawnInterval;
            }
        }

        /// <summary>
        /// Creates a black cube obstacle in a random lane (never the same as the previous one).
        /// </summary>
        private void SpawnObstacle()
        {
            int lane = PickRandomLane();
            float xPos = (lane - 1) * laneDistance; // lane 0 = left, 1 = center, 2 = right
            float yPos = obstacleSize.y / 2f;       // sit on ground

            Vector3 spawnPos = new Vector3(xPos, yPos, spawnDistance);

            // Create a primitive cube at runtime
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Obstacle";
            obstacle.tag = "Obstacle";
            obstacle.transform.position = spawnPos;
            obstacle.transform.localScale = obstacleSize;

            // Apply black material
            Renderer renderer = obstacle.GetComponent<Renderer>();
            if (renderer != null && obstacleMaterial != null)
            {
                renderer.material = obstacleMaterial;
            }

            // Make the collider a trigger so it doesn't push the player around
            BoxCollider col = obstacle.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Add movement/collision script
            ObstacleController controller = obstacle.AddComponent<ObstacleController>();
            controller.moveSpeed = obstacleSpeed;

            spawnedObstacles.Add(obstacle);
            CleanNullReferences();
        }

        /// <summary>
        /// Picks a random lane index (0, 1, or 2) that is different from
        /// the last spawned lane, ensuring alternation.
        /// </summary>
        private int PickRandomLane()
        {
            int lane;
            if (lastLane < 0)
            {
                // First spawn – any lane is fine
                lane = Random.Range(0, 3);
            }
            else
            {
                // Pick from the remaining 2 lanes
                lane = Random.Range(0, 2);
                if (lane >= lastLane) lane++;
            }

            lastLane = lane;
            return lane;
        }

        /// <summary>
        /// Destroys all active obstacles and resets the spawn timer.
        /// Called when the game is restarted.
        /// </summary>
        public void ClearAllObstacles()
        {
            foreach (GameObject obs in spawnedObstacles)
            {
                if (obs != null) Destroy(obs);
            }
            spawnedObstacles.Clear();
            lastLane = -1;
            ResetTimers();
        }

        /// <summary>
        /// Resets both the initial delay and spawn timers so the 10-second
        /// grace period applies again (e.g. after a restart).
        /// </summary>
        private void ResetTimers()
        {
            delayTimer = initialDelay;
            delayElapsed = false;
            spawnTimer = spawnInterval;
        }

        /// <summary>
        /// Removes null entries from the tracked list (destroyed obstacles).
        /// </summary>
        private void CleanNullReferences()
        {
            spawnedObstacles.RemoveAll(o => o == null);
        }
    }
}
