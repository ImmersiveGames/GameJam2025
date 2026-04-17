using System;
using ImmersiveGames.GameJam2025.Experience.Audio.Config;
using UnityEngine.Audio;
namespace ImmersiveGames.GameJam2025.Experience.Audio.Runtime.Core
{
    public sealed class AudioRoutingResolver : IAudioRoutingResolver
    {
        private readonly AudioDefaultsAsset _defaults;

        public AudioRoutingResolver(AudioDefaultsAsset defaults)
        {
            _defaults = defaults ?? throw new InvalidOperationException("[FATAL][Audio] AudioDefaultsAsset obrigatorio ausente para AudioRoutingResolver.");
        }

        public string MasterVolumeParameter => RequireParameter(_defaults.MasterVolumeParameter, "MasterVolumeParameter");

        public string BgmVolumeParameter => RequireParameter(_defaults.BgmVolumeParameter, "BgmVolumeParameter");

        public string SfxVolumeParameter => RequireParameter(_defaults.SfxVolumeParameter, "SfxVolumeParameter");

        public AudioMixerGroup ResolveBgmMixerGroup(AudioBgmCueAsset cue)
        {
            if (cue != null && cue.MixerGroup != null)
            {
                return cue.MixerGroup;
            }

            return _defaults.DefaultBgmMixerGroup;
        }

        public AudioMixerGroup ResolveSfxMixerGroup(AudioSfxCueAsset cue)
        {
            if (cue != null && cue.MixerGroup != null)
            {
                return cue.MixerGroup;
            }

            return _defaults.DefaultSfxMixerGroup;
        }

        private static string RequireParameter(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"[FATAL][Audio] AudioDefaultsAsset obrigatorio precisa definir {parameterName}.");
            }

            return value;
        }
    }
}

