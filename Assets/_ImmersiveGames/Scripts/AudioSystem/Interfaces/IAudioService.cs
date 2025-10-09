using _ImmersiveGames.Scripts.AudioSystem.Configs;
namespace _ImmersiveGames.Scripts.AudioSystem.Interfaces
{
    /// <summary>
    /// Serviço global de áudio — apenas para BGM / mixer global.
    /// SFX devem ser tocados por AudioControllerBase (pools locais).
    /// </summary>
    public interface IAudioService
    {
        // BGM
        void PlayBGM(SoundData bgmData, bool loop = true, float fadeInDuration = 0f);
        void StopBGM(float fadeOutDuration = 0f);
        void StopBGMImmediate();
        void PauseBGM();
        void ResumeBGM();
        void SetBGMVolume(float volume);
        void CrossfadeBGM(SoundData newBgmData, float fadeDuration = 2f);
    }
    public interface IAudioMathService
    {
        /// <summary>
        /// Cálculo final de volume linear (0..1) combinando as camadas:
        /// clipVolume * configVolume * categoryVolume * categoryMultiplier * masterVolume * contextMultiplier
        /// Se volumeOverride >= 0, esse valor será usado como resultado final (após clamp).
        /// </summary>
        float CalculateFinalVolume(
            float clipVolume,
            float configVolume,
            float categoryVolume,
            float categoryMultiplier,
            float masterVolume,
            float contextMultiplier,
            float volumeOverride = -1f);

        float ApplyRandomPitch(float basePitch, float variation);
        float ToDecibels(float linear);
        float ToLinear(float decibels);
    }
}