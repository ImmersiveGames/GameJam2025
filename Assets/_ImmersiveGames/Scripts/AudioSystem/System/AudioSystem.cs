using _ImmersiveGames.Scripts.AudioSystem.Configs;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    public static class AudioSystemHelper
    {
        public static bool IsInitialized => AudioSystemInitializer.IsInitialized();
        public static IAudioService Service => AudioSystemInitializer.GetAudioService();

        // SFX (existente)
        public static void PlaySound(SoundData soundData, Vector3 position)
        {
            Service?.PlaySound(soundData, position);
        }

        public static void PlaySound(SoundData soundData)
        {
            Service?.PlaySound(soundData, Vector3.zero);
        }

        public static void SetSfxVolume(float volume)
        {
            Service?.SetSfxVolume(volume);
        }

        public static void StopAllSounds()
        {
            Service?.StopAllSounds();
        }

        // BGM (novo)
        public static void PlayBGM(SoundData bgmData, bool loop = true, float fadeInDuration = 0f)
        {
            Service?.PlayBGM(bgmData, loop, fadeInDuration);
        }

        public static void StopBGM(float fadeOutDuration = 0f)
        {
            Service?.StopBGM(fadeOutDuration);
        }

        public static void PauseBGM()
        {
            Service?.PauseBGM();
        }

        public static void ResumeBGM()
        {
            Service?.ResumeBGM();
        }

        public static void SetBGMVolume(float volume)
        {
            Service?.SetBGMVolume(volume);
        }

        public static void CrossfadeBGM(SoundData newBgmData, float fadeDuration = 2f)
        {
            Service?.CrossfadeBGM(newBgmData, fadeDuration);
        }

        // Estado
        public static bool IsBGMPlaying => Service?.IsBGMPlaying ?? false;
        public static SoundData CurrentBGM => Service?.CurrentBGM;

        [RuntimeInitializeOnLoadMethod]
        private static void AutoInitialize()
        {
            AudioSystemInitializer.EnsureAudioSystemInitialized();
        }
    }
}