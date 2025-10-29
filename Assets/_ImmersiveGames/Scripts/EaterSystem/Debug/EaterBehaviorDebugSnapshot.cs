using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.Debug
{
    /// <summary>
    /// Snapshot simplificado com as principais informações do comportamento atual do Eater.
    /// </summary>
    public readonly struct EaterBehaviorDebugSnapshot
    {
        public static EaterBehaviorDebugSnapshot Empty => default;

        public bool IsValid { get; }
        public string CurrentState { get; }
        public bool IsHungry { get; }
        public bool IsEating { get; }
        public bool HasTarget { get; }
        public string TargetName { get; }
        public bool HasProximityContact { get; }
        public Vector3 LastProximityPoint { get; }

        public EaterBehaviorDebugSnapshot(
            bool isValid,
            string currentState,
            bool isHungry,
            bool isEating,
            bool hasTarget,
            string targetName,
            bool hasProximityContact,
            Vector3 lastProximityPoint)
        {
            IsValid = isValid;
            CurrentState = currentState;
            IsHungry = isHungry;
            IsEating = isEating;
            HasTarget = hasTarget;
            TargetName = targetName;
            HasProximityContact = hasProximityContact;
            LastProximityPoint = lastProximityPoint;
        }
    }
}
