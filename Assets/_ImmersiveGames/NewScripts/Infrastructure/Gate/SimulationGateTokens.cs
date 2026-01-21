namespace _ImmersiveGames.NewScripts.Infrastructure.Gate
{
    /// <summary>
    /// Tokens padrão para bloquear/liberar simulação.
    /// Evita "string solta" espalhada.
    ///
    /// Política recomendada:
    /// - Use "flow.*" para travas de infraestrutura (transição, loading, reset, etc).
    /// - Use "state.pause" para pausa (caso especial), preferencialmente via handles (ref-count).
    /// - Evite usar "state.*" para macro-estados (ready/gameover/victory) como fonte de verdade;
    ///   isso deve vir do GameLoop/StateMachine e eventos, não do Gate.
    /// </summary>
    public static class SimulationGateTokens
    {
        // Estado especial suportado (pausa).
        public const string Pause = "state.pause";

        // Fluxos (infra)
        public const string SceneTransition = "flow.scene_transition";
        public const string Cinematic = "flow.cinematic";
        public const string SoftReset = "flow.soft_reset";
        public const string Loading = "flow.loading";
        // ContentSwap (alias de PhaseChange, preserva compatibilidade).
        public const string ContentSwapTransition = "flow.phase_transition";
        public const string ContentSwapInPlace = "flow.phase_inplace";

        // Legacy (PhaseChange).
        public const string PhaseTransition = ContentSwapTransition;
        public const string PhaseInPlace = ContentSwapInPlace;

        // Simulação de gameplay (deve bloquear apenas lógica de gameplay, não UI).
        public const string GameplaySimulation = "sim.gameplay";

        // QA / Debug
        public const string QaGameplayPause = "qa.gameplay_pause";
    }
}
