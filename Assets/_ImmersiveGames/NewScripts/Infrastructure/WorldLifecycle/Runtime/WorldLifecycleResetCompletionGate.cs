using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime
{
    /// <summary>
    /// Gate de conclusão para SceneFlow:
    /// aguarda WorldLifecycleResetCompletedEvent cuja assinatura (ContextSignature) corresponda
    /// à assinatura do contexto da transição atual.
    ///
    /// Objetivo: garantir que FadeOut/Completed só aconteçam após o reset terminar (ou ser skipped).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldLifecycleResetCompletionGate : ISceneTransitionCompletionGate, IDisposable
    {
        private readonly EventBinding<WorldLifecycleResetCompletedEvent> _binding;

        private readonly Dictionary<string, TaskCompletionSource<string>> _pending =
            new Dictionary<string, TaskCompletionSource<string>>(StringComparer.Ordinal);

        private readonly Dictionary<string, string> _completedReasons =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private readonly int _timeoutMs;

        // Cache defensivo para evitar crescimento infinito em sessões longas.
        private const int MaxCompletedCacheEntries = 128;

        public WorldLifecycleResetCompletionGate(int timeoutMs = 20000)
        {
            _timeoutMs = timeoutMs;

            _binding = new EventBinding<WorldLifecycleResetCompletedEvent>(OnCompleted);
            EventBus<WorldLifecycleResetCompletedEvent>.Register(_binding);

            DebugUtility.LogVerbose(typeof(WorldLifecycleResetCompletionGate),
                $"[SceneFlowGate] WorldLifecycleResetCompletionGate registrado. timeoutMs={_timeoutMs}.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            EventBus<WorldLifecycleResetCompletedEvent>.Unregister(_binding);

            lock (_pending)
            {
                _pending.Clear();
                _completedReasons.Clear();
            }
        }

        public async Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
        {
            // Contrato atual: SceneTransitionSignatureUtil.Compute(context) == context.ToString()
            var signature = SceneTransitionSignatureUtil.Compute(context);

            // Se já chegou antes (caso raro), retorna imediatamente.
            lock (_pending)
            {
                if (_completedReasons.ContainsKey(signature))
                {
                    DebugUtility.LogVerbose(typeof(WorldLifecycleResetCompletionGate),
                        $"[SceneFlowGate] Já concluído (cached). signature='{signature}'.");
                    return;
                }
            }

            TaskCompletionSource<string> tcs;

            lock (_pending)
            {
                if (!_pending.TryGetValue(signature, out tcs))
                {
                    tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _pending[signature] = tcs;
                }
            }

            // Timeout defensivo para não travar o flow.
            Task completed = await Task.WhenAny(tcs.Task, Task.Delay(_timeoutMs));
            if (completed != tcs.Task)
            {
                // Importante: evita acumular pending em caso de evento nunca publicado.
                lock (_pending)
                {
                    if (_pending.TryGetValue(signature, out var current) && ReferenceEquals(current, tcs))
                    {
                        _pending.Remove(signature);
                    }
                }

                DebugUtility.LogWarning(typeof(WorldLifecycleResetCompletionGate),
                    $"[SceneFlowGate] Timeout aguardando WorldLifecycleResetCompletedEvent. signature='{signature}', timeoutMs={_timeoutMs}.");
                return;
            }

            string reason = await tcs.Task;

            DebugUtility.LogVerbose(typeof(WorldLifecycleResetCompletionGate),
                $"[SceneFlowGate] Concluído. signature='{signature}', reason='{reason ?? "<null>"}'.");
        }

        private void OnCompleted(WorldLifecycleResetCompletedEvent evt)
        {
            string signature = evt.ContextSignature ?? string.Empty;
            string reason = evt.Reason;

            TaskCompletionSource<string> tcs = null;

            lock (_pending)
            {
                // Cache defensivo: impede crescimento infinito.
                PruneCompletedCacheIfNeeded();

                // Marca como concluído (para chamadas futuras).
                _completedReasons.TryAdd(signature, reason);

                _pending.Remove(signature, out tcs);
            }

            if (tcs != null)
            {
                tcs.TrySetResult(reason);
            }

            DebugUtility.LogVerbose(typeof(WorldLifecycleResetCompletionGate),
                $"[SceneFlowGate] WorldLifecycleResetCompletedEvent recebido. signature='{signature}', reason='{reason ?? "<null>"}'.");
        }

        private void PruneCompletedCacheIfNeeded()
        {
            // Estratégia simples e estável: ao estourar, limpa tudo.
            // (Evita overhead de LRU; suficiente como proteção contra crescimento infinito.)
            if (_completedReasons.Count <= MaxCompletedCacheEntries)
            {
                return;
            }

            _completedReasons.Clear();

            DebugUtility.LogVerbose(typeof(WorldLifecycleResetCompletionGate),
                $"[SceneFlowGate] Cache de completedReasons limpo (atingiu limite > {MaxCompletedCacheEntries}).");
        }
    }
}
