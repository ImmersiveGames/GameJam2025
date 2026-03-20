using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Runtime
{
    public sealed class AudioSettingsService : IAudioSettingsService
    {
        private float _masterVolume;
        private float _bgmVolume;
        private float _sfxVolume;
        private float _bgmCategoryMultiplier;
        private float _sfxCategoryMultiplier;

        public AudioSettingsService(
            float masterVolume,
            float bgmVolume,
            float sfxVolume,
            float bgmCategoryMultiplier,
            float sfxCategoryMultiplier)
        {
            _masterVolume = Mathf.Clamp01(masterVolume);
            _bgmVolume = Mathf.Clamp01(bgmVolume);
            _sfxVolume = Mathf.Clamp01(sfxVolume);
            _bgmCategoryMultiplier = Mathf.Max(0f, bgmCategoryMultiplier);
            _sfxCategoryMultiplier = Mathf.Max(0f, sfxCategoryMultiplier);
        }

        public float MasterVolume
        {
            get => _masterVolume;
            set => _masterVolume = Mathf.Clamp01(value);
        }

        public float BgmVolume
        {
            get => _bgmVolume;
            set => _bgmVolume = Mathf.Clamp01(value);
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set => _sfxVolume = Mathf.Clamp01(value);
        }

        public float BgmCategoryMultiplier
        {
            get => _bgmCategoryMultiplier;
            set => _bgmCategoryMultiplier = Mathf.Max(0f, value);
        }

        public float SfxCategoryMultiplier
        {
            get => _sfxCategoryMultiplier;
            set => _sfxCategoryMultiplier = Mathf.Max(0f, value);
        }
    }
}
