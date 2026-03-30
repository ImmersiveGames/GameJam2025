using _ImmersiveGames.NewScripts.Experience.Audio.Config;
using _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Models;
namespace _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Core
{
    /// <summary>
    /// Serviço para playback de SFX global (não-entity).
    /// </summary>
    public interface IGlobalAudioService
    {
        IAudioPlaybackHandle Play(AudioSfxCueAsset cue, AudioPlaybackContext context);
    }
}
