using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime
{

    /// <summary>
    /// Observa SceneTransitionStarted/Completed e guarda a Ãºltima assinatura disponÃ­vel.
    /// </summary>
        /// <summary>
    /// OWNER: cache da ultima assinatura/profile/cena para consumidores de leitura.
    /// NAO E OWNER: dedupe de fluxo de transicao ou controle de gates.
    /// PUBLISH/CONSUME: consome SceneTransitionStartedEvent e SceneTransitionCompletedEvent; nao publica eventos.
    /// Fases tocadas: TransitionStarted e TransitionCompleted.
    /// </summary>
public sealed class SceneFlowSignatureCache : ISceneFlowSignatureCache, System.IDisposable
    {
        private readonly EventBinding<SceneTransitionStartedEvent> _startedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _completedBinding;

        // Boundary: read-model da ultima assinatura observada (Started/Completed).
        // Nao decide aceite/rejeicao de request; dedupe canonico fica no SceneTransitionService.
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
            _lastSignature = SceneTransitionSignature.Compute(context);
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



