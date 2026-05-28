using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Players.Runtime
{
    public enum PlayerActorParticipationStateKind
    {
        Unknown = 0,
        ActiveInActivity = 1,
        ExitedActivity = 2,
    }

    public enum PlayerActorRetentionKind
    {
        Unknown = 0,
        RetainedForRoute = 1,
    }

    [DisallowMultipleComponent]
    public sealed class PlayerActorParticipationState : MonoBehaviour
    {
        [SerializeField] private PlayerActorParticipationStateKind participationState = PlayerActorParticipationStateKind.ActiveInActivity;
        [SerializeField] private PlayerActorRetentionKind retention = PlayerActorRetentionKind.RetainedForRoute;
        [SerializeField] private string currentActivityId;
        [SerializeField] private int currentEntrySequence;

        public PlayerActorParticipationStateKind ParticipationState => participationState;
        public PlayerActorRetentionKind Retention => retention;
        public string CurrentActivityId => string.IsNullOrWhiteSpace(currentActivityId) ? string.Empty : currentActivityId.Trim();
        public int CurrentEntrySequence => currentEntrySequence;

        public void MarkActiveInActivity(string activityId, int entrySequence)
        {
            currentActivityId = string.IsNullOrWhiteSpace(activityId) ? string.Empty : activityId.Trim();
            currentEntrySequence = entrySequence < 0 ? 0 : entrySequence;
            participationState = PlayerActorParticipationStateKind.ActiveInActivity;
            retention = PlayerActorRetentionKind.RetainedForRoute;
        }

        public void MarkExitedActivityRetainedForRoute()
        {
            participationState = PlayerActorParticipationStateKind.ExitedActivity;
            retention = PlayerActorRetentionKind.RetainedForRoute;
        }
    }
}
