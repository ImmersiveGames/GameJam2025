namespace _ImmersiveGames.NewScripts.Infrastructure.Scene
{
    /// <summary>
    /// Snapshot simples de readiness do jogo.
    /// Consumidores devem tratá-lo como um "view" do estado atual e não como fonte de verdade.
    /// </summary>
    public readonly struct ReadinessSnapshot
    {
        public bool GameplayReady { get; }
        public bool GateOpen { get; }
        public int ActiveTokens { get; }
        public string Reason { get; }

        public ReadinessSnapshot(bool gameplayReady, bool gateOpen, int activeTokens, string reason)
        {
            GameplayReady = gameplayReady;
            GateOpen = gateOpen;
            ActiveTokens = activeTokens;
            Reason = reason ?? string.Empty;
        }

        public override string ToString()
            => $"ReadinessSnapshot(GameplayReady={GameplayReady}, GateOpen={GateOpen}, ActiveTokens={ActiveTokens}, Reason='{Reason}')";
    }
}
