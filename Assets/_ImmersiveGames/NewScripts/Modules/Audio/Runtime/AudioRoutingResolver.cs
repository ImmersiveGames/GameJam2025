using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using UnityEngine.Audio;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Runtime
{
    public sealed class AudioRoutingResolver : IAudioRoutingResolver
    {
        private readonly AudioDefaultsAsset _defaults;

        public AudioRoutingResolver(AudioDefaultsAsset defaults)
        {
            _defaults = defaults;
        }

        public string MasterVolumeParameter => string.IsNullOrWhiteSpace(_defaults?.MasterVolumeParameter)
            ? "MasterVolume"
            : _defaults.MasterVolumeParameter;

        public string BgmVolumeParameter => string.IsNullOrWhiteSpace(_defaults?.BgmVolumeParameter)
            ? "BGM_Volume"
            : _defaults.BgmVolumeParameter;

        public string SfxVolumeParameter => string.IsNullOrWhiteSpace(_defaults?.SfxVolumeParameter)
            ? "SFX_Volume"
            : _defaults.SfxVolumeParameter;

        public AudioMixerGroup ResolveBgmMixerGroup(AudioBgmCueAsset cue)
        {
            if (cue != null && cue.MixerGroup != null)
                return cue.MixerGroup;

            return _defaults != null ? _defaults.DefaultBgmMixerGroup : null;
        }

        public AudioMixerGroup ResolveSfxMixerGroup(AudioSfxCueAsset cue)
        {
            if (cue != null && cue.MixerGroup != null)
                return cue.MixerGroup;

            return _defaults != null ? _defaults.DefaultSfxMixerGroup : null;
        }
    }
}
