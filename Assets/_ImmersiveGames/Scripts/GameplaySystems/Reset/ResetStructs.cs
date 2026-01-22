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
        public readonly ResetScope Scope;
        public readonly string Reason;
        public readonly IReadOnlyList<string> ActorIds;

        public ResetRequest(ResetScope scope, string reason = null, IReadOnlyList<string> actorIds = null)
        {
            Scope = scope;
            Reason = reason;
            ActorIds = actorIds;
        }

        public override string ToString()
        {
            int count = ActorIds != null ? ActorIds.Count : 0;
            return $"ResetRequest(Scope={Scope}, Reason='{Reason ?? "null"}', ActorIds={count})";
        }
    }
    public readonly struct ResetContext
    {
        public readonly string SceneName;
        public readonly ResetRequest Request;
        public readonly int RequestSerial;
        public readonly int FrameStarted;
        public readonly ResetStructs currentStructs;

        public ResetContext(
            string sceneName,
            ResetRequest request,
            int requestSerial,
            int frameStarted,
            ResetStructs currentStructs)
        {
            SceneName = sceneName;
            Request = request;
            RequestSerial = requestSerial;
            FrameStarted = frameStarted;
            this.currentStructs = currentStructs;
        }

        public ResetContext WithStep(ResetStructs structs)
        {
            return new ResetContext(SceneName, Request, RequestSerial, FrameStarted, structs);
        }

        public override string ToString()
        {
            return $"ResetContext(Scene='{SceneName}', Serial={RequestSerial}, Frame={FrameStarted}, Step={currentStructs}, {Request})";
        }
    }
}
