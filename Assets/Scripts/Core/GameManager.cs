using System;
using UnityEngine;

namespace InfiniteRunner.Core
{
    public enum GameState
    {
        Stopped,
        Running
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State Settings")]
        [SerializeField] private GameState initialState = GameState.Stopped;

        private GameState currentState;

        // Events for decoupled communication
        public event Action<GameState> OnStateChanged;
        public event Action OnGameRestarted;

        public GameState CurrentState => currentState;

        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SetState(initialState);
        }

        /// <summary>
        /// Changes the active game state and notifies subscribers.
        /// </summary>
        public void SetState(GameState newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(currentState);
        }

        /// <summary>
        /// Sets state to Running.
        /// </summary>
        public void StartGame()
        {
            SetState(GameState.Running);
        }

        /// <summary>
        /// Sets state to Stopped.
        /// </summary>
        public void StopGame()
        {
            SetState(GameState.Stopped);
        }

        /// <summary>
        /// Resets the game to its initial state (Stopped) and requests other managers to reset.
        /// </summary>
        public void RestartGame()
        {
            SetState(GameState.Stopped);
            OnGameRestarted?.Invoke();
        }
    }
}
