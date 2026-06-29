#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using InfiniteRunner.Difficulty;

namespace InfiniteRunner.Tests
{
    public static class DifficultyTests
    {
        private static DifficultySettings CreateTestSettings()
        {
            return new DifficultySettings
            {
                initialSpeed = 10f,
                maxSpeed = 20f,
                speedIncrementPerLevel = 2f,
                initialSpawnDistance = 30f,
                minSpawnDistance = 10f,
                spawnDistanceDecrementPerLevel = 5f,
                timeNeededToEvolve = 10f,
                distanceNeededToEvolve = 100f
            };
        }

        [MenuItem("Infinite Runner/Run Difficulty Tests")]
        public static void RunTests()
        {
            Debug.Log("── Starting Difficulty System Tests ──");
            
            Test_CA001_InitialDifficulty_IsOne();
            Test_CA002_Difficulty_IncreasesWithProgress();
            Test_CA003_CurrentSpeed_IncreasesWithDifficulty();
            Test_CA003_Speed_ClampsToMaxSpeed();
            Test_CA004_SpawnDistance_DecreasesWithDifficulty();
            Test_CA004_SpawnDistance_ClampsToMinDistance();
            Test_Rule5_Calculations_AreDeterministic();

            Debug.Log("── All Difficulty System Tests Passed Successfully! ──");
        }

        private static void Test_CA001_InitialDifficulty_IsOne()
        {
            var settings = CreateTestSettings();
            var calculator = new DefaultDifficultyCalculator();

            float raw = calculator.CalculateRawDifficulty(0f, 0f, settings);
            int level = calculator.CalculateDifficultyLevel(raw);

            Debug.Assert(level == 1, $"[CA-001] Difficulty level must be 1 initially. Got {level}");
        }

        private static void Test_CA002_Difficulty_IncreasesWithProgress()
        {
            var settings = CreateTestSettings();
            var calculator = new DefaultDifficultyCalculator();

            // Progress in time only
            float rawTime = calculator.CalculateRawDifficulty(10f, 0f, settings);
            int levelTime = calculator.CalculateDifficultyLevel(rawTime);
            Debug.Assert(levelTime == 2, $"[CA-002] Level should be 2 after enough time. Got {levelTime}");

            // Progress in distance only
            float rawDistance = calculator.CalculateRawDifficulty(0f, 100f, settings);
            int levelDistance = calculator.CalculateDifficultyLevel(rawDistance);
            Debug.Assert(levelDistance == 2, $"[CA-002] Level should be 2 after enough distance. Got {levelDistance}");

            // Combined progress
            float rawCombined = calculator.CalculateRawDifficulty(10f, 100f, settings);
            int levelCombined = calculator.CalculateDifficultyLevel(rawCombined);
            Debug.Assert(levelCombined == 3, $"[CA-002] Combined level should be 3. Got {levelCombined}");
        }

        private static void Test_CA003_CurrentSpeed_IncreasesWithDifficulty()
        {
            var settings = CreateTestSettings();
            var calculator = new DefaultDifficultyCalculator();

            float rawInitial = calculator.CalculateRawDifficulty(0f, 0f, settings);
            float speedInitial = calculator.CalculateSpeed(rawInitial, settings);

            float rawHigh = calculator.CalculateRawDifficulty(20f, 0f, settings);
            float speedHigh = calculator.CalculateSpeed(rawHigh, settings);

            Debug.Assert(speedInitial == settings.initialSpeed, $"[CA-003] Initial speed must match. Got {speedInitial}");
            Debug.Assert(speedHigh > speedInitial, $"[CA-003] High difficulty speed must be greater. Got {speedHigh}");
            Debug.Assert(speedHigh == 14f, $"[CA-003] Speed at level 3 should be 14. Got {speedHigh}");
        }

        private static void Test_CA003_Speed_ClampsToMaxSpeed()
        {
            var settings = CreateTestSettings();
            var calculator = new DefaultDifficultyCalculator();

            float rawExtreme = calculator.CalculateRawDifficulty(100f, 1000f, settings);
            float speedExtreme = calculator.CalculateSpeed(rawExtreme, settings);

            Debug.Assert(speedExtreme == settings.maxSpeed, $"[CA-003] Speed must clamp to max. Got {speedExtreme}");
        }

        private static void Test_CA004_SpawnDistance_DecreasesWithDifficulty()
        {
            var settings = CreateTestSettings();
            var calculator = new DefaultDifficultyCalculator();

            float rawInitial = calculator.CalculateRawDifficulty(0f, 0f, settings);
            float spawnInitial = calculator.CalculateSpawnDistance(rawInitial, settings);

            float rawHigh = calculator.CalculateRawDifficulty(20f, 0f, settings);
            float spawnHigh = calculator.CalculateSpawnDistance(rawHigh, settings);

            Debug.Assert(spawnInitial == settings.initialSpawnDistance, $"[CA-004] Initial spawn distance must match. Got {spawnInitial}");
            Debug.Assert(spawnHigh < spawnInitial, $"[CA-004] High difficulty spawn distance must be smaller. Got {spawnHigh}");
            Debug.Assert(spawnHigh == 20f, $"[CA-004] Spawn distance at level 3 should be 20. Got {spawnHigh}");
        }

        private static void Test_CA004_SpawnDistance_ClampsToMinDistance()
        {
            var settings = CreateTestSettings();
            var calculator = new DefaultDifficultyCalculator();

            float rawExtreme = calculator.CalculateRawDifficulty(100f, 1000f, settings);
            float spawnExtreme = calculator.CalculateSpawnDistance(rawExtreme, settings);

            Debug.Assert(spawnExtreme == settings.minSpawnDistance, $"[CA-004] Spawn distance must clamp to min. Got {spawnExtreme}");
        }

        private static void Test_Rule5_Calculations_AreDeterministic()
        {
            var settings = CreateTestSettings();
            var calculator = new DefaultDifficultyCalculator();

            float raw1 = calculator.CalculateRawDifficulty(5.5f, 55f, settings);
            float speed1 = calculator.CalculateSpeed(raw1, settings);
            float spawn1 = calculator.CalculateSpawnDistance(raw1, settings);

            float raw2 = calculator.CalculateRawDifficulty(5.5f, 55f, settings);
            float speed2 = calculator.CalculateSpeed(raw2, settings);
            float spawn2 = calculator.CalculateSpawnDistance(raw2, settings);

            Debug.Assert(raw1 == raw2, "[Rule 5] Raw difficulty calculation is not deterministic.");
            Debug.Assert(speed1 == speed2, "[Rule 5] Speed calculation is not deterministic.");
            Debug.Assert(spawn1 == spawn2, "[Rule 5] Spawn distance calculation is not deterministic.");
        }
    }
}
#endif
