using UnityEngine;

namespace InfiniteRunner.Difficulty
{
    public class DefaultDifficultyCalculator : IDifficultyCalculator
    {
        public float CalculateRawDifficulty(float survivalTime, float distanceTravelled, DifficultySettings settings)
        {
            float timeProgress = settings.timeNeededToEvolve > 0f ? (survivalTime / settings.timeNeededToEvolve) : 0f;
            float distanceProgress = settings.distanceNeededToEvolve > 0f ? (distanceTravelled / settings.distanceNeededToEvolve) : 0f;

            // Difficulty starts at 1.0. Time and distance progression combine to increase difficulty.
            return 1.0f + timeProgress + distanceProgress;
        }

        public int CalculateDifficultyLevel(float rawDifficulty)
        {
            // Floor of the raw difficulty, ensuring it starts at 1 and increases in integer steps.
            return Mathf.Max(1, Mathf.FloorToInt(rawDifficulty));
        }

        public float CalculateSpeed(float rawDifficulty, DifficultySettings settings)
        {
            float difficultyProgress = rawDifficulty - 1.0f;
            float calculatedSpeed = settings.initialSpeed + (difficultyProgress * settings.speedIncrementPerLevel);

            // Regra 3: CurrentSpeed aumenta conforme a dificuldade (clamped to maxSpeed).
            return Mathf.Min(calculatedSpeed, settings.maxSpeed);
        }

        public float CalculateSpawnDistance(float rawDifficulty, DifficultySettings settings)
        {
            float difficultyProgress = rawDifficulty - 1.0f;
            float calculatedSpawnDistance = settings.initialSpawnDistance - (difficultyProgress * settings.spawnDistanceDecrementPerLevel);

            // Regra 4: SpawnDistance diminui conforme a dificuldade (never less than minSpawnDistance).
            return Mathf.Max(calculatedSpawnDistance, settings.minSpawnDistance);
        }
    }
}
