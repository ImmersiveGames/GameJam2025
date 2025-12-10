using UnityEngine;
using UnityEngine.Audio;

namespace _ImmersiveGames.Scripts.AudioSystem.Configs
{
    /// <summary>
    /// Descrição completa de um som individual (SFX ou BGM):
    /// clip, volume base, comportamento de loop, prioridade e parâmetros espaciais.
    ///
    /// Este é o ponto principal de edição para game designers ajustarem sons específicos.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoundData",
        menuName = "ImmersiveGames/Audio/Sound Data",
        order = 1)]
    public class SoundData : ScriptableObject
    {
        [Header("Audio Clip")]
        [Tooltip("Clip de áudio que será reproduzido.")]
        public AudioClip clip;

        [Tooltip("Mixer Group opcional para este som específico. Se nulo, será usado o mixer padrão definido em AudioConfig.")]
        public AudioMixerGroup mixerGroup;

        [Header("Audio Settings")]
        [Tooltip("Volume base do som (antes de master, categoria e multiplicadores de contexto).")]
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("Prioridade do AudioSource (0 = mais alta, 256 = mais baixa).")]
        [Range(0, 256)] public int priority = 128;

        [Tooltip("Se verdadeiro, este som será reproduzido em loop quando disparado em modo loop.")]
        public bool loop;

        [Tooltip("Se verdadeiro, o som será reproduzido automaticamente ao ser criado (em casos onde isso fizer sentido).")]
        public bool playOnAwake;

        [Header("Behavior")]
        [Tooltip("Se verdadeiro, aplica uma variação aleatória de pitch a cada reprodução.")]
        public bool randomPitch;

        [Tooltip("Intensidade da variação de pitch ao redor do valor base (0 = sem variação).")]
        [Range(0f, 0.5f)] public float pitchVariation = 0.05f;

        [Header("Spatial Settings")]
        [Tooltip("0 = som 2D (sem posição); 1 = som totalmente 3D. Valores intermediários misturam 2D/3D.")]
        [Range(0f, 1f)] public float spatialBlend;

        [Tooltip("Distância máxima efetiva do som em 3D (caso spatialBlend > 0).")]
        public float maxDistance = 50f;
    }
}
