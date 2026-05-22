using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneComposition;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static class RuntimePersistentScenesComposition
    {
        private static Task _guaranteeTask;
        private static RuntimeModeConfig _guaranteeRuntimeModeConfig;
        private static bool _policySourceLogged;

        public static void Install(RuntimeModeConfig runtimeModeConfig)
        {
            ValidateRuntimeModeConfigOrFail(runtimeModeConfig);
            EnsureStartupRouteCompatibilityOrFail(runtimeModeConfig);

            DebugUtility.LogVerbose(typeof(RuntimePersistentScenesComposition),
                "[OBS][RuntimeMode][PersistentScenes] installer='validated' status='ready'.",
                DebugUtility.Colors.Info);
        }

        public static void ComposeRuntime(RuntimeModeConfig runtimeModeConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(RuntimePersistentScenesComposition));

            ValidateRuntimeModeConfigOrFail(runtimeModeConfig);
            EnsureStartupRouteCompatibilityOrFail(runtimeModeConfig);

            if (_guaranteeTask != null)
            {
                return;
            }

            _guaranteeRuntimeModeConfig = runtimeModeConfig;
            _guaranteeTask = EnsurePersistentScenesGuaranteedAsync(runtimeModeConfig);

            DebugUtility.Log(typeof(RuntimePersistentScenesComposition),
                "[OBS][RuntimeMode][PersistentScenes] guarantee='started'.",
                DebugUtility.Colors.Info);
        }

        public static async Task AwaitGuaranteedAsync(RuntimeModeConfig runtimeModeConfig)
        {
            ValidateRuntimeModeConfigOrFail(runtimeModeConfig);

            if (_guaranteeTask == null)
            {
                string message = "[FATAL][Config][RuntimeMode][PersistentScenes] garantia obrigatoria ausente. ComposeRuntime deve executar antes do startup route.";
                DebugUtility.LogError(typeof(RuntimePersistentScenesComposition), message);
                throw new InvalidOperationException(message);
            }

            if (!ReferenceEquals(_guaranteeRuntimeModeConfig, runtimeModeConfig))
            {
                string message = "[FATAL][Config][RuntimeMode][PersistentScenes] runtimeModeConfig inconsistente para a garantia persistente.";
                DebugUtility.LogError(typeof(RuntimePersistentScenesComposition), message);
                throw new InvalidOperationException(message);
            }

            await _guaranteeTask;
        }

        private static async Task EnsurePersistentScenesGuaranteedAsync(RuntimeModeConfig runtimeModeConfig)
        {
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy = ResolvePersistentScenesPolicyOrFail(runtimeModeConfig);
            if (persistentScenesPolicy == null)
            {
                DebugUtility.Log(typeof(RuntimePersistentScenesComposition),
                    "[OBS][RuntimeMode][PersistentScenes] policy='none' status='skipped'.",
                    DebugUtility.Colors.Info);
                return;
            }

            IReadOnlyList<string> persistentSceneNames = persistentScenesPolicy.ResolveSceneNamesOrFail(nameof(RuntimePersistentScenesComposition));
            if (persistentSceneNames.Count == 0)
            {
                DebugUtility.Log(typeof(RuntimePersistentScenesComposition),
                    $"[OBS][RuntimeMode][PersistentScenes] policyId='{persistentScenesPolicy.PolicyId}' status='no_entries'.",
                    DebugUtility.Colors.Info);
                return;
            }

            SceneCompositionExecutor sceneCompositionExecutor = new();
            SceneCompositionResult result = await sceneCompositionExecutor.ApplyAsync(
                new SceneCompositionRequest(
                    SceneCompositionScope.Local,
                    reason: $"runtime_persistent_scenes:{persistentScenesPolicy.PolicyId}",
                    correlationId: $"{persistentScenesPolicy.PolicyId}|persistent_scenes",
                    scenesToLoad: persistentSceneNames,
                    scenesToUnload: Array.Empty<string>(),
                    activeScene: string.Empty));

            if (!result.Success)
            {
                string message =
                    $"[FATAL][Config][RuntimeMode][PersistentScenes] preload failed policyId='{persistentScenesPolicy.PolicyId}' correlationId='{result.CorrelationId}' reason='{result.Reason}'.";
                DebugUtility.LogError(typeof(RuntimePersistentScenesComposition), message);
                throw new InvalidOperationException(message);
            }

            DebugUtility.Log(typeof(RuntimePersistentScenesComposition),
                $"[OBS][RuntimeMode][PersistentScenes] guaranteed policyId='{persistentScenesPolicy.PolicyId}' scenes=[{string.Join(", ", persistentSceneNames)}] correlationId='{result.CorrelationId}'.",
                DebugUtility.Colors.Info);
        }

        private static void ValidateRuntimeModeConfigOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][RuntimeMode][PersistentScenes] RuntimeModeConfig obrigatorio ausente.");
            }

            if (runtimeModeConfig.compositionProfile != CompositionProfileKind.Base11Sandbox)
            {
                throw new InvalidOperationException($"[FATAL][Config][RuntimeMode][PersistentScenes] compositionProfile invalido. compositionProfile='{runtimeModeConfig.compositionProfile}'.");
            }
        }

        private static void EnsureStartupRouteCompatibilityOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            OperationalRouteAsset startupRoute = SessionOperationalRuntimeConfigResolver.ResolveStartupRouteOrFail(runtimeModeConfig);
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy = ResolvePersistentScenesPolicyOrFail(runtimeModeConfig);

            if (startupRoute == null || persistentScenesPolicy == null)
            {
                return;
            }

            if (!startupRoute.TryValidateAgainstPersistentScenesPolicy(persistentScenesPolicy, out string validationError))
            {
                throw new InvalidOperationException($"[FATAL][Config][RuntimeMode][PersistentScenes] {validationError}");
            }
        }

        private static RuntimePersistentScenesPolicyAsset ResolvePersistentScenesPolicyOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (RuntimeConfigRegistry.TryGetSnapshot(out IRuntimeConfigSnapshotReadOnly snapshot) && snapshot != null)
            {
                IRuntimePolicyConfigGroupReadOnly runtimePolicy = snapshot.RuntimePolicy;
                if (runtimePolicy == null)
                {
                    throw new InvalidOperationException("[FATAL][Config][RuntimeMode][PersistentScenes] RuntimeConfigRegistry invariant breach: snapshot.RuntimePolicy obrigatorio ausente.");
                }

                RuntimePersistentScenesPolicyAsset registryPolicy = runtimePolicy.RuntimePersistentScenesPolicy;
                string policyValidationError = string.Empty;
                bool registryPolicyValid = registryPolicy != null && registryPolicy.TryValidate(out policyValidationError);
                if (!registryPolicyValid)
                {
                    throw new InvalidOperationException($"[FATAL][Config][RuntimeMode][PersistentScenes] RuntimeConfigRegistry invariant breach: RuntimePersistentScenesPolicyAsset ausente/invalido no snapshot. detail='{policyValidationError}'.");
                }

                LogPolicySourceOnce(registryPolicy);
                return registryPolicy;
            }

            throw new InvalidOperationException("[FATAL][Config][RuntimeMode][PersistentScenes] RuntimeConfigRegistry snapshot obrigatorio ausente para RuntimePersistentScenesPolicy migrado.");
        }

        private static void LogPolicySourceOnce(RuntimePersistentScenesPolicyAsset policy)
        {
            if (_policySourceLogged)
            {
                return;
            }

            _policySourceLogged = true;

            DebugUtility.Log(typeof(RuntimePersistentScenesComposition),
                $"[OBS][RuntimePolicy][Config] RuntimePersistentScenesComposition using RuntimeConfigRegistry persistentScenesPolicy. policyId='{policy.PolicyId}'.",
                DebugUtility.Colors.Info);
        }
    }
}


