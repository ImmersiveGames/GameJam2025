using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Contracts
{
    public sealed class AudioPreferencesSnapshot
    {
        public const string BootstrapProfileId = "default";
        public const string BootstrapSlotId = "audio";

        public AudioPreferencesSnapshot(
            string profileId,
            string slotId,
            float masterVolume,
            float bgmVolume,
            float sfxVolume)
        {
            ProfileId = NormalizeIdentity(profileId, nameof(profileId));
            SlotId = NormalizeIdentity(slotId, nameof(slotId));
            MasterVolume = Mathf.Clamp01(masterVolume);
            BgmVolume = Mathf.Clamp01(bgmVolume);
            SfxVolume = Mathf.Clamp01(sfxVolume);
        }

        public string ProfileId { get; }
        public string SlotId { get; }
        public float MasterVolume { get; }
        public float BgmVolume { get; }
        public float SfxVolume { get; }

        public static AudioPreferencesSnapshot CaptureFrom(
            string profileId,
            string slotId,
            IAudioSettingsService audioSettings)
        {
            if (audioSettings == null)
            {
                throw new ArgumentNullException(nameof(audioSettings));
            }

            return new AudioPreferencesSnapshot(
                profileId,
                slotId,
                audioSettings.MasterVolume,
                audioSettings.BgmVolume,
                audioSettings.SfxVolume);
        }

        public void ApplyTo(IAudioSettingsService audioSettings)
        {
            if (audioSettings == null)
            {
                throw new ArgumentNullException(nameof(audioSettings));
            }

            audioSettings.MasterVolume = MasterVolume;
            audioSettings.BgmVolume = BgmVolume;
            audioSettings.SfxVolume = SfxVolume;
        }

        public override string ToString()
        {
            return $"profile='{ProfileId}' slot='{SlotId}' master={MasterVolume:0.###} bgm={BgmVolume:0.###} sfx={SfxVolume:0.###}";
        }

        private static string NormalizeIdentity(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Identity is required.", paramName);
            }

            return value.Trim();
        }
    }
}

