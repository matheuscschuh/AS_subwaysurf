using System.Collections.Generic;
using UnityEngine;
using InfiniteRunner.Core;

namespace InfiniteRunner.Track
{
    public class TrackManager : MonoBehaviour
    {
        [Header("Track Configurations")]
        [Tooltip("Prefab of a single track segment (white track).")]
        [SerializeField] private GameObject segmentPrefab;

        [Tooltip("Speed at which the track moves towards the player.")]
        [SerializeField] private float trackSpeed = 15.0f;

        [Tooltip("Length of each track segment along the Z axis.")]
        [SerializeField] private float segmentLength = 30.0f;

        [Tooltip("Number of track segments to pool and reuse.")]
        [SerializeField] private int segmentCount = 5;

        private List<GameObject> activeSegments = new List<GameObject>();
        private List<Vector3> initialPositions = new List<Vector3>();

        private void Start()
        {
            InitializePool();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameRestarted += ResetTrack;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameRestarted -= ResetTrack;
            }
        }

        private void Update()
        {
            // Only move track and check recycling if the game is in the Running state
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Running)
            {
                MoveTrack();
                CheckRecycle();
            }
        }

        /// <summary>
        /// Instantiates track segments and places them sequentially on the Z axis.
        /// One segment is placed behind the player to cover the camera view.
        /// </summary>
        private void InitializePool()
        {
            if (segmentPrefab == null)
            {
                Debug.LogError("TrackManager: Segment Prefab is not assigned!");
                return;
            }

            for (int i = 0; i < segmentCount; i++)
            {
                // Calculate position along the Z axis (start from i - 1 to place one segment behind the player)
                Vector3 spawnPosition = new Vector3(0, 0, (i - 1) * segmentLength);
                GameObject segmentInstance = Instantiate(segmentPrefab, spawnPosition, Quaternion.identity, transform);
                
                activeSegments.Add(segmentInstance);
                initialPositions.Add(spawnPosition);
            }
        }

        /// <summary>
        /// Moves all active segments backwards along the Z axis.
        /// </summary>
        private void MoveTrack()
        {
            float displacement = trackSpeed * Time.deltaTime;
            for (int i = 0; i < activeSegments.Count; i++)
            {
                activeSegments[i].transform.Translate(0, 0, -displacement);
            }
        }

        /// <summary>
        /// Checks if the oldest segment has moved completely past the camera
        /// and repositions it at the end of the track.
        /// </summary>
        private void CheckRecycle()
        {
            // Recycle threshold is behind the player and camera (e.g. Z < -2 * segmentLength)
            float recycleThreshold = -2.0f * segmentLength;

            for (int i = 0; i < activeSegments.Count; i++)
            {
                GameObject segment = activeSegments[i];
                if (segment.transform.position.z < recycleThreshold)
                {
                    // Move the segment to the end of the track.
                    // To prevent small gaps caused by frame rate changes, we add the total track length (segmentCount * segmentLength)
                    // to its current Z position instead of setting it to a hardcoded position.
                    float newZ = segment.transform.position.z + (segmentCount * segmentLength);
                    
                    segment.transform.position = new Vector3(
                        segment.transform.position.x, 
                        segment.transform.position.y, 
                        newZ
                    );
                }
            }
        }

        /// <summary>
        /// Resets all segments to their initial positions.
        /// </summary>
        public void ResetTrack()
        {
            for (int i = 0; i < activeSegments.Count; i++)
            {
                if (activeSegments[i] != null)
                {
                    activeSegments[i].transform.position = initialPositions[i];
                }
            }
        }
    }
}
