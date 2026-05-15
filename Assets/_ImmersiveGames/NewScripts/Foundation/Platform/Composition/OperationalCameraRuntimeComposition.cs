using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;

namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static class OperationalCameraRuntimeComposition
    {
        private static bool _runtimeComposed;

        public static void Install(RuntimeModeConfig runtimeModeConfig)
        {
            ValidateRuntimeModeConfigOrFail(runtimeModeConfig);
            _ = CameraRuntimeConfigResolver.ResolveOrFail(runtimeModeConfig);

            DebugUtility.LogVerbose(typeof(OperationalCameraRuntimeComposition),
                "[OBS][RuntimeMode][OperationalCamera] installer='validated' status='ready'.",
                DebugUtility.Colors.Info);
        }

        public static void ComposeRuntime(RuntimeModeConfig runtimeModeConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(OperationalCameraRuntimeComposition));
            ValidateRuntimeModeConfigOrFail(runtimeModeConfig);

            if (_runtimeComposed)
            {
                return;
            }

            _ = UnityOperationalCameraRuntimeAdapter.EnsureOperationalCameraOrFail(
                runtimeModeConfig,
                source: "OperationalCameraRuntimeComposition",
                reason: "composition_bootstrap");

            _runtimeComposed = true;
        }

        private static void ValidateRuntimeModeConfigOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new System.InvalidOperationException("[FATAL][Config][OperationalCameraRuntimeComposition] RuntimeModeConfig obrigatorio ausente.");
            }

            if (runtimeModeConfig.compositionProfile != CompositionProfileKind.Base11Sandbox)
            {
                throw new System.InvalidOperationException($"[FATAL][Config][OperationalCameraRuntimeComposition] compositionProfile invalido. compositionProfile='{runtimeModeConfig.compositionProfile}'.");
            }
        }
    }
}
