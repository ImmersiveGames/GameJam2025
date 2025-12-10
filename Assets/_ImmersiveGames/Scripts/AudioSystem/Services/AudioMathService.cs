using UnityEngine;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;

namespace _ImmersiveGames.Scripts.AudioSystem.Services
{
    /// <summary>
    /// Serviço puro (não-Mono) que centraliza todas as regras de cálculo de volume/pitch/decibéis.
    /// Pode ser instanciado e registrado no DI (DependencyManager).
    /// </summary>
    public class AudioMathService : IAudioMathService
    {
        /// <summary>
        /// Calcula o volume final combinando todas as camadas:
        /// - clipVolume: volume definido no SoundData.
        /// - configVolume: volume padrão do AudioConfig.
        /// - categoryVolume: volume da categoria (BGM/SFX) vindo de AudioServiceSettings.
        /// - categoryMultiplier: multiplicador de balance da categoria.
        /// - masterVolume: volume master global.
        /// - contextMultiplier: multiplicador vindo do contexto (ex.: distância, efeitos).
        /// - contextOverride: se >= 0, substitui completamente o cálculo e é usado como valor final.
        /// </summary>
        public float CalculateFinalVolume(
            float clipVolume,
            float configVolume,
            float categoryVolume,
            float categoryMultiplier,
            float masterVolume,
            float contextMultiplier,
            float contextOverride = -1f)
        {
            clipVolume = Mathf.Clamp01(clipVolume);
            configVolume = Mathf.Clamp01(configVolume);
            categoryVolume = Mathf.Clamp01(categoryVolume);
            categoryMultiplier = Mathf.Clamp01(categoryMultiplier);
            masterVolume = Mathf.Clamp01(masterVolume);
            contextMultiplier = Mathf.Max(0f, contextMultiplier);

            if (contextOverride >= 0f)
            {
                return Mathf.Clamp01(contextOverride);
            }

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
