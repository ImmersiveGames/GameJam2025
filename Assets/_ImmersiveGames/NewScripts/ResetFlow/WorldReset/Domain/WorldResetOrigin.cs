namespace ImmersiveGames.GameJam2025.Orchestration.WorldReset.Domain
{
    /// <summary>
    /// Origem do request de reset.
    /// </summary>
    public enum WorldResetOrigin
    {
        Unknown = 0,
        SceneFlow = 1,
        Manual = 2,
        Command = 3
    }
}

