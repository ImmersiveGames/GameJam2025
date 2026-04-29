namespace _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core
{
    /// <summary>
    /// Handle can�nico de playback.
    /// </summary>
    public interface IAudioPlaybackHandle
    {
        bool IsValid { get; }
        bool IsPlaying { get; }

        void Stop(float fadeOutSeconds = 0f);
    }
}

