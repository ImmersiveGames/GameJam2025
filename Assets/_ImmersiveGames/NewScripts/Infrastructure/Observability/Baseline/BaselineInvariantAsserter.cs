using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun;
using _ImmersiveGames.NewScripts.Experience.PostRun.Handoff;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Contracts;
namespace _ImmersiveGames.NewScripts.Infrastructure.Observability.Baseline
{
    /// <summary>
    /// Opt-in (dev/QA) asserter para tornar o Baseline 2.0 "autofail":
    /// valida ordem de eventos SceneFlow, emissão do ResetCompleted antes do FadeOut,
    /// coerência de tokens do SimulationGate e idempotência de fim de run.
    ///
    /// Importante:
    /// - Não é parte do pipeline de produção por padrão.
    /// - Recomendado habilitar via define 'NEWSCRIPTS_BASELINE_ASSERTS' (para "fail alto").
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
        private readonly EventBinding<WorldResetCompletedEvent> _resetCompleted;

        private readonly EventBinding<GameRunStartedEvent> _runStarted;
        private readonly EventBinding<GameRunEndedEvent> _runEnded;

        private readonly EventBinding<PauseWillEnterEvent> _pauseWillEnter;
        private readonly EventBinding<PauseWillExitEvent> _pauseWillExit;
        private readonly EventBinding<PauseStateChangedEvent> _pauseStateChanged;

        private readonly EventBinding<PostStageStartRequestedEvent> _postStageStartRequested;
        private readonly EventBinding<PostStageStartedEvent> _postStageStarted;
        private readonly EventBinding<PostStageCompletedEvent> _postStageCompleted;

        // Captura dos buses usados (para Unregister consistente)
        private IEventBus<SceneTransitionStartedEvent> _startedBus;
        private IEventBus<SceneTransitionScenesReadyEvent> _scenesReadyBus;
        private IEventBus<SceneTransitionBeforeFadeOutEvent> _beforeFadeOutBus;
        private IEventBus<SceneTransitionCompletedEvent> _completedBus;
        private IEventBus<WorldResetCompletedEvent> _resetCompletedBus;

        private IEventBus<GameRunStartedEvent> _runStartedBus;
        private IEventBus<GameRunEndedEvent> _runEndedBus;

        private IEventBus<PauseWillEnterEvent> _pauseWillEnterBus;
        private IEventBus<PauseWillExitEvent> _pauseWillExitBus;
        private IEventBus<PauseStateChangedEvent> _pauseStateChangedBus;

        private IEventBus<PostStageStartRequestedEvent> _postStageStartRequestedBus;
        private IEventBus<PostStageStartedEvent> _postStageStartedBus;
        private IEventBus<PostStageCompletedEvent> _postStageCompletedBus;

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
            _resetCompleted = new EventBinding<WorldResetCompletedEvent>(OnResetCompleted);

            _runStarted = new EventBinding<GameRunStartedEvent>(OnRunStarted);
            _runEnded = new EventBinding<GameRunEndedEvent>(OnRunEnded);

            _pauseWillEnter = new EventBinding<PauseWillEnterEvent>(OnPauseWillEnter);
            _pauseWillExit = new EventBinding<PauseWillExitEvent>(OnPauseWillExit);
            _pauseStateChanged = new EventBinding<PauseStateChangedEvent>(OnPauseStateChanged);

            _postStageStartRequested = new EventBinding<PostStageStartRequestedEvent>(OnPostStageStartRequested);
            _postStageStarted = new EventBinding<PostStageStartedEvent>(OnPostStageStarted);
            _postStageCompleted = new EventBinding<PostStageCompletedEvent>(OnPostStageCompleted);

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

                    SafeUnregister(_pauseWillEnterBus, _pauseWillEnter);
                    SafeUnregister(_pauseWillExitBus, _pauseWillExit);
                    SafeUnregister(_pauseStateChangedBus, _pauseStateChanged);

