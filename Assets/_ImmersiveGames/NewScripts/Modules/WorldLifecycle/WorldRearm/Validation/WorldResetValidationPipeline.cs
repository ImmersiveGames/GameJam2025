using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Policies;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Validation
{
    public sealed class WorldResetValidationPipeline
    {
        private readonly IReadOnlyList<IWorldResetValidator> _validators;

        public WorldResetValidationPipeline(IReadOnlyList<IWorldResetValidator> validators)
        {
            _validators = validators ?? System.Array.Empty<IWorldResetValidator>();
        }

        public ResetDecision Validate(WorldResetRequest request, IWorldResetPolicy policy)
        {
            for (int i = 0; i < _validators.Count; i++)
            {
                var validator = _validators[i];
                if (validator == null)
                {
                    continue;
                }

                ResetDecision decision = validator.Validate(request, policy);
                if (!decision.ShouldProceed)
                {
                    return decision;
                }
            }

            return ResetDecision.Proceed();
        }
    }
}
