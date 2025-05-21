using System;
using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.EnemySystem;
using _ImmersiveGames.Scripts.ScriptableObjects;
using _ImmersiveGames.Scripts.StateMachine;
using _ImmersiveGames.Scripts.StateMachine.GameStates;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityUtils;
using Random = UnityEngine.Random;

namespace _ImmersiveGames.Scripts
{
    [DefaultExecutionOrder(-100), DebugLevel(DebugLevel.Verbose)]
    public sealed class GameManager : Singleton<GameManager>
    {
        [SerializeField] public GameConfigSo gameConfig;
        
        
        public string Score { get; private set; }
        private bool _gameInitialized;
        
        public event Action EventStartGame;
        public event Action EventGameOver;
        public event Action EventVictory;
        public event Action<bool> EventPauseGame;

        protected override void Awake()
        {
            base.Awake();
            // Inicializa o gerenciador de estados
            GameManagerStateMachine.Instance.InitializeStateMachine(this);
        }
        
       public bool ShouldGameStart() => _gameInitialized;

       /// <summary>
        /// Inicia o jogo e faz o spawn dos planetas
        /// </summary>
        public void StartGame()
        {
            OnEventStartGame();
            _gameInitialized = true;
        }

        internal bool CheckGameOver()
        {
            // Implementar lógica de game over
            return false;
        }

        internal bool CheckVictory()
        {
            // Implementar lógica de vitória

            return false;
        }
        private void OnEventStartGame()
        {
            DebugUtility.LogVerbose<GameManager>($"Iniciou o jogo");
            EventStartGame?.Invoke();
        }
        public void OnEventGameOver()
        {
            DebugUtility.LogVerbose<GameManager>($"Terminou o jogo");
            EventGameOver?.Invoke();
        }
        public void OnEventVictory()
        {
            DebugUtility.LogVerbose<GameManager>($"Venceu o jogo");
            EventVictory?.Invoke();
        }
        public void OnEventPauseGame(bool pause)
        {
            DebugUtility.LogVerbose<GameManager>($"Jogo pausado: {pause}");
            EventPauseGame?.Invoke(pause);
        }
    }
}
