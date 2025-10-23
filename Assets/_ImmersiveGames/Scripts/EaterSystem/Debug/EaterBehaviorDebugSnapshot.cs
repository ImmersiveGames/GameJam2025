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
            bool pendingHungryEffects)
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
        }
    }
}
