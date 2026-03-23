using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.ResetInterop.Runtime
{
        /// <summary>
    /// OWNER: gate V1 de correlacao do WorldResetCompletedEvent para liberar SceneFlow.
    /// NAO E OWNER: execucao do reset em si (driver/service/orchestrator do WorldReset).
    /// PUBLISH/CONSUME: consome WorldResetCompletedEvent; nao publica eventos.
    /// Fases tocadas: Gate entre ScenesReady e BeforeFadeOut.
    /// </summary>
[DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldResetCompletionGate : ISceneTransitionCompletionGate, IDisposable
    {
        private readonly EventBinding<WorldResetCompletedEvent> _binding;

        private readonly Dictionary<string, TaskCompletionSource<string>> _pending = new(StringComparer.Ordinal);

        private readonly Dictionary<string, string> _completedReasons = new(StringComparer.Ordinal);

        private readonly int _timeoutMs;

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
            EventBus<WorldResetCompletedEvent>.Unregister(_binding);

            lock (_pending)
            {
                // Cancela awaiters pendentes (defensivo).
                foreach (KeyValuePair<string, TaskCompletionSource<string>> kv in _pending)
                {
                    kv.Value.TrySetCanceled();
                }

                _pending.Clear();
                _completedReasons.Clear();
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
                if (_completedReasons.ContainsKey(signature))
                {
                    DebugUtility.LogVerbose(typeof(WorldResetCompletionGate),
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

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(_timeoutMs));
            if (completed != tcs.Task)
            {
                lock (_pending)
                {
                    if (_pending.TryGetValue(signature, out TaskCompletionSource<string> current) && ReferenceEquals(current, tcs))
                    {
                        _pending.Remove(signature);
                    }
                }

                DebugUtility.LogWarning(typeof(WorldResetCompletionGate),
                    $"[ResetTimeoutProceed] [SceneFlowGate] Timeout aguardando WorldResetCompletedEvent. signature='{signature}', timeoutMs={_timeoutMs}.");
                return;
            }

            string reason = await tcs.Task;

            DebugUtility.LogVerbose(typeof(WorldResetCompletionGate),
                $"[SceneFlowGate] Concluído. signature='{signature}', reason='{reason ?? "<null>"}'.");
        }

        private void OnCompleted(WorldResetCompletedEvent evt)
        {
            string signature = evt.ContextSignature ?? string.Empty;
            string reason = evt.Reason;

            if (string.IsNullOrEmpty(signature))
            {
                DebugUtility.LogWarning(typeof(WorldResetCompletionGate),
                    $"[SceneFlowGate] WorldResetCompletedEvent recebido com ContextSignature vazia. reason='{reason ?? "<null>"}'.");

                // Não faz cache nem tenta completar awaiters sem assinatura correlacionável.
                return;
            }

            TaskCompletionSource<string> tcs = null;

            lock (_pending)
            {
                PruneCompletedCacheIfNeeded();

                // Mantém o primeiro reason (estável p/ debug). Se preferir "último ganha", troque por _completedReasons[signature] = reason;
                if (!_completedReasons.ContainsKey(signature))
                {
                    _completedReasons.Add(signature, reason);
                }

                if (_pending.TryGetValue(signature, out tcs))
                {
                    _pending.Remove(signature);
                }
            }

            if (tcs != null)
            {
                tcs.TrySetResult(reason);
            }

            DebugUtility.LogVerbose(typeof(WorldResetCompletionGate),
                $"[SceneFlowGate] WorldResetCompletedEvent recebido. signature='{signature}', reason='{reason ?? "<null>"}'.");
        }

        private void PruneCompletedCacheIfNeeded()
        {
            if (_completedReasons.Count <= MaxCompletedCacheEntries)
            {
                return;
            }

            _completedReasons.Clear();

            DebugUtility.LogVerbose(typeof(WorldResetCompletionGate),
                $"[SceneFlowGate] Cache de completedReasons limpo (atingiu limite > {MaxCompletedCacheEntries}).");
        }
    }
}




