using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline;
using UnityEngine;
using UnityEngine.Serialization;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.QA
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Actors/Attributes/Actor Attribute Runtime Command QA Probe")]
    public sealed class ActorAttributeRuntimeCommandQaProbe : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private SessionActivityHost host;

        [Header("Command Target")]
        [FormerlySerializedAs("nonPlayerActorId")]
        [FormerlySerializedAs("actorId")]
        [Tooltip("Use o ActorId ativo, por exemplo 'actor.player.primary' ou 'npc.generic.01'. Nao use PlayerSlotId, PlayerSelectionId, PlayerActorId ou ActorInstanceRuntimeId.")]
        [SerializeField] private string sceneActorId = "scene.actor.generic.01";
        [SerializeField] private ActorAttributeDefinitionAsset attributeDefinition;

        [Header("Command Values")]
        [SerializeField] private float addAmount = 5f;
        [SerializeField] private float subtractAmount = 10f;
        [SerializeField] private float setValue = 42f;

        [ContextMenu("QA/Subtract Attribute")]
        public void QaSubtractAttribute()
        {
            if (!TryResolveAttributeIdOrLog(out string attributeId))
            {
                return;
            }

            RequireHost().QaSubtractActorAttribute(sceneActorId, attributeId, subtractAmount);
        }

        [ContextMenu("QA/Add Attribute")]
        public void QaAddAttribute()
        {
            if (!TryResolveAttributeIdOrLog(out string attributeId))
            {
                return;
            }

            RequireHost().QaAddActorAttribute(sceneActorId, attributeId, addAmount);
        }

        [ContextMenu("QA/Set Attribute")]
        public void QaSetAttribute()
        {
            if (!TryResolveAttributeIdOrLog(out string attributeId))
            {
                return;
            }

            RequireHost().QaSetActorAttribute(sceneActorId, attributeId, setValue);
        }

        [ContextMenu("QA/Reset Attribute To Initial")]
        public void QaResetAttributeToInitial()
        {
            if (!TryResolveAttributeIdOrLog(out string attributeId))
            {
                return;
            }

            RequireHost().QaResetActorAttributeToInitial(sceneActorId, attributeId);
        }

        [ContextMenu("QA/Restore Attribute To Max")]
        public void QaRestoreAttributeToMax()
        {
            if (!TryResolveAttributeIdOrLog(out string attributeId))
            {
                return;
            }

            RequireHost().QaRestoreActorAttributeToMax(sceneActorId, attributeId);
        }

        [ContextMenu("QA/Mutation Receiver/Subtract Attribute")]
        public void QaMutationSubtractAttribute()
        {
            if (!TryResolveAttributeIdOrLog(out string attributeId))
            {
                return;
            }

            RequireHost().QaMutationSubtractActorAttribute(sceneActorId, attributeId, subtractAmount);
        }

        [ContextMenu("QA/Mutation Receiver/Add Attribute")]
        public void QaMutationAddAttribute()
        {
            if (!TryResolveAttributeIdOrLog(out string attributeId))
            {
                return;
            }

            RequireHost().QaMutationAddActorAttribute(sceneActorId, attributeId, addAmount);
        }

        [ContextMenu("QA/Mutation Receiver/Set Attribute")]
        public void QaMutationSetAttribute()
        {
            if (!TryResolveAttributeIdOrLog(out string attributeId))
            {
                return;
            }

            RequireHost().QaMutationSetActorAttribute(sceneActorId, attributeId, setValue);
        }

        [ContextMenu("QA/Mutation Receiver/Reset Attribute To Initial")]
        public void QaMutationResetAttributeToInitial()
        {
            if (!TryResolveAttributeIdOrLog(out string attributeId))
            {
                return;
            }

            RequireHost().QaMutationResetActorAttributeToInitial(sceneActorId, attributeId);
        }

        [ContextMenu("QA/Mutation Receiver/Restore Attribute To Max")]
        public void QaMutationRestoreAttributeToMax()
        {
            if (!TryResolveAttributeIdOrLog(out string attributeId))
            {
                return;
            }

            RequireHost().QaMutationRestoreActorAttributeToMax(sceneActorId, attributeId);
        }

        private bool TryResolveAttributeIdOrLog(out string attributeId)
        {
            attributeId = string.Empty;
            if (attributeDefinition == null)
            {
                DebugUtility.LogError(typeof(ActorAttributeRuntimeCommandQaProbe), $"[FATAL][ActorAttributeRuntimeCommandQaProbe] attributeDefinition is required. actorId='{sceneActorId.TrimToEmpty()}'.");
                return false;
            }

            var runtimeId = ActorAttributeId.FromDefinition(attributeDefinition);
            if (!runtimeId.IsValid)
            {
                DebugUtility.LogError(typeof(ActorAttributeRuntimeCommandQaProbe), $"[FATAL][ActorAttributeRuntimeCommandQaProbe] attributeDefinition has invalid attributeId. definition='{attributeDefinition.name}' actorId='{sceneActorId.TrimToEmpty()}'.");
                return false;
            }

            attributeId = runtimeId.ToString();
            DebugUtility.LogVerbose(typeof(ActorAttributeRuntimeCommandQaProbe), $"attributeDefinitionResolved definition='{attributeDefinition.name}' attributeId='{attributeId}' actorId='{sceneActorId.TrimToEmpty()}'.");
            return true;
        }

        private SessionActivityHost RequireHost()
        {
            if (host != null)
            {
                return host;
            }

            throw new InvalidOperationException("[FATAL][ActorAttributeRuntimeCommandQaProbe] SessionActivityHost reference is required.");
        }
}
}