                    SafeUnregister(_postStageStartRequestedBus, _postStageStartRequested);
                    SafeUnregister(_postStageStartedBus, _postStageStarted);
                    SafeUnregister(_postStageCompletedBus, _postStageCompleted);
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
                _resetCompletedBus != EventBus<WorldResetCompletedEvent>.GlobalBus ||
                _runStartedBus != EventBus<GameRunStartedEvent>.GlobalBus ||
                _runEndedBus != EventBus<GameRunEndedEvent>.GlobalBus ||
                _pauseWillEnterBus != EventBus<PauseWillEnterEvent>.GlobalBus ||
                _pauseWillExitBus != EventBus<PauseWillExitEvent>.GlobalBus ||
                _pauseStateChangedBus != EventBus<PauseStateChangedEvent>.GlobalBus ||
                _postStageStartRequestedBus != EventBus<PostStageStartRequestedEvent>.GlobalBus ||
                _postStageStartedBus != EventBus<PostStageStartedEvent>.GlobalBus ||
                _postStageCompletedBus != EventBus<PostStageCompletedEvent>.GlobalBus;
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

                SafeUnregister(_pauseWillEnterBus, _pauseWillEnter);
                SafeUnregister(_pauseWillExitBus, _pauseWillExit);
                SafeUnregister(_pauseStateChangedBus, _pauseStateChanged);

                SafeUnregister(_postStageStartRequestedBus, _postStageStartRequested);
                SafeUnregister(_postStageStartedBus, _postStageStarted);
                SafeUnregister(_postStageCompletedBus, _postStageCompleted);

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
                _resetCompletedBus = EventBus<WorldResetCompletedEvent>.GlobalBus;

                _runStartedBus = EventBus<GameRunStartedEvent>.GlobalBus;
                _runEndedBus = EventBus<GameRunEndedEvent>.GlobalBus;

                _pauseWillEnterBus = EventBus<PauseWillEnterEvent>.GlobalBus;
                _pauseWillExitBus = EventBus<PauseWillExitEvent>.GlobalBus;
                _pauseStateChangedBus = EventBus<PauseStateChangedEvent>.GlobalBus;

                _postStageStartRequestedBus = EventBus<PostStageStartRequestedEvent>.GlobalBus;
                _postStageStartedBus = EventBus<PostStageStartedEvent>.GlobalBus;
                _postStageCompletedBus = EventBus<PostStageCompletedEvent>.GlobalBus;

                SafeRegister(_startedBus, _started);
                SafeRegister(_scenesReadyBus, _scenesReady);
                SafeRegister(_beforeFadeOutBus, _beforeFadeOut);
                SafeRegister(_completedBus, _completed);
                SafeRegister(_resetCompletedBus, _resetCompleted);

                SafeRegister(_runStartedBus, _runStarted);
                SafeRegister(_runEndedBus, _runEnded);

                SafeRegister(_pauseWillEnterBus, _pauseWillEnter);
                SafeRegister(_pauseWillExitBus, _pauseWillExit);
                SafeRegister(_pauseStateChangedBus, _pauseStateChanged);

                SafeRegister(_postStageStartRequestedBus, _postStageStartRequested);
                SafeRegister(_postStageStartedBus, _postStageStarted);
                SafeRegister(_postStageCompletedBus, _postStageCompleted);

                _bindingsRegistered = true;

                DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                    "[Baseline] Bindings registrados (SceneFlow + WorldLifecycle + GameLoop + Pause + PostRun).",
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
            string sig = SceneTransitionSignature.Compute(evt.context);
            var state = GetOrCreate(sig);

            if (state.started && !state.completed)
            {
                Fail(sig, "I2",
                    "Nova transição Started observada, mas a transição anterior não completou (estado inconsistente).",
                    evt.context.ToString());
                state.Reset();
            }

            state.started = true;
            state.startedFrame = UnityEngine.Time.frameCount;

