using UnityEngine;

namespace _ImmersiveGames.NewScripts.Players.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PlayerCameraEndpoint : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform lookAtTarget;
        [SerializeField] private string pipelineId;
        [SerializeField] private string sessionId;
        [SerializeField] private string activityId;
        [SerializeField] private int activityOrdinal;
        [SerializeField] private int entrySequence;
        [SerializeField] private string playerSlotId;
        [SerializeField] private string playerActorId;

        public Transform FollowTarget => followTarget;
        public Transform LookAtTarget => lookAtTarget;
        public string PipelineId => pipelineId;
        public string SessionId => sessionId;
        public string ActivityId => activityId;
        public int ActivityOrdinal => activityOrdinal;
        public int EntrySequence => entrySequence;
        public string PlayerSlotId => playerSlotId;
        public string PlayerActorId => playerActorId;

        public bool HasValidTargets => followTarget != null;

        public void Bind(
            string bindPipelineId,
            string bindSessionId,
            string bindActivityId,
            int bindActivityOrdinal,
            int bindEntrySequence,
            string bindPlayerSlotId,
            string bindPlayerActorId)
        {
            pipelineId = Normalize(bindPipelineId);
            sessionId = Normalize(bindSessionId);
            activityId = Normalize(bindActivityId);
            activityOrdinal = bindActivityOrdinal < 0 ? 0 : bindActivityOrdinal;
            entrySequence = bindEntrySequence < 0 ? 0 : bindEntrySequence;
            playerSlotId = Normalize(bindPlayerSlotId);
            playerActorId = Normalize(bindPlayerActorId);
        }

        public bool IsBoundTo(
            string expectedPipelineId,
            string expectedSessionId,
            string expectedActivityId,
            int expectedActivityOrdinal,
            int expectedEntrySequence,
            string expectedPlayerSlotId,
            string expectedPlayerActorId)
        {
            return string.Equals(pipelineId, Normalize(expectedPipelineId), System.StringComparison.Ordinal) &&
                   string.Equals(sessionId, Normalize(expectedSessionId), System.StringComparison.Ordinal) &&
                   string.Equals(activityId, Normalize(expectedActivityId), System.StringComparison.Ordinal) &&
                   activityOrdinal == expectedActivityOrdinal &&
                   entrySequence == expectedEntrySequence &&
                   string.Equals(playerSlotId, Normalize(expectedPlayerSlotId), System.StringComparison.Ordinal) &&
                   string.Equals(playerActorId, Normalize(expectedPlayerActorId), System.StringComparison.Ordinal);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
