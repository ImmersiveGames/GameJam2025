using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Phases
{
    /// <summary>
    /// Define parâmetros mínimos de uma fase para o SpawnPlan.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PhaseDefinition",
        menuName = "ImmersiveGames/Phases/Phase Definition",
        order = 0)]
    public sealed class PhaseDefinition : ScriptableObject
    {
        [SerializeField]
        private string id = "Phase1";

        [SerializeField]
        private string displayName = "Phase 1";

        [SerializeField]
        [Min(0)]
        private int enemyCount = 1;

        public PhaseId Id => new PhaseId(id);

        public string DisplayName => displayName;

        public int EnemyCount => enemyCount;
    }
}
