using System;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Experience.Audio.Runtime
{
    /// <summary>
    /// Estado runtime mutável da sessão do jogador.
    /// É semeado pelos defaults técnicos no boot, mas não carrega seleção de conteúdo.
    /// </summary>
    public sealed class AudioSettingsService : IAudioSettingsService
    {
        private float _masterVolume;
        private float _bgmVolume;
        private float _sfxVolume;
        private float _bgmCategoryMultiplier;
        private float _sfxCategoryMultiplier;

        public event Action<string> VolumeChanged;

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
            set => SetClampedVolume(ref _masterVolume, value, "MasterVolumeChanged");
        }

        public float BgmVolume
        {
            get => _bgmVolume;
            set => SetClampedVolume(ref _bgmVolume, value, "BgmVolumeChanged");
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set => SetClampedVolume(ref _sfxVolume, value, "SfxVolumeChanged");
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

        private void SetClampedVolume(ref float field, float value, string reason)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(field, clamped))
            {
                return;
            }

            field = clamped;
            VolumeChanged?.Invoke(reason);
        }
    }
}
