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
        [SerializeField] private AudioSfxPlaybackMode playbackMode = AudioSfxPlaybackMode.Global;
        [SerializeField] [Range(0f, 1f)] private float spatialBlend = 1f;
        [SerializeField] [Min(0f)] private float minDistance = 1f;
        [SerializeField] [Min(0f)] private float maxDistance = 40f;
        [SerializeField] private AudioSfxExecutionMode executionMode = AudioSfxExecutionMode.DirectOneShot;
        [SerializeField] private AudioSfxVoiceProfileAsset voiceProfileOverride;
        [SerializeField] [Min(1)] private int maxSimultaneousInstances = 1;
        [SerializeField] [Min(0f)] private float sfxRetriggerCooldownSeconds;

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
