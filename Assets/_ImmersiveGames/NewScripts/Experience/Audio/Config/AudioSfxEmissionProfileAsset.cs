using UnityEngine;
namespace _ImmersiveGames.NewScripts.Experience.Audio.Config
{
    /// <summary>
    /// Define as propriedades de emissão de áudio SFX: modo de emissão e configurações espaciais.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AudioSfxEmissionProfile",
        menuName = "ImmersiveGames/NewScripts/Audio/Audio SFX Emission Profile",
        order = 3)]
    public sealed class AudioSfxEmissionProfileAsset : ScriptableObject
    {
        /// <summary>
        /// Modo de emissão: Global (não espacial) ou Spatial (3D).
        /// </summary>
        [SerializeField] private AudioSfxPlaybackMode emissionMode = AudioSfxPlaybackMode.Global;
        /// <summary>
        /// Blenda espacial: 0 = mono (global), 1 = totalmente espacial (3D).
        /// </summary>
        [SerializeField] [Range(0f, 1f)] private float spatialBlend = 1f;
        /// <summary>
        /// Distância mínima para começar o fade de atenuação.
        /// </summary>
        [SerializeField] [Min(0f)] private float minDistance = 1f;
        /// <summary>
        /// Distância máxima além da qual o som não é mais audível.
        /// </summary>
        [SerializeField] [Min(0f)] private float maxDistance = 40f;

        public AudioSfxPlaybackMode EmissionMode => emissionMode;
        public float SpatialBlend => spatialBlend;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
    }
}
