using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore
{
    public sealed class SceneFlowSignatureCache : ISceneFlowSignatureCache, System.IDisposable
    {
        private readonly EventBinding<SceneTransitionStartedEvent> _startedBinding;
        private readonly EventBinding<SceneTransitionFadeInCompletedEvent> _fadeInCompletedBinding;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _scenesReadyBinding;
        private readonly EventBinding<SceneTransitionBeforeFadeOutEvent> _beforeFadeOutBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _completedBinding;

        private string _lastSignature = string.Empty;
        private string _lastTargetScene = string.Empty;
        private bool _disposed;

        public SceneFlowSignatureCache()
        {
            _startedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            _fadeInCompletedBinding = new EventBinding<SceneTransitionFadeInCompletedEvent>(OnTransitionFadeInCompleted);
            _scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnTransitionScenesReady);
            _beforeFadeOutBinding = new EventBinding<SceneTransitionBeforeFadeOutEvent>(OnTransitionBeforeFadeOut);
            _completedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);

            EventBus<SceneTransitionStartedEvent>.Register(_startedBinding);
            EventBus<SceneTransitionFadeInCompletedEvent>.Register(_fadeInCompletedBinding);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_scenesReadyBinding);
            EventBus<SceneTransitionBeforeFadeOutEvent>.Register(_beforeFadeOutBinding);
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

        private void OnTransitionFadeInCompleted(SceneTransitionFadeInCompletedEvent evt)
        {
            UpdateFromContext(evt.context);
        }

        private void OnTransitionScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            UpdateFromContext(evt.context);
        }

        private void OnTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
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
            try { EventBus<SceneTransitionFadeInCompletedEvent>.Unregister(_fadeInCompletedBinding); } catch { }
            try { EventBus<SceneTransitionScenesReadyEvent>.Unregister(_scenesReadyBinding); } catch { }
            try { EventBus<SceneTransitionBeforeFadeOutEvent>.Unregister(_beforeFadeOutBinding); } catch { }
            try { EventBus<SceneTransitionCompletedEvent>.Unregister(_completedBinding); } catch { }
        }
    }
}

