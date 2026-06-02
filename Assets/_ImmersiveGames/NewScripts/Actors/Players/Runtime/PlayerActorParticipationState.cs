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

    [DisallowMultipleComponent]
    public sealed class PlayerActorParticipationState : MonoBehaviour
    {
        [SerializeField, HideInInspector] private PlayerActorParticipationStateKind participationState = PlayerActorParticipationStateKind.ActiveInActivity;
        [SerializeField, HideInInspector] private string currentActivityId;
        [SerializeField, HideInInspector] private int currentEntrySequence;

        public PlayerActorParticipationStateKind ParticipationState => participationState;
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
        }

        public void MarkExitedActivity()
        {
            participationState = PlayerActorParticipationStateKind.ExitedActivity;
        }

        public void Clear()
        {
            currentActivityId = string.Empty;
            currentEntrySequence = 0;
            participationState = PlayerActorParticipationStateKind.Unknown;
        }
    }
}
