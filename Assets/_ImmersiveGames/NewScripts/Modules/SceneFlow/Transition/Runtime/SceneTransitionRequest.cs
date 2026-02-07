#nullable enable
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime
{
    /// <summary>
    /// Descreve um pedido explícito de transição de cena para o pipeline NewScripts.
    /// </summary>
    public sealed class SceneTransitionRequest
    {
        public IReadOnlyList<string> ScenesToLoad { get; }
        public IReadOnlyList<string> ScenesToUnload { get; }
        public string TargetActiveScene { get; }
        public bool UseFade { get; }

        public SceneRouteId RouteId { get; }
        public TransitionStyleId StyleId { get; }
        public SceneTransitionPayload Payload { get; }
        public string Reason { get; }

        public SceneFlowProfileId TransitionProfileId { get; }

        // Compatibilidade: logging / debug pode exibir o texto do profile.
        public string TransitionProfileName => TransitionProfileId.Value;

        /// <summary>
        /// Assinatura/correlation id para observabilidade do contexto.
        /// Em geral é preenchida por SceneTransitionSignature.Compute(BuildContext(request)).
        /// </summary>
        public string ContextSignature { get; }

        /// <summary>
        /// (Opcional) Origem do pedido para diagnóstico (ex.: "QA/Levels/InPlace/L01", "Navigation/MenuPlayButton").
        /// </summary>
        public string RequestedBy { get; }

        public SceneTransitionRequest(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            bool useFade = true,
            SceneFlowProfileId transitionProfileId = default,
            string? contextSignature = null,
            string? requestedBy = null,
            string? reason = null)
        {
            ScenesToLoad = scenesToLoad;
            ScenesToUnload = scenesToUnload;
            TargetActiveScene = targetActiveScene;
            UseFade = useFade;
            TransitionProfileId = transitionProfileId;

            RouteId = SceneRouteId.None;
            StyleId = TransitionStyleId.None;
            Payload = SceneTransitionPayload.Empty;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();

            // Mantém propriedades não-nulas (evita NRT warnings/erros).
            ContextSignature = string.IsNullOrWhiteSpace(contextSignature) ? string.Empty : contextSignature.Trim();
            RequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? string.Empty : requestedBy.Trim();
        }

        public SceneTransitionRequest(
            SceneRouteId routeId,
            TransitionStyleId styleId,
            SceneTransitionPayload payload,
            SceneFlowProfileId transitionProfileId = default,
            bool useFade = true,
            string? contextSignature = null,
            string? requestedBy = null,
            string? reason = null)
            : this(
                payload?.ScenesToLoad ?? new List<string>(),
                payload?.ScenesToUnload ?? new List<string>(),
                payload?.TargetActiveScene ?? string.Empty,
                useFade,
                transitionProfileId,
                contextSignature,
                requestedBy,
                reason)
        {
            RouteId = routeId;
            StyleId = styleId;
            Payload = payload ?? SceneTransitionPayload.Empty;
        }
    }
}
