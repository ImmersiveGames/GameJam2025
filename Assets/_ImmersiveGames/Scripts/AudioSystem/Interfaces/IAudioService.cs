using _ImmersiveGames.Scripts.AudioSystem.Configs;
using UnityEngine;
namespace _ImmersiveGames.Scripts.AudioSystem.Interfaces
{
    public interface IAudioService
    {
        // SFX (existente)
        void PlaySound(SoundData soundData, Vector3 position, AudioConfig config = null);
        void SetSfxVolume(float volume);
        void StopAllSounds();

        // BGM (novo)
        void PlayBGM(SoundData bgmData, bool loop = true, float fadeInDuration = 0f);
        void StopBGM(float fadeOutDuration = 0f);
        void PauseBGM();
        void ResumeBGM();
        void SetBGMVolume(float volume);
        void CrossfadeBGM(SoundData newBgmData, float fadeDuration = 2f);
        
        // Estado
        bool IsBGMPlaying { get; }
        SoundData CurrentBGM { get; }
        bool CanPlaySound(SoundData soundData);
        SoundEmitter Get();
        void ReturnToPool(SoundEmitter soundEmitter);
        void RegisterFrequentSound(SoundEmitter soundEmitter);
    }
}