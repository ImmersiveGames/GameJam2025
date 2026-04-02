using System;
using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Experience.Audio.Config
{
    /// <summary>
    /// Mapeia um propósito semântico para uma configuração de áudio SFX com perfis de emissão e execução.
    /// </summary>
    [Serializable]
    public sealed class EntityAudioPurposeEntry
    {
        /// <summary>
        /// Identificador único do propósito de áudio (ex: "footstep", "jump", "impact").
        /// </summary>
        [SerializeField] private string purpose;
        /// <summary>
        /// Cue de áudio a ser tocado para este propósito.
        /// </summary>
        [SerializeField] private AudioSfxCueAsset cue;
        /// <summary>
        /// Perfil de emissão alternativo, sobrescreve o padrão da cue se fornecido.
        /// </summary>
        [SerializeField] private AudioSfxEmissionProfileAsset emissionProfileOverride;
        /// <summary>
        /// Perfil de execução alternativo, sobrescreve o padrão da cue se fornecido.
        /// </summary>
        [SerializeField] private AudioSfxExecutionProfileAsset executionProfileOverride;
        /// <summary>
        /// Perfil de vozes alternativo, sobrescreve o padrão da cue se fornecido.
        /// </summary>
        [SerializeField] private AudioSfxVoiceProfileAsset voiceProfileOverride;
        /// <summary>
        /// Se verdadeiro, a posição do proprietário (owner) é usada como alvo de seguimento de áudio.
        /// </summary>
        [SerializeField] private bool useOwnerAsFollowTarget = true;
        /// <summary>
        /// Multiplicador de volume aplicado a esta entrada específica.
        /// </summary>
        [SerializeField] [Min(0f)] private float volumeScaleMultiplier = 1f;
        /// <summary>
        /// Tag de razão para logging/debug.
        /// </summary>
        [SerializeField] private string reasonTag;

        public string Purpose => purpose;
        public AudioSfxCueAsset Cue => cue;
        public AudioSfxEmissionProfileAsset EmissionProfileOverride => emissionProfileOverride;
        public AudioSfxExecutionProfileAsset ExecutionProfileOverride => executionProfileOverride;
        public AudioSfxVoiceProfileAsset VoiceProfileOverride => voiceProfileOverride;
        public bool UseOwnerAsFollowTarget => useOwnerAsFollowTarget;
        public float VolumeScaleMultiplier => volumeScaleMultiplier;
        public string ReasonTag => reasonTag;

        /// <summary>
        /// Verifica se este propósito corresponde ao valor fornecido (comparação case-insensitive).
        /// </summary>
        /// <param name="value">Valor a comparar.</param>
        /// <returns>Verdadeiro se os valores correspondem.</returns>
        public bool Matches(string value)
        {
            if (string.IsNullOrWhiteSpace(purpose) || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return string.Equals(
                purpose.Trim(),
                value.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }
    }
    /// <summary>
         /// Mapa semântico que associa propósitos de áudio a configurações SFX.
         /// Permite resolver cues e perfis de áudio baseado em identificadores de propósito.
         /// </summary>
    [CreateAssetMenu(
        fileName = "EntityAudioSemanticMap",
        menuName = "ImmersiveGames/NewScripts/Audio/Entity Audio Semantic Map",
        order = 6)]

    public sealed class EntityAudioSemanticMapAsset : ScriptableObject
    {
        /// <summary>
        /// Lista de entradas que mapeiam propósitos para configurações de áudio.
        /// </summary>
        [SerializeField] private List<EntityAudioPurposeEntry> entries = new();

        public IReadOnlyList<EntityAudioPurposeEntry> Entries => entries;

        /// <summary>
        /// Tenta resolver uma entrada de propósito de áudio.
        /// </summary>
        /// <param name="purpose">Identificador do propósito (ex: "footstep", "jump").</param>
        /// <param name="entry">Entrada encontrada, se a busca for bem-sucedida.</param>
        /// <returns>Verdadeiro se uma entrada correspondente foi encontrada.</returns>
        public bool TryResolve(string purpose, out EntityAudioPurposeEntry entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(purpose) || entries == null || entries.Count == 0)
            {
                return false;
            }

            foreach (var candidate in entries)
            {
                if (candidate?.Cue == null)
                {
                    continue;
                }

                if (!candidate.Matches(purpose))
                {
                    continue;
                }


                entry = candidate;
                return true;
            }

            return false;
        }
    }

}
