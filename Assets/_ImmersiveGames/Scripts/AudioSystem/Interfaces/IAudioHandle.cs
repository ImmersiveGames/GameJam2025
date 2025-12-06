namespace _ImmersiveGames.Scripts.AudioSystem.Interfaces
{
    /// <summary>
    /// Representa um handle para uma instância de áudio em execução.
    /// </summary>
    public interface IAudioHandle
    {
        bool IsPlaying { get; }
        void Stop(float fadeOutSeconds = 0f);
    }
}
