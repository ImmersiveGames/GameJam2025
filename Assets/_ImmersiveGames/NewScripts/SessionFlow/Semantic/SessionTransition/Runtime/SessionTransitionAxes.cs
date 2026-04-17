using ImmersiveGames.GameJam2025.Experience.PostRun.Contracts;

namespace ImmersiveGames.GameJam2025.Orchestration.SessionTransition.Runtime
{
    /// <summary>
    /// Identificador canonico dos eixos compostos por SessionTransition.
    /// </summary>
    public enum SessionTransitionAxisId
    {
        Continuity = 0,
        PhaseTransition = 1,
        WorldReset = 2,
        Reconstruction = 3,
        ContentSpawn = 4,
        CarryOver = 5,
    }

    /// <summary>
    /// Mapa minimo dos eixos da transicao de sessao.
    /// Nao executa nada; apenas congela vocabulario e composicao.
    /// </summary>
    public readonly struct SessionTransitionAxisMap
    {
        public SessionTransitionAxisMap(
            RunContinuationKind continuity,
            SessionTransitionPhaseAction phaseTransition,
            SessionTransitionResetAction worldReset,
            bool reconstruction,
            bool contentSpawn,
            bool carryOver)
        {
            Continuity = continuity;
            PhaseTransition = phaseTransition;
            WorldReset = worldReset;
            Reconstruction = reconstruction;
            ContentSpawn = contentSpawn;
            CarryOver = carryOver;
        }

        public RunContinuationKind Continuity { get; }
        public SessionTransitionPhaseAction PhaseTransition { get; }
        public SessionTransitionResetAction WorldReset { get; }
        public bool Reconstruction { get; }
        public bool ContentSpawn { get; }
        public bool CarryOver { get; }

        public bool ComposesPhaseTransition => PhaseTransition != SessionTransitionPhaseAction.None;
        public bool ComposesWorldReset => WorldReset != SessionTransitionResetAction.None;

        public override string ToString()
        {
            return $"Continuity='{Continuity}', PhaseTransition='{PhaseTransition}', WorldReset='{WorldReset}', Reconstruction='{Reconstruction}', ContentSpawn='{ContentSpawn}', CarryOver='{CarryOver}'";
        }
    }
}

