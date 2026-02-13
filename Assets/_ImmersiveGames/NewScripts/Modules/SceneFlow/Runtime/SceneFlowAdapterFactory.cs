using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Adapters;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime
{
    /// <summary>
    /// Adapters para integrar SceneFlow no pipeline NewScripts sem depender de tipos/DI legados.
    ///
    /// Regras:
    /// - Fade: somente IFadeService (sem fallback legado).
    /// - Loader: enquanto não migra, usa SceneManagerLoaderAdapter como fallback.
    /// - Profile: exige resolver registrado em DI (configurado no bootstrap fail-fast).
    /// - Strict/Release: policy via IRuntimeModeProvider + DEGRADED_MODE via IDegradedModeReporter.
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
            // Comentário: o adapter é responsável por aplicar policy Strict/Release e por reportar DEGRADED_MODE.
            // O serviço de Fade deve ser “duro”: se pré-condições não são atendidas, ele falha explicitamente.
            ResolveOrCreatePolicy(provider, out var modeProvider, out var degradedReporter);

            IFadeService fadeService = null;
            if (provider != null && provider.TryGetGlobal<IFadeService>(out var resolved) && resolved != null)
            {
                fadeService = resolved;
            }

            if (fadeService != null)
            {
                DebugUtility.LogVerbose(typeof(SceneFlowAdapterFactory),
                    "[SceneFlow] Usando IFadeService via adapter (NewScripts).");
            }
            else
            {
                // Comentário: não é erro imediato aqui. O adapter decide: Strict => throw; Release => DEGRADED_MODE + no-op.
                DebugUtility.LogWarning(typeof(SceneFlowAdapterFactory),
                    "[SceneFlow] IFadeService não encontrado no DI global. " +
                    "O comportamento dependerá da policy (Strict/Release).");
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
                profileResolver,
                modeProvider,
                degradedReporter);
        }

        private static void ResolveOrCreatePolicy(
            IDependencyProvider provider,
            out IRuntimeModeProvider modeProvider,
            out IDegradedModeReporter degradedReporter)
        {
            modeProvider = null;
            degradedReporter = null;

            if (provider != null)
            {
                provider.TryGetGlobal(out modeProvider);
                provider.TryGetGlobal(out degradedReporter);
            }

            modeProvider ??= new UnityRuntimeModeProvider();
            degradedReporter ??= new DegradedModeReporter();
        }
    }
}
