using UnityEngine;
namespace _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config
{
    /// <summary>
    /// Define o modo de emiss�o de �udio: global (n�o espacial) ou espacial (3D).
    /// </summary>
    public enum AudioSfxPlaybackMode
    {
        /// <summary>
        /// �udio emitido globalmente, sem posicionamento espacial 3D.
        /// </summary>
        Global = 0,
        /// <summary>
        /// �udio com posicionamento espacial 3D baseado na posi��o de origem.
        /// </summary>
        Spatial = 1
    }

    /// <summary>
    /// Define o modo de execu��o/reprodu��o de efeitos sonoros.
    /// </summary>
    public enum AudioSfxExecutionMode
    {
        /// <summary>
        /// Execu��o direta: toca uma �nica inst�ncia sem pool de objetos.
        /// </summary>
        DirectOneShot = 0,
        /// <summary>
        /// Execu��o em pool: reutiliza inst�ncias de �udio do pool para efici�ncia.
        /// </summary>
        PooledOneShot = 1
    }

    /// <summary>
    /// Cue de �udio para efeitos sonoros (SFX).
    /// Define propriedades de emiss�o (global/espacial) e execu��o (direto/pool).
    /// </summary>
    [CreateAssetMenu(
        fileName = "AudioSfxCue",
        menuName = "ImmersiveGames/NewScripts/Audio/Audio SFX Cue",
        order = 1)]
    public sealed class AudioSfxCueAsset : AudioCueAsset
    {
        /// <summary>
        /// Perfil de emiss�o que define modo e par�metros espaciais.
        /// </summary>
        [SerializeField] private AudioSfxEmissionProfileAsset emissionProfile;
        /// <summary>
        /// Perfil de execu��o que define modo e par�metros de pool.
        /// </summary>
        [SerializeField] private AudioSfxExecutionProfileAsset executionProfile;

        /// <summary>
        /// Pol�tica ativa de concorr�ncia para SFX.
        /// </summary>
        [SerializeField] [Min(1)] private int maxSimultaneousInstances = 1;
        /// <summary>
        /// Janela ativa de cooldown entre retriggers.
        /// </summary>
        [SerializeField] [Min(0f)] private float sfxRetriggerCooldownSeconds;

        public AudioSfxEmissionProfileAsset EmissionProfile => emissionProfile;
        public AudioSfxExecutionProfileAsset ExecutionProfile => executionProfile;
        public int MaxSimultaneousInstances => maxSimultaneousInstances;
        public float SfxRetriggerCooldownSeconds => sfxRetriggerCooldownSeconds;
    }
}

