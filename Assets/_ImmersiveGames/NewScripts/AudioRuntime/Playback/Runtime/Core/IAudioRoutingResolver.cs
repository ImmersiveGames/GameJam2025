using ImmersiveGames.GameJam2025.Experience.Audio.Config;
using UnityEngine.Audio;
namespace ImmersiveGames.GameJam2025.Experience.Audio.Runtime.Core
{
    /// <summary>
    /// Resolvedor base de routing/mixer do módulo de áudio.
    /// </summary>
    public interface IAudioRoutingResolver
    {
        string MasterVolumeParameter { get; }
        string BgmVolumeParameter { get; }
        string SfxVolumeParameter { get; }

        AudioMixerGroup ResolveBgmMixerGroup(AudioBgmCueAsset cue);
        AudioMixerGroup ResolveSfxMixerGroup(AudioSfxCueAsset cue);
    }
}

