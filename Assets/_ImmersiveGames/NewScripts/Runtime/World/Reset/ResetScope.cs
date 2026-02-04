namespace _ImmersiveGames.NewScripts.Runtime.World.Reset
{
    /// <summary>
    /// Escopos suportados para reset parcial (soft reset) do WorldLifecycle.
    /// </summary>
    public enum ResetScope
    {
        World = 0,
        Players = 1,
        Boss = 2,
        Stage = 3,
        Custom = 99
    }
}

