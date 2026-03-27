using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Runtime
{
    /// <summary>
    /// Serviço semântico de áudio local por entidade.
    /// </summary>
    public interface IEntityAudioService
    {
        IAudioPlaybackHandle PlayCue(AudioSfxCueAsset cue, AudioPlaybackContext context);

        IAudioPlaybackHandle PlayPurpose(string purpose, Transform owner, AudioPlaybackContext context);
    }
}
