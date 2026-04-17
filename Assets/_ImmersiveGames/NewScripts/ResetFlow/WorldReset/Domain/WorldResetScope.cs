namespace ImmersiveGames.GameJam2025.Orchestration.WorldReset.Domain
{
    /// <summary>
    /// Escopos suportados para reset parcial (soft reset) do WorldReset/SceneReset.
    /// </summary>
    public enum WorldResetScope
    {
        World = 0,
        Players = 1,
        Boss = 2,
        Stage = 3,
        Custom = 99
    }
}


