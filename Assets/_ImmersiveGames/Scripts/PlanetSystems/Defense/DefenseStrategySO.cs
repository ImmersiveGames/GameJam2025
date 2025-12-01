using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    public class DefenseStrategySo : ScriptableObject, IDefenseStrategy
    {
        [Header("Identidade da estratégia")]
        [SerializeField]
        private string strategyId = "default";

        [Header("Configuração do alvo")]
        [Tooltip("Role preferido pelo planeta ao engajar defesas; permanece Unknown se a estratégia não tiver preferência.")]
        [SerializeField]
        private DefenseRole targetRole = DefenseRole.Unknown;

        [Header("Configuração externa de roles (opcional)")]
        [Tooltip("Config de role compartilhada; usada como fallback se os mapeamentos embutidos não cobrirem o identifier.")]
        [SerializeField]
        private DefenseRoleConfig roleConfig;

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
            DebugUtility.LogVerbose<DefenseStrategySo>(
                $"[Strategy] {StrategyId} engajada para {planet?.ActorName ?? "Unknown"} ({detectionType?.TypeName ?? "Unknown"}).");
        }

        public virtual void OnDisengaged(PlanetsMaster planet, DetectionType detectionType)
        {
            DebugUtility.LogVerbose<DefenseStrategySo>(
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

        public virtual DefenseRole ResolveTargetRole(string targetIdentifier, DefenseRole requestedRole)
        {
            // Role explícito do evento sempre tem prioridade.
            if (requestedRole != DefenseRole.Unknown)
            {
                return requestedRole;
            }

            // Tenta resolver pelos mapeamentos embutidos da própria estratégia.
            var mappedRole = ResolveRole(targetIdentifier);
            if (mappedRole != DefenseRole.Unknown)
            {
                return mappedRole;
            }

            // Caso exista um DefenseRoleConfig compartilhado, usa-o como fallback externo.
            if (roleConfig != null)
            {
                var configRole = roleConfig.ResolveRole(targetIdentifier);
                if (configRole != DefenseRole.Unknown)
                {
                    return configRole;
                }
            }

            // Último fallback: preferência declarada da estratégia.
            return targetRole;
        }

        /// <summary>
        /// Resolve um DefenseRole usando os mapeamentos embutidos na estratégia.
        /// Permite eliminar o uso de DefenseRoleConfig separado quando a estratégia
        /// já expressa a preferência de alvo ou precisa mapear labels de forma determinística.
        /// </summary>
        private DefenseRole ResolveRole(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return fallbackRole != DefenseRole.Unknown ? fallbackRole : targetRole;
            foreach (var binding in roleMappings.Where(binding => binding != null && !string.IsNullOrWhiteSpace(binding.Identifier)).Where(binding => binding.Identifier == identifier))
            {
                return binding.Role;
            }

            return fallbackRole != DefenseRole.Unknown ? fallbackRole : targetRole;

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
