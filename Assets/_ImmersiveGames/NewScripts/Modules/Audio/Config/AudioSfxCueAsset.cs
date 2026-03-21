using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Config
{
    public enum AudioSfxPlaybackMode
    {
        Global = 0,
        Spatial = 1
    }

    public enum AudioSfxExecutionMode
    {
        DirectOneShot = 0,
        PooledOneShot = 1
    }

    [CreateAssetMenu(
        fileName = "AudioSfxCue",
        menuName = "ImmersiveGames/NewScripts/Audio/Audio SFX Cue",
        order = 1)]
    public sealed class AudioSfxCueAsset : AudioCueAsset
    {
        [SerializeField] private AudioSfxEmissionProfileAsset emissionProfile;
        [SerializeField] private AudioSfxExecutionProfileAsset executionProfile;

        // Compatibilidade legada: sera migrado gradualmente para EmissionProfile.
        [SerializeField] private AudioSfxPlaybackMode playbackMode = AudioSfxPlaybackMode.Global;
        // Compatibilidade legada: sera migrado gradualmente para EmissionProfile.
        [SerializeField] [Range(0f, 1f)] private float spatialBlend = 1f;
        // Compatibilidade legada: sera migrado gradualmente para EmissionProfile.
        [SerializeField] [Min(0f)] private float minDistance = 1f;
        // Compatibilidade legada: sera migrado gradualmente para EmissionProfile.
        [SerializeField] [Min(0f)] private float maxDistance = 40f;
        // Compatibilidade legada: sera migrado gradualmente para ExecutionProfile.
        [SerializeField] private AudioSfxExecutionMode executionMode = AudioSfxExecutionMode.DirectOneShot;
        // Compatibilidade legada: sera migrado gradualmente para ExecutionProfile (pooled).
        [SerializeField] private AudioSfxVoiceProfileAsset voiceProfileOverride;
        // Compatibilidade legada: concern de policy sera movido gradualmente para profile dedicado.
        [SerializeField] [Min(1)] private int maxSimultaneousInstances = 1;
        // Compatibilidade legada: concern de policy sera movido gradualmente para profile dedicado.
        [SerializeField] [Min(0f)] private float sfxRetriggerCooldownSeconds;

        public AudioSfxEmissionProfileAsset EmissionProfile => emissionProfile;
        public AudioSfxExecutionProfileAsset ExecutionProfile => executionProfile;
        public AudioSfxPlaybackMode PlaybackMode => playbackMode;
        public float SpatialBlend => spatialBlend;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public AudioSfxExecutionMode ExecutionMode => executionMode;
        public AudioSfxVoiceProfileAsset VoiceProfileOverride => voiceProfileOverride;
        public int MaxSimultaneousInstances => maxSimultaneousInstances;
        public float SfxRetriggerCooldownSeconds => sfxRetriggerCooldownSeconds;
    }
}
