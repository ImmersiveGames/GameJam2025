using System.Collections.Generic;
namespace _ImmersiveGames.Scripts.GameplaySystems.Reset
{
    public enum ResetStructs
    {
        Cleanup = 0,
        Restore = 1,
        Rebind = 2
    }
    public enum ResetScope
    {
        AllActorsInScene = 0,
        PlayersOnly = 1,
        EaterOnly = 2,
        ActorIdSet = 3
    }
    public readonly struct ResetRequest
    {
        public readonly ResetScope scope;
        public readonly string reason;
        public readonly IReadOnlyList<string> actorIds;

        public ResetRequest(ResetScope scope, string reason = null, IReadOnlyList<string> actorIds = null)
        {
            this.scope = scope;
            this.reason = reason;
            this.actorIds = actorIds;
        }

        public override string ToString()
        {
            int count = actorIds != null ? actorIds.Count : 0;
            return $"ResetRequest(Scope={scope}, Reason='{reason ?? "null"}', ActorIds={count})";
        }
    }
    public readonly struct ResetContext
    {
        public readonly string sceneName;
        public readonly ResetRequest request;
        public readonly int requestSerial;
        public readonly int frameStarted;
        public readonly ResetStructs currentStructs;

        public ResetContext(
            string sceneName,
            ResetRequest request,
            int requestSerial,
            int frameStarted,
            ResetStructs currentStructs)
        {
            this.sceneName = sceneName;
            this.request = request;
            this.requestSerial = requestSerial;
            this.frameStarted = frameStarted;
            this.currentStructs = currentStructs;
        }

        public ResetContext WithStep(ResetStructs structs)
        {
            return new ResetContext(sceneName, request, requestSerial, frameStarted, structs);
        }

        public override string ToString()
        {
            return $"WorldResetContext(Scene='{sceneName}', Serial={requestSerial}, Frame={frameStarted}, Step={currentStructs}, {request})";
        }
    }
}
