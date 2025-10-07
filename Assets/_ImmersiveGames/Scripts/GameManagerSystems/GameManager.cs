using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.System;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.LoaderSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.ScriptableObjects;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityUtils;

namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    [DefaultExecutionOrder(-101), DebugLevel(DebugLevel.Verbose)]
    public sealed class GameManager : Singleton<GameManager>, IGameManager
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private Transform worldEater;
        
        [SerializeField] private SoundData mainMenuBGM;
        [Header("Settings")]
        [SerializeField] private float bgmFadeDuration = 2f;
        public GameConfig GameConfig => gameConfig;
        public Transform WorldEater => worldEater;

        private EventBinding<GameStartEvent> _gameStartEvent;
        private IActorResourceOrchestrator _orchestrator;
        protected override void Awake()
        {
            base.Awake();
            if (!DependencyManager.Instance.TryGetGlobal(out _orchestrator))
            {
                _orchestrator = new ActorResourceOrchestratorService();
                DependencyManager.Instance.RegisterGlobal<IActorResourceOrchestrator>(_orchestrator);
            }
            if (!SceneManager.GetSceneByName("UI").isLoaded)
            {
                DebugUtility.LogVerbose<GameManager>($"Carregando cena de UI em modo aditivo.");
                SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
            }
            Initialize();
        }

        private void Start()
        {
            AudioSystemHelper.PlayBGM(mainMenuBGM, loop: true, bgmFadeDuration);
        }

        private void Initialize()
        {
            // Inicializa a FSM
            GameManagerStateMachine.Instance.InitializeStateMachine(this);

            // Registra listener para GameStart
            _gameStartEvent = new EventBinding<GameStartEvent>(OnGameStart);
            EventBus<GameStartEvent>.Register(_gameStartEvent);

            DebugUtility.LogVerbose<GameManager>("GameManager inicializado.");
        }
        // Novo: Centralizar inicialização
        public void InitializeBindings(IEnumerable<ResourceSystem> actors, IEnumerable<CanvasResourceBinder> canvases)
        {
            foreach (var actor in actors)
            {
                if (!_orchestrator.IsActorRegistered(actor.EntityId))
                    _orchestrator.RegisterActor(actor);
            }

            foreach (var canvas in canvases)
            {
                _orchestrator.RegisterCanvas(canvas);
            }

            DebugUtility.LogVerbose<GameManager>("Inicialização de bindings concluída");
        }

        private void OnDestroy()
        {
            EventBus<GameStartEvent>.Unregister(_gameStartEvent);
        }

        public bool IsGameActive()
        {
            return GameManagerStateMachine.Instance.CurrentState?.IsGameActive() ?? false;
        }

        public void ResetGame()
        {
            DebugUtility.LogVerbose<GameManager>("Resetando o jogo.");
            GameManagerStateMachine.Instance.InitializeStateMachine(this); // Reinicializa a FSM
            SceneLoader.Instance.ReloadCurrentScene();
            EventBus<GameStartEvent>.Raise(new GameStartEvent());
        }

        private void OnGameStart(GameStartEvent evt)
        {
            DebugUtility.LogVerbose<GameManager>("Evento de início de jogo recebido.");
            // Pode ser usado para inicializar sistemas adicionais
        }
    }
}