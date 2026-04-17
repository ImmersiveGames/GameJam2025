using System;
using ImmersiveGames.GameJam2025.Experience.Audio.Config;
using ImmersiveGames.GameJam2025.Experience.Audio.Runtime.Models;
namespace ImmersiveGames.GameJam2025.Experience.Audio.Runtime.Core
{
    /// <summary>
    /// Helper puro para manter a precedencia canonica de profiles e intencao espacial em um unico lugar.
    /// Nao executa playback; apenas resolve dados de configuracao.
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
            source = "missing_profile";
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
            source = "missing_profile";
            return false;
        }

        public static bool ResolveUseSpatial(
            AudioSfxEmissionProfileAsset resolvedEmissionProfile)
        {
            if (resolvedEmissionProfile == null)
            {
                throw new InvalidOperationException("[FATAL][Audio] AudioSfxEmissionProfileAsset obrigatorio ausente.");
            }

            return resolvedEmissionProfile.EmissionMode == AudioSfxPlaybackMode.Spatial;
        }

        public static AudioSfxExecutionMode ResolveExecutionMode(
            AudioSfxExecutionProfileAsset resolvedExecutionProfile)
        {
            if (resolvedExecutionProfile == null)
            {
                throw new InvalidOperationException("[FATAL][Audio] AudioSfxExecutionProfileAsset obrigatorio ausente.");
            }

            return resolvedExecutionProfile.ExecutionMode;
        }
    }
}

