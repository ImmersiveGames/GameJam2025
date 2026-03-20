using _ImmersiveGames.NewScripts.Modules.Audio.Config;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Runtime
{
    /// <summary>
    /// Serviço para playback de SFX global (não-entity).
    /// </summary>
    public interface IGlobalAudioService
    {
        IAudioPlaybackHandle Play(AudioSfxCueAsset cue, AudioPlaybackContext context);
    }
}
