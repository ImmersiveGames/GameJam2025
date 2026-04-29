using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using UnityEngine.Audio;
namespace _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core
{
    /// <summary>
    /// Resolvedor base de routing/mixer do m�dulo de �udio.
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

