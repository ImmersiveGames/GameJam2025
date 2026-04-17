namespace ImmersiveGames.GameJam2025.Infrastructure.InputModes.Runtime
{
    /// <summary>
    /// Leitura canonica do modo de input atualmente ativo.
    /// </summary>
    public interface IInputModeStateService
    {
        InputModeRequestKind CurrentMode { get; }
    }
}

