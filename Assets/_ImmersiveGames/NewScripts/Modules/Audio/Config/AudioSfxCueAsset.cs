using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Config
{
    /// <summary>
    /// Define o modo de emissão de áudio: global (não espacial) ou espacial (3D).
    /// </summary>
    public enum AudioSfxPlaybackMode
    {
        /// <summary>
        /// Áudio emitido globalmente, sem posicionamento espacial 3D.
        /// </summary>
        Global = 0,
        /// <summary>
        /// Áudio com posicionamento espacial 3D baseado na posição de origem.
        /// </summary>
        Spatial = 1
    }

    /// <summary>
    /// Define o modo de execução/reprodução de efeitos sonoros.
    /// </summary>
    public enum AudioSfxExecutionMode
    {
        /// <summary>
        /// Execução direta: toca uma única instância sem pool de objetos.
        /// </summary>
        DirectOneShot = 0,
        /// <summary>
        /// Execução em pool: reutiliza instâncias de áudio do pool para eficiência.
        /// </summary>
        PooledOneShot = 1
    }

    /// <summary>
    /// Cue de áudio para efeitos sonoros (SFX).
    /// Define propriedades de emissão (global/espacial) e execução (direto/pool).
    /// </summary>
    [CreateAssetMenu(
        fileName = "AudioSfxCue",
        menuName = "ImmersiveGames/NewScripts/Audio/Audio SFX Cue",
        order = 1)]
    public sealed class AudioSfxCueAsset : AudioCueAsset
    {
        /// <summary>
        /// Perfil de emissão que define modo e parâmetros espaciais.
        /// </summary>
        [SerializeField] private AudioSfxEmissionProfileAsset emissionProfile;
        /// <summary>
        /// Perfil de execução que define modo e parâmetros de pool.
        /// </summary>
        [SerializeField] private AudioSfxExecutionProfileAsset executionProfile;

        /// <summary>
        /// Compatibilidade legada: será migrado gradualmente para EmissionProfile.
        /// </summary>
        [SerializeField] private AudioSfxPlaybackMode playbackMode = AudioSfxPlaybackMode.Global;
        /// <summary>
        /// Compatibilidade legada: será migrado gradualmente para EmissionProfile.
        /// </summary>
        [SerializeField] [Range(0f, 1f)] private float spatialBlend = 1f;
        /// <summary>
        /// Compatibilidade legada: será migrado gradualmente para EmissionProfile.
        /// </summary>
        [SerializeField] [Min(0f)] private float minDistance = 1f;
        /// <summary>
        /// Compatibilidade legada: será migrado gradualmente para EmissionProfile.
        /// </summary>
        [SerializeField] [Min(0f)] private float maxDistance = 40f;
        /// <summary>
        /// Compatibilidade legada: será migrado gradualmente para ExecutionProfile.
        /// </summary>
        [SerializeField] private AudioSfxExecutionMode executionMode = AudioSfxExecutionMode.DirectOneShot;
        /// <summary>
        /// Compatibilidade legada: será migrado gradualmente para ExecutionProfile (pooled).
        /// </summary>
        [SerializeField] private AudioSfxVoiceProfileAsset voiceProfileOverride;
        /// <summary>
        /// Compatibilidade legada: preocupação de política será movida gradualmente para profile dedicado.
        /// </summary>
        [SerializeField] [Min(1)] private int maxSimultaneousInstances = 1;
        /// <summary>
        /// Compatibilidade legada: preocupação de política será movida gradualmente para profile dedicado.
        /// </summary>
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
