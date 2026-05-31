using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Players.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PlayerActorInputBindingState : MonoBehaviour
    {
        [SerializeField] private string pipelineId;
        [SerializeField] private string sessionId;
        [SerializeField] private string activityId;
        [SerializeField] private int entrySequence;
        [SerializeField] private string playerSlotId;
        [SerializeField] private string playerActorId;
        [SerializeField] private int playerInputInstanceId;
        [SerializeField] private int playerInputIndex;
        [SerializeField] private string playerInputName;

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
            string bindPipelineId,
            string bindSessionId,
            string bindActivityId,
            int bindEntrySequence,
            PlayerSlotId bindPlayerSlotId,
            PlayerActorId bindPlayerActorId,
            int bindPlayerInputInstanceId,
            int bindPlayerInputIndex,
            string bindPlayerInputName)
        {
            pipelineId = Normalize(bindPipelineId);
            sessionId = Normalize(bindSessionId);
            activityId = Normalize(bindActivityId);
            entrySequence = bindEntrySequence < 0 ? 0 : bindEntrySequence;
            playerSlotId = bindPlayerSlotId.IsValid ? bindPlayerSlotId.Value : string.Empty;
            playerActorId = bindPlayerActorId.IsValid ? bindPlayerActorId.Value : string.Empty;
            playerInputInstanceId = bindPlayerInputInstanceId;
            playerInputIndex = bindPlayerInputIndex < 0 ? 0 : bindPlayerInputIndex;
            playerInputName = Normalize(bindPlayerInputName);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
