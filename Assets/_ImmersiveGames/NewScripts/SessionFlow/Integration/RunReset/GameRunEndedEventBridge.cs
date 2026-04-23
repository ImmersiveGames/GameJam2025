using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Ownership;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset
{
    public interface IRunContinuationOperationalHandoffService
    {
        Task DispatchAsync(RunContinuationSelection selection);
    }

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

        private void Awake()
        {
            _runEndMaterializationService = ResolveRequired<IRunEndMaterializationService>(
                "[FATAL][Config][GameplaySessionFlow] IRunEndMaterializationService ausente no DI global antes de compor GameRunEndedEventBridge.");
            _runContinuationSelectionRoutingService = ResolveRequired<IRunContinuationSelectionRoutingService>(
                "[FATAL][Config][GameplaySessionFlow] IRunContinuationSelectionRoutingService ausente no DI global antes de compor GameRunEndedEventBridge.");
            _runContinuationOwnershipService = ResolveRequired<IRunContinuationOwnershipService>(
                "[FATAL][Config][GameplaySessionFlow] IRunContinuationOwnershipService ausente no DI global antes de compor GameRunEndedEventBridge.");

            _binding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);
            _runContinuationSelectionResolvedBinding = new EventBinding<RunContinuationSelectionResolvedEvent>(OnRunContinuationSelectionResolved);
            RegisterBinding();
        }

        private void OnEnable() => RegisterBinding();

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

        private static T ResolveRequired<T>(string errorMessage)
            where T : class
        {
            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                throw new InvalidOperationException(errorMessage);
            }

            return service;
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
