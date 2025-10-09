using UnityEngine;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;

namespace _ImmersiveGames.Scripts.AudioSystem.Services
{
    /// <summary>
    /// Serviço puro (não-Mono) que centraliza todas as regras de cálculo de volume/pitch/decibéis.
    /// Pode ser instanciado e registrado no DI (DependencyManager).
    /// </summary>
    public class AudioMathUtility : IAudioMathService
    {
        public float CalculateFinalVolume(
            float clipVolume,
            float configVolume,
            float categoryVolume,
            float categoryMultiplier,
            float masterVolume,
            float contextMultiplier,
            float volumeOverride = -1f)
        {
            if (volumeOverride >= 0f)
                return Mathf.Clamp01(volumeOverride);

            float val = clipVolume * configVolume * categoryVolume * categoryMultiplier * masterVolume * contextMultiplier;
            return Mathf.Clamp01(val);
        }

        public float ApplyRandomPitch(float basePitch, float variation)
        {
            if (variation <= 0f) return basePitch;
            return basePitch * Random.Range(1f - variation, 1f + variation);
        }

        public float ToDecibels(float linear) => Mathf.Log10(Mathf.Max(linear, 0.0001f)) * 20f;
        public float ToLinear(float decibels) => Mathf.Pow(10f, decibels / 20f);
    }
}