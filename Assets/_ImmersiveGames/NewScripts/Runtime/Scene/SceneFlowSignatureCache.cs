using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Runtime.SceneFlow;
namespace _ImmersiveGames.NewScripts.Runtime.Scene
{
    /// <summary>
    /// Cache simples para expor a última assinatura de SceneFlow observada em runtime.
    /// </summary>
    public interface ISceneFlowSignatureCache
    {
        bool TryGetLast(out string signature, out SceneFlowProfileId profileId, out string targetScene);
    }

    /// <summary>
    /// Observa SceneTransitionStarted/Completed e guarda a última assinatura disponível.
    /// </summary>
    public sealed class SceneFlowSignatureCache : ISceneFlowSignatureCache, System.IDisposable
    {
        private readonly EventBinding<SceneTransitionStartedEvent> _startedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _completedBinding;

        private string _lastSignature = string.Empty;
        private SceneFlowProfileId _lastProfileId;
        private string _lastTargetScene = string.Empty;
        private bool _disposed;

        public SceneFlowSignatureCache()
        {
            _startedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            _completedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);

            EventBus<SceneTransitionStartedEvent>.Register(_startedBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_completedBinding);
        }

        public bool TryGetLast(out string signature, out SceneFlowProfileId profileId, out string targetScene)
        {
            signature = _lastSignature;
            profileId = _lastProfileId;
            targetScene = _lastTargetScene;
            return !string.IsNullOrWhiteSpace(signature);
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            UpdateFromContext(evt.Context);
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            UpdateFromContext(evt.Context);
        }

        private void UpdateFromContext(SceneTransitionContext context)
        {
            _lastSignature = SceneTransitionSignatureUtil.Compute(context);
            _lastProfileId = context.TransitionProfileId;
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
