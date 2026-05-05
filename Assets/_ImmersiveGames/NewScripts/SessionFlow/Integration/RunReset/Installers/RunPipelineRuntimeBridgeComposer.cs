using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Ownership;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset.Installers
{
    /// <summary>
    /// Composer temporario de runtime para a bridge Unity-driven do Run Pipeline.
    /// </summary>
    public static class RunPipelineRuntimeBridgeComposer
    {
        private const string RunEndBridgeObjectName = "[NewScripts] GameRunEndedEventBridge";

        private static bool _runtimeComposed;

        public static void ComposeRuntime()
        {
            if (_runtimeComposed)
            {
                DebugUtility.Log(typeof(RunPipelineRuntimeBridgeComposer),
                    "[OBS][RunPipeline][Integration] runtime_bridge_already_composed",
                    DebugUtility.Colors.Info);
                return;
            }

            ValidateNoManualSceneBridgeOrFail();
            RunEndBridgeRuntimeComposer.ComposeOrFail();
            CreateAndInitializeBridgeOrFail();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(RunPipelineRuntimeBridgeComposer),
                "[OBS][RunPipeline][Integration] runtime_bridge_composed",
                DebugUtility.Colors.Info);
        }

        private static void ValidateNoManualSceneBridgeOrFail()
        {
            if (Object.FindFirstObjectByType<GameRunEndedEventBridge>() == null)
            {
                return;
            }

            DebugUtility.Log(typeof(RunPipelineRuntimeBridgeComposer),
                "[OBS][RunPipeline][Integration] runtime_bridge_invalid_manual_scene_instance",
                DebugUtility.Colors.Error);

            throw new InvalidOperationException(
                "[FATAL][Config][RunPipeline] GameRunEndedEventBridge manual scene instance detected. Use RunPipelineRuntimeBridgeComposer only.");
        }

        private static void CreateAndInitializeBridgeOrFail()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IRunEndMaterializationService>(out var runEndMaterializationService) || runEndMaterializationService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][RunPipeline] IRunEndMaterializationService ausente apos RunEndBridgeRuntimeComposer.ComposeOrFail().");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IRunContinuationSelectionRoutingService>(out var runContinuationSelectionRoutingService) || runContinuationSelectionRoutingService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][RunPipeline] IRunContinuationSelectionRoutingService ausente apos RunEndBridgeRuntimeComposer.ComposeOrFail().");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IRunContinuationOwnershipService>(out var runContinuationOwnershipService) || runContinuationOwnershipService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][RunPipeline] IRunContinuationOwnershipService ausente apos RunEndBridgeRuntimeComposer.ComposeOrFail().");
            }

            var go = new GameObject(RunEndBridgeObjectName);
            go.SetActive(false);

            var bridge = go.AddComponent<GameRunEndedEventBridge>();
            if (bridge == null)
            {
                throw new InvalidOperationException("[FATAL][Config][RunPipeline] Nao foi possivel adicionar GameRunEndedEventBridge ao GameObject de composicao.");
            }

            bridge.Initialize(
                runEndMaterializationService,
                runContinuationSelectionRoutingService,
                runContinuationOwnershipService);

            Object.DontDestroyOnLoad(go);
            go.SetActive(true);
        }
    }
}
