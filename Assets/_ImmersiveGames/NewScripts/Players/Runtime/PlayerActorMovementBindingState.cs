using UnityEngine;

namespace _ImmersiveGames.NewScripts.Players.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PlayerActorMovementBindingState : MonoBehaviour
    {
        [SerializeField] private string pipelineId;
        [SerializeField] private string sessionId;
        [SerializeField] private string activityId;
        [SerializeField] private int entrySequence;
        [SerializeField] private string playerSlotId;
        [SerializeField] private string playerActorId;
        [SerializeField] private string endpointType;

        public string PipelineId => pipelineId;
        public string SessionId => sessionId;
        public string ActivityId => activityId;
        public int EntrySequence => entrySequence;
        public string PlayerSlotId => playerSlotId;
        public string PlayerActorId => playerActorId;
        public string EndpointType => endpointType;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(pipelineId) &&
            !string.IsNullOrWhiteSpace(sessionId) &&
            !string.IsNullOrWhiteSpace(activityId) &&
            entrySequence > 0 &&
            !string.IsNullOrWhiteSpace(playerSlotId) &&
            !string.IsNullOrWhiteSpace(playerActorId) &&
            !string.IsNullOrWhiteSpace(endpointType);

        public void Bind(
            string bindPipelineId,
            string bindSessionId,
            string bindActivityId,
            int bindEntrySequence,
            string bindPlayerSlotId,
            string bindPlayerActorId,
            string bindEndpointType)
        {
            pipelineId = Normalize(bindPipelineId);
            sessionId = Normalize(bindSessionId);
            activityId = Normalize(bindActivityId);
            entrySequence = bindEntrySequence < 0 ? 0 : bindEntrySequence;
            playerSlotId = Normalize(bindPlayerSlotId);
            playerActorId = Normalize(bindPlayerActorId);
            endpointType = Normalize(bindEndpointType);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
