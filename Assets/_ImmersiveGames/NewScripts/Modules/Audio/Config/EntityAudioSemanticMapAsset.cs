using System;
using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Config
{
    [Serializable]
    public sealed class EntityAudioPurposeEntry
    {
        [SerializeField] private string purpose;
        [SerializeField] private AudioSfxCueAsset cue;
        [SerializeField] private AudioSfxEmissionProfileAsset emissionProfileOverride;
        [SerializeField] private AudioSfxExecutionProfileAsset executionProfileOverride;
        [SerializeField] private AudioSfxVoiceProfileAsset voiceProfileOverride;
        [SerializeField] private bool useOwnerAsFollowTarget = true;
        [SerializeField] [Min(0f)] private float volumeScaleMultiplier = 1f;
        [SerializeField] private string reasonTag;

        public string Purpose => purpose;
        public AudioSfxCueAsset Cue => cue;
        public AudioSfxEmissionProfileAsset EmissionProfileOverride => emissionProfileOverride;
        public AudioSfxExecutionProfileAsset ExecutionProfileOverride => executionProfileOverride;
        public AudioSfxVoiceProfileAsset VoiceProfileOverride => voiceProfileOverride;
        public bool UseOwnerAsFollowTarget => useOwnerAsFollowTarget;
        public float VolumeScaleMultiplier => volumeScaleMultiplier;
        public string ReasonTag => reasonTag;

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

    [CreateAssetMenu(
        fileName = "EntityAudioSemanticMap",
        menuName = "ImmersiveGames/NewScripts/Audio/Entity Audio Semantic Map",
        order = 6)]
    public sealed class EntityAudioSemanticMapAsset : ScriptableObject
    {
        [SerializeField] private List<EntityAudioPurposeEntry> entries = new List<EntityAudioPurposeEntry>();

        public IReadOnlyList<EntityAudioPurposeEntry> Entries => entries;

        public bool TryResolve(string purpose, out EntityAudioPurposeEntry entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(purpose) || entries == null || entries.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var candidate = entries[i];
                if (candidate == null)
                {
                    continue;
                }

                if (!candidate.Matches(purpose))
                {
                    continue;
                }

                if (candidate.Cue == null)
                {
                    return false;
                }

                entry = candidate;
                return true;
            }

            return false;
        }
    }
}
