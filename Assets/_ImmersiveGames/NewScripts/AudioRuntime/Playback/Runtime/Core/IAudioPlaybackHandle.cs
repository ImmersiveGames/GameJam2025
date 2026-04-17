namespace ImmersiveGames.GameJam2025.Experience.Audio.Runtime.Core
{
    /// <summary>
    /// Handle canônico de playback.
    /// </summary>
    public interface IAudioPlaybackHandle
    {
        bool IsValid { get; }
        bool IsPlaying { get; }

        void Stop(float fadeOutSeconds = 0f);
    }
}

