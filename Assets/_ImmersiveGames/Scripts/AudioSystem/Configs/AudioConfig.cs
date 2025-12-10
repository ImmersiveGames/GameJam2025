using UnityEngine;
using UnityEngine.Audio;

namespace _ImmersiveGames.Scripts.AudioSystem.Configs
{
    /// <summary>
    /// Configuração padrão de áudio para SFX:
    /// mixer default, volume base e parâmetros 3D.
    ///
    /// Esta configuração funciona como "perfil" padrão para emissores que
    /// não possuem ajustes específicos por som.
    /// </summary>
    [CreateAssetMenu(
        menuName = "ImmersiveGames/Audio/Audio Config",
        fileName = "AudioConfig",
        order = 0)]
    public class AudioConfig : ScriptableObject
    {
        [Header("Volume Defaults")]
        [Tooltip("Volume padrão usado quando o SoundData não especifica outro volume ou como referência para balance geral.")]
        [Range(0f, 1f)]
        public float defaultVolume = 1f;

        [Tooltip("Mixer Group padrão utilizado quando o SoundData não define um mixer próprio.")]
        public AudioMixerGroup defaultMixerGroup;

        [Header("3D Settings")]
        [Tooltip("Distância máxima padrão para sons 3D. Pode ser sobrescrita no SoundData.")]
        public float maxDistance = 50f;

        [Tooltip("Se verdadeiro, emissores que usam esta config tendem a ser tratados como sons em 3D por padrão.")]
        public bool useSpatialBlend = true;
    }
}