using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Players.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PlayerActorInputBindingState : MonoBehaviour
    {
        [SerializeField, HideInInspector] private string pipelineId;
        [SerializeField, HideInInspector] private string sessionId;
        [SerializeField, HideInInspector] private string activityId;
        [SerializeField, HideInInspector] private int entrySequence;
        [SerializeField, HideInInspector] private string playerSlotId;
        [SerializeField, HideInInspector] private string playerActorId;
        [SerializeField, HideInInspector] private int playerInputInstanceId;
        [SerializeField, HideInInspector] private int playerInputIndex;
        [SerializeField, HideInInspector] private string playerInputName;

        public string PipelineId => pipelineId;
        public string SessionId => sessionId;
        public string ActivityId => activityId;
        public int EntrySequence => entrySequence;
        public PlayerSlotId PlayerSlotId => new(playerSlotId);
        public PlayerActorId PlayerActorId => new(playerActorId);
        public int PlayerInputInstanceId => playerInputInstanceId;
        public int PlayerInputIndex => playerInputIndex;
        public string PlayerInputName => playerInputName;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(pipelineId) &&
            !string.IsNullOrWhiteSpace(sessionId) &&
            !string.IsNullOrWhiteSpace(activityId) &&
            entrySequence > 0 &&
            PlayerSlotId.IsValid &&
            PlayerActorId.IsValid &&
            playerInputInstanceId != 0 &&
            playerInputIndex >= 0 &&
            !string.IsNullOrWhiteSpace(playerInputName);

        public void Bind(
            SessionActivityIdentity identity,
            PlayerSlotId bindPlayerSlotId,
            PlayerActorId bindPlayerActorId,
            int bindPlayerInputInstanceId,
            int bindPlayerInputIndex,
            string bindPlayerInputName)
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
            playerInputInstanceId = bindPlayerInputInstanceId;
            playerInputIndex = bindPlayerInputIndex < 0 ? 0 : bindPlayerInputIndex;
            playerInputName = Normalize(bindPlayerInputName);
        }

        public void Clear()
        {
            pipelineId = string.Empty;
            sessionId = string.Empty;
            activityId = string.Empty;
            entrySequence = 0;
            playerSlotId = string.Empty;
            playerActorId = string.Empty;
            playerInputInstanceId = 0;
            playerInputIndex = 0;
            playerInputName = string.Empty;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
