using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Configs;

namespace _ImmersiveGames.Scripts.AudioSystem.Interfaces
{
    /// <summary>
    /// Serviço global para reprodução de SFX desacoplado de pools ou emitters específicos.
    /// </summary>
    public interface IAudioSfxService
    {
        IAudioHandle PlayOneShot(SoundData sound, AudioContext context, float fadeInSeconds = 0f);
        IAudioHandle PlayLoop(SoundData sound, AudioContext context, float fadeInSeconds = 0f);
    }
}
