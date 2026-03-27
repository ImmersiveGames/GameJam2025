using UnityEngine;
using UnityEngine.Audio;

namespace _ImmersiveGames.Scripts.AudioSystem.Configs
{
    /// <summary>
    /// DescriÃ§Ã£o completa de um som individual (SFX ou BGM):
    /// clip, volume base, comportamento de loop, prioridade e parÃ¢metros espaciais.
    ///
    /// Este Ã© o ponto principal de ediÃ§Ã£o para game designers ajustarem sons especÃ­ficos.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoundData",
        menuName = "ImmersiveGames/Legacy/Audio/Sound Data",
        order = 1)]
    public class SoundData : ScriptableObject
    {
        [Header("Audio Clip")]
        [Tooltip("Clip de Ã¡udio que serÃ¡ reproduzido.")]
        public AudioClip clip;

        [Tooltip("Mixer Group opcional para este som especÃ­fico. Se nulo, serÃ¡ usado o mixer padrÃ£o definido em AudioConfig.")]
        public AudioMixerGroup mixerGroup;

        [Header("Audio Settings")]
        [Tooltip("Volume base do som (antes de master, categoria e multiplicadores de contexto).")]
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("Prioridade do AudioSource (0 = mais alta, 256 = mais baixa).")]
        [Range(0, 256)] public int priority = 128;

        [Tooltip("Se verdadeiro, este som serÃ¡ reproduzido em loop quando disparado em modo loop.")]
        public bool loop;

        [Tooltip("Se verdadeiro, o som serÃ¡ reproduzido automaticamente ao ser criado (em casos onde isso fizer sentido).")]
        public bool playOnAwake;

        [Header("Behavior")]
        [Tooltip("Se verdadeiro, aplica uma variaÃ§Ã£o aleatÃ³ria de pitch a cada reproduÃ§Ã£o.")]
        public bool randomPitch;

        [Tooltip("Intensidade da variaÃ§Ã£o de pitch ao redor do valor base (0 = sem variaÃ§Ã£o).")]
        [Range(0f, 0.5f)] public float pitchVariation = 0.05f;

        [Header("Spatial Settings")]
        [Tooltip("0 = som 2D (sem posiÃ§Ã£o); 1 = som totalmente 3D. Valores intermediÃ¡rios misturam 2D/3D.")]
        [Range(0f, 1f)] public float spatialBlend;

        [Tooltip("DistÃ¢ncia mÃ¡xima efetiva do som em 3D (caso spatialBlend > 0).")]
        public float maxDistance = 50f;
    }
}
