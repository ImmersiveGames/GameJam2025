using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    public readonly struct ActorAttributeSetupStageResult
    {
        public ActorAttributeSetupStageResult(
            NonPlayerActorRuntimeEntry entry,
            ActorAttributeEndpoint endpoint,
            ActorAttributeProfileAsset profile,
            ActorAttributeSetupResult setupResult,
            string attributeIds,
            string pipelineIdentity,
            string activityIdentity)
        {
            Entry = entry;
            Endpoint = endpoint;
            Profile = profile;
            SetupResult = setupResult;
            AttributeIds = Normalize(attributeIds);
            PipelineIdentity = Normalize(pipelineIdentity);
            ActivityIdentity = Normalize(activityIdentity);
        }

        public NonPlayerActorRuntimeEntry Entry { get; }
        public ActorAttributeEndpoint Endpoint { get; }
        public ActorAttributeProfileAsset Profile { get; }
        public ActorAttributeSetupResult SetupResult { get; }
        public string AttributeIds { get; }
        public string PipelineIdentity { get; }
        public string ActivityIdentity { get; }
        public string NonPlayerActorId => Entry.ActorIdentity.NonPlayerActorId;
        public bool HasEndpoint => Endpoint != null;
        public bool IsReady => SetupResult.IsReady;
        public bool IsSkippedNoContent => SetupResult.IsSkippedNoContent;
        public bool Failed => SetupResult.Failed;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public static class ActorAttributeSetupStage
    {
        public static bool TryExecute(
            NonPlayerActorRuntimeEntry entry,
            string pipelineIdentity,
            string activityIdentity,
            out ActorAttributeSetupStageResult result)
        {
            ActorAttributeEndpoint endpoint = entry.ActorInstance != null
                ? entry.ActorInstance.GetComponent<ActorAttributeEndpoint>()
                : null;

            if (endpoint == null)
            {
                result = new ActorAttributeSetupStageResult(
                    entry,
                    null,
                    null,
                    ActorAttributeSetupResult.SkippedNoContent(entry.ActorIdentity.NonPlayerActorId, "no_actor_attribute_endpoint"),
                    string.Empty,
                    pipelineIdentity,
                    activityIdentity);
                return true;
            }

            ActorAttributeProfileAsset profile = endpoint.AttributeProfile;
            if (profile == null)
            {
                result = new ActorAttributeSetupStageResult(
                    entry,
                    endpoint,
                    null,
                    ActorAttributeSetupResult.Fail(entry.ActorIdentity.NonPlayerActorId, "attribute_profile_missing"),
                    string.Empty,
                    pipelineIdentity,
                    activityIdentity);
                return false;
            }

            if (!endpoint.TryInitialize(entry.ActorIdentity.NonPlayerActorId, pipelineIdentity, activityIdentity, out ActorAttributeSetupResult setupResult))
            {
                result = new ActorAttributeSetupStageResult(
                    entry,
                    endpoint,
                    profile,
                    setupResult,
                    string.Empty,
                    pipelineIdentity,
                    activityIdentity);
                return false;
            }

            result = new ActorAttributeSetupStageResult(
                entry,
                endpoint,
                profile,
                setupResult,
                BuildAttributeIdList(endpoint),
                pipelineIdentity,
                activityIdentity);
            return true;
        }

        private static string BuildAttributeIdList(ActorAttributeEndpoint endpoint)
        {
            if (endpoint == null || endpoint.RuntimeStates == null || endpoint.RuntimeStates.Count == 0)
            {
                return "<none>";
            }

            List<string> ids = new(endpoint.RuntimeStates.Count);
            for (int index = 0; index < endpoint.RuntimeStates.Count; index++)
            {
                ActorAttributeState state = endpoint.RuntimeStates[index];
                if (state == null || !state.AttributeId.IsValid)
                {
                    continue;
                }

                ids.Add(state.AttributeId.ToString());
            }

            if (ids.Count == 0)
            {
                return "<none>";
            }

            ids.Sort(StringComparer.Ordinal);
            return string.Join(",", ids);
        }
    }
}
