using System;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    public static class RuntimePolicyConfigResolver
    {
        public static RuntimePersistentScenesPolicyAsset ResolvePersistentScenesPolicyOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][RuntimePolicy] RuntimeModeConfig obrigatorio ausente para resolver RuntimePersistentScenesPolicyAsset.");
            }

            if (RuntimeConfigRegistry.TryGetSnapshot(out var snapshot) && snapshot != null)
            {
                var runtimePolicy = snapshot.RuntimePolicy;
                if (runtimePolicy == null)
                {
                    throw new InvalidOperationException("[FATAL][Config][RuntimePolicy] RuntimeConfigRegistry invariant breach: snapshot.RuntimePolicy obrigatorio ausente.");
                }

                var policy = runtimePolicy.RuntimePersistentScenesPolicy;
                string validationError = string.Empty;
                bool valid = policy != null && policy.TryValidate(out validationError);
                if (!valid)
                {
                    throw new InvalidOperationException($"[FATAL][Config][RuntimePolicy] RuntimeConfigRegistry invariant breach: RuntimePersistentScenesPolicyAsset ausente/invalido no snapshot. detail='{validationError}'.");
                }

                return policy;
            }

            throw new InvalidOperationException("[FATAL][Config][RuntimePolicy] RuntimeConfigRegistry snapshot obrigatorio ausente para RuntimePersistentScenesPolicy migrado.");
        }
    }
}
