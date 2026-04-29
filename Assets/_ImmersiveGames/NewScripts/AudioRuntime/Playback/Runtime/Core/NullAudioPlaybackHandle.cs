namespace _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core
{
    /// <summary>
    /// Handle no-op seguro para fases sem playback real.
    /// </summary>
    public sealed class NullAudioPlaybackHandle : IAudioPlaybackHandle
    {
        public static readonly NullAudioPlaybackHandle Instance = new NullAudioPlaybackHandle();

        public bool IsValid => false;
        public bool IsPlaying => false;

        public void Stop(float fadeOutSeconds = 0f)
        {
        }
    }
}

