using UnityEngine;
namespace ImmersiveGames.GameJam2025.Experience.Audio.Config
{
    /// <summary>
    /// Define como os efeitos sonoros SFX são executados: diretamente ou por um pool.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AudioSfxExecutionProfile",
        menuName = "ImmersiveGames/NewScripts/Audio/Audio SFX Execution Profile",
        order = 4)]
    public sealed class AudioSfxExecutionProfileAsset : ScriptableObject
    {
        /// <summary>
        /// Modo de execução: DirectOneShot ou PooledOneShot.
        /// </summary>
        [SerializeField] private AudioSfxExecutionMode executionMode = AudioSfxExecutionMode.DirectOneShot;
        /// <summary>
        /// Perfil de vozes a ser usado para execução em pool.
        /// </summary>
        [SerializeField] private AudioSfxVoiceProfileAsset pooledVoiceProfile;

        public AudioSfxExecutionMode ExecutionMode => executionMode;
        public AudioSfxVoiceProfileAsset PooledVoiceProfile => pooledVoiceProfile;
    }
}

