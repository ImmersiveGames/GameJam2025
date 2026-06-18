using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Players.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PlayerActorIdentity : MonoBehaviour
    {
        [SerializeField] [HideInInspector] private string pipelineId;
        [SerializeField] [HideInInspector] private string sessionId;
        [SerializeField] [HideInInspector] private string activityId;
        [SerializeField] [HideInInspector] private int activityOrdinal;
        [SerializeField] [HideInInspector] private int entrySequence;
        [SerializeField] [HideInInspector] private string playerSlotId;
        [SerializeField] [HideInInspector] private string playerActorId;

        public string PipelineId => pipelineId;
        public string SessionId => sessionId;
        public string ActivityId => activityId;
        public int ActivityOrdinal => activityOrdinal;
        public int EntrySequence => entrySequence;
        public PlayerSlotId PlayerSlotId => new(playerSlotId);
        public PlayerActorId PlayerActorId => new(playerActorId);

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(pipelineId) &&
            !string.IsNullOrWhiteSpace(sessionId) &&
            !string.IsNullOrWhiteSpace(activityId) &&
            activityOrdinal > 0 &&
            entrySequence > 0 &&
            PlayerSlotId.IsValid &&
            PlayerActorId.IsValid;

        public void Bind(SessionActivityIdentity identity, PlayerActorIdentityRecord actorIdentity)
        {
            if (!identity.IsValid || !actorIdentity.IsValid)
            {
                Clear();
                return;
            }

            pipelineId = identity.PipelineId;
            sessionId = identity.SessionId;
            activityId = identity.ActivityId;
            activityOrdinal = identity.ActivityOrdinal;
            entrySequence = identity.EntrySequence;
            playerSlotId = actorIdentity.PlayerSlotId.Value;
            playerActorId = actorIdentity.PlayerActorId.Value;
        }

        public void Clear()
        {
            pipelineId = string.Empty;
            sessionId = string.Empty;
            activityId = string.Empty;
            activityOrdinal = 0;
            entrySequence = 0;
            playerSlotId = string.Empty;
            playerActorId = string.Empty;
        }
    }
}
