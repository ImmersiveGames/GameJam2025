using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Phases
{
    /// <summary>
    /// Constrói o PhaseSpawnPlan a partir do snapshot e da definição resolvida.
    /// </summary>
    public sealed class PhaseSpawnPlanBuilder
    {
        public PhaseSpawnPlan Build(PhaseSnapshot snapshot, PhaseDefinition definition)
        {
            if (definition == null)
            {
                DebugUtility.LogWarning(typeof(PhaseSpawnPlanBuilder),
                    $"[Phase] SpawnPlan build skipped (PhaseDefinition null). phaseId='{snapshot.CurrentPhaseId.Value}'.");
                return null;
            }

            var plan = new PhaseSpawnPlan(
                phaseId: snapshot.CurrentPhaseId,
                enemyCount: definition.EnemyCount,
                displayName: definition.DisplayName);

            DebugUtility.Log(typeof(PhaseSpawnPlanBuilder),
                $"[Phase] SpawnPlan built phaseId='{plan.PhaseId.Value}' enemyCount={plan.EnemyCount} displayName='{plan.DisplayName}'.");

            return plan;
        }
    }
}
