using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.Debug
{
    /// <summary>
    /// Estrutura imutável que captura um snapshot do comportamento do Eater para fins de debug.
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
        public bool HasWanderingTimer { get; }
        public bool WanderingTimerRunning { get; }
        public bool WanderingTimerFinished { get; }
        public float WanderingTimerValue { get; }
        public float WanderingDuration { get; }
        public Vector3 Position { get; }
        public bool HasPlayerAnchor { get; }
        public Vector3 PlayerAnchor { get; }
        public bool HasAutoFlow { get; }
        public bool AutoFlowActive { get; }
        public bool DesiresActive { get; }
        public bool PendingHungryEffects { get; }
        public bool HasMovementSample { get; }
        public Vector3 MovementDirection { get; }
        public float MovementSpeed { get; }
        public bool HasHungryMetrics { get; }
        public float PlayerAnchorDistance { get; }
        public float PlayerAnchorAlignment { get; }
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
            bool hasWanderingTimer,
            bool wanderingTimerRunning,
            bool wanderingTimerFinished,
            float wanderingTimerValue,
            float wanderingDuration,
            Vector3 position,
            bool hasPlayerAnchor,
            Vector3 playerAnchor,
            bool hasAutoFlow,
            bool autoFlowActive,
            bool desiresActive,
            bool pendingHungryEffects,
            bool hasMovementSample,
            Vector3 movementDirection,
            float movementSpeed,
            bool hasHungryMetrics,
            float playerAnchorDistance,
            float playerAnchorAlignment,
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
            HasWanderingTimer = hasWanderingTimer;
            WanderingTimerRunning = wanderingTimerRunning;
            WanderingTimerFinished = wanderingTimerFinished;
            WanderingTimerValue = wanderingTimerValue;
            WanderingDuration = wanderingDuration;
            Position = position;
            HasPlayerAnchor = hasPlayerAnchor;
            PlayerAnchor = playerAnchor;
            HasAutoFlow = hasAutoFlow;
            AutoFlowActive = autoFlowActive;
            DesiresActive = desiresActive;
            PendingHungryEffects = pendingHungryEffects;
            HasMovementSample = hasMovementSample;
            MovementDirection = movementDirection;
            MovementSpeed = movementSpeed;
            HasHungryMetrics = hasHungryMetrics;
            PlayerAnchorDistance = playerAnchorDistance;
            PlayerAnchorAlignment = playerAnchorAlignment;
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
