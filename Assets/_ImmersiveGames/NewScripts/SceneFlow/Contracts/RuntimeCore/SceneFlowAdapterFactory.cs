using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.LoadingFade.Fade.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Adapters;
namespace _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore
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
            if (provider != null && provider.TryGetGlobal<IFadeService>(out var resolved) && resolved != null)
            {
                DebugUtility.LogVerbose(typeof(SceneFlowAdapterFactory),
                    "[SceneFlow] FadeService resolvido explicitamente para o SceneFlowFadeAdapter.");
                return new SceneFlowFadeAdapter(resolved);
            }

            throw new InvalidOperationException("[FATAL][Config][SceneFlow] IFadeService obrigatorio ausente no DI global antes da composicao do SceneFlowFadeAdapter.");
        }
    }
}

