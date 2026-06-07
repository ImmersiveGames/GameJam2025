using System;
using _ImmersiveGames.NewScripts.Actors.ObjectEmission.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;

namespace _ImmersiveGames.NewScripts.Actors.ObjectEmission.Bootstrap
{
    public static class ObjectEmissionRuntimeComposer
    {
        private static bool _runtimeComposed;

        public static void Install(RuntimeModeConfig runtimeModeConfig)
        {
            ValidateRuntimeModeConfigOrFail(runtimeModeConfig);

            DebugUtility.Log(typeof(ObjectEmissionRuntimeComposer),
                "[OBS][ObjectEmission][Composer] installer concluded.",
                DebugUtility.Colors.Info);
        }

        public static void ComposeRuntime(RuntimeModeConfig runtimeModeConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(ObjectEmissionRuntimeComposer));

            if (_runtimeComposed)
            {
                return;
            }

            ValidateRuntimeModeConfigOrFail(runtimeModeConfig);

            if (!DependencyManager.Provider.TryGetGlobal<IPoolService>(out var poolService) || poolService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ObjectEmission] IPoolService obrigatorio ausente para compor o ObjectEmission runtime.");
            }

            ObjectEmissionPoolRuntimeBridge.EnsureCreated(poolService);

            _runtimeComposed = true;

            DebugUtility.Log(typeof(ObjectEmissionRuntimeComposer),
                "[OBS][ObjectEmission][Composer] runtime bridge composed.",
                DebugUtility.Colors.Info);
        }

        private static void ValidateRuntimeModeConfigOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ObjectEmission] RuntimeModeConfig obrigatorio ausente.");
            }

            if (runtimeModeConfig.compositionProfile != CompositionProfileKind.Base11Sandbox)
            {
                throw new InvalidOperationException($"[FATAL][Config][ObjectEmission] compositionProfile invalido. compositionProfile='{runtimeModeConfig.compositionProfile}'.");
            }
        }
    }
}
