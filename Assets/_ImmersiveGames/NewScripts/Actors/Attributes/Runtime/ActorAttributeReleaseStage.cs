using _ImmersiveGames.NewScripts.Actors.ActivitySetup;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    public readonly struct ActorAttributeReleaseStageResult
    {
        public ActorAttributeReleaseStageResult(
            NonPlayerActorRuntimeEntry entry,
            ActorAttributeEndpoint endpoint,
            ActorAttributeReleaseResult releaseResult,
            string pipelineIdentity,
            string activityIdentity)
        {
            Entry = entry;
            Endpoint = endpoint;
            ReleaseResult = releaseResult;
            PipelineIdentity = Normalize(pipelineIdentity);
            ActivityIdentity = Normalize(activityIdentity);
        }

        public NonPlayerActorRuntimeEntry Entry { get; }
        public ActorAttributeEndpoint Endpoint { get; }
        public ActorAttributeReleaseResult ReleaseResult { get; }
        public string PipelineIdentity { get; }
        public string ActivityIdentity { get; }
        public string NonPlayerActorId => Entry.ActorIdentity.NonPlayerActorId;
        public bool HasEndpoint => Endpoint != null;
        public bool Released => ReleaseResult.Released;
        public bool SkippedNoContent => ReleaseResult.SkippedNoContent;
        public bool Rejected => ReleaseResult.Rejected;
        public bool Failed => ReleaseResult.Failed;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public static class ActorAttributeReleaseStage
    {
        public static bool TryExecute(
            NonPlayerActorRuntimeEntry entry,
            ActorAttributeEndpoint endpoint,
            string pipelineIdentity,
            string activityIdentity,
            out ActorAttributeReleaseStageResult result)
        {
            if (endpoint == null)
            {
                result = new ActorAttributeReleaseStageResult(
                    entry,
                    null,
                    ActorAttributeReleaseResult.Fail(entry.ActorIdentity.NonPlayerActorId, "attribute_endpoint_missing"),
                    pipelineIdentity,
                    activityIdentity);
                return false;
            }

            if (!endpoint.TryRelease(pipelineIdentity, activityIdentity, out ActorAttributeReleaseResult releaseResult))
            {
                result = new ActorAttributeReleaseStageResult(
                    entry,
                    endpoint,
                    releaseResult,
                    pipelineIdentity,
                    activityIdentity);
                return false;
            }

            result = new ActorAttributeReleaseStageResult(
                entry,
                endpoint,
                releaseResult,
                pipelineIdentity,
                activityIdentity);
            return true;
        }
    }
}
