using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using UnityEngine;
using UnityEngine.Serialization;
namespace _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config
{
    /// <summary>
    /// Define o perfil de vozes para execução de áudio em pool.
    /// Gerencia orçamento de vozes simultâneas e comportamento de fallback.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AudioSfxVoiceProfile",
        menuName = "ImmersiveGames/NewScripts/Audio/Audio SFX Voice Profile",
        order = 2)]
    public sealed class AudioSfxVoiceProfileAsset : ScriptableObject
    {
        /// <summary>
        /// Definição do pool de vozes para reutilização de instâncias de áudio.
        /// </summary>
        [FormerlySerializedAs("pooledVoicePool")]
        [SerializeField] private PoolDefinitionAsset pooledVoicePoolDefinition;
        /// <summary>
        /// Se verdadeiro, permite fallback para execução direta quando o pool está cheio.
        /// </summary>
        [SerializeField] private bool allowDirectFallback = true;
        /// <summary>
        /// Número padrão de vozes simultâneas permitidas.
        /// </summary>
        [SerializeField] [Min(0)] private int defaultVoiceBudget = 16;
        /// <summary>
        /// Tempo de graça antes de liberar uma voz no pool (em segundos).
        /// </summary>
        [SerializeField] [Min(0f)] private float releaseGraceSeconds = 0.05f;

        public PoolDefinitionAsset PooledVoicePoolDefinition => pooledVoicePoolDefinition;
        public bool AllowDirectFallback => allowDirectFallback;
        public int DefaultVoiceBudget => defaultVoiceBudget;
        public float ReleaseGraceSeconds => releaseGraceSeconds;
    }
}

