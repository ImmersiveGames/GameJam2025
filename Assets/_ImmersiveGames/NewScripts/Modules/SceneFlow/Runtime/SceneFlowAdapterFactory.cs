using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Adapters;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime
{
    /// <summary>
    /// Adapters para integrar SceneFlow no pipeline NewScripts sem depender de tipos/DI legados.
    /// </summary>
    public static class SceneFlowAdapterFactory
    {
        public static ISceneFlowLoaderAdapter CreateLoaderAdapter()
        {
            DebugUtility.LogVerbose(typeof(SceneFlowAdapterFactory),
                "[SceneFlow] Usando SceneManagerLoaderAdapter (loader nativo).");
            return new SceneManagerLoaderAdapter();
        }

        public static ISceneFlowFadeAdapter CreateFadeAdapter(IDependencyProvider provider)
        {
            IFadeService fadeService = null;
            if (provider != null && provider.TryGetGlobal<IFadeService>(out var resolved) && resolved != null)
            {
                fadeService = resolved;
            }

            if (provider == null || !provider.TryGetGlobal(out SceneTransitionProfileResolver profileResolver) || profileResolver == null)
            {
                throw new InvalidOperationException(
                    "SceneTransitionProfileResolver não encontrado no DI global. " +
                    "Garanta que RegisterSceneFlowTransitionProfiles seja executado antes de RegisterSceneFlowNative.");
            }

            DebugUtility.LogVerbose(typeof(SceneFlowAdapterFactory),
                "[SceneFlow] Usando SceneTransitionProfileResolver via DI global.");

            return new SceneFlowFadeAdapter(
                fadeService,
                profileResolver);
        }
    }
}
