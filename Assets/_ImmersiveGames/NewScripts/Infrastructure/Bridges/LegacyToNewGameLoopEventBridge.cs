using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using LegacyGamePauseEvent = _ImmersiveGames.Scripts.GameManagerSystems.Events.GamePauseEvent;
using LegacyGameResetRequestedEvent = _ImmersiveGames.Scripts.GameManagerSystems.Events.GameResetRequestedEvent;
using LegacyGameResumeRequestedEvent = _ImmersiveGames.Scripts.GameManagerSystems.Events.GameResumeRequestedEvent;
using LegacyGameStartEvent = _ImmersiveGames.Scripts.GameManagerSystems.Events.GameStartEvent;
using LegacyEventBinding = _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBinding;
using LegacyEventBus = _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBus;

namespace _ImmersiveGames.NewScripts.Infrastructure.Bridges
{
    /// <summary>
    /// Bridge de compatibilidade: traduz eventos legados de GameLoop para os eventos NewScripts.
    /// Não publica eventos de volta para evitar loops.
    /// </summary>
    public sealed class LegacyToNewGameLoopEventBridge : IDisposable
    {
        private LegacyEventBinding<LegacyGameStartEvent> _legacyStartBinding;
        private LegacyEventBinding<LegacyGamePauseEvent> _legacyPauseBinding;
        private LegacyEventBinding<LegacyGameResumeRequestedEvent> _legacyResumeBinding;
        private LegacyEventBinding<LegacyGameResetRequestedEvent> _legacyResetBinding;

        private bool _registered;
        private bool _disposed;

        public LegacyToNewGameLoopEventBridge()
        {
            TryRegisterLegacyBindings();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            if (!_registered)
            {
                return;
            }

            LegacyEventBus<LegacyGameStartEvent>.Unregister(_legacyStartBinding);
            LegacyEventBus<LegacyGamePauseEvent>.Unregister(_legacyPauseBinding);
            LegacyEventBus<LegacyGameResumeRequestedEvent>.Unregister(_legacyResumeBinding);
            LegacyEventBus<LegacyGameResetRequestedEvent>.Unregister(_legacyResetBinding);

            _registered = false;
        }

        private void TryRegisterLegacyBindings()
        {
            try
            {
                _legacyStartBinding = new LegacyEventBinding<LegacyGameStartEvent>(_ => EventBus<GameStartEvent>.Raise(new GameStartEvent()));
                _legacyPauseBinding = new LegacyEventBinding<LegacyGamePauseEvent>(OnLegacyPause);
                _legacyResumeBinding = new LegacyEventBinding<LegacyGameResumeRequestedEvent>(_ => EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent()));
                _legacyResetBinding = new LegacyEventBinding<LegacyGameResetRequestedEvent>(_ => EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent()));

                LegacyEventBus<LegacyGameStartEvent>.Register(_legacyStartBinding);
                LegacyEventBus<LegacyGamePauseEvent>.Register(_legacyPauseBinding);
                LegacyEventBus<LegacyGameResumeRequestedEvent>.Register(_legacyResumeBinding);
                LegacyEventBus<LegacyGameResetRequestedEvent>.Register(_legacyResetBinding);

                _registered = true;
                DebugUtility.LogVerbose<LegacyToNewGameLoopEventBridge>("[GameLoopBridge] Registrado para traduzir eventos legados → NewScripts.");
            }
            catch (Exception ex)
            {
                _registered = false;
                DebugUtility.LogWarning<LegacyToNewGameLoopEventBridge>(
                    $"[GameLoopBridge] Falha ao registrar bridge de eventos legados (seguirá inativo): {ex}");
            }
        }

        private static void OnLegacyPause(LegacyGamePauseEvent legacyEvent)
        {
            bool isPaused = legacyEvent != null && legacyEvent.IsPaused;
            EventBus<GamePauseEvent>.Raise(new GamePauseEvent(isPaused));
        }
    }
}
