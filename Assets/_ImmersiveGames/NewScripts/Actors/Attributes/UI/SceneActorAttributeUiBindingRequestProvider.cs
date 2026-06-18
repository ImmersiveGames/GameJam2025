using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Actors/Attributes/Scene Actor Attribute UI Binding Request Provider")]
    public sealed class SceneActorAttributeUiBindingRequestProvider : MonoBehaviour, IActorAttributeUiBindingRequestProvider
    {
        [Header("Binding Requests")]
        [SerializeField] private List<ActorAttributeUiBindingRequestAuthoringEntry> bindingRequests = new();

        public IReadOnlyList<ActorAttributeUiBindingRequestEntry> GetRequests()
        {
            if (!isActiveAndEnabled)
            {
                DebugUtility.LogVerbose(
                    typeof(SceneActorAttributeUiBindingRequestProvider),
                    $"event='ActorAttributeUiSceneRequestProviderSkipped' provider='{name}' reason='component_disabled' scene='{gameObject.scene.name}'",
                    DebugUtility.Colors.Info);
                return Array.Empty<ActorAttributeUiBindingRequestEntry>();
            }

            List<ActorAttributeUiBindingRequestEntry> validRequests = new(bindingRequests.Count);
            int acceptedCount = 0;
            int rejectedCount = 0;

            for (int index = 0; index < bindingRequests.Count; index++)
            {
                var bindingRequest = bindingRequests[index];
                if (!TryBuildEntry(bindingRequest, index, out var request))
                {
                    rejectedCount += 1;
                    continue;
                }

                acceptedCount += 1;
                validRequests.Add(request);
            }

            DebugUtility.Log(
                typeof(SceneActorAttributeUiBindingRequestProvider),
                $"event='ActorAttributeUiSceneRequestProviderCollected' provider='{name}' scene='{gameObject.scene.name}' entryCount='{bindingRequests.Count}' acceptedCount='{acceptedCount}' rejectedCount='{rejectedCount}'",
                acceptedCount > 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            return validRequests;
        }

        private bool TryBuildEntry(
            ActorAttributeUiBindingRequestAuthoringEntry bindingRequest,
            int index,
            out ActorAttributeUiBindingRequestEntry request)
        {
            request = default;
            if (bindingRequest == null)
            {
                LogEntryRejected(index, string.Empty, string.Empty, string.Empty, "entry_missing");
                return false;
            }

            if (!bindingRequest.RequestEnabled)
            {
                LogEntrySkipped(index, "entry_disabled");
                return false;
            }

            string selectorKind = bindingRequest.SelectorKind.ToString();
            string attributeId = bindingRequest.AttributeId.ToString();
            string sinkType = bindingRequest.ImageFillSink == null ? string.Empty : bindingRequest.ImageFillSink.GetType().Name;

            if (bindingRequest.AttributeDefinition == null)
            {
                LogEntryRejected(index, selectorKind, attributeId, sinkType, "attribute_definition_missing");
                return false;
            }

            if (!bindingRequest.AttributeId.IsValid)
            {
                LogEntryRejected(index, selectorKind, attributeId, sinkType, "attribute_definition_id_missing");
                return false;
            }

            if (bindingRequest.ImageFillSink == null)
            {
                LogEntryRejected(index, selectorKind, attributeId, sinkType, "sink_reference_missing");
                return false;
            }

            if (!bindingRequest.ImageFillSink.IsReady)
            {
                LogEntryRejected(index, selectorKind, attributeId, sinkType, "sink_not_ready");
                return false;
            }

            ActorAttributeUiTargetSelector selector;
            switch (bindingRequest.SelectorKind)
            {
                case ActorAttributeUiTargetSelectorKind.PrimaryPlayer:
                    selector = ActorAttributeUiTargetSelector.Create(ActorAttributeUiTargetSelectorKind.PrimaryPlayer);
                    break;
                case ActorAttributeUiTargetSelectorKind.ExplicitActorId:
                    if (!bindingRequest.ExplicitActorId.IsValid)
                    {
                        LogEntryRejected(index, selectorKind, attributeId, sinkType, "explicit_actor_id_missing");
                        return false;
                    }

                    selector = ActorAttributeUiTargetSelector.Create(bindingRequest.ExplicitActorId);
                    break;
                case ActorAttributeUiTargetSelectorKind.ExplicitActorInstanceRuntimeId:
                    if (!bindingRequest.ExplicitActorInstanceRuntimeId.IsValid)
                    {
                        LogEntryRejected(index, selectorKind, attributeId, sinkType, "explicit_actor_instance_runtime_id_missing");
                        return false;
                    }

                    selector = ActorAttributeUiTargetSelector.Create(bindingRequest.ExplicitActorInstanceRuntimeId);
                    break;
                default:
                    LogEntryRejected(index, selectorKind, attributeId, sinkType, "unsupported_selector_kind");
                    return false;
            }

            var runtimeRequest = new ActorAttributeUiBindingRequest(selector, bindingRequest.AttributeId);
            request = new ActorAttributeUiBindingRequestEntry(runtimeRequest, bindingRequest.ImageFillSink);

            DebugUtility.LogVerbose(
                typeof(SceneActorAttributeUiBindingRequestProvider),
                $"event='ActorAttributeUiSceneRequestProviderEntryAccepted' provider='{name}' scene='{gameObject.scene.name}' entryIndex='{index}' selectorKind='{selector.Kind}' attributeId='{bindingRequest.AttributeId}' sinkType='{bindingRequest.ImageFillSink.GetType().Name}'",
                DebugUtility.Colors.Success);
            return true;
        }

        private void LogEntrySkipped(int index, string reason)
        {
            DebugUtility.LogVerbose(
                typeof(SceneActorAttributeUiBindingRequestProvider),
                $"event='ActorAttributeUiSceneRequestProviderSkipped' provider='{name}' scene='{gameObject.scene.name}' entryIndex='{index}' reason='{reason}'",
                DebugUtility.Colors.Info);
        }

        private void LogEntryRejected(int index, string selectorKind, string attributeId, string sinkType, string reason)
        {
            DebugUtility.LogWarning(
                typeof(SceneActorAttributeUiBindingRequestProvider),
                $"event='ActorAttributeUiSceneRequestProviderEntryRejected' provider='{name}' scene='{gameObject.scene.name}' entryIndex='{index}' selectorKind='{selectorKind.TrimToEmpty()}' attributeId='{attributeId.TrimToEmpty()}' sinkType='{sinkType.TrimToEmpty()}' failureReason='{reason.TrimToEmpty()}'");
        }
    }
}
