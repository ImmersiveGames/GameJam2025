using _ImmersiveGames.NewScripts.Experience.Audio.Config;
using _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Core;
using _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Models;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Experience.Audio.Semantics
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