            if (!EnsureGateResolved() || !_gate.IsTokenActive(SceneTransitionToken))
            {
                Fail(sig, "I1",
                    $"Token de transição '{SceneTransitionToken}' não está ativo após Started.",
                    evt.context.ToString());
            }

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] Started signature='{sig}' profile='{evt.context.TransitionProfileName}' activeTarget='{evt.context.TargetActiveScene}'.",
                DebugUtility.Colors.Info);
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            string sig = SceneTransitionSignature.Compute(evt.context);
            var state = GetOrCreate(sig);

            if (!state.started)
            {
                Fail(sig, "I2", "ScenesReady observado antes de Started (ordem inválida).", evt.context.ToString());
            }

            state.scenesReady = true;
            state.scenesReadyFrame = UnityEngine.Time.frameCount;

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] ScenesReady signature='{sig}'.",
                DebugUtility.Colors.Info);
        }

        private void OnResetCompleted(WorldResetCompletedEvent evt)
        {
            string rawSig = evt.ContextSignature ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawSig))
            {
                Fail("<missing-signature>", "I3",
                    "WorldResetCompletedEvent recebido sem ContextSignature (correlação quebrada).",
                    contextText: "<no-scene-context>",
                    extra: evt.ToString());

                rawSig = "<missing-signature>";
            }

            string sig = rawSig;
            var state = GetOrCreate(sig);

            if (state.resetCompleted)
            {
                Fail(sig, "I3",
                    "ResetCompleted duplicado para a mesma signature (emissão duplicada).",
                    contextText: "<no-scene-context>",
                    extra: evt.ToString());
            }

            if (!state.scenesReady)
            {
                Fail(sig, "I3",
                    "ResetCompleted observado antes de ScenesReady (ordem inválida).",
                    contextText: "<no-scene-context>",
                    extra: evt.ToString());
            }

            state.resetCompleted = true;
            state.resetCompletedFrame = UnityEngine.Time.frameCount;
            state.resetReason = evt.Reason ?? string.Empty;

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] ResetCompleted signature='{sig}' reason='{evt.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private void OnBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
        {
            string sig = SceneTransitionSignature.Compute(evt.context);
            var state = GetOrCreate(sig);

            if (!state.scenesReady)
            {
                Fail(sig, "I2", "BeforeFadeOut observado antes de ScenesReady (ordem inválida).", evt.context.ToString());
            }

            state.beforeFadeOut = true;
            state.beforeFadeOutFrame = UnityEngine.Time.frameCount;

            if (!state.resetCompleted)
            {
                Fail(sig, "I3", "BeforeFadeOut observado antes do ResetCompleted (gate/regra quebrada).", evt.context.ToString());
            }

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] BeforeFadeOut signature='{sig}'.",
                DebugUtility.Colors.Info);
        }

        private void OnCompleted(SceneTransitionCompletedEvent evt)
        {
            string sig = SceneTransitionSignature.Compute(evt.context);
            var state = GetOrCreate(sig);

            if (!state.scenesReady)
            {
                Fail(sig, "I2", "Completed observado antes de ScenesReady (ordem inválida).", evt.context.ToString());
            }

            state.completed = true;
            state.completedFrame = UnityEngine.Time.frameCount;

            if (!state.resetCompleted)
            {
                Fail(sig, "I3",
                    "Completed observado antes do ResetCompleted (ordem inválida).",
                    evt.context.ToString());
            }

            if (EnsureGateResolved() && _gate.IsTokenActive(SceneTransitionToken))
            {
                Fail(sig, "I1",
                    $"Token de transição '{SceneTransitionToken}' ainda está ativo após Completed (token preso).",
                    evt.context.ToString());
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

        private void OnPauseWillEnter(PauseWillEnterEvent evt)
        {
            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] PauseWillEnter reason='{evt?.Reason ?? string.Empty}'.",
                DebugUtility.Colors.Info);
        }

        private void OnPauseWillExit(PauseWillExitEvent evt)
        {
            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] PauseWillExit reason='{evt?.Reason ?? string.Empty}'.",
                DebugUtility.Colors.Info);
        }

        private void OnPauseStateChanged(PauseStateChangedEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (evt.IsPaused)
            {
                if (!EnsureGateResolved() || !_gate.IsTokenActive(PauseToken))
                {
                    Fail("<pause>", "I6",
                        $"PauseStateChangedEvent(true) observado, mas token '{PauseToken}' nao esta ativo.",
                        contextText: "<no-scene-context>");
                }

                DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                    $"[Baseline] PauseStateChanged isPaused='true' tokenActive={(EnsureGateResolved() && _gate.IsTokenActive(PauseToken))}.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!EnsureGateResolved())
            {
                return;
            }

            if (_gate.IsTokenActive(PauseToken))
            {
                Fail("<pause>", "I6",
                    $"PauseStateChangedEvent(false) observado, mas token '{PauseToken}' ainda esta ativo.",
                    contextText: "<no-scene-context>");
            }

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] PauseStateChanged isPaused='false' tokenActive={_gate.IsTokenActive(PauseToken)}.",
                DebugUtility.Colors.Info);
        }

        private void OnPostStageStartRequested(PostStageStartRequestedEvent evt)
        {
            string sig = evt.Context.Signature;
            var state = GetOrCreate(sig);

            state.postStageStarted = false;
            state.postStageCompleted = false;
            state.postStageStartedFrame = 0;
            state.postStageCompletedFrame = 0;
            state.postStageCompletionKind = PostStageCompletionKind.Unknown;
            state.postStageStartRequested = true;
            state.postStageStartRequestedFrame = UnityEngine.Time.frameCount;

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] PostStageStartRequested signature='{sig}' scene='{evt.Context.SceneName}' outcome='{evt.Context.Outcome}'.",
                DebugUtility.Colors.Info);
        }

        private void OnPostStageStarted(PostStageStartedEvent evt)
        {
            string sig = evt.Context.Signature;
            var state = GetOrCreate(sig);

            if (!state.postStageStartRequested)
            {
                Fail(sig, "I7",
                    "PostStageStarted observado antes de PostStageStartRequested.",
                    evt.Context.ToString());
            }

            state.postStageStarted = true;
            state.postStageStartedFrame = UnityEngine.Time.frameCount;

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] PostStageStarted signature='{sig}' scene='{evt.Context.SceneName}' outcome='{evt.Context.Outcome}'.",
                DebugUtility.Colors.Info);
        }

        private void OnPostStageCompleted(PostStageCompletedEvent evt)
        {
            string sig = evt.Context.Signature;
            var state = GetOrCreate(sig);

            if (!state.postStageStarted)
            {
                Fail(sig, "I7",
                    "PostStageCompleted observado antes de PostStageStarted.",
                    evt.Context.ToString());
            }

            state.postStageCompleted = true;
            state.postStageCompletedFrame = UnityEngine.Time.frameCount;
            state.postStageCompletionKind = evt.Completion.Kind;

            DebugUtility.LogVerbose<BaselineInvariantAsserter>(
                $"[Baseline] PostStageCompleted signature='{sig}' kind='{evt.Completion.Kind}' reason='{evt.Completion.Reason}'.",
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
            string ctx = string.IsNullOrWhiteSpace(contextText) ? "<no-context>" : contextText;
            string extraText = string.IsNullOrWhiteSpace(extra) ? string.Empty : $" extra={extra}";

            string full = $"[Baseline][FAIL] {invariantId} signature='{signature}': {message} ctx={ctx}{extraText}";

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
            public bool started;
            public bool scenesReady;
            public bool resetCompleted;
            public bool beforeFadeOut;
            public bool completed;
            public bool postStageStartRequested;
            public bool postStageStarted;
            public bool postStageCompleted;

            public int startedFrame;
            public int scenesReadyFrame;
            public int resetCompletedFrame;
            public int beforeFadeOutFrame;
            public int completedFrame;
            public int postStageStartRequestedFrame;
            public int postStageStartedFrame;
            public int postStageCompletedFrame;

            public string resetReason = string.Empty;
            public PostStageCompletionKind postStageCompletionKind = PostStageCompletionKind.Unknown;

            public void Reset()
            {
                started = false;
                scenesReady = false;
                resetCompleted = false;
                beforeFadeOut = false;
                completed = false;
                postStageStartRequested = false;
                postStageStarted = false;
                postStageCompleted = false;

                startedFrame = 0;
                scenesReadyFrame = 0;
                resetCompletedFrame = 0;
                beforeFadeOutFrame = 0;
                completedFrame = 0;
                postStageStartRequestedFrame = 0;
                postStageStartedFrame = 0;
                postStageCompletedFrame = 0;

                resetReason = string.Empty;
                postStageCompletionKind = PostStageCompletionKind.Unknown;
            }
        }
    }
}



