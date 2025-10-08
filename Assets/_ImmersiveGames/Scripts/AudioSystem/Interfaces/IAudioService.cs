using _ImmersiveGames.Scripts.AudioSystem.Configs;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    public interface IAudioService
    {
        // SFX
        void PlaySound(SoundData soundData, AudioContext context, AudioConfig config = null);

        // Mixer controls
        void SetSfxVolume(float volume);
        void StopAllSounds();

        // BGM controls (optional)
        void PlayBGM(SoundData bgmData, bool loop = true, float fadeInDuration = 0f);
        void StopBGM(float fadeOutDuration = 0f);
        void PauseBGM();
        void ResumeBGM();
        void SetBGMVolume(float volume);
        void CrossfadeBGM(SoundData newBgmData, float fadeDuration = 2f);

        // Pool & state
        bool CanPlaySound(SoundData soundData);
        SoundEmitter GetEmitterFromPool();
        void ReturnEmitterToPool(SoundEmitter emitter);
        void RegisterFrequentSound(SoundEmitter emitter);
    }
}