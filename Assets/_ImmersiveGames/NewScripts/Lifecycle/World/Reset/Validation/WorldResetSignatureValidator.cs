using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Domain;
using _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Policies;

namespace _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Validation
{
    public sealed class WorldResetSignatureValidator : IWorldResetValidator
    {
        public ResetDecision Validate(WorldResetRequest request, IResetPolicy policy)
        {
            if (request.HasSignature)
            {
                return ResetDecision.Proceed();
            }

            DebugUtility.LogWarning(typeof(WorldResetSignatureValidator),
                $"[{ResetLogTags.ValidationFailed}][DEGRADED_MODE] ContextSignature vazia. reset será SKIP. request={request}");

            string reason = string.IsNullOrWhiteSpace(request.Reason) ? "Validation_MissingSignature" : request.Reason;
            return ResetDecision.Skip(reason, "ContextSignature vazia", publishCompletion: true, isViolation: true);
        }
    }
}
