using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Players.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PlayerActorMovementBindingState : MonoBehaviour
    {
        [SerializeField, HideInInspector] private string pipelineId;
        [SerializeField, HideInInspector] private string sessionId;
        [SerializeField, HideInInspector] private string activityId;
        [SerializeField, HideInInspector] private int entrySequence;
        [SerializeField, HideInInspector] private string playerSlotId;
        [SerializeField, HideInInspector] private string playerActorId;
        [SerializeField, HideInInspector] private string endpointType;

        public string PipelineId => pipelineId;
        public string SessionId => sessionId;
        public string ActivityId => activityId;
        public int EntrySequence => entrySequence;
        public PlayerSlotId PlayerSlotId => new(playerSlotId);
        public PlayerActorId PlayerActorId => new(playerActorId);
        public string EndpointType => endpointType;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(pipelineId) &&
            !string.IsNullOrWhiteSpace(sessionId) &&
            !string.IsNullOrWhiteSpace(activityId) &&
            entrySequence > 0 &&
            PlayerSlotId.IsValid &&
            PlayerActorId.IsValid &&
            !string.IsNullOrWhiteSpace(endpointType);

        public void Bind(
            SessionActivityIdentity identity,
            PlayerSlotId bindPlayerSlotId,
            PlayerActorId bindPlayerActorId,
            string bindEndpointType)
        {
            if (!identity.IsValid || !bindPlayerSlotId.IsValid || !bindPlayerActorId.IsValid)
            {
                Clear();
                return;
            }

            pipelineId = identity.PipelineId;
            sessionId = identity.SessionId;
            activityId = identity.ActivityId;
            entrySequence = identity.EntrySequence;
            playerSlotId = bindPlayerSlotId.Value;
            playerActorId = bindPlayerActorId.Value;
            endpointType = bindEndpointType.TrimToEmpty();
        }

        public void Clear()
        {
            pipelineId = string.Empty;
            sessionId = string.Empty;
            activityId = string.Empty;
            entrySequence = 0;
            playerSlotId = string.Empty;
            playerActorId = string.Empty;
            endpointType = string.Empty;
        }
}
}
