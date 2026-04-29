using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Models;
namespace _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core
{
    /// <summary>
    /// Servi�o para playback de SFX global (n�o-entity).
    /// </summary>
    public interface IGlobalAudioService
    {
        IAudioPlaybackHandle Play(AudioSfxCueAsset cue, AudioPlaybackContext context);
    }
}

