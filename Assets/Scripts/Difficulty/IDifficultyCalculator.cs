namespace InfiniteRunner.Difficulty
{
    public interface IDifficultyCalculator
    {
        /// <summary>
        /// Calculates the continuous raw difficulty value based on survival time and distance.
        /// </summary>
        float CalculateRawDifficulty(float survivalTime, float distanceTravelled, DifficultySettings settings);

        /// <summary>
        /// Converts the raw difficulty value to an integer difficulty level.
        /// </summary>
        int CalculateDifficultyLevel(float rawDifficulty);

        /// <summary>
        /// Calculates the movement speed based on the raw difficulty and settings.
        /// </summary>
        float CalculateSpeed(float rawDifficulty, DifficultySettings settings);

        /// <summary>
        /// Calculates the spawn distance based on the raw difficulty and settings.
        /// </summary>
        float CalculateSpawnDistance(float rawDifficulty, DifficultySettings settings);
    }
}
