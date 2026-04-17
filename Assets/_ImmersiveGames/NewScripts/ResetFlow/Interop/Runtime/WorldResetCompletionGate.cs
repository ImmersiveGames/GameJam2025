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
        private readonly Dictionary<string, TaskCompletionSource<WorldResetCompletedEvent>> _pending = new(StringComparer.Ordinal);
        private readonly Dictionary<string, WorldResetCompletedEvent> _completedEvents = new(StringComparer.Ordinal);
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
                foreach (KeyValuePair<string, TaskCompletionSource<WorldResetCompletedEvent>> kv in _pending)
                {
                    kv.Value.TrySetResult(new WorldResetCompletedEvent(
                        kind: ResetKind.Macro,
                        macroRouteId: SceneRouteId.None,
                        reason: WorldResetReasons.GateDisposed,
                        contextSignature: kv.Key,
                        phaseSignature: PhaseContextSignature.Empty,
                        outcome: WorldResetOutcome.Disposed,
                        detail: WorldResetReasons.GateDisposed,
                        origin: WorldResetOrigin.Unknown,
                        targetScene: string.Empty,
                        sourceSignature: kv.Key));
                }

                _pending.Clear();
                _completedEvents.Clear();
            }
        }

        public async Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
        {
            string signature = SceneTransitionSignature.Compute(context);

            if (string.IsNullOrEmpty(signature))
            {
                DebugUtility.LogWarning(typeof(WorldResetCompletionGate),
                    "[SceneFlowGate] ContextSignature vazia. Não é possível correlacionar gate; liberando sem aguardar reset.");
                return;
            }

            lock (_pending)
            {
                if (_completedEvents.TryGetValue(signature, out WorldResetCompletedEvent cachedEvent))
                {
                    DebugUtility.LogVerbose(typeof(WorldResetCompletionGate),
                        $"[SceneFlowGate] Já concluído (cached). signature='{signature}', outcome='{cachedEvent.Outcome}', reason='{cachedEvent.Reason}', detail='{cachedEvent.Detail}'.");
                    return;
                }
            }

            TaskCompletionSource<WorldResetCompletedEvent> tcs;

            lock (_pending)
            {
                if (!_pending.TryGetValue(signature, out tcs))
                {
                    tcs = new TaskCompletionSource<WorldResetCompletedEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _pending[signature] = tcs;
                }
            }

            Task completed = await Task.WhenAny(tcs.Task, Task.Delay(_timeoutMs));
            if (!ReferenceEquals(completed, tcs.Task))
            {
                lock (_pending)
                {
                    if (_pending.TryGetValue(signature, out TaskCompletionSource<WorldResetCompletedEvent> current) && ReferenceEquals(current, tcs))
                    {
                        _pending.Remove(signature);
                    }
                }

                DebugUtility.LogWarning(typeof(WorldResetCompletionGate),
                    $"[ResetTimeoutProceed] [SceneFlowGate] Timeout aguardando WorldResetCompletedEvent. signature='{signature}', timeoutMs={_timeoutMs}.");
                return;
            }

            WorldResetCompletedEvent completionEvent = await tcs.Task;

            DebugUtility.LogVerbose(typeof(WorldResetCompletionGate),
                $"[SceneFlowGate] Concluído. signature='{signature}', outcome='{completionEvent.Outcome}', reason='{completionEvent.Reason}', detail='{completionEvent.Detail}'.");
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

            string signature = evt.ContextSignature ?? string.Empty;
            string reason = evt.Reason;

            if (string.IsNullOrEmpty(signature))
            {
                DebugUtility.LogWarning(typeof(WorldResetCompletionGate),
                    $"[SceneFlowGate] WorldResetCompletedEvent MACRO recebido com ContextSignature vazia. outcome='{evt.Outcome}', reason='{reason ?? "<null>"}', detail='{evt.Detail ?? "<null>"}'.");
                return;
            }

            TaskCompletionSource<WorldResetCompletedEvent> tcs = null;

            lock (_pending)
            {
                PruneCompletedCacheIfNeeded();

                if (!_completedEvents.ContainsKey(signature))
                {
                    _completedEvents.Add(signature, evt);
                }

                if (_pending.TryGetValue(signature, out tcs))
                {
                    _pending.Remove(signature);
                }
            }

            tcs?.TrySetResult(evt);

            DebugUtility.LogVerbose(typeof(WorldResetCompletionGate),
                $"[SceneFlowGate] WorldResetCompletedEvent recebido. signature='{signature}', outcome='{evt.Outcome}', reason='{reason ?? "<null>"}', detail='{evt.Detail ?? "<null>"}'.");
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

