using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Policies;
namespace _ImmersiveGames.NewScripts.Orchestration.WorldReset.Validation
{
    public sealed class WorldResetSignatureValidator : IWorldResetValidator
    {
        public ResetDecision Validate(WorldResetRequest request, IWorldResetPolicy policy)
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
