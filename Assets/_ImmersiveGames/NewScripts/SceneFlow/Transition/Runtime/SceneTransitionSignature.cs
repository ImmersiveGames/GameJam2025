using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
namespace _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime
{
    /// <summary>
    /// Centraliza a assinatura de correlacao usada entre SceneFlow e WorldReset.
    ///
    /// Importante:
    /// - A assinatura canonica e <see cref="SceneTransitionContext.ContextSignature"/>.
    /// - <see cref="SceneTransitionContext.ToString"/> deve ser tratado como string de debug/log.
    /// </summary>
    public static class SceneTransitionSignature
    {
        public static string Compute(SceneTransitionContext context)
        {
            return context.ContextSignature ?? string.Empty;
        }

        public static SceneTransitionContext BuildContext(SceneTransitionRequest request, SceneRouteKind routeKind = SceneRouteKind.Unspecified)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            IReadOnlyList<string> loadList = NormalizeList(request.ScenesToLoad);
            IReadOnlyList<string> unloadList = NormalizeList(request.ScenesToUnload);
            return new SceneTransitionContext(
                scenesToLoad: loadList,
                scenesToUnload: unloadList,
                targetActiveScene: request.TargetActiveScene,
                useFade: request.UseFade,
                routeId: request.RouteId,
                routeKind: routeKind,
                transitionStyle: request.TransitionStyle,
                reason: request.Reason,
                transitionProfile: request.TransitionProfile,
                routeRef: request.ResolvedRouteRef,
                contextSignature: request.ContextSignature);
        }

        private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string> source)
        {
            if (source == null)
            {
                return Array.Empty<string>();
            }

            return source
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
                .Select(entry => entry.Trim())
                .ToArray();
        }
    }
}

