using _ImmersiveGames.NewScripts.Experience.Audio.Config;
namespace _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Core
{
    /// <summary>
    /// Serviço canônico para trilha de BGM global.
    /// Fases iniciais do ADR-0028 registram apenas o contrato.
    /// </summary>
    public interface IAudioBgmService
    {
        AudioBgmCueAsset ActiveCue { get; }

        void Play(AudioBgmCueAsset cue, float fadeInSeconds = -1f, string reason = null);

        void Stop(float fadeOutSeconds = -1f, string reason = null);

        void StopImmediate(string reason = null);

        void SetPauseDucking(bool paused, string reason = null);
    }
}
