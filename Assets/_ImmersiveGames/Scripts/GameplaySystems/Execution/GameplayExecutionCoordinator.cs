using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;
using UnityEngine;

namespace _ImmersiveGames.Scripts.GameplaySystems.Execution
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-120)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayExecutionCoordinator : MonoBehaviour, IGameplayExecutionCoordinator
    {
        [Header("Diagnostics")]
        [SerializeField] private bool logStateChanges = true;

        [Tooltip("Se verdadeiro, registra automaticamente todos os GameplayExecutionParticipantBehaviour encontrados na cena.")]
        [SerializeField] private bool autoDiscoverParticipants = true;

        private IOldSimulationGateService _gate;
        private readonly HashSet<IGameplayExecutionParticipant> _participants = new();

        private bool _isExecutionAllowed = true;
        public bool IsExecutionAllowed => _isExecutionAllowed;

        private string _sceneName;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;

            // 1) Registrar Coordinator no DI por cena (scene-scoped)
            DependencyManager.Provider.RegisterForScene<IGameplayExecutionCoordinator>(_sceneName, this, allowOverride: true);

            // 2) Resolver Gate global
            if (!DependencyManager.Provider.TryGetGlobal<IOldSimulationGateService>(out _gate) || _gate == null)
            {
                DebugUtility.LogWarning<GameplayExecutionCoordinator>(
                    "IOldSimulationGateService não encontrado no DI global. Coordinator ficará inativo.",
                    this);
                return;
            }

            // 3) Inicializar estado com o estado atual do gate e assinar eventos
            ApplyGateState(_gate.IsOpen);
            _gate.GateChanged += OnGateChanged;

            if (logStateChanges)
            {
                DebugUtility.LogVerbose<GameplayExecutionCoordinator>(
                    $"Coordinator inicializado para a cena '{_sceneName}'. IsExecutionAllowed={_isExecutionAllowed}");
            }
        }

        private void Start()
        {
            if (_gate == null)
                return;

            if (autoDiscoverParticipants)
            {
                AutoDiscoverAndRegisterParticipants();
            }

            // Reaplica estado após registrar participantes para garantir consistência imediata.
            ApplyGateState(_gate.IsOpen, forceReapplyToParticipants: true);
        }

        private void OnDestroy()
        {
            if (_gate != null)
            {
                _gate.GateChanged -= OnGateChanged;
            }

            _participants.Clear();
        }

        public void Register(IGameplayExecutionParticipant participant)
        {
            if (participant == null)
                return;

            if (_participants.Add(participant))
            {
                participant.SetExecutionAllowed(_isExecutionAllowed);
            }
        }

        public void Unregister(IGameplayExecutionParticipant participant)
        {
            if (participant == null)
                return;

            _participants.Remove(participant);
        }

        private void OnGateChanged(bool isOpen)
        {
            ApplyGateState(isOpen);
        }

        private void ApplyGateState(bool isOpen, bool forceReapplyToParticipants = false)
        {
            bool allowed = isOpen;

            if (!forceReapplyToParticipants && _isExecutionAllowed == allowed)
                return;

            _isExecutionAllowed = allowed;

            foreach (var p in _participants)
            {
                try
                {
                    p?.SetExecutionAllowed(_isExecutionAllowed);
                }
                catch (Exception ex)
                {
                    DebugUtility.LogWarning<GameplayExecutionCoordinator>(
                        $"Falha ao aplicar ExecutionAllowed={_isExecutionAllowed} em participante. Ex={ex.Message}",
                        this);
                }
            }

            if (logStateChanges)
            {
                DebugUtility.Log<GameplayExecutionCoordinator>(
                    $"ExecutionAllowed => {_isExecutionAllowed}. Participants={_participants.Count}",
                    _isExecutionAllowed ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning,
                    this);
            }
        }

        private void AutoDiscoverAndRegisterParticipants()
        {
            // Busca somente dentro da mesma cena (evita pegar coisas da UIGlobalScene, etc.)
            var found = FindObjectsByType<GameplayExecutionParticipantBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            int registered = 0;

            for (int i = 0; i < found.Length; i++)
            {
                var p = found[i];
                if (p == null)
                    continue;

                if (p.gameObject.scene.name != _sceneName)
                    continue;

                Register(p);
                registered++;
            }

            if (logStateChanges)
            {
                DebugUtility.LogVerbose<GameplayExecutionCoordinator>(
                    $"AutoDiscover: encontrados {found.Length}, registrados {registered} participantes na cena '{_sceneName}'.");
            }
        }
    }
}

