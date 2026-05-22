using UnityEngine;

namespace _ImmersiveGames.NewScripts.Players.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PlayerActorIdentity : MonoBehaviour
    {
        [SerializeField] private string pipelineId;
        [SerializeField] private string sessionId;
        [SerializeField] private string activityId;
        [SerializeField] private int activityOrdinal;
        [SerializeField] private int entrySequence;
        [SerializeField] private string playerId;
        [SerializeField] private string playerActorId;

        public string PipelineId => pipelineId;
        public string SessionId => sessionId;
        public string ActivityId => activityId;
        public int ActivityOrdinal => activityOrdinal;
        public int EntrySequence => entrySequence;
        public string PlayerId => playerId;
        public string PlayerActorId => playerActorId;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(pipelineId) &&
            !string.IsNullOrWhiteSpace(sessionId) &&
            !string.IsNullOrWhiteSpace(activityId) &&
            activityOrdinal > 0 &&
            entrySequence > 0 &&
            !string.IsNullOrWhiteSpace(playerId) &&
            !string.IsNullOrWhiteSpace(playerActorId);

        public void Bind(
            string bindPipelineId,
            string bindSessionId,
            string bindActivityId,
            int bindActivityOrdinal,
            int bindEntrySequence,
            string bindPlayerId,
            string bindPlayerActorId)
        {
            pipelineId = Normalize(bindPipelineId);
            sessionId = Normalize(bindSessionId);
            activityId = Normalize(bindActivityId);
            activityOrdinal = bindActivityOrdinal < 0 ? 0 : bindActivityOrdinal;
            entrySequence = bindEntrySequence < 0 ? 0 : bindEntrySequence;
            playerId = Normalize(bindPlayerId);
            playerActorId = Normalize(bindPlayerActorId);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
