using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
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
        [SerializeField, HideInInspector] private PlayerActorParticipationStateKind participationState = PlayerActorParticipationStateKind.ActiveInActivity;
        [SerializeField, HideInInspector] private PlayerActorRetentionKind retention = PlayerActorRetentionKind.RetainedForRoute;
        [SerializeField, HideInInspector] private string currentActivityId;
        [SerializeField, HideInInspector] private int currentEntrySequence;

        public PlayerActorParticipationStateKind ParticipationState => participationState;
        public PlayerActorRetentionKind Retention => retention;
        public string CurrentActivityId => string.IsNullOrWhiteSpace(currentActivityId) ? string.Empty : currentActivityId.Trim();
        public int CurrentEntrySequence => currentEntrySequence;

        public void MarkActiveInActivity(SessionActivityIdentity identity)
        {
            if (!identity.IsValid)
            {
                Clear();
                return;
            }

            currentActivityId = identity.ActivityId;
            currentEntrySequence = identity.EntrySequence;
            participationState = PlayerActorParticipationStateKind.ActiveInActivity;
            retention = PlayerActorRetentionKind.RetainedForRoute;
        }

        public void MarkExitedActivityRetainedForRoute()
        {
            participationState = PlayerActorParticipationStateKind.ExitedActivity;
            retention = PlayerActorRetentionKind.RetainedForRoute;
        }

        public void Clear()
        {
            currentActivityId = string.Empty;
            currentEntrySequence = 0;
            participationState = PlayerActorParticipationStateKind.Unknown;
            retention = PlayerActorRetentionKind.Unknown;
        }
    }
}
