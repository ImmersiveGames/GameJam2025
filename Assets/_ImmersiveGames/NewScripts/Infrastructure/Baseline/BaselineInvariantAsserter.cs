using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;

namespace _ImmersiveGames.NewScripts.Infrastructure.Baseline
{
    /// <summary>
    /// Opt-in (dev/QA) asserter para tornar o Baseline 2.0 "auto-fail":
    /// valida ordem de eventos SceneFlow, emissão do ResetCompleted antes do FadeOut,
    /// coerência de tokens do SimulationGate e idempotência de fim de run.
    ///
    /// Importante:
    /// - Não é parte do pipeline de produção por padrão.
    /// - Recomendado habilitar via define `NEWSCRIPTS_BASELINE_ASSERTS` (para "fail alto").
    /// - Pode ser instalado manualmente chamando <see cref="TryInstall"/>.
    ///
    /// Nota de robustez:
    /// - Não altera EventBus. Captura o GlobalBus atual no momento do register para
    ///   garantir que Unregister remova do mesmo bus, mesmo se QA trocar GlobalBus depois.
    /// - Com Domain Reload desativado, o serviço pode persistir entre Play sessions;
    ///   por isso existe rebind idempotente para sobreviver a EventBus.Clear().
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class BaselineInvariantAsserter : IDisposable
    {
        private const string SceneTransitionToken = SimulationGateTokens.SceneTransition;
        private const string PauseToken = SimulationGateTokens.Pause;

        private readonly Dictionary<string, TransitionState> _statesBySignature = new();

        private ISimulationGateService _gate;
        private bool _disposed;

        // Run tracking
        private bool _runActive;
        private int _runEndedCount;
        private int _runIndex;

        private readonly EventBinding<SceneTransitionStartedEvent> _started;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _scenesReady;
        private readonly EventBinding<SceneTransitionBeforeFadeOutEvent> _beforeFadeOut;
        private readonly EventBinding<SceneTransitionCompletedEvent> _completed;
        private readonly EventBinding<WorldLifecycleResetCompletedEvent> _resetCompleted;

        private readonly EventBinding<GameRunStartedEvent> _runStarted;
        private readonly EventBinding<GameRunEndedEvent> _runEnded;

        private readonly EventBinding<GamePauseCommandEvent> _pause;
        private readonly EventBinding<GameResumeRequestedEvent> _resume;

        // Captura dos buses usados (para Unregister consistente)
        private IEventBus<SceneTransitionStartedEvent> _startedBus;
        private IEventBus<SceneTransitionScenesReadyEvent> _scenesReadyBus;
        private IEventBus<SceneTransitionBeforeFadeOutEvent> _beforeFadeOutBus;
        private IEventBus<SceneTransitionCompletedEvent> _completedBus;
        private IEventBus<WorldLifecycleResetCompletedEvent> _resetCompletedBus;

        private IEventBus<GameRunStartedEvent> _runStartedBus;
        private IEventBus<GameRunEndedEvent> _runEndedBus;

        private IEventBus<GamePauseCommandEvent> _pauseBus;
        private IEventBus<GameResumeRequestedEvent> _resumeBus;

        private bool _bindingsRegistered;

        /// <summary>
        /// Instala o asserter no DI global (idempotente). Retorna true se instalado.
        /// </summary>
        public static bool TryInstall()
        {
            try
            {
                var provider = DependencyManager.Provider;
                if (provider == null)
                {
                    return false;
                }

                if (provider.TryGetGlobal<BaselineInvariantAsserter>(out var existing) && existing != null)
                {
                    // Importante: com Domain Reload OFF, este serviço pode sobreviver a EventBus.Clear().
                    // Então garantimos rebind idempotente.
                    existing.EnsureActive();
                    return true;
                }

                provider.TryGetGlobal<ISimulationGateService>(out var gate);

                var instance = new BaselineInvariantAsserter(gate);
                provider.RegisterGlobal(instance, allowOverride: false);

                DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                    "[Baseline] BaselineInvariantAsserter instalado no DI global.",
                    DebugUtility.Colors.Info);

                return true;
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<BaselineInvariantAsserter>(
                    $"[Baseline] Falha ao instalar BaselineInvariantAsserter: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        public BaselineInvariantAsserter(ISimulationGateService gate)
        {
            _gate = gate;

            _started = new EventBinding<SceneTransitionStartedEvent>(OnStarted);
            _scenesReady = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            _beforeFadeOut = new EventBinding<SceneTransitionBeforeFadeOutEvent>(OnBeforeFadeOut);
            _completed = new EventBinding<SceneTransitionCompletedEvent>(OnCompleted);
            _resetCompleted = new EventBinding<WorldLifecycleResetCompletedEvent>(OnResetCompleted);

            _runStarted = new EventBinding<GameRunStartedEvent>(OnRunStarted);
            _runEnded = new EventBinding<GameRunEndedEvent>(OnRunEnded);

            _pause = new EventBinding<GamePauseCommandEvent>(OnPause);
            _resume = new EventBinding<GameResumeRequestedEvent>(_ => CheckPauseTokenReleased("GameResumeRequestedEvent"));

            EnsureActive();
        }

        /// <summary>
        /// Garante que o asserter está "vivo": gate resolvido quando possível e bindings registrados
        /// no GlobalBus atual (idempotente). Crucial para cenários com Domain Reload OFF + EventBus.Clear().
        /// </summary>
        private void EnsureActive()
        {
            if (_disposed)
            {
                return;
            }

            EnsureGateResolved();

            if (!_bindingsRegistered || IsBusMismatchWithCurrentGlobal())
            {
                // Best-effort: tenta limpar bindings antigos antes de re-registrar.
                if (_bindingsRegistered)
                {
                    SafeUnregister(_startedBus, _started);
                    SafeUnregister(_scenesReadyBus, _scenesReady);
                    SafeUnregister(_beforeFadeOutBus, _beforeFadeOut);
                    SafeUnregister(_completedBus, _completed);
                    SafeUnregister(_resetCompletedBus, _resetCompleted);

                    SafeUnregister(_runStartedBus, _runStarted);
                    SafeUnregister(_runEndedBus, _runEnded);

                    SafeUnregister(_pauseBus, _pause);
                    SafeUnregister(_resumeBus, _resume);
                }

                TryRegisterBindings();
            }
        }

        private bool IsBusMismatchWithCurrentGlobal()
        {
            // Se algum bus capturado não corresponde ao GlobalBus atual, precisamos rebind.
            return
                _startedBus != EventBus<SceneTransitionStartedEvent>.GlobalBus ||
                _scenesReadyBus != EventBus<SceneTransitionScenesReadyEvent>.GlobalBus ||
                _beforeFadeOutBus != EventBus<SceneTransitionBeforeFadeOutEvent>.GlobalBus ||
                _completedBus != EventBus<SceneTransitionCompletedEvent>.GlobalBus ||
                _resetCompletedBus != EventBus<WorldLifecycleResetCompletedEvent>.GlobalBus ||
                _runStartedBus != EventBus<GameRunStartedEvent>.GlobalBus ||
                _runEndedBus != EventBus<GameRunEndedEvent>.GlobalBus ||
                _pauseBus != EventBus<GamePauseCommandEvent>.GlobalBus ||
                _resumeBus != EventBus<GameResumeRequestedEvent>.GlobalBus;
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
                // Unregister sempre no bus capturado (não no EventBus<T>.GlobalBus atual)
                SafeUnregister(_startedBus, _started);
                SafeUnregister(_scenesReadyBus, _scenesReady);
                SafeUnregister(_beforeFadeOutBus, _beforeFadeOut);
                SafeUnregister(_completedBus, _completed);
                SafeUnregister(_resetCompletedBus, _resetCompleted);

                SafeUnregister(_runStartedBus, _runStarted);
                SafeUnregister(_runEndedBus, _runEnded);

                SafeUnregister(_pauseBus, _pause);
                SafeUnregister(_resumeBus, _resume);

                _bindingsRegistered = false;
            }
        }

        private static void SafeRegister<T>(IEventBus<T> bus, EventBinding<T> binding)
        {
            bus?.Register(binding);
        }

        private static void SafeUnregister<T>(IEventBus<T> bus, EventBinding<T> binding)
        {
            bus?.Unregister(binding);
        }

        private void TryRegisterBindings()
        {
            try
            {
                // Captura do GlobalBus atual por tipo (sem alterar EventBus)
                _startedBus = EventBus<SceneTransitionStartedEvent>.GlobalBus;
                _scenesReadyBus = EventBus<SceneTransitionScenesReadyEvent>.GlobalBus;
                _beforeFadeOutBus = EventBus<SceneTransitionBeforeFadeOutEvent>.GlobalBus;
                _completedBus = EventBus<SceneTransitionCompletedEvent>.GlobalBus;
                _resetCompletedBus = EventBus<WorldLifecycleResetCompletedEvent>.GlobalBus;

                _runStartedBus = EventBus<GameRunStartedEvent>.GlobalBus;
                _runEndedBus = EventBus<GameRunEndedEvent>.GlobalBus;

                _pauseBus = EventBus<GamePauseCommandEvent>.GlobalBus;
                _resumeBus = EventBus<GameResumeRequestedEvent>.GlobalBus;

                SafeRegister(_startedBus, _started);
                SafeRegister(_scenesReadyBus, _scenesReady);
                SafeRegister(_beforeFadeOutBus, _beforeFadeOut);
                SafeRegister(_completedBus, _completed);
                SafeRegister(_resetCompletedBus, _resetCompleted);

                SafeRegister(_runStartedBus, _runStarted);
                SafeRegister(_runEndedBus, _runEnded);

                SafeRegister(_pauseBus, _pause);
                SafeRegister(_resumeBus, _resume);

                _bindingsRegistered = true;

                DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                    "[Baseline] Bindings registrados (SceneFlow + WorldLifecycle + GameLoop + Pause).",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                _bindingsRegistered = false;

                DebugUtility.LogWarning<BaselineInvariantAsserter>(
                    $"[Baseline] EventBus indisponível; asserter não ativo ({ex.GetType().Name}).");
            }
        }

        private void OnStarted(SceneTransitionStartedEvent evt)
        {
            var sig = SceneTransitionSignatureUtil.Compute(evt.Context);
            var state = GetOrCreate(sig);

            if (state.Started && !state.Completed)
            {
                Fail(sig, "I2",
                    "Nova transição Started observada, mas a transição anterior não completou (estado inconsistente).",
                    evt.Context.ToString());
                state.Reset();
            }

            state.Started = true;
            state.StartedFrame = UnityEngine.Time.frameCount;

            if (!EnsureGateResolved() || !_gate.IsTokenActive(SceneTransitionToken))
            {
                Fail(sig, "I1",
                    $"Token de transição '{SceneTransitionToken}' não está ativo após Started.",
                    evt.Context.ToString());
            }

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] Started signature='{sig}' profile='{evt.Context.TransitionProfileName}' activeTarget='{evt.Context.TargetActiveScene}'.",
                DebugUtility.Colors.Info);
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            var sig = SceneTransitionSignatureUtil.Compute(evt.Context);
            var state = GetOrCreate(sig);

            if (!state.Started)
            {
                Fail(sig, "I2", "ScenesReady observado antes de Started (ordem inválida).", evt.Context.ToString());
            }

            state.ScenesReady = true;
            state.ScenesReadyFrame = UnityEngine.Time.frameCount;

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] ScenesReady signature='{sig}'.",
                DebugUtility.Colors.Info);
        }

        private void OnResetCompleted(WorldLifecycleResetCompletedEvent evt)
        {
            var rawSig = evt.ContextSignature ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawSig))
            {
                Fail("<missing-signature>", "I3",
                    "WorldLifecycleResetCompletedEvent recebido sem ContextSignature (correlação quebrada).",
                    contextText: "<no-scene-context>",
                    extra: evt.ToString());

                rawSig = "<missing-signature>";
            }

            var sig = rawSig;
            var state = GetOrCreate(sig);

            if (state.ResetCompleted)
            {
                Fail(sig, "I3",
                    "ResetCompleted duplicado para a mesma signature (emissão duplicada).",
                    contextText: "<no-scene-context>",
                    extra: evt.ToString());
            }

            if (!state.ScenesReady)
            {
                Fail(sig, "I3",
                    "ResetCompleted observado antes de ScenesReady (ordem inválida).",
                    contextText: "<no-scene-context>",
                    extra: evt.ToString());
            }

            state.ResetCompleted = true;
            state.ResetCompletedFrame = UnityEngine.Time.frameCount;
            state.ResetReason = evt.Reason ?? string.Empty;

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] ResetCompleted signature='{sig}' reason='{evt.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private void OnBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
        {
            var sig = SceneTransitionSignatureUtil.Compute(evt.Context);
            var state = GetOrCreate(sig);

            if (!state.ScenesReady)
            {
                Fail(sig, "I2", "BeforeFadeOut observado antes de ScenesReady (ordem inválida).", evt.Context.ToString());
            }

            state.BeforeFadeOut = true;
            state.BeforeFadeOutFrame = UnityEngine.Time.frameCount;

            if (!state.ResetCompleted)
            {
                Fail(sig, "I3", "BeforeFadeOut observado antes do ResetCompleted (gate/regra quebrada).", evt.Context.ToString());
            }

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] BeforeFadeOut signature='{sig}'.",
                DebugUtility.Colors.Info);
        }

        private void OnCompleted(SceneTransitionCompletedEvent evt)
        {
            var sig = SceneTransitionSignatureUtil.Compute(evt.Context);
            var state = GetOrCreate(sig);

            if (!state.ScenesReady)
            {
                Fail(sig, "I2", "Completed observado antes de ScenesReady (ordem inválida).", evt.Context.ToString());
            }

            state.Completed = true;
            state.CompletedFrame = UnityEngine.Time.frameCount;

            if (!state.ResetCompleted)
            {
                Fail(sig, "I3",
                    "Completed observado antes do ResetCompleted (ordem inválida).",
                    evt.Context.ToString());
            }

            if (EnsureGateResolved() && _gate.IsTokenActive(SceneTransitionToken))
            {
                Fail(sig, "I1",
                    $"Token de transição '{SceneTransitionToken}' ainda está ativo após Completed (token preso).",
                    evt.Context.ToString());
            }

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] Completed signature='{sig}'.",
                DebugUtility.Colors.Info);
        }

        private void OnRunStarted(GameRunStartedEvent evt)
        {
            if (_runActive && _runEndedCount == 0)
            {
                Fail("<run>", "I5",
                    "GameRunStartedEvent observado enquanto a run anterior ainda está ativa (faltou GameRunEndedEvent).",
                    contextText: "<no-scene-context>",
                    extra: $"prevRunIdx={_runIndex} newStateId={evt.StateId}");
            }

            _runIndex++;
            _runActive = true;
            _runEndedCount = 0;

            if (evt.StateId != GameLoopStateId.Playing)
            {
                Fail("<run>", "I4",
                    $"GameRunStartedEvent emitido com StateId='{evt.StateId}', esperado '{GameLoopStateId.Playing}'.",
                    contextText: "<no-scene-context>");
            }

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] RunStarted idx={_runIndex} state='{evt.StateId}'.",
                DebugUtility.Colors.Info);
        }

        private void OnRunEnded(GameRunEndedEvent evt)
        {
            if (!_runActive)
            {
                Fail("<run>", "I5",
                    "GameRunEndedEvent observado sem run ativa (faltou RunStarted ou estado não resetou).",
                    contextText: "<no-scene-context>",
                    extra: $"Outcome={evt.Outcome} Reason={evt.Reason}");
            }

            _runEndedCount++;

            if (_runEndedCount > 1)
            {
                Fail("<run>", "I5",
                    $"GameRunEndedEvent repetido na mesma run (count={_runEndedCount}).",
                    contextText: "<no-scene-context>",
                    extra: $"Outcome={evt.Outcome} Reason={evt.Reason}");
            }

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] RunEnded idx={_runIndex} outcome='{evt.Outcome}' reason='{evt.Reason}' count={_runEndedCount}.",
                DebugUtility.Colors.Info);
        }

        private void OnPause(GamePauseCommandEvent evt)
        {
            if (evt is { IsPaused: true })
            {
                // IMPORTANTE: a ordem de subscribers no EventBus pode fazer este asserter rodar antes do bridge
                // que adquire o token. Para evitar falso FAIL, aqui vira WARNING; a validação forte fica no "token preso".
                if (!EnsureGateResolved() || !_gate.IsTokenActive(PauseToken))
                {
                    DebugUtility.LogWarning<BaselineInvariantAsserter>(
                        $"[Baseline] [WARN] Pause solicitado, mas token '{PauseToken}' ainda não está ativo (possível ordem de subscribers).");
                }

                DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                    $"[Baseline] Pause ON token='{PauseToken}' active={(EnsureGateResolved() && _gate.IsTokenActive(PauseToken))}.",
                    DebugUtility.Colors.Info);
            }
            else
            {
                CheckPauseTokenReleased("GamePauseCommandEvent(paused=false)");
            }
        }

        private void CheckPauseTokenReleased(string reason)
        {
            if (!EnsureGateResolved())
            {
                return;
            }

            if (_gate.IsTokenActive(PauseToken))
            {
                Fail("<pause>", "I6",
                    $"Pause liberado ({reason}), mas token '{PauseToken}' ainda está ativo.",
                    contextText: "<no-scene-context>");
            }

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] Pause OFF ({reason}) tokenActive={_gate.IsTokenActive(PauseToken)}.",
                DebugUtility.Colors.Info);
        }

        private TransitionState GetOrCreate(string signature)
        {
            signature ??= string.Empty;

            if (_statesBySignature.TryGetValue(signature, out var state))
            {
                return state;
            }

            state = new TransitionState();
            _statesBySignature[signature] = state;
            return state;
        }

        private bool EnsureGateResolved()
        {
            if (_gate != null)
            {
                return true;
            }

            try
            {
                var provider = DependencyManager.Provider;
                if (provider == null)
                {
                    return false;
                }

                if (provider.TryGetGlobal<ISimulationGateService>(out var resolved) && resolved != null)
                {
                    _gate = resolved;
                    return true;
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

        private static void Fail(string signature, string invariantId, string message, string contextText, string extra = null)
        {
            var ctx = string.IsNullOrWhiteSpace(contextText) ? "<no-context>" : contextText;
            var extraText = string.IsNullOrWhiteSpace(extra) ? string.Empty : $" extra={extra}";

            var full = $"[Baseline][FAIL] {invariantId} signature='{signature}': {message} ctx={ctx}{extraText}";

            DebugUtility.LogError<BaselineInvariantAsserter>(full);

#if NEWSCRIPTS_BASELINE_ASSERTS
            throw new BaselineInvariantException(full);
#endif
        }

#if NEWSCRIPTS_BASELINE_ASSERTS
        private sealed class BaselineInvariantException : InvalidOperationException
        {
            public BaselineInvariantException(string message) : base(message) { }
        }
#endif

        private sealed class TransitionState
        {
            public bool Started;
            public bool ScenesReady;
            public bool ResetCompleted;
            public bool BeforeFadeOut;
            public bool Completed;

            public int StartedFrame;
            public int ScenesReadyFrame;
            public int ResetCompletedFrame;
            public int BeforeFadeOutFrame;
            public int CompletedFrame;

            public string ResetReason = string.Empty;

            public void Reset()
            {
                Started = false;
                ScenesReady = false;
                ResetCompleted = false;
                BeforeFadeOut = false;
                Completed = false;

                StartedFrame = 0;
                ScenesReadyFrame = 0;
                ResetCompletedFrame = 0;
                BeforeFadeOutFrame = 0;
                CompletedFrame = 0;

                ResetReason = string.Empty;
            }
        }
    }
}
