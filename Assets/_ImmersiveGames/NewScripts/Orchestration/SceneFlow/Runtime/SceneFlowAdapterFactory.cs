using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Fade.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Adapters;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Runtime
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

            DebugUtility.LogVerbose(typeof(SceneFlowAdapterFactory),
                "[SceneFlow] Usando SceneFlowFadeAdapter com SceneTransitionProfile direto (sem resolver por id).");

            return new SceneFlowFadeAdapter(fadeService);
        }
    }
}
