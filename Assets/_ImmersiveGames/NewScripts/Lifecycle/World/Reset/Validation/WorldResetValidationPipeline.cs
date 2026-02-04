using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Domain;
using _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Policies;

namespace _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Validation
{
    public sealed class WorldResetValidationPipeline
    {
        private readonly IReadOnlyList<IWorldResetValidator> _validators;

        public WorldResetValidationPipeline(IReadOnlyList<IWorldResetValidator> validators)
        {
            _validators = validators ?? System.Array.Empty<IWorldResetValidator>();
        }

        public ResetDecision Validate(WorldResetRequest request, IResetPolicy policy)
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
