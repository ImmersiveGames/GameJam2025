using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.Debug
{
    /// <summary>
    /// Estrutura imutável contendo apenas os dados relevantes para inspeção do comportamento do Eater.
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
        public float StateTimer { get; }
        public Vector3 Position { get; }
        public bool HasPlayerAnchor { get; }
        public Vector3 PlayerAnchor { get; }
        public float PlayerAnchorDistance { get; }
        public float PlayerAnchorAlignment { get; }
        public bool HasAutoFlow { get; }
        public bool AutoFlowActive { get; }
        public bool DesiresActive { get; }
        public bool HasCurrentDesire { get; }
        public string CurrentDesireName { get; }
        public bool CurrentDesireAvailable { get; }
        public float CurrentDesireRemaining { get; }
        public float CurrentDesireDuration { get; }
        public int CurrentDesireAvailableCount { get; }
        public float CurrentDesireWeight { get; }

        public EaterBehaviorDebugSnapshot(
            bool isValid,
            string currentState,
            bool isHungry,
            bool isEating,
            bool hasTarget,
            string targetName,
            float stateTimer,
            Vector3 position,
            bool hasPlayerAnchor,
            Vector3 playerAnchor,
            float playerAnchorDistance,
            float playerAnchorAlignment,
            bool hasAutoFlow,
            bool autoFlowActive,
            bool desiresActive,
            bool hasCurrentDesire,
            string currentDesireName,
            bool currentDesireAvailable,
            float currentDesireRemaining,
            float currentDesireDuration,
            int currentDesireAvailableCount,
            float currentDesireWeight)
        {
            IsValid = isValid;
            CurrentState = currentState;
            IsHungry = isHungry;
            IsEating = isEating;
            HasTarget = hasTarget;
            TargetName = targetName;
            StateTimer = stateTimer;
            Position = position;
            HasPlayerAnchor = hasPlayerAnchor;
            PlayerAnchor = playerAnchor;
            PlayerAnchorDistance = playerAnchorDistance;
            PlayerAnchorAlignment = playerAnchorAlignment;
            HasAutoFlow = hasAutoFlow;
            AutoFlowActive = autoFlowActive;
            DesiresActive = desiresActive;
            HasCurrentDesire = hasCurrentDesire;
            CurrentDesireName = currentDesireName;
            CurrentDesireAvailable = currentDesireAvailable;
            CurrentDesireRemaining = currentDesireRemaining;
            CurrentDesireDuration = currentDesireDuration;
            CurrentDesireAvailableCount = currentDesireAvailableCount;
            CurrentDesireWeight = currentDesireWeight;
        }
    }
}
