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
        [Header("Volumes")]
        [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float bgmVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;

        [Header("Category Multipliers")]
        [SerializeField] [Min(0f)] private float bgmCategoryMultiplier = 1f;
        [SerializeField] [Min(0f)] private float sfxCategoryMultiplier = 1f;

        [Header("BGM Defaults")]
        [SerializeField] [Min(0f)] private float defaultBgmFadeSeconds = 1f;
        [SerializeField] [Range(0f, 1f)] private float pauseDuckingScale = 0.35f;
        [SerializeField] private AudioBgmCueAsset startupBgmCue;
        [SerializeField] private AudioBgmCueAsset frontendBgmCue;
        [SerializeField] private AudioBgmCueAsset gameplayBgmCue;

        [Header("Mixer Routing Base")]
        [SerializeField] private AudioMixerGroup defaultBgmMixerGroup;
        [SerializeField] private AudioMixerGroup defaultSfxMixerGroup;
        [SerializeField] private string masterVolumeParameter = "MasterVolume";
        [SerializeField] private string bgmVolumeParameter = "BGM_Volume";
        [SerializeField] private string sfxVolumeParameter = "SFX_Volume";

        [Header("Pooled Voice Profiles")]
        [SerializeField] private AudioSfxVoiceProfileAsset defaultGlobalPooledSfxVoiceProfile;
        [SerializeField] private AudioSfxVoiceProfileAsset defaultSpatialPooledSfxVoiceProfile;

        public float MasterVolume => masterVolume;
        public float BgmVolume => bgmVolume;
        public float SfxVolume => sfxVolume;
        public float BgmCategoryMultiplier => bgmCategoryMultiplier;
        public float SfxCategoryMultiplier => sfxCategoryMultiplier;
        public float DefaultBgmFadeSeconds => defaultBgmFadeSeconds;
        public float PauseDuckingScale => pauseDuckingScale;

        public AudioBgmCueAsset StartupBgmCue => startupBgmCue;
        public AudioBgmCueAsset FrontendBgmCue => frontendBgmCue;
        public AudioBgmCueAsset GameplayBgmCue => gameplayBgmCue;

        public AudioMixerGroup DefaultBgmMixerGroup => defaultBgmMixerGroup;
        public AudioMixerGroup DefaultSfxMixerGroup => defaultSfxMixerGroup;
        public string MasterVolumeParameter => masterVolumeParameter;
        public string BgmVolumeParameter => bgmVolumeParameter;
        public string SfxVolumeParameter => sfxVolumeParameter;

        public AudioSfxVoiceProfileAsset DefaultGlobalPooledSfxVoiceProfile => defaultGlobalPooledSfxVoiceProfile;
        public AudioSfxVoiceProfileAsset DefaultSpatialPooledSfxVoiceProfile => defaultSpatialPooledSfxVoiceProfile;
    }
}
