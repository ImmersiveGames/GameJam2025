using UnityEngine;
using UnityEngine.Audio;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Config
{
    [CreateAssetMenu(
        fileName = "AudioDefaults",
        menuName = "ImmersiveGames/NewScripts/Audio/Audio Defaults",
        order = 3)]
    public sealed class AudioDefaultsAsset : ScriptableObject
    {
        /// <summary>
        /// Controles de volume principais para diferentes categorias de áudio.
        /// </summary>
        [Header("Volumes")]
        [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float bgmVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;

        /// <summary>
        /// Multiplicadores de volume para categorias específicas de áudio.
        /// </summary>
        [Header("Category Multipliers")]
        [SerializeField] [Min(0f)] private float bgmCategoryMultiplier = 1f;
        [SerializeField] [Min(0f)] private float sfxCategoryMultiplier = 1f;

        /// <summary>
        /// Configurações padrão para reprodução de música de fundo (BGM).
        /// </summary>
        [Header("BGM Defaults")]
        [SerializeField] [Min(0f)] private float defaultBgmFadeSeconds = 1f;
        [SerializeField] [Range(0f, 1f)] private float pauseDuckingScale = 0.35f;

        /// <summary>
        /// Roteamento de áudio através do mixer, defining os grupos e parâmetros de volume.
        /// </summary>
        [Header("Mixer Routing Base")]
        [SerializeField] private AudioMixerGroup defaultBgmMixerGroup;
        [SerializeField] private AudioMixerGroup defaultSfxMixerGroup;
        [SerializeField] private string masterVolumeParameter = "MasterVolume";
        [SerializeField] private string bgmVolumeParameter = "BGM_Volume";
        [SerializeField] private string sfxVolumeParameter = "SFX_Volume";

        public float MasterVolume => masterVolume;
        public float BgmVolume => bgmVolume;
        public float SfxVolume => sfxVolume;
        public float BgmCategoryMultiplier => bgmCategoryMultiplier;
        public float SfxCategoryMultiplier => sfxCategoryMultiplier;
        public float DefaultBgmFadeSeconds => defaultBgmFadeSeconds;
        public float PauseDuckingScale => pauseDuckingScale;

        public AudioMixerGroup DefaultBgmMixerGroup => defaultBgmMixerGroup;
        public AudioMixerGroup DefaultSfxMixerGroup => defaultSfxMixerGroup;
        public string MasterVolumeParameter => masterVolumeParameter;
        public string BgmVolumeParameter => bgmVolumeParameter;
        public string SfxVolumeParameter => sfxVolumeParameter;
    }
}
