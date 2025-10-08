using _ImmersiveGames.Scripts.AudioSystem.Configs;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    public static class AudioSystemHelper
    {
        public static IAudioService Service => AudioSystemInitializer.GetAudioService();

        public static void PlaySound(SoundData soundData, Vector3 position, float volumeMultiplier = 1f)
        {
            var ctx = AudioContext.Default(position, true, volumeMultiplier);
            Service?.PlaySound(soundData, ctx);
        }

        public static void PlaySound(SoundData soundData, float volumeMultiplier = 1f)
        {
            var ctx = AudioContext.NonSpatial(volumeMultiplier);
            Service?.PlaySound(soundData, ctx);
        }

        public static void SetSfxVolume(float volume) => Service?.SetSfxVolume(volume);
        public static void StopAllSounds() => Service?.StopAllSounds();

        // BGM helpers forwarded if implemented
        public static void PlayBGM(SoundData bgmData, bool loop = true, float fadeInDuration = 0f) => Service?.PlayBGM(bgmData, loop, fadeInDuration);
        public static void StopBGM(float fadeOutDuration = 0f) => Service?.StopBGM(fadeOutDuration);
        public static void PauseBGM() => Service?.PauseBGM();
        public static void ResumeBGM() => Service?.ResumeBGM();
        public static void SetBGMVolume(float volume) => Service?.SetBGMVolume(volume);
        public static void CrossfadeBGM(SoundData newBgmData, float fadeDuration = 2f) => Service?.CrossfadeBGM(newBgmData, fadeDuration);
    }
}