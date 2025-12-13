namespace _ImmersiveGames.Scripts.UISystems.TerminalOverlay
{
    public interface ITerminalOverlayService
    {
        void ShowVictory(string reason = null);
        void ShowGameOver(string reason = null);
        void Hide();
        bool IsVisible { get; }
    }
}