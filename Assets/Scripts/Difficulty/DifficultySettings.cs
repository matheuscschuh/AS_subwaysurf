using System;
using UnityEngine;

namespace InfiniteRunner.Difficulty
{
    [Serializable]
    public class DifficultySettings
    {
        [Header("Speed Settings")]
        [Tooltip("Initial movement speed of the game.")]
        public float initialSpeed = 15.0f;

        [Tooltip("Maximum movement speed the game can reach.")]
        public float maxSpeed = 30.0f;

        [Tooltip("How much the speed increases per difficulty level.")]
        public float speedIncrementPerLevel = 0.5f;

        [Header("Spawn Settings")]
        [Tooltip("Initial spawn distance between obstacle patterns.")]
        public float initialSpawnDistance = 20.0f;

        [Tooltip("Minimum spawn distance between obstacle patterns.")]
        public float minSpawnDistance = 10.0f;

        [Tooltip("How much the spawn distance decreases per difficulty level.")]
        public float spawnDistanceDecrementPerLevel = 0.5f;

        [Header("Evolution Settings")]
        [Tooltip("Survival time (in seconds) required to increase difficulty by 1 level.")]
        public float timeNeededToEvolve = 10.0f;

        [Tooltip("Distance travelled (in meters) required to increase difficulty by 1 level.")]
        public float distanceNeededToEvolve = 100.0f;
    }
}
