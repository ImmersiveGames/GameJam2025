using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class BindHandler
    {
        private readonly string _targetResourceId;
        private readonly string _targetActorId;
        private readonly ResourceType _targetResourceType;

        public BindHandler(string targetResourceId, string targetActorId, ResourceType targetResourceType)
        {
            _targetResourceId = targetResourceId;
            _targetActorId = targetActorId;
            _targetResourceType = targetResourceType;
        }

        public bool ValidateBind(ResourceBindEvent evt)
        {
            string resourceId = evt.UniqueId;
            if (!string.IsNullOrEmpty(evt.ActorId) && evt.UniqueId.StartsWith(evt.ActorId + "_"))
            {
                resourceId = evt.UniqueId.Substring(evt.ActorId.Length + 1);
            }

            bool valid = resourceId == _targetResourceId &&
                         (_targetResourceType == ResourceType.Custom || evt.Type == _targetResourceType) &&
                         (string.IsNullOrEmpty(_targetActorId) || evt.ActorId == _targetActorId);

            if (!valid)
            {
                DebugUtility.LogVerbose<BindHandler>($"ValidateBind: Ignorado - UniqueId={evt.UniqueId}, ResourceId={resourceId}, ExpectedResourceId={_targetResourceId}, ActorId={evt.ActorId}, ExpectedActorId={_targetActorId}, Type={evt.Type}, ExpectedType={_targetResourceType}, Source={evt.Source.name}");
            }
            return valid;
        }

        public bool ValidateValueChanged(ResourceValueChangedEvent evt, GameObject expectedSource)
        {
            string resourceId = evt.UniqueId;
            if (!string.IsNullOrEmpty(evt.ActorId) && evt.UniqueId.StartsWith(evt.ActorId + "_"))
            {
                resourceId = evt.UniqueId.Substring(evt.ActorId.Length + 1);
            }

            bool valid = resourceId == _targetResourceId &&
                         evt.Source == expectedSource &&
                         (string.IsNullOrEmpty(_targetActorId) || evt.ActorId == _targetActorId);

            if (!valid)
            {
                DebugUtility.LogVerbose<BindHandler>($"ValidateValueChanged: Ignorado - UniqueId={evt.UniqueId}, ResourceId={resourceId}, ExpectedResourceId={_targetResourceId}, ActorId={evt.ActorId}, ExpectedActorId={_targetActorId}, Source={evt.Source?.name}, ExpectedSource={expectedSource?.name}");
            }
            return valid;
        }

        public bool ValidateThresholdCrossed(ResourceThresholdCrossedEvent evt, GameObject expectedSource)
        {
            string resourceId = evt.UniqueId;
            if (!string.IsNullOrEmpty(evt.ActorId) && evt.UniqueId.StartsWith(evt.ActorId + "_"))
            {
                resourceId = evt.UniqueId.Substring(evt.ActorId.Length + 1);
            }

            bool valid = resourceId == _targetResourceId &&
                         evt.Source == expectedSource &&
                         (string.IsNullOrEmpty(_targetActorId) || evt.ActorId == _targetActorId);

            if (!valid)
            {
                DebugUtility.LogVerbose<BindHandler>($"ValidateThresholdCrossed: Ignorado - UniqueId={evt.UniqueId}, ResourceId={resourceId}, ExpectedResourceId={_targetResourceId}, ActorId={evt.ActorId}, ExpectedActorId={_targetActorId}, Source={evt.Source?.name}, ExpectedSource={expectedSource?.name}");
            }
            return valid;
        }

        public bool ValidateModifierApplied(ModifierAppliedEvent evt)
        {
            string resourceId = evt.UniqueId;
            if (!string.IsNullOrEmpty(evt.ActorId) && evt.UniqueId.StartsWith(evt.ActorId + "_"))
            {
                resourceId = evt.UniqueId.Substring(evt.ActorId.Length + 1);
            }

            bool valid = resourceId == _targetResourceId &&
                         (string.IsNullOrEmpty(_targetActorId) || evt.ActorId == _targetActorId);

            if (!valid)
            {
                DebugUtility.LogVerbose<BindHandler>($"ValidateModifierApplied: Ignorado - UniqueId={evt.UniqueId}, ResourceId={resourceId}, ExpectedResourceId={_targetResourceId}, ActorId={evt.ActorId}, ExpectedActorId={_targetActorId}, Source={evt.Source?.name}");
            }
            return valid;
        }
    }
}