using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Domain;
using _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Policies;

namespace _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Validation
{
    /// <summary>
    /// Valida o perfil do SceneFlow: apenas gameplay requer reset determinístico.
    /// </summary>
    public sealed class WorldResetProfileValidator : IWorldResetValidator
    {
        public ResetDecision Validate(WorldResetRequest request, IResetPolicy policy)
        {
            if (request.Origin != WorldResetOrigin.SceneFlow)
            {
                return ResetDecision.Proceed();
            }

            if (request.IsGameplayProfile)
            {
                return ResetDecision.Proceed();
            }

            string reason = request.Reason;
            if (string.IsNullOrWhiteSpace(reason) ||
                !reason.StartsWith(WorldResetReasons.SkippedStartupOrFrontendPrefix, StringComparison.Ordinal))
            {
                string target = string.IsNullOrWhiteSpace(request.TargetScene) ? "<unknown>" : request.TargetScene;
                string profile = string.IsNullOrWhiteSpace(request.ProfileName) ? "<unknown>" : request.ProfileName;
                reason = $"{WorldResetReasons.SkippedStartupOrFrontendPrefix}:profile={profile};scene={target}";
            }

            DebugUtility.LogVerbose(typeof(WorldResetProfileValidator),
                $"[{ResetLogTags.Skipped}] Reset SKIP (profile != gameplay). reason='{reason}'. request={request}");

            return ResetDecision.Skip(reason, "Profile != gameplay", publishCompletion: true, isViolation: false);
        }
    }
}
