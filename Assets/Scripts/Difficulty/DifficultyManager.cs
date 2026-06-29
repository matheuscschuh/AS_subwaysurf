using System;
using UnityEngine;
using InfiniteRunner.Core;

namespace InfiniteRunner.Difficulty
{
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        [Header("Difficulty Configuration")]
        [SerializeField] private DifficultySettings settings = new DifficultySettings();

        // Tracker variables
        private float survivalTime;
        private float distanceTravelled;
        private float maxRawDifficulty = 1.0f;

        private IDifficultyCalculator calculator;

        // Public properties (outputs for other systems)
        public int DifficultyLevel { get; private set; } = 1;
        public float CurrentSpeed { get; private set; }
        public float CurrentSpawnDistance { get; private set; }
        
        public float RawDifficulty => maxRawDifficulty;
        public float SurvivalTime => survivalTime;
        public float DistanceTravelled => distanceTravelled;

        // Events for other systems to react to difficulty changes
        public event Action<int> OnDifficultyLevelChanged;

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            calculator = new DefaultDifficultyCalculator();
            ResetDifficulty();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameRestarted += ResetDifficulty;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameRestarted -= ResetDifficulty;
            }
        }

        private void Update()
        {
            // Only progress difficulty if the game is running
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Running)
            {
                // Acompanhar o tempo de sobrevivência
                survivalTime += Time.deltaTime;

                // Acompanhar a distância percorrida
                distanceTravelled += CurrentSpeed * Time.deltaTime;

                // Atualizar a dificuldade e os parâmetros derivados
                UpdateDifficultyMetrics();
            }
        }

        /// <summary>
        /// Recalculates difficulty values using the active calculator strategy.
        /// </summary>
        private void UpdateDifficultyMetrics()
        {
            if (calculator == null) return;

            float calculatedRaw = calculator.CalculateRawDifficulty(survivalTime, distanceTravelled, settings);

            // Regra 2: Difficulty nunca diminui.
            if (calculatedRaw > maxRawDifficulty)
            {
                maxRawDifficulty = calculatedRaw;
            }

            int previousLevel = DifficultyLevel;
            
            // Calculate outputs
            DifficultyLevel = calculator.CalculateDifficultyLevel(maxRawDifficulty);
            CurrentSpeed = calculator.CalculateSpeed(maxRawDifficulty, settings);
            CurrentSpawnDistance = calculator.CalculateSpawnDistance(maxRawDifficulty, settings);

            // Fire event if level increased
            if (DifficultyLevel > previousLevel)
            {
                OnDifficultyLevelChanged?.Invoke(DifficultyLevel);
            }
        }

        /// <summary>
        /// Resets all values to the start values. Triggered by game reset.
        /// </summary>
        public void ResetDifficulty()
        {
            survivalTime = 0.0f;
            distanceTravelled = 0.0f;
            maxRawDifficulty = 1.0f;

            if (calculator != null)
            {
                DifficultyLevel = calculator.CalculateDifficultyLevel(maxRawDifficulty);
                CurrentSpeed = calculator.CalculateSpeed(maxRawDifficulty, settings);
                CurrentSpawnDistance = calculator.CalculateSpawnDistance(maxRawDifficulty, settings);
            }
            else
            {
                DifficultyLevel = 1;
                CurrentSpeed = settings.initialSpeed;
                CurrentSpawnDistance = settings.initialSpawnDistance;
            }
        }

        /// <summary>
        /// Swaps the difficulty calculation formula dynamically at runtime.
        /// </summary>
        public void SetCalculator(IDifficultyCalculator newCalculator)
        {
            if (newCalculator != null)
            {
                calculator = newCalculator;
                UpdateDifficultyMetrics();
            }
        }

        /// <summary>
        /// Exposes settings for inspector configuration or external components.
        /// </summary>
        public DifficultySettings Settings => settings;
    }
}
