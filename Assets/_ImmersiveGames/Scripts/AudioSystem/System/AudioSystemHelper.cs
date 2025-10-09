using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    public static class AudioSystemHelper
    {
        public static IAudioService Service => AudioSystemInitializer.GetAudioService();

        // BGM convenience
        public static void PlayBGM(SoundData bgmData, bool loop = true, float fadeIn = 0f) => Service?.PlayBGM(bgmData, loop, fadeIn);
        public static void StopBGM(float fadeOut = 0f) => Service?.StopBGM(fadeOut);
        public static void PauseBGM() => Service?.PauseBGM();
        public static void ResumeBGM() => Service?.ResumeBGM();
        public static void SetBGMVolume(float v) => Service?.SetBGMVolume(v);
        public static void CrossfadeBGM(SoundData newBgm, float d = 2f) => Service?.CrossfadeBGM(newBgm, d);
        public static void StopBGMImmediate() => Service?.StopBGM(fadeOutDuration: 0f);

        [UnityEngine.RuntimeInitializeOnLoadMethod]
        private static void AutoInit() => AudioSystemInitializer.EnsureAudioSystemInitialized();
    }
}