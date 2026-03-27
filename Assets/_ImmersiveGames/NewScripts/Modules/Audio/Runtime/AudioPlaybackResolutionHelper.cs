using _ImmersiveGames.NewScripts.Modules.Audio.Config;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Runtime
{
    /// <summary>
    /// Helper puro para manter a precedência canônica de profiles e intenção espacial em um único lugar.
    /// Não executa playback; apenas resolve dados de configuração.
    /// </summary>
    internal static class AudioPlaybackResolutionHelper
    {
        public static bool TryResolveEmissionProfile(
            AudioSfxCueAsset cue,
            AudioPlaybackContext context,
            out AudioSfxEmissionProfileAsset profile,
            out string source)
        {
            if (context.EmissionProfile != null)
            {
                profile = context.EmissionProfile;
                source = "context";
                return true;
            }

            if (cue != null && cue.EmissionProfile != null)
            {
                profile = cue.EmissionProfile;
                source = "emission_profile";
                return true;
            }

            profile = null;
            source = "legacy_cue";
            return false;
        }

        public static bool TryResolveExecutionProfile(
            AudioSfxCueAsset cue,
            AudioPlaybackContext context,
            out AudioSfxExecutionProfileAsset profile,
            out string source)
        {
            if (context.ExecutionProfile != null)
            {
                profile = context.ExecutionProfile;
                source = "context";
                return true;
            }

            if (cue != null && cue.ExecutionProfile != null)
            {
                profile = cue.ExecutionProfile;
                source = "execution_profile";
                return true;
            }

            profile = null;
            source = "legacy_cue";
            return false;
        }

        public static bool ResolveUseSpatial(
            AudioSfxCueAsset cue,
            AudioPlaybackContext context)
        {
            TryResolveEmissionProfile(cue, context, out var emissionProfile, out _);
            return ResolveUseSpatial(cue, context, emissionProfile);
        }

        public static bool ResolveUseSpatial(
            AudioSfxCueAsset cue,
            AudioPlaybackContext context,
            AudioSfxEmissionProfileAsset resolvedEmissionProfile)
        {
            if (resolvedEmissionProfile != null)
            {
                return resolvedEmissionProfile.EmissionMode == AudioSfxPlaybackMode.Spatial;
            }

            return context.UseSpatial || (cue != null && cue.PlaybackMode == AudioSfxPlaybackMode.Spatial);
        }

        public static float ResolveLegacySpatialBlend(AudioSfxCueAsset cue)
        {
            return cue != null ? cue.SpatialBlend : 1f;
        }

        public static float ResolveLegacyMinDistance(AudioSfxCueAsset cue)
        {
            return cue != null ? cue.MinDistance : 1f;
        }

        public static float ResolveLegacyMaxDistance(AudioSfxCueAsset cue)
        {
            return cue != null ? cue.MaxDistance : 40f;
        }

        public static AudioSfxExecutionMode ResolveExecutionMode(
            AudioSfxCueAsset cue,
            AudioSfxExecutionProfileAsset resolvedExecutionProfile)
        {
            if (resolvedExecutionProfile != null)
            {
                return resolvedExecutionProfile.ExecutionMode;
            }

            return cue != null ? cue.ExecutionMode : AudioSfxExecutionMode.DirectOneShot;
        }
    }
}
