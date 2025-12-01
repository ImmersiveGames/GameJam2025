using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using System.Collections.Generic;

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

        [Header("Mapeamento de roles (embutido)")]
        [Tooltip("Mapeamentos opcionais incorporados para evitar SOs extras como DefenseRoleConfig.")]
        [SerializeField]
        private List<DefenseRoleBinding> roleMappings = new();

        [Tooltip("Role de fallback aplicado caso nenhum mapeamento seja encontrado.")]
        [SerializeField]
        private DefenseRole fallbackRole = DefenseRole.Unknown;

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

        public virtual DefenseMinionBehaviorProfileSO SelectMinionProfile(
            DefenseRole role,
            DefenseMinionBehaviorProfileSO waveProfile,
            DefenseMinionBehaviorProfileSO minionProfile)
        {
            // Fallback padrão: respeita o profile definido na wave e depois o do minion/pool.
            return waveProfile != null ? waveProfile : minionProfile;
        }

        /// <summary>
        /// Resolve um DefenseRole usando os mapeamentos embutidos na estratégia.
        /// Permite eliminar o uso de DefenseRoleConfig separado quando a estratégia
        /// já expressa a preferência de alvo ou precisa mapear labels de forma determinística.
        /// </summary>
        public DefenseRole ResolveRole(string identifier)
        {
            if (!string.IsNullOrWhiteSpace(identifier))
            {
                foreach (DefenseRoleBinding binding in roleMappings)
                {
                    if (binding == null || string.IsNullOrWhiteSpace(binding.Identifier))
                    {
                        continue;
                    }

                    if (binding.Identifier == identifier)
                    {
                        return binding.Role;
                    }
                }
            }

            if (fallbackRole != DefenseRole.Unknown)
            {
                return fallbackRole;
            }

            return targetRole;
        }

        [System.Serializable]
        private class DefenseRoleBinding
        {
            [Tooltip("Chave textual (ex.: ActorName) mapeada para um DefenseRole.")]
            [SerializeField]
            private string identifier;

            [Tooltip("Role que a estratégia deseja aplicar ao identifier informado.")]
            [SerializeField]
            private DefenseRole role = DefenseRole.Unknown;

            public string Identifier => identifier;
            public DefenseRole Role => role;
        }
    }
}
