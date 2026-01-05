namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Phases
{
    /// <summary>
    /// Plano imutável de spawn gerado para o reset atual.
    /// </summary>
    public sealed class PhaseSpawnPlan
    {
        public PhaseSpawnPlan(PhaseId phaseId, int enemyCount, string displayName)
        {
            PhaseId = phaseId;
            EnemyCount = enemyCount;
            DisplayName = displayName ?? string.Empty;
        }

        public PhaseId PhaseId { get; }

        public int EnemyCount { get; }

        public string DisplayName { get; }
    }
}
