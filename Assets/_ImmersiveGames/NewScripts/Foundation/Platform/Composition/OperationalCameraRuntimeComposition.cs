using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

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
                "installer='validated' status='ready'.",
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
            EnsureOperationalCameraProviderRegisteredOrFail();

            _runtimeComposed = true;
        }

        private static void EnsureOperationalCameraProviderRegisteredOrFail()
        {
            var dependencyManager = DependencyManager.Instance;
            if (dependencyManager == null)
            {
                throw new System.InvalidOperationException("[FATAL][Config][OperationalCameraRuntimeComposition] DependencyManager.Instance obrigatorio ausente para registrar IOperationalCameraProvider.");
            }

            if (dependencyManager.TryGetGlobal<IOperationalCameraProvider>(out _))
            {
                throw new System.InvalidOperationException("[FATAL][Config][OperationalCameraRuntimeComposition] IOperationalCameraProvider ja registrado antes do OperationalCameraRuntime composition.");
            }

            IOperationalCameraProvider provider = new UnityOperationalCameraProvider();
            dependencyManager.RegisterGlobal<IOperationalCameraProvider>(provider, allowOverride: false);

            DebugUtility.Log(typeof(OperationalCameraRuntimeComposition),
                "provider='UnityOperationalCameraProvider' registered contract='IOperationalCameraProvider'.",
                DebugUtility.Colors.Info);
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
