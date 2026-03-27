using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime
{
    public sealed class SceneFlowSignatureCache : ISceneFlowSignatureCache, System.IDisposable
    {
        private readonly EventBinding<SceneTransitionStartedEvent> _startedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _completedBinding;

        private string _lastSignature = string.Empty;
        private string _lastTargetScene = string.Empty;
        private bool _disposed;

        public SceneFlowSignatureCache()
        {
            _startedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            _completedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);

            EventBus<SceneTransitionStartedEvent>.Register(_startedBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_completedBinding);
        }

        public bool TryGetLast(out string signature, out string targetScene)
        {
            signature = _lastSignature;
            targetScene = _lastTargetScene;
            return !string.IsNullOrWhiteSpace(signature);
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            UpdateFromContext(evt.context);
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            UpdateFromContext(evt.context);
        }

        private void UpdateFromContext(SceneTransitionContext context)
        {
            _lastSignature = SceneTransitionSignature.Compute(context);
            _lastTargetScene = context.TargetActiveScene ?? string.Empty;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try { EventBus<SceneTransitionStartedEvent>.Unregister(_startedBinding); } catch { }
            try { EventBus<SceneTransitionCompletedEvent>.Unregister(_completedBinding); } catch { }
        }
    }
}
