/*
 * ChangeLog
 * - Ponte de pause: converte GamePauseCommandEvent/GameResumeRequestedEvent em gate SimulationGateTokens.Pause sem congelar fisica.
 * - Improvement: resolve gate sob demanda (lazy) para cenarios em que DI global ainda nao esta pronto no ctor.
 * - Fix: ownership deterministico. Bridge NUNCA libera token que nao foi adquirido por ela.
 * - Hardening: Release NAO depende de gate resolvido (evita leak em teardown) e protege Provider nulo.
 * - Fix: Resume sem ownership nao gera ruido ("release ignorado") - evento pode ocorrer fora do ciclo de pause.
 */

using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
namespace _ImmersiveGames.NewScripts.Infrastructure.SimulationGate.Interop
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GamePauseGateBridge : IDisposable
    {
        private const string PauseToken = SimulationGateTokens.Pause;
        private const string ReasonResumeRequested = "GameResumeRequestedEvent";
        private const string ReasonExitToMenuRequested = "GameExitToMenuRequestedEvent";

        private ISimulationGateService _gateService;

        private readonly EventBinding<GamePauseCommandEvent> _pauseBinding;
        private readonly EventBinding<GameResumeRequestedEvent> _resumeBinding;

        private IDisposable _activeHandle;
        private bool _bindingsRegistered;
        private bool _loggedMissingGate;
        private bool _disposed;
        private int _lastPauseFrame = -1;
        private string _lastPauseKey = string.Empty;
        private int _lastResumeFrame = -1;
        private string _lastResumeKey = string.Empty;

        public GamePauseGateBridge(ISimulationGateService gateService)
        {
            _gateService = gateService;

            _pauseBinding = new EventBinding<GamePauseCommandEvent>(OnGamePause);
            _resumeBinding = new EventBinding<GameResumeRequestedEvent>(OnGameResumeRequested);

            TryRegisterBindings();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_bindingsRegistered)
            {
                EventBus<GamePauseCommandEvent>.Unregister(_pauseBinding);
                EventBus<GameResumeRequestedEvent>.Unregister(_resumeBinding);
                _bindingsRegistered = false;
            }

            ReleasePauseGate("Dispose");
        }

        internal void ReleaseForExitToMenu(string reason)
        {
            ReleasePauseGate(ReasonExitToMenuRequested);
        }

        private void TryRegisterBindings()
        {
            try
            {
                EventBus<GamePauseCommandEvent>.Register(_pauseBinding);
                EventBus<GameResumeRequestedEvent>.Register(_resumeBinding);
                _bindingsRegistered = true;

                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    "[PauseBridge] Registrado nos eventos GamePauseCommandEvent/GameResumeRequestedEvent -> SimulationGate.");
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<GamePauseGateBridge>(
                    $"[PauseBridge] EventBus indisponivel; pause/resume nao serao refletidos no gate ({ex.GetType().Name}).");
                _bindingsRegistered = false;
            }
        }

        private void OnGamePause(GamePauseCommandEvent evt)
        {
            string key = BuildPauseKey(evt);
            int frame = UnityEngine.Time.frameCount;
            if (ShouldDedupePause(key, frame))
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[OBS][GRS] GamePauseCommandEvent dedupe_same_frame consumer='{nameof(GamePauseGateBridge)}' key='{key}' frame='{frame}'",
                    DebugUtility.Colors.Info);
                return;
            }

            MarkPauseConsumed(key, frame);
            DebugUtility.LogVerbose<GamePauseGateBridge>(
                $"[OBS][GRS] GamePauseCommandEvent consumed consumer='{nameof(GamePauseGateBridge)}' key='{key}' frame='{frame}'",
                DebugUtility.Colors.Info);

            bool shouldPause = evt is { IsPaused: true };
            if (shouldPause)
            {
                AcquirePauseGate();
            }
            else
            {
                ReleasePauseGate("GamePauseCommandEvent(paused=false)");
            }
        }

        private void OnGameResumeRequested(GameResumeRequestedEvent evt)
        {
            string key = BuildResumeKey(evt);
            int frame = UnityEngine.Time.frameCount;
            if (ShouldDedupeResume(key, frame))
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[OBS][GRS] GameResumeRequestedEvent dedupe_same_frame consumer='{nameof(GamePauseGateBridge)}' key='{key}' frame='{frame}'",
                    DebugUtility.Colors.Info);
                return;
            }

            MarkResumeConsumed(key, frame);
            DebugUtility.LogVerbose<GamePauseGateBridge>(
                $"[OBS][GRS] GameResumeRequestedEvent consumed consumer='{nameof(GamePauseGateBridge)}' key='{key}' frame='{frame}'",
                DebugUtility.Colors.Info);

            ReleasePauseGate(ReasonResumeRequested);
        }

        private void AcquirePauseGate()
        {
            if (!EnsureGateResolved())
            {
                LogGateUnavailable();
                return;
            }

            if (_activeHandle != null)
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[PauseBridge] Acquire ignorado: handle ja ativo. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
                return;
            }

            _activeHandle = _gateService.Acquire(PauseToken);

            DebugUtility.LogVerbose<GamePauseGateBridge>(
                $"[PauseBridge] Gate adquirido com token='{PauseToken}'. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
        }

        private void ReleasePauseGate(string reason)
        {
            if (_activeHandle == null)
            {
                if (reason == ReasonResumeRequested || reason == ReasonExitToMenuRequested)
                {
                    return;
                }

                if (_gateService != null)
                {
                    DebugUtility.LogVerbose<GamePauseGateBridge>(
                        $"[PauseBridge] Release ignorado ({reason}) - sem handle ativo (ownership inexistente). IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
                }
                else
                {
                    DebugUtility.LogVerbose<GamePauseGateBridge>(
                        $"[PauseBridge] Release ignorado ({reason}) - sem handle ativo (ownership inexistente).");
                }

                return;
            }

            try
            {
                _activeHandle.Dispose();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<GamePauseGateBridge>(
                    $"[PauseBridge] Erro ao liberar handle ativo ({reason}): {ex}");
            }
            finally
            {
                _activeHandle = null;
            }

            if (_gateService != null)
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[PauseBridge] Gate liberado ({reason}) token='{PauseToken}'. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
            }
            else
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[PauseBridge] Gate liberado ({reason}) token='{PauseToken}'. (gate indisponivel para snapshot)");
            }
        }

        private bool EnsureGateResolved()
        {
            if (_gateService != null)
            {
                return true;
            }

            var provider = DependencyManager.Provider;
            if (provider == null)
            {
                return false;
            }

            if (provider.TryGetGlobal<ISimulationGateService>(out var resolved) && resolved != null)
            {
                _gateService = resolved;
                _loggedMissingGate = false;

                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    "[PauseBridge] ISimulationGateService resolvido via DependencyManager (lazy).");

                return true;
            }

            return false;
        }

        private void LogGateUnavailable()
        {
            if (_loggedMissingGate)
            {
                return;
            }

            DebugUtility.LogWarning<GamePauseGateBridge>(
                "[PauseBridge] ISimulationGateService indisponivel; nao e possivel refletir pause/resume no gate.");
            _loggedMissingGate = true;
        }

        private static string BuildPauseKey(GamePauseCommandEvent evt)
        {
            string reason = "<null>";
            bool isPaused = evt is { IsPaused: true };
            return $"pause|isPaused={isPaused}|reason={reason}";
        }

        private static string BuildResumeKey(GameResumeRequestedEvent evt)
        {
            string reason = NormalizeReason(null);
            return $"resume|reason={reason}";
        }

        private static string NormalizeReason(string reason)
            => string.IsNullOrWhiteSpace(reason) ? "<null>" : reason.Trim();

        private bool ShouldDedupePause(string key, int frame)
            => _lastPauseFrame == frame && string.Equals(_lastPauseKey, key, StringComparison.Ordinal);

        private bool ShouldDedupeResume(string key, int frame)
            => _lastResumeFrame == frame && string.Equals(_lastResumeKey, key, StringComparison.Ordinal);

        private void MarkPauseConsumed(string key, int frame)
        {
            _lastPauseFrame = frame;
            _lastPauseKey = key;
        }

        private void MarkResumeConsumed(string key, int frame)
        {
            _lastResumeFrame = frame;
            _lastResumeKey = key;
        }
    }
}
