using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Contracts;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
namespace _ImmersiveGames.NewScripts.ResetFlow.Interop.Runtime
{
    /// <summary>
    /// OWNER: gate de correlação do WorldResetCompletedEvent para liberar SceneFlow.
    /// NÃO É OWNER: execução do reset em si (driver/service/orchestrator do WorldReset).
    /// PUBLISH/CONSUME: consome WorldResetCompletedEvent; não publica eventos.
    /// Fases tocadas: Gate entre ScenesReady e BeforeFadeOut.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldResetCompletionGate : ISceneTransitionCompletionGate, IDisposable
    {
        private readonly EventBinding<WorldResetCompletedEvent> _binding;
        private readonly Dictionary<WorldResetCorrelationKey, TaskCompletionSource<WorldResetCompletedEvent>> _pending = new();
        private readonly Dictionary<WorldResetCorrelationKey, WorldResetCompletedEvent> _completedEvents = new();
        private readonly int _timeoutMs;
        private bool _disposed;

        private const int MaxCompletedCacheEntries = 128;

        public WorldResetCompletionGate(int timeoutMs = 20000)
        {
            _timeoutMs = timeoutMs;

            _binding = new EventBinding<WorldResetCompletedEvent>(OnCompleted);
            EventBus<WorldResetCompletedEvent>.Register(_binding);

            DebugUtility.LogVerbose(typeof(WorldResetCompletionGate),
                $"[SceneFlowGate] WorldResetCompletionGate registrado. timeoutMs={_timeoutMs}.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<WorldResetCompletedEvent>.Unregister(_binding);

            lock (_pending)
            {
                foreach (KeyValuePair<WorldResetCorrelationKey, TaskCompletionSource<WorldResetCompletedEvent>> kv in _pending)
                {
                    kv.Value.TrySetResult(new WorldResetCompletedEvent(
                        kind: ResetKind.Macro,
                        macroRouteId: SceneRouteId.None,
                        reason: WorldResetReasons.GateDisposed,
                        contextSignature: kv.Key.Value,
                        phaseSignature: PhaseContextSignature.Empty,
                        outcome: WorldResetOutcome.Disposed,
                        detail: WorldResetReasons.GateDisposed,
                        origin: WorldResetOrigin.Unknown,
                        targetScene: string.Empty,
                        sourceSignature: kv.Key.Value));
                }

                _pending.Clear();
                _completedEvents.Clear();
            }
        }

        public async Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
        {
            string signature = SceneTransitionSignature.Compute(context);
            WorldResetCorrelationKey correlationKey = WorldResetCorrelationKey.FromContextSignature(signature);

            if (!correlationKey.IsValid)
            {
                DebugUtility.LogWarning(typeof(WorldResetCompletionGate),
                    "[SceneFlowGate] CorrelationKey ausente. Não é possível correlacionar gate; liberando sem aguardar reset.");
                return;
            }

            lock (_pending)
            {
                if (_completedEvents.TryGetValue(correlationKey, out WorldResetCompletedEvent cachedEvent))
                {
                    DebugUtility.LogVerbose(typeof(WorldResetCompletionGate),
                        $"[SceneFlowGate] Já concluído (cached). correlationKey='{correlationKey}', signature='{signature}', outcome='{cachedEvent.Outcome}', reason='{cachedEvent.Reason}', detail='{cachedEvent.Detail}'.");
                    return;
                }
            }

            TaskCompletionSource<WorldResetCompletedEvent> tcs;

            lock (_pending)
            {
                if (!_pending.TryGetValue(correlationKey, out tcs))
                {
                    tcs = new TaskCompletionSource<WorldResetCompletedEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _pending[correlationKey] = tcs;
                }
            }

            Task completed = await Task.WhenAny(tcs.Task, Task.Delay(_timeoutMs));
            if (!ReferenceEquals(completed, tcs.Task))
            {
                lock (_pending)
                {
                    if (_pending.TryGetValue(correlationKey, out TaskCompletionSource<WorldResetCompletedEvent> current) && ReferenceEquals(current, tcs))
                    {
                        _pending.Remove(correlationKey);
                    }
                }

                DebugUtility.LogWarning(typeof(WorldResetCompletionGate),
                    $"[ResetTimeoutProceed] [SceneFlowGate] Timeout aguardando WorldResetCompletedEvent. correlationKey='{correlationKey}', signature='{signature}', timeoutMs={_timeoutMs}.");
                return;
            }

            WorldResetCompletedEvent completionEvent = await tcs.Task;

            DebugUtility.LogVerbose(typeof(WorldResetCompletionGate),
                $"[SceneFlowGate] Concluído. correlationKey='{correlationKey}', signature='{signature}', outcome='{completionEvent.Outcome}', reason='{completionEvent.Reason}', detail='{completionEvent.Detail}'.");
        }

        private void OnCompleted(WorldResetCompletedEvent evt)
        {
            if (evt.Kind != ResetKind.Macro)
            {
                DebugUtility.LogVerbose(typeof(WorldResetCompletionGate),
                    $"[SceneFlowGate] WorldResetCompletedEvent ignorado (non-macro). kind='{evt.Kind}', outcome='{evt.Outcome}', reason='{evt.Reason ?? "<null>"}', detail='{evt.Detail ?? "<null>"}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            WorldResetCorrelationKey correlationKey = WorldResetCorrelationKey.FromContextSignature(evt.ContextSignature);
            string reason = evt.Reason;

            if (!correlationKey.IsValid)
            {
                DebugUtility.LogWarning(typeof(WorldResetCompletionGate),
                    $"[SceneFlowGate] WorldResetCompletedEvent MACRO recebido sem CorrelationKey. outcome='{evt.Outcome}', reason='{reason ?? "<null>"}', detail='{evt.Detail ?? "<null>"}'.");
                return;
            }

            TaskCompletionSource<WorldResetCompletedEvent> tcs = null;

            lock (_pending)
            {
                PruneCompletedCacheIfNeeded();

                if (!_completedEvents.ContainsKey(correlationKey))
                {
                    _completedEvents.Add(correlationKey, evt);
                }

                if (_pending.TryGetValue(correlationKey, out tcs))
                {
                    _pending.Remove(correlationKey);
                }
            }

            tcs?.TrySetResult(evt);

            DebugUtility.LogVerbose(typeof(WorldResetCompletionGate),
                $"[SceneFlowGate] WorldResetCompletedEvent recebido. correlationKey='{correlationKey}', signature='{evt.ContextSignature}', outcome='{evt.Outcome}', reason='{reason ?? "<null>"}', detail='{evt.Detail ?? "<null>"}'.");
        }

        private void PruneCompletedCacheIfNeeded()
        {
            if (_completedEvents.Count <= MaxCompletedCacheEntries)
            {
                return;
            }

            _completedEvents.Clear();

            DebugUtility.LogVerbose(typeof(WorldResetCompletionGate),
                $"[SceneFlowGate] Cache de completions limpo (atingiu limite > {MaxCompletedCacheEntries}).");
        }
    }
}

