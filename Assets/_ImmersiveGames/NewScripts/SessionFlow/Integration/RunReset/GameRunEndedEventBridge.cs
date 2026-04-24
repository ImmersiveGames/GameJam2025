using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset.Installers;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Ownership;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset
{
    public interface IRunContinuationOperationalHandoffService
    {
        Task DispatchAsync(RunContinuationSelection selection);
    }

    /// <summary>
    /// Bridge que reage a eventos de fim de run e coordena transição pós-run.
    ///
    /// ⚠️ INICIALIZAÇÃO OBRIGATÓRIA:
    /// - NÃO inicializa automaticamente em Awake
    /// - DEVE ser inicializado explicitamente via Initialize(...)
    /// - Validação: se Initialize() não foi chamado, Awake() lança InvalidOperationException
    ///
    /// DEPENDÊNCIAS (recebidas via Initialize, não resolvidas do DI global):
    /// - IRunEndMaterializationService
    /// - IRunContinuationSelectionRoutingService
    /// - IRunContinuationOwnershipService
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunEndedEventBridge : MonoBehaviour
    {
        private IRunEndMaterializationService _runEndMaterializationService;
        private IRunContinuationSelectionRoutingService _runContinuationSelectionRoutingService;
        private IRunContinuationOwnershipService _runContinuationOwnershipService;
        private EventBinding<GameRunEndedEvent> _binding;
        private EventBinding<GameRunStartedEvent> _runStartedBinding;
        private EventBinding<RunContinuationSelectionResolvedEvent> _runContinuationSelectionResolvedBinding;
        private bool _registered;
        private bool _postStagePending;
        private bool _initialized;

        /// <summary>
        /// ✅ Explicit initialization with all dependencies.
        /// Must be called after RunEndBridgeRuntimeComposer.ComposeOrFail() and before activation.
        /// </summary>
        public void Initialize(
            IRunEndMaterializationService runEndMaterializationService,
            IRunContinuationSelectionRoutingService runContinuationSelectionRoutingService,
            IRunContinuationOwnershipService runContinuationOwnershipService)
        {
            if (runEndMaterializationService == null)
                throw new ArgumentNullException(nameof(runEndMaterializationService));
            if (runContinuationSelectionRoutingService == null)
                throw new ArgumentNullException(nameof(runContinuationSelectionRoutingService));
            if (runContinuationOwnershipService == null)
                throw new ArgumentNullException(nameof(runContinuationOwnershipService));

            _runEndMaterializationService = runEndMaterializationService;
            _runContinuationSelectionRoutingService = runContinuationSelectionRoutingService;
            _runContinuationOwnershipService = runContinuationOwnershipService;

            _binding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);
            _runContinuationSelectionResolvedBinding = new EventBinding<RunContinuationSelectionResolvedEvent>(OnRunContinuationSelectionResolved);

            _initialized = true;
        }

        private void Awake()
        {
            if (!RunEndBridgeRuntimeComposer.IsComposed)
            {
                throw new InvalidOperationException(
                    "[FATAL][Config][GameplaySessionFlow] GameRunEndedEventBridge requires RunEndBridgeRuntimeComposer.ComposeOrFail() before the component is instantiated.");
            }
        }

        private void OnEnable()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException(
                    "[FATAL][Config][GameplaySessionFlow] GameRunEndedEventBridge activated before explicit initialization.");
            }

            RegisterBinding();
        }

        private void OnDisable() => UnregisterBinding();

        private void OnDestroy() => UnregisterBinding();

        private void RegisterBinding()
        {
            if (_registered)
            {
                return;
            }

            EventBus<GameRunEndedEvent>.Register(_binding);
            EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
            EventBus<RunContinuationSelectionResolvedEvent>.Register(_runContinuationSelectionResolvedBinding);
            _registered = true;
        }

        private void UnregisterBinding()
        {
            if (!_registered)
            {
                return;
            }

            EventBus<GameRunEndedEvent>.Unregister(_binding);
            EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
            EventBus<RunContinuationSelectionResolvedEvent>.Unregister(_runContinuationSelectionResolvedBinding);
            _registered = false;
        }

        private void OnGameRunEnded(GameRunEndedEvent evt)
        {
            if (_postStagePending)
            {
                DebugUtility.LogVerbose<GameRunEndedEventBridge>(
                    "[OBS][GameplaySessionFlow][Operational] ExitStageRunEndIgnored reason='already_pending'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _postStagePending = true;
            HandleGameRunEnded(evt);
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            _postStagePending = false;
            _runContinuationOwnershipService?.ClearCurrentContext("GameRunStarted");
        }

        private void OnRunContinuationSelectionResolved(RunContinuationSelectionResolvedEvent evt)
        {
            if (!evt.Selection.IsValid)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    "[FATAL][GameplaySessionFlow] RunContinuationSelection recebida com estado invalido.");
                return;
            }

            DebugUtility.Log<GameRunEndedEventBridge>(
                $"[OBS][GameplaySessionFlow][RunDecision] RunContinuationSelectionResolved continuation='{evt.Selection.SelectedContinuation}' reason='{evt.Selection.Reason}' nextState='{evt.Selection.NextState}'.",
                DebugUtility.Colors.Info);

            _runContinuationSelectionRoutingService.RouteSelection(evt.Selection);
        }

        private void HandleGameRunEnded(GameRunEndedEvent evt)
        {
            try
            {
                _runEndMaterializationService.MaterializeAndDispatch(evt, nameof(GameRunEndedEventBridge));
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    $"[FATAL][GameplaySessionFlow] Falha inesperada ao executar RunResultStage. ex='{ex.GetType().Name}: {ex.Message}'.");
                _postStagePending = false;
            }
        }

    }

    public interface IRunContinuationSelectionRoutingService
    {
        void RouteSelection(RunContinuationSelection selection);
    }

    public interface IRunEndMaterializationService
    {
        void MaterializeAndDispatch(GameRunEndedEvent evt, string source);
    }
}
