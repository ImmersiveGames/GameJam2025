using _ImmersiveGames.Scripts.AudioSystem.Configs;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    public interface IAudioService
    {
        // SFX
        void PlaySound(SoundData soundData, AudioContext context, AudioConfig config = null);
        void SetSfxVolume(float volume);
        void StopAllSounds();

        // BGM
        void PlayBGM(SoundData bgmData, bool loop = true, float fadeInDuration = 0f);
        void StopBGM(float fadeOutDuration = 0f);
        void PauseBGM();
        void ResumeBGM();
        void SetBGMVolume(float volume);
        void CrossfadeBGM(SoundData newBgmData, float fadeDuration = 2f);

        // Estado / Pool
        bool IsBGMPlaying { get; }
        SoundData CurrentBGM { get; }
        bool CanPlaySound(SoundData soundData);
        SoundEmitter Get();
        void ReturnToPool(SoundEmitter soundEmitter);
        void RegisterFrequentSound(SoundEmitter soundEmitter);
    }
}