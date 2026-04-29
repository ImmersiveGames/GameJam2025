using System;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.StateGate.Core
{
    public readonly struct GameplayOperationalStateSnapshot : IEquatable<GameplayOperationalStateSnapshot>
    {
        public GameplayOperationalStateSnapshot(
            bool gateOpen,
            bool sceneReady,
            bool actorsOperationalReady,
            bool interactionReady,
            bool gameRunStarted,
            bool paused,
            string gameLoopState,
            int activeTokens,
            string reason)
        {
            GateOpen = gateOpen;
            SceneReady = sceneReady;
            ActorsOperationalReady = actorsOperationalReady;
            InteractionReady = interactionReady;
            GameRunStarted = gameRunStarted;
            Paused = paused;
            GameLoopState = string.IsNullOrWhiteSpace(gameLoopState) ? string.Empty : gameLoopState.Trim();
            ActiveTokens = activeTokens < 0 ? 0 : activeTokens;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public bool GateOpen { get; }
        public bool SceneReady { get; }
        public bool ActorsOperationalReady { get; }
        public bool InteractionReady { get; }
        public bool GameRunStarted { get; }
        public bool Paused { get; }
        public string GameLoopState { get; }
        public int ActiveTokens { get; }
        public string Reason { get; }

        public static GameplayOperationalStateSnapshot Empty =>
            new(
                gateOpen: false,
                sceneReady: false,
                actorsOperationalReady: false,
                interactionReady: false,
                gameRunStarted: false,
                paused: false,
                gameLoopState: string.Empty,
                activeTokens: 0,
                reason: string.Empty);

        public bool Equals(GameplayOperationalStateSnapshot other)
        {
            return GateOpen == other.GateOpen &&
                   SceneReady == other.SceneReady &&
                   ActorsOperationalReady == other.ActorsOperationalReady &&
                   InteractionReady == other.InteractionReady &&
                   GameRunStarted == other.GameRunStarted &&
                   Paused == other.Paused &&
                   ActiveTokens == other.ActiveTokens &&
                   string.Equals(GameLoopState, other.GameLoopState, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayOperationalStateSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                GateOpen,
                SceneReady,
                ActorsOperationalReady,
                InteractionReady,
                GameRunStarted,
                Paused,
                GameLoopState,
                ActiveTokens);
        }

        public static bool operator ==(GameplayOperationalStateSnapshot left, GameplayOperationalStateSnapshot right) => left.Equals(right);

        public static bool operator !=(GameplayOperationalStateSnapshot left, GameplayOperationalStateSnapshot right) => !left.Equals(right);
    }
}
