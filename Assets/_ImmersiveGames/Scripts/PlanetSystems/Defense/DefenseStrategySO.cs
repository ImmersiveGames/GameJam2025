using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Base ScriptableObject para estratégias de defesa por planeta.
    /// Fornece implementação padrão (no-op) para permitir dados puros
    /// sem exigir código adicional até que estratégias concretas existam.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefenseStrategy",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Strategies/Defense Strategy (Base)")]
    public class DefenseStrategySO : ScriptableObject, IDefenseStrategy
    {
        [Header("Identidade da estratégia")]
        [SerializeField]
        private string strategyId = "default";

        [Header("Configuração do alvo")]
        [Tooltip("Role preferido pelo planeta ao engajar defesas; permanece Unknown se a estratégia não tiver preferência.")]
        [SerializeField]
        private DefenseRole targetRole = DefenseRole.Unknown;

        public string StrategyId => string.IsNullOrWhiteSpace(strategyId) ? name : strategyId;

        public DefenseRole TargetRole => targetRole;

        public virtual void ConfigureContext(PlanetDefenseSetupContext context)
        {
            // Estratégias concretas podem ajustar pool, profile ou outros dados do contexto.
        }

        public virtual void OnEngaged(PlanetsMaster planet, DetectionType detectionType)
        {
            DebugUtility.LogVerbose<DefenseStrategySO>(
                $"[Strategy] {StrategyId} engajada para {planet?.ActorName ?? "Unknown"} ({detectionType?.TypeName ?? "Unknown"}).");
        }

        public virtual void OnDisengaged(PlanetsMaster planet, DetectionType detectionType)
        {
            DebugUtility.LogVerbose<DefenseStrategySO>(
                $"[Strategy] {StrategyId} desengajada para {planet?.ActorName ?? "Unknown"} ({detectionType?.TypeName ?? "Unknown"}).");
        }
    }
}
