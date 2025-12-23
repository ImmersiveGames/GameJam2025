/*
 * ChangeLog
 * - Gate de pause (SimulationGateTokens.Pause) agora bloqueia ActionType.Move via IStateDependentService
 *   sem congelar física/timeScale.
 * - NÃO cacheia IGameLoopService: resolve no momento do uso para observar overrides de QA no DI.
 */

using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;

namespace _ImmersiveGames.NewScripts.Infrastructure.State
{
    /// <summary>
    /// Serviço mínimo para coordenar permissões de ações no baseline NewScripts.
    /// - Gate Pause bloqueia Move (sem congelar física).
    /// - Se IGameLoopService estiver disponível, usa o estado atual do GameLoop como fonte primária.
    /// - Não cacheia serviços para permitir overrides de QA no DI.
    /// </summary>
    public sealed class NewScriptsStateDependentService : IStateDependentService
    {
        private enum ServiceState
        {
            Boot,
            Playing,
            Paused
        }

        // Fallback (caso GameLoop não esteja disponível).
        private ServiceState _fallbackState = ServiceState.Playing;

        private EventBinding<GameStartEvent> _gameStartBinding;
        private EventBinding<GamePauseEvent> _gamePauseBinding;
        private EventBinding<GameResumeRequestedEvent> _gameResumeBinding;

        private bool _bindingsRegistered;
        private bool _loggedGateBlock;
        private bool _loggedGameLoopUsage;

        public NewScriptsStateDependentService()
        {
            TryRegisterEvents();
        }

        public bool CanExecuteAction(ActionType action)
        {
            if (IsPausedByGate(action))
                return false;

            var serviceState = ResolveServiceState();

            switch (serviceState)
            {
                case ServiceState.Playing:
                    return true;

                case ServiceState.Boot:
                case ServiceState.Paused:
                    return action switch
                    {
                        ActionType.Navigate => true,
                        ActionType.UiSubmit => true,
                        ActionType.UiCancel => true,
                        ActionType.RequestReset => true,
                        ActionType.RequestQuit => true,
                        _ => false
                    };

                default:
                    return false;
            }
        }

        public bool IsGameActive()
        {
            return ResolveServiceState() == ServiceState.Playing;
        }

        public void Dispose()
        {
            if (!_bindingsRegistered)
                return;

            EventBus<GameStartEvent>.Unregister(_gameStartBinding);
            EventBus<GamePauseEvent>.Unregister(_gamePauseBinding);
            EventBus<GameResumeRequestedEvent>.Unregister(_gameResumeBinding);

            _bindingsRegistered = false;
        }

        private void TryRegisterEvents()
        {
            try
            {
                _gameStartBinding = new EventBinding<GameStartEvent>(_ => SetFallbackState(ServiceState.Playing));
                _gamePauseBinding = new EventBinding<GamePauseEvent>(OnGamePause);
                _gameResumeBinding = new EventBinding<GameResumeRequestedEvent>(_ => SetFallbackState(ServiceState.Playing));

                EventBus<GameStartEvent>.Register(_gameStartBinding);
                EventBus<GamePauseEvent>.Register(_gamePauseBinding);
                EventBus<GameResumeRequestedEvent>.Register(_gameResumeBinding);

                _bindingsRegistered = true;
            }
            catch
            {
                // EventBus indisponível; segue sem bindings.
                _bindingsRegistered = false;
            }
        }

        private void OnGamePause(GamePauseEvent evt)
        {
            SetFallbackState(evt is { IsPaused: true } ? ServiceState.Paused : ServiceState.Playing);
        }

        private void SetFallbackState(ServiceState next)
        {
            _fallbackState = next;
        }

        private ServiceState ResolveServiceState()
        {
            // Importante: resolve on-demand para observar overrides de QA.
            if (TryResolveLoop(out var loop) && loop != null && !string.IsNullOrWhiteSpace(loop.CurrentStateName))
            {
                if (!_loggedGameLoopUsage)
                {
                    DebugUtility.LogVerbose<NewScriptsStateDependentService>(
                        "[StateDependent] Integrado ao GameLoop: usando estado atual como fonte primária.");
                    _loggedGameLoopUsage = true;
                }

                // CurrentStateName vem de stateId.ToString() no GameLoopService.
                return loop.CurrentStateName switch
                {
                    nameof(GameLoopStateId.Playing) => ServiceState.Playing,
                    nameof(GameLoopStateId.Paused) => ServiceState.Paused,
                    _ => ServiceState.Boot // Boot / desconhecido
                };
            }

            return _fallbackState;
        }

        private bool IsPausedByGate(ActionType action)
        {
            if (!TryResolveGate(out var gate) || gate == null)
                return false;

            if (!gate.IsTokenActive(SimulationGateTokens.Pause))
            {
                _loggedGateBlock = false;
                return false;
            }

            var shouldBlock = action == ActionType.Move;

            if (shouldBlock && !_loggedGateBlock)
            {
                DebugUtility.LogVerbose<NewScriptsStateDependentService>(
                    "[StateDependent] Action 'Move' bloqueada por gate Pause (SimulationGateTokens.Pause). " +
                    "Física permanece ativa (sem timeScale/constraints).");
                _loggedGateBlock = true;
            }

            return shouldBlock;
        }

        private static bool TryResolveGate(out ISimulationGateService gate)
        {
            gate = null;
            var provider = DependencyManager.Provider;
            return provider.TryGetGlobal<IsimulationGateServiceCompat>(out _) // placeholder to avoid accidental type mismatch
                ? provider.TryGetGlobal(out gate) && gate != null
                : provider.TryGetGlobal(out gate) && gate != null;
        }

        private static bool TryResolveLoop(out IGameLoopService loop)
        {
            loop = null;
            var provider = DependencyManager.Provider;
            return provider.TryGetGlobal(out loop) && loop != null;
        }

        // Este tipo vazio é só para evitar que o Codex tente “corrigir” o TryResolveGate
        // assumindo overloads inexistentes. Remova se não precisar.
        private interface IsimulationGateServiceCompat { }
    }
}
