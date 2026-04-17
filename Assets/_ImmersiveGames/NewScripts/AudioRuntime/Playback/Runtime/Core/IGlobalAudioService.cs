using ImmersiveGames.GameJam2025.Experience.Audio.Config;
using ImmersiveGames.GameJam2025.Experience.Audio.Runtime.Models;
namespace ImmersiveGames.GameJam2025.Experience.Audio.Runtime.Core
{
    /// <summary>
    /// Serviço para playback de SFX global (não-entity).
    /// </summary>
    public interface IGlobalAudioService
    {
        IAudioPlaybackHandle Play(AudioSfxCueAsset cue, AudioPlaybackContext context);
    }
}

