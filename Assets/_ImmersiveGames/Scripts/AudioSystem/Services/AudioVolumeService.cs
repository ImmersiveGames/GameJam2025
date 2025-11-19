using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem.Services
{
    /// <summary>
    /// Implementação padrão de cálculo de volumes, isolando a combinação de camadas
    /// (clip, config, categoria, master e contexto) em um ponto único.
    /// </summary>
    public class AudioVolumeService : IAudioVolumeService
    {
        private readonly IAudioMathService _mathService;

        public AudioVolumeService(IAudioMathService mathService)
        {
            _mathService = mathService;
        }

        public float CalculateBgmVolume(SoundData soundData, AudioServiceSettings settings, float contextMultiplier = 1f)
        {
            if (soundData == null) return 0f;

            float master = settings != null ? settings.masterVolume : 1f;
            float categoryVolume = settings != null ? settings.bgmVolume : 1f;
            float categoryMultiplier = settings != null ? settings.bgmMultiplier : 1f;

            return _mathService?.CalculateFinalVolume(
                soundData.volume,
                1f,
                categoryVolume,
                categoryMultiplier,
                master,
                contextMultiplier) ?? Mathf.Clamp01(soundData.volume * categoryVolume * master * contextMultiplier);
        }

        public float CalculateSfxVolume(SoundData soundData, AudioConfig config, AudioServiceSettings settings, AudioContext context)
        {
            if (soundData == null) return 0f;

            float master = settings != null ? settings.masterVolume : 1f;
            float categoryVolume = settings != null ? settings.sfxVolume : 1f;
            float categoryMultiplier = settings != null ? settings.sfxMultiplier : 1f;
            float configDefault = config != null ? config.defaultVolume : 1f;

            return _mathService?.CalculateFinalVolume(
                soundData.volume,
                configDefault,
                categoryVolume,
                categoryMultiplier,
                master,
                context.volumeMultiplier,
                context.volumeOverride) ?? Mathf.Clamp01(soundData.volume * configDefault * categoryVolume * master * context.volumeMultiplier);
        }
    }
}
