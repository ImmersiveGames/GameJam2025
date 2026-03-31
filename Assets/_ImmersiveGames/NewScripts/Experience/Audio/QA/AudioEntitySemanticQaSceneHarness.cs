using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.Audio.Config;
using _ImmersiveGames.NewScripts.Experience.Audio.Runtime;
using _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Core;
using _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Models;
using _ImmersiveGames.NewScripts.Experience.Audio.Semantics;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Experience.Audio.QA
{
    /// <summary>
    /// Harness de QA para validar F6 standalone sem emitter de cena.
    /// </summary>
    public sealed class AudioEntitySemanticQaSceneHarness : MonoBehaviour
    {
        [Header("Semantic Map")]
        [SerializeField] private EntityAudioSemanticMapAsset semanticMap;
        [SerializeField] private string globalPurpose = "semantic_global_direct";
        [SerializeField] private string spatialPurpose = "semantic_spatial_direct";
        [SerializeField] private string pooledPurpose = "semantic_spatial_pooled";
        [SerializeField] private string missingPurpose = "semantic_missing";

        [Header("Owner Context")]
        [SerializeField] private Transform ownerTransform;
        [SerializeField] private Vector3 fallbackSpatialPosition = new Vector3(0f, 1.5f, 2f);

        [Header("QA Cue Delegation")]
        [SerializeField] private AudioSfxCueAsset explicitCue;

        [SerializeField] private bool verboseLogs = true;

        private IEntityAudioService _entityAudioService;
        private IAudioPlaybackHandle _lastHandle = NullAudioPlaybackHandle.Instance;

        [ContextMenu("QA/Audio/Entity/Validate Setup")]
        private void ValidateSetup()
        {
            if (!TryEnsureService())
            {
                LogError("ValidateSetup", "IEntityAudioService not available in global DI");
                return;
            }

            string mapInfo = semanticMap != null ? semanticMap.name : "null";
            int entries = semanticMap != null && semanticMap.Entries != null ? semanticMap.Entries.Count : 0;
            LogInfo("ValidateSetup",
                $"ok map='{mapInfo}' entries={entries} globalPurpose='{globalPurpose}' spatialPurpose='{spatialPurpose}' pooledPurpose='{pooledPurpose}' owner='{SafeName(ownerTransform)}'");
        }

        [ContextMenu("QA/Audio/Entity/Play Purpose Global")]
        private void PlayPurposeGlobal()
        {
            if (!TryEnsureService())
            {
                return;
            }

            var handle = _entityAudioService.PlayPurpose(
                purpose: globalPurpose,
                owner: ownerTransform,
                context: AudioPlaybackContext.Global(reason: "qa_entity_purpose_global"));

            LogHandle("PlayPurposeGlobal", handle, globalPurpose);
        }

        [ContextMenu("QA/Audio/Entity/Play Purpose Spatial")]
        private void PlayPurposeSpatial()
        {
            if (!TryEnsureService())
            {
                return;
            }

            var handle = _entityAudioService.PlayPurpose(
                purpose: spatialPurpose,
                owner: ownerTransform,
                context: AudioPlaybackContext.Spatial(
                    worldPosition: ownerTransform != null ? ownerTransform.position : fallbackSpatialPosition,
                    followTarget: ownerTransform,
                    reason: "qa_entity_purpose_spatial"));

            LogHandle("PlayPurposeSpatial", handle, spatialPurpose);
        }

        [ContextMenu("QA/Audio/Entity/Play Purpose Pooled")]
        private void PlayPurposePooled()
        {
            if (!TryEnsureService())
            {
                return;
            }

            var handle = _entityAudioService.PlayPurpose(
                purpose: pooledPurpose,
                owner: ownerTransform,
                context: AudioPlaybackContext.Spatial(
                    worldPosition: ownerTransform != null ? ownerTransform.position : fallbackSpatialPosition,
                    followTarget: ownerTransform,
                    reason: "qa_entity_purpose_pooled"));

            LogHandle("PlayPurposePooled", handle, pooledPurpose);
        }

        [ContextMenu("QA/Audio/Entity/Play Purpose Missing")]
        private void PlayPurposeMissing()
        {
            if (!TryEnsureService())
            {
                return;
            }

            var handle = _entityAudioService.PlayPurpose(
                purpose: missingPurpose,
                owner: ownerTransform,
                context: AudioPlaybackContext.Global(reason: "qa_entity_purpose_missing"));

            LogHandle("PlayPurposeMissing", handle, missingPurpose);
        }

        [ContextMenu("QA/Audio/Entity/Play Cue")]
        private void PlayCue()
        {
            if (!TryEnsureService())
            {
                return;
            }

            if (explicitCue == null)
            {
                LogError("PlayCue", "explicitCue is null");
                return;
            }

            var handle = _entityAudioService.PlayCue(
                cue: explicitCue,
                context: AudioPlaybackContext.Global(reason: "qa_entity_play_cue_direct"));

            LogHandle("PlayCue", handle, explicitCue.name);
        }

        [ContextMenu("QA/Audio/Entity/Stop Last Handle")]
        private void StopLastHandle()
        {
            if (_lastHandle == null || !_lastHandle.IsValid)
            {
                LogInfo("StopLastHandle", "no valid handle to stop");
                return;
            }

            _lastHandle.Stop();
            LogInfo("StopLastHandle", $"requested handleIsPlayingAfterStop={_lastHandle.IsPlaying}");
        }

        [ContextMenu("QA/Audio/Entity/Log Harness State")]
        private void LogHarnessState()
        {
            bool serviceResolved = TryEnsureService();
            bool handleValid = _lastHandle != null && _lastHandle.IsValid;
            bool handlePlaying = _lastHandle != null && _lastHandle.IsPlaying;

            DebugUtility.Log(typeof(AudioEntitySemanticQaSceneHarness),
                $"[QA][Audio][Entity] action='LogHarnessState' serviceResolved={serviceResolved} map='{SafeName(semanticMap)}' globalPurpose='{globalPurpose}' spatialPurpose='{spatialPurpose}' pooledPurpose='{pooledPurpose}' missingPurpose='{missingPurpose}' owner='{SafeName(ownerTransform)}' explicitCue='{SafeName(explicitCue)}' lastHandleValid={handleValid} lastHandlePlaying={handlePlaying}.",
                DebugUtility.Colors.Info);
        }

        private bool TryEnsureService()
        {
            if (_entityAudioService != null)
            {
                return true;
            }

            if (!Application.isPlaying)
            {
                return false;
            }

            if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
            {
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal(out _entityAudioService) || _entityAudioService == null)
            {
                return false;
            }

            // Permite QA standalone sem depender de wiring extra no bootstrap.
            if (_entityAudioService is AudioEntitySemanticService semanticService)
            {
                semanticService.SetSemanticMap(semanticMap, "qa_harness");
            }

            LogInfo("ResolveService", "IEntityAudioService resolved from global DI");
            return true;
        }

        private void LogHandle(string action, IAudioPlaybackHandle handle, string payload)
        {
            _lastHandle = handle ?? NullAudioPlaybackHandle.Instance;
            bool valid = handle != null && handle.IsValid;
            bool playing = handle != null && handle.IsPlaying;
            LogInfo(action, $"payload='{payload}' handleValid={valid} isPlaying={playing}");
        }

        private static string SafeName(Object target)
        {
            return target != null ? target.name : "null";
        }

        private void LogInfo(string action, string detail)
        {
            if (!verboseLogs)
            {
                return;
            }

            DebugUtility.Log(typeof(AudioEntitySemanticQaSceneHarness),
                $"[QA][Audio][Entity] action='{action}' detail='{detail}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogError(string action, string detail)
        {
            DebugUtility.LogError(typeof(AudioEntitySemanticQaSceneHarness),
                $"[QA][Audio][Entity] action='{action}' detail='{detail}'.");
        }
    }
}
