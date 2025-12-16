namespace _ImmersiveGames.Scripts.GameplaySystems.Execution
{
    /// <summary>
    /// Tokens padrão para bloquear/liberar simulação.
    /// Evita "string solta" espalhada.
    /// </summary>
    public static class SimulationGateTokens
    {
        public const string Menu = "state.menu";
        public const string Pause = "state.pause";
        public const string GameOver = "state.gameover";
        public const string Victory = "state.victory";

        // Reservas para uso futuro (scene flow, cutscenes, resets, etc.)
        public const string SceneTransition = "flow.scene_transition";
        public const string Cinematic = "flow.cinematic";
        public const string SoftReset = "flow.soft_reset";
        public const string Loading = "flow.loading";

        // QA / Debug
        public const string QaGameplayPause = "qa.gameplay_pause";
    }
}